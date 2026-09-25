using System.Globalization;
using Kyrios.Game;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Kyrios.MobilePreview;

/// <summary>
/// Executa o roteiro, um comando por linha (o tempo é o do jogo, então o resultado não depende da velocidade da máquina):
/// <code>
/// wait 1.5                 espera
/// shot nome                salva nome.png
/// tap L 120 300            toque rápido (L = coordenadas lógicas do jogo, P = fração da tela, X = pixels)
/// down 1 L 58 520          encosta o dedo 1 (fica até "up 1")
/// move 1 P 0.3 0.9         arrasta o dedo 1
/// up 1                     solta o dedo 1
/// press gas 2.0            segura um controle da corrida (left, right, gas, brake, boost, stick) por N segundos
/// stick 45 1.0 2.0         joystick: ângulo (graus), força (0-1), segundos
/// tilt 0.6                 inclinação do aparelho (-1 a 1)
/// back | interrupt | lowmem   botão voltar / app interrompido / pouca memória
/// expect-state Racing      falha o roteiro se a tela atual não for essa
/// </code>
/// </summary>
public sealed class PreviewDirector : DrawableGameComponent
{
    private readonly GameRoot _game;
    private readonly PreviewPlatform _platform;
    private readonly PreviewOptions _options;
    private readonly Queue<string> _lines;
    private readonly Dictionary<int, Vector2> _fingers = [];
    private readonly HashSet<int> _newFingers = [];
    private readonly List<(int Id, float Until)> _timedReleases = [];
    private float _time;
    private float _waitUntil;
    private string _pendingShot;
    private int _tapId = 100;
    private (float Angle, float Amount, float Until)? _stick;

    public List<string> Failures { get; } = [];

    public PreviewDirector(GameRoot game, PreviewPlatform platform, PreviewOptions options)
        : base(game)
    {
        _game = game;
        _platform = platform;
        _options = options;
        _lines = new Queue<string>(options.ScriptPath is null ? ["wait 3", "shot menu"] : File.ReadAllLines(options.ScriptPath));
        UpdateOrder = int.MinValue;
    }

    public override void Update(GameTime gameTime)
    {
        _time += (float)gameTime.ElapsedGameTime.TotalSeconds;

        foreach ((int id, float until) in _timedReleases.ToList())
        {
            if (_time >= until)
            {
                _fingers.Remove(id);
                _timedReleases.Remove((id, until));
            }
        }

        if (_stick is { } stick && _time >= stick.Until)
        {
            _fingers.Remove(-1);
            _stick = null;
        }

        while (_pendingShot is null && _time >= _waitUntil && _lines.Count > 0)
        {
            Run(_lines.Dequeue().Trim());
        }

        if (_lines.Count == 0 && _pendingShot is null && _time >= _waitUntil)
        {
            File.WriteAllLines(Path.Combine(_options.OutputDirectory, $"log-{_options.DeviceName}.txt"), _platform.Log.Concat(Failures.Select(f => "FALHA: " + f)));
            _game.Exit();
        }

        _platform.SetTouches(_fingers.Select(pair => new TouchPoint(pair.Key, pair.Value, _newFingers.Contains(pair.Key))));
        _newFingers.Clear();
    }

