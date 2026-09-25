using Kyrios.Game;
using Microsoft.Xna.Framework;

namespace Kyrios.MobilePreview;

/// <summary>Um aparelho de mentira: tela, densidade, área segura, toques, inclinação e avisos do sistema vêm do
/// roteiro (<see cref="PreviewDirector"/>). Vibrações e pedidos ao sistema viram linhas no console.</summary>
public sealed class PreviewPlatform(PreviewOptions options) : GamePlatform
{
    private readonly List<TouchPoint> _touches = [];
    private bool _back;
    private bool _interrupted;
    private bool _lowMemory;

    public float? Tilt { get; set; }

    public List<string> Log { get; } = [];

    public override bool IsMobile => true;

    public override bool CanQuit => false;

    public override bool ControlsFullscreen => false;

    public override bool PrefersTouch => true;

    public override bool ShowsLoadingScreen => true;

    public override (int Width, int Height)? DesiredBackBufferSize => (options.Width, options.Height);

    public override float PixelsPerDp => options.Density;

    public override SafeInsets SafeInsets => options.Insets;

    public override ISaveStore SaveStore => new FileSaveStore(options.SavePath);

    public override DeviceTier DeviceTier => options.Tier;

    public override int SceneryCacheSize => 3;

    public override bool CanVibrate => true;

    public override bool HasTiltSensor => true;

    public void SetTouches(IEnumerable<TouchPoint> touches)
    {
        _touches.Clear();
        _touches.AddRange(touches);
    }

    public override void ReadTouches(List<TouchPoint> touches) => touches.AddRange(_touches);

    public override float? ReadTilt() => Tilt;

    public void RequestBack() => _back = true;

    public void Interrupt()
    {
        _interrupted = true;
        RaiseSuspending();
    }

    public void WarnLowMemory() => _lowMemory = true;

    public override bool ConsumeBackRequest() => Take(ref _back);

    public override bool ConsumeInterruption() => Take(ref _interrupted);

    public override bool ConsumeLowMemoryWarning() => Take(ref _lowMemory);

    private static bool Take(ref bool flag)
    {
        bool value = flag;
        flag = false;
        return value;
    }

    public override void Vibrate(Haptic kind) => Record($"vibrar {kind}");

    public override void SetTiltSensorEnabled(bool enabled) => Record($"sensor de inclinação {(enabled ? "ligado" : "desligado")}");

    public override void LeaveToBackground() => Record("app pro fundo");

    public override void SetPreferredFrameRate(int framesPerSecond) => Record($"taxa de quadros {framesPerSecond}");

    public override void GameplayStart() => Record("jogando");

    public override void GameplayStop() => Record("parou de jogar");

    private void Record(string line)
    {
        Log.Add(line);
        Console.WriteLine($"[plataforma] {line}");
    }
}

/// <summary>Configuração do aparelho simulado (linha de comando).</summary>
public sealed class PreviewOptions
{
    /// <summary>Aparelhos de referência: tamanho em pixels (deitado), densidade (px por dp) e área segura.</summary>
    public static readonly Dictionary<string, (int W, int H, float Density, SafeInsets Insets)> Devices = new()
    {
        ["phone-20x9"] = (2400, 1080, 2.625f, new SafeInsets(96, 0, 0, 0)),
        ["phone-19.5x9"] = (2340, 1080, 2.75f, new SafeInsets(0, 0, 110, 0)),
        ["phone-18x9"] = (2160, 1080, 2.75f, new SafeInsets(0, 0, 0, 0)),
        ["phone-16x9"] = (1920, 1080, 2.75f, new SafeInsets(0, 0, 0, 0)),
        ["phone-21x9"] = (2520, 1080, 2.625f, new SafeInsets(80, 0, 0, 0)),
        ["phone-small"] = (1280, 720, 2f, new SafeInsets(0, 0, 0, 0)),
        ["tablet-16x10"] = (2560, 1600, 2f, new SafeInsets(0, 0, 0, 0)),
        ["tablet-4x3"] = (2048, 1536, 2f, new SafeInsets(0, 0, 0, 0)),
        ["foldable"] = (2208, 1840, 2.625f, new SafeInsets(0, 0, 0, 0)),
        ["portrait"] = (1080, 2400, 2.625f, new SafeInsets(0, 96, 0, 0)),
    };

    public string DeviceName { get; private set; } = "phone-20x9";

    public int Width { get; private set; }

    public int Height { get; private set; }

    public float Density { get; private set; }

    public SafeInsets Insets { get; private set; }

    public DeviceTier Tier { get; private set; } = DeviceTier.Medium;

    public string SavePath { get; private set; }

    public string ScriptPath { get; private set; }

    public string OutputDirectory { get; private set; } = ".";

    public static PreviewOptions Parse(string[] args)
    {
        var options = new PreviewOptions();
        string sourceSave = null;
        for (int i = 0; i < args.Length - 1; i += 2)
        {
            string value = args[i + 1];
            switch (args[i])
            {
                case "--device":
                    options.DeviceName = value;
                    break;
                case "--save":
                    sourceSave = value;
                    break;
                case "--script":
                    options.ScriptPath = value;
                    break;
                case "--out":
                    options.OutputDirectory = value;
                    break;
                case "--tier":
                    options.Tier = Enum.Parse<DeviceTier>(value, ignoreCase: true);
                    break;
            }
        }

        (int w, int h, float density, SafeInsets insets) = Devices[options.DeviceName];
        options.Width = w;
        options.Height = h;
        options.Density = density;
        options.Insets = insets;

        Directory.CreateDirectory(options.OutputDirectory);
        options.SavePath = Path.Combine(options.OutputDirectory, $"save-{options.DeviceName}.json");
        foreach (string stale in new[] { options.SavePath, options.SavePath + ".bak", options.SavePath + ".tmp" })
        {
            File.Delete(stale);
        }

        if (sourceSave is not null)
        {
            File.Copy(sourceSave, options.SavePath, overwrite: true);
        }

        return options;
    }
}
