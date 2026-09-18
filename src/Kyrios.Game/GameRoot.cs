using Kyrios.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Kyrios.Game;

/// <summary>Jogo de corrida visto de cima. Toda a física/regras vêm de Kyrios.Core; esta classe só desenha e lê o teclado.</summary>
public sealed class GameRoot : Microsoft.Xna.Framework.Game
{
    private const int CellSize = 20;
    private const int HudPaddingTop = 16;
    private const int HudPaddingBottom = 16;
    private const int HudLineGap = 6;
    private const float HudTextSize = 3f;

    private enum State
    {
        Intro,
        Racing,
        Results,
    }

    private static readonly Color BackgroundGrass = new(34, 120, 54);
    private static readonly Color WallColor = new(35, 35, 40);
    private static readonly Color TrackColor = new(72, 72, 78);
    private static readonly Color GrassCellColor = new(46, 138, 66);
    private static readonly Color CheckpointTint = new(255, 215, 0, 90);
    private static readonly Color TextColor = Color.White;
    private static readonly Color AccentColor = new(255, 215, 0);

    private static readonly Color[] AiPalette =
    [
        new Color(64, 200, 220),
        new Color(220, 90, 220),
        new Color(120, 220, 90),
        new Color(230, 90, 70),
        new Color(90, 120, 230),
        new Color(230, 160, 60),
    ];

    private readonly GraphicsDeviceManager _graphics;
    private readonly GameInput _input = new();

    private SpriteBatch _spriteBatch = null!;
    private Texture2D _pixel = null!;

    private RaceSimulation _race = null!;
    private RaceEntrant _player = null!;
    private State _state = State.Intro;

    public GameRoot()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.Title = "Kyrios - Corrida Top-Vision";