    private void Run(string line)
    {
        if (line.Length == 0 || line.StartsWith('#'))
        {
            return;
        }

        string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        float F(int i) => float.Parse(parts[i], CultureInfo.InvariantCulture);
        switch (parts[0])
        {
            case "wait":
                _waitUntil = _time + F(1);
                break;
            case "shot":
                _pendingShot = parts[1];
                break;
            case "tap":
                int id = _tapId++;
                Put(id, Point(parts, 1));
                _timedReleases.Add((id, _time + 0.08f));
                _waitUntil = _time + 0.25f;
                break;
            case "down":
                Put(int.Parse(parts[1], CultureInfo.InvariantCulture), Point(parts, 2));
                break;
            case "move":
                _fingers[int.Parse(parts[1], CultureInfo.InvariantCulture)] = Point(parts, 2);
                break;
            case "up":
                _fingers.Remove(int.Parse(parts[1], CultureInfo.InvariantCulture));
                break;
            case "press":
                int pressId = _tapId++;
                Put(pressId, ControlPoint(parts[1]));
                _timedReleases.Add((pressId, _time + F(2)));
                break;
            case "stick":
                TouchLayout layout = _game.TouchLayoutForTools;
                float angle = MathHelper.ToRadians(F(1));
                Vector2 center = layout.JoystickRest.Center;
                Put(-1, center);
                _fingers[-1] = center;
                _stick = (angle, F(2), _time + F(3));
                // O dedo encosta no centro e, no quadro seguinte, vai pra direção pedida.
                _timedMoves.Add((-1, center + (new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * layout.JoystickRest.Radius * F(2)), _time + 0.05f));
                break;
            case "tilt":
                _platform.Tilt = F(1);
                break;
            case "back":
                _platform.RequestBack();
                _waitUntil = _time + 0.2f;
                break;
            case "interrupt":
                _platform.Interrupt();
                _waitUntil = _time + 0.2f;
                break;
            case "lowmem":
                _platform.WarnLowMemory();
                break;
            case "expect-state":
                if (_game.StateNameForTools != parts[1])
                {
                    Failures.Add($"esperava a tela {parts[1]}, estava em {_game.StateNameForTools} (linha: {line})");
                    Console.WriteLine($"[roteiro] FALHA: {Failures[^1]}");
                }

                break;
            case "log":
                Console.WriteLine($"[roteiro] {string.Join(' ', parts.Skip(1))}");
                break;
            default:
                throw new InvalidOperationException($"comando desconhecido no roteiro: {line}");
        }
    }

    private readonly List<(int Id, Vector2 To, float At)> _timedMoves = [];

    public override void Draw(GameTime gameTime)
    {
        foreach ((int id, Vector2 to, float at) in _timedMoves.ToList())
        {
            if (_time >= at)
            {
                if (_fingers.ContainsKey(id))
                {
                    _fingers[id] = to;
                }

                _timedMoves.Remove((id, to, at));
            }
        }

        if (_pendingShot is null)
        {
            return;
        }

        int width = GraphicsDevice.PresentationParameters.BackBufferWidth;
        int height = GraphicsDevice.PresentationParameters.BackBufferHeight;
        var pixels = new Color[width * height];
        GraphicsDevice.GetBackBufferData(pixels);
        using var texture = new Texture2D(GraphicsDevice, width, height);
        texture.SetData(pixels);
        string path = Path.Combine(_options.OutputDirectory, $"{_options.DeviceName}-{_pendingShot}.png");
        using (FileStream file = File.Create(path))
        {
            texture.SaveAsPng(file, width, height);
        }

        Console.WriteLine($"[roteiro] captura {path}");
        _pendingShot = null;
    }

    private void Put(int id, Vector2 position)
    {
        _fingers[id] = position;
        _newFingers.Add(id);
    }

    private Vector2 Point(string[] parts, int index)
    {
        float x = float.Parse(parts[index + 1], CultureInfo.InvariantCulture);
        float y = float.Parse(parts[index + 2], CultureInfo.InvariantCulture);
        return parts[index] switch
        {
            "L" => _game.LogicalToScreen(new Vector2(x, y)),
            "P" => new Vector2(x * _options.Width, y * _options.Height),
            _ => new Vector2(x, y),
        };
    }

    private Vector2 ControlPoint(string name)
    {
        TouchLayout layout = _game.TouchLayoutForTools;
        return name switch
        {
            "left" => layout.Left.Center,
            "right" => layout.Right.Center,
            "gas" => layout.Gas.Center,
            "brake" => layout.Brake.Center,
            "boost" => layout.Boost.Center,
            _ => layout.JoystickRest.Center,
        };
    }
}