        StartNewRace();
        SizeWindowForCurrentRace();
    }

    private void StartNewRace()
    {
        _race = RaceFactory.CreateDefaultRace(aiOpponents: 3, targetLaps: 3);
        _player = _race.Entrants.First(e => e.Kind == DriverKind.Human);
    }

    private void SizeWindowForCurrentRace()
    {
        int trackPixelWidth = _race.Track.Width * CellSize;
        int trackPixelHeight = _race.Track.Height * CellSize;

        int hudLines = 4 + _race.Entrants.Count;
        int hudHeight = HudPaddingTop + HudPaddingBottom
            + (int)(hudLines * (PixelFont.LineHeight(HudTextSize) + HudLineGap));

        _graphics.PreferredBackBufferWidth = Math.Max(trackPixelWidth, 640);
        _graphics.PreferredBackBufferHeight = trackPixelHeight + hudHeight;
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Update();

        switch (_state)
        {
            case State.Intro:
                if (_input.AnyKeyJustPressed())
                {
                    _state = State.Racing;
                }

                break;

            case State.Racing:
                if (_input.WasJustPressed(Keys.Escape))
                {
                    Exit();
                    return;
                }

                float dt = Math.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 0.1f);
                _race.Update(dt, _input.BuildCarInput());
                if (_race.IsRaceOver)
                {
                    _state = State.Results;
                }

                break;

            case State.Results:
                if (_input.WasJustPressed(Keys.Escape))
                {
                    Exit();
                    return;
                }

                if (_input.WasJustPressed(Keys.R))
                {
                    StartNewRace();
                    _state = State.Racing;
                }

                break;
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(BackgroundGrass);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        switch (_state)
        {
            case State.Intro:
                DrawIntro();
                break;

            case State.Racing:
                DrawTrack();
                DrawCars();
                DrawHud(raceOver: false);
                break;

            case State.Results:
                DrawTrack();
                DrawCars();
                DrawHud(raceOver: true);
                break;
        }

        _spriteBatch.End();
        base.Draw(gameTime);
    }

    private void DrawTrack()
    {
        Track track = _race.Track;
        for (int x = 0; x < track.Width; x++)
        {
            for (int y = 0; y < track.Height; y++)
            {
                char cell = track.CellAt(x, y);
                var rect = new Rectangle(x * CellSize, y * CellSize, CellSize, CellSize);

                Color baseColor = cell switch
                {
                    '#' => WallColor,
                    ',' => GrassCellColor,
                    _ => TrackColor,
                };

                _spriteBatch.Draw(_pixel, rect, baseColor);

                if (cell == 'S')
                {
                    DrawFinishChecker(rect);
                }
                else if (cell is >= '1' and <= '9')
                {
                    _spriteBatch.Draw(_pixel, rect, CheckpointTint);
                }
            }
        }
    }

    private void DrawFinishChecker(Rectangle cellRect)
    {
        int half = CellSize / 2;
        var topLeft = new Rectangle(cellRect.X, cellRect.Y, half, half);
        var bottomRight = new Rectangle(cellRect.X + half, cellRect.Y + half, CellSize - half, CellSize - half);
        var topRight = new Rectangle(cellRect.X + half, cellRect.Y, CellSize - half, half);
        var bottomLeft = new Rectangle(cellRect.X, cellRect.Y + half, half, CellSize - half);

        _spriteBatch.Draw(_pixel, topLeft, Color.White);
        _spriteBatch.Draw(_pixel, bottomRight, Color.White);
        _spriteBatch.Draw(_pixel, topRight, Color.Black);
        _spriteBatch.Draw(_pixel, bottomLeft, Color.Black);
    }

    private void DrawCars()
    {
        for (int i = 0; i < _race.Entrants.Count; i++)
        {
            RaceEntrant entrant = _race.Entrants[i];
            Car car = entrant.Car;
            Color color = entrant.Kind == DriverKind.Human ? AccentColor : AiPalette[i % AiPalette.Length];

            var center = new Vector2(car.Position.X * CellSize, car.Position.Y * CellSize);
            float length = CellSize * 1.3f;
            float width = CellSize * 0.8f;

            _spriteBatch.Draw(
                _pixel,
                center,
                null,
                color,
                car.Angle,
                new Vector2(0.5f, 0.5f),
                new Vector2(length, width),
                SpriteEffects.None,
                0f);

            var forward = new Vector2(MathF.Cos(car.Angle), MathF.Sin(car.Angle));
            Vector2 nosePos = center + (forward * (length * 0.32f));
            float noseSize = CellSize * 0.28f;

            _spriteBatch.Draw(
                _pixel,
                nosePos,
                null,
                Color.White,
                car.Angle,
                new Vector2(0.5f, 0.5f),
                new Vector2(noseSize, noseSize),
                SpriteEffects.None,
                0f);
        }
    }

    private void DrawHud(bool raceOver)
    {
        float y = (_race.Track.Height * CellSize) + HudPaddingTop;
        const float x = 14f;
        float lineHeight = PixelFont.LineHeight(HudTextSize) + HudLineGap;

        void Line(string text, Color color)
        {
            PixelFont.Draw(_spriteBatch, _pixel, text, new Vector2(x, y), HudTextSize, color);
            y += lineHeight;
        }

        if (raceOver)
        {
            Line("CORRIDA FINALIZADA", AccentColor);
        }
        else
        {
            int lap = Math.Min(_player.Car.LapsCompleted + 1, _race.TargetLaps);
            Line($"VOLTA {lap}/{_race.TargetLaps}   TEMPO {FormatTime(_player.Car.CurrentLapTime)}   VEL {_player.Car.Speed:0}", TextColor);
        }

        string best = _player.Car.BestLapTime is { } b ? FormatTime(b) : "--:--.---";
        Line($"MELHOR VOLTA {best}   TEMPO TOTAL {FormatTime(_player.Car.TotalRaceTime)}", TextColor);

        Line("CLASSIFICACAO", AccentColor);

        int position = 1;
        foreach (RaceEntrant entrant in _race.GetStandings())
        {
            bool isHuman = entrant.Kind == DriverKind.Human;
            string displayName = isHuman ? "VOCE" : entrant.Car.Name.ToUpperInvariant();
            string status = entrant.Finished
                ? $"CHEGOU {FormatTime(entrant.FinishTime ?? 0f)}"
                : $"VOLTA {entrant.Car.LapsCompleted}/{_race.TargetLaps}";

            Line($"{position,2} {displayName,-6} {status}", isHuman ? AccentColor : TextColor);
            position++;
        }

        Line(raceOver ? "R: CORRER DE NOVO   ESC: SAIR" : "SETAS/WASD DIRIGIR   ESPACO FREIO   ESC SAIR", TextColor);
    }

    private void DrawIntro()
    {
        const float x = 40f;
        float y = 50f;

        PixelFont.Draw(_spriteBatch, _pixel, "KYRIOS", new Vector2(x, y), 6f, AccentColor);
        y += PixelFont.LineHeight(6f) + 24f;

        string[] lines =
        [
            "CORRIDA TOP-VISION",
            "",
            "SETAS OU WASD: DIRIGIR",
            "ESPACO: FREIO DE MAO",
            "ESC: SAIR",
            "",
            "COMPLETE 3 VOLTAS PASSANDO",
            "PELOS CHECKPOINTS NA ORDEM",
            "CERTA ANTES DA CHEGADA",
            "",
            "APERTE QUALQUER TECLA",
        ];

        float lineHeight = PixelFont.LineHeight(3f) + 8f;
        foreach (string line in lines)
        {
            PixelFont.Draw(_spriteBatch, _pixel, line, new Vector2(x, y), 3f, TextColor);
            y += lineHeight;
        }
    }

    private static string FormatTime(float seconds)
    {
        if (seconds < 0f)
        {
            seconds = 0f;
        }

        var span = TimeSpan.FromSeconds(seconds);
        return $"{(int)span.TotalMinutes:00}:{span.Seconds:00}.{span.Milliseconds:000}";
    }
}
