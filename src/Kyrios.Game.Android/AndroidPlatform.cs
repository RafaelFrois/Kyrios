using Android.App;
using Android.Content;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using Android.Views;

namespace Kyrios.Game.Android;

/// <summary>
/// A "casca" Android do jogo: tudo o que muda entre o celular e as outras plataformas passa por aqui (ver
/// <see cref="GamePlatform"/>). O jogo em si é o mesmo <see cref="GameRoot"/> do desktop e da web.
/// Os avisos do sistema (voltar, interrupção, memória) chegam pela <see cref="MainActivity"/> e ficam anotados até o
/// jogo consultá-los no próximo quadro — o laço do jogo e a Activity rodam na mesma thread (a da interface).
/// </summary>
public sealed class AndroidPlatform : GamePlatform
{
    private readonly Activity _activity;
    private readonly string _savePath;
    private readonly Vibrator _vibrator;
    private readonly TiltSensor _tilt;
    private readonly Lazy<DeviceTier> _deviceTier;
    private SafeInsets _safeInsets;
    private bool _backRequested;
    private bool _interrupted;
    private bool _lowMemory;
    private long _lastVibrationTicks;
    private int _preferredFrameRate;

    public AndroidPlatform(Activity activity)
    {
        _activity = activity;
        _savePath = Path.Combine(activity.FilesDir!.AbsolutePath, "records.json");
        _vibrator = GetVibrator(activity);
        _tilt = new TiltSensor(activity);
        _deviceTier = new Lazy<DeviceTier>(() => DetectDeviceTier(activity));
    }

    public override bool IsMobile => true;

    public override bool CanQuit => false;

    public override bool ControlsFullscreen => false;

    public override bool PrefersTouch => true;

    public override bool ShowsLoadingScreen => true;

    public override bool CanLoseGraphicsContext => true;

    public override int SceneryCacheSize => DeviceTier == DeviceTier.Low ? 2 : 3;

    /// <summary>Armazenamento interno do app (só ele lê). Entra no backup automático do Android: o progresso volta
    /// numa reinstalação ou num aparelho novo.</summary>
    public override ISaveStore SaveStore => new FileSaveStore(_savePath);

    public override float PixelsPerDp => _activity.Resources?.DisplayMetrics?.Density is > 0f and var density ? density : 1f;

    public override SafeInsets SafeInsets => _safeInsets;

    public override DeviceTier DeviceTier => _deviceTier.Value;

    // ---------- Avisos do sistema (anotados pela Activity) ----------

    public void RequestBack() => _backRequested = true;

    public void MarkInterrupted() => _interrupted = true;

    public void MarkLowMemory() => _lowMemory = true;

    public void NotifySuspending()
    {
        _interrupted = true;
        _tilt.Pause();
        RaiseSuspending();
    }

    public void NotifyResumed() => _tilt.Resume();

    public override bool ConsumeBackRequest() => Take(ref _backRequested);

    public override bool ConsumeInterruption() => Take(ref _interrupted);

    public override bool ConsumeLowMemoryWarning() => Take(ref _lowMemory);

    private static bool Take(ref bool flag)
    {
        bool value = flag;
        flag = false;
        return value;
    }

    public override void LeaveToBackground() => _activity.MoveTaskToBack(true);

    // ---------- Área segura ----------

    /// <summary>Notch/furo da câmera, bordas curvas e cantos arredondados, a partir dos recortes informados pelo
    /// sistema. Os cantos arredondados viram uma folga nas laterais (a cena é mais larga que alta).</summary>
    public void UpdateSafeInsets(WindowInsets insets)
    {
        int left = 0, top = 0, right = 0, bottom = 0;
        if (OperatingSystem.IsAndroidVersionAtLeast(28) && insets.DisplayCutout is { } cutout)
        {
            left = cutout.SafeInsetLeft;
            top = cutout.SafeInsetTop;
            right = cutout.SafeInsetRight;
            bottom = cutout.SafeInsetBottom;
            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                global::Android.Graphics.Insets waterfall = cutout.WaterfallInsets;
                left = Math.Max(left, waterfall.Left);
                top = Math.Max(top, waterfall.Top);
                right = Math.Max(right, waterfall.Right);
                bottom = Math.Max(bottom, waterfall.Bottom);
            }
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            int corner = 0;
            foreach (RoundedCornerPosition position in new[] { RoundedCornerPosition.TopLeft, RoundedCornerPosition.TopRight, RoundedCornerPosition.BottomLeft, RoundedCornerPosition.BottomRight })
            {
                if (insets.GetRoundedCorner((int)position) is { } rounded)
                {
                    corner = Math.Max(corner, rounded.Radius);
                }
            }

            int pad = (int)(corner * 0.3f);
            left = Math.Max(left, pad);
            right = Math.Max(right, pad);
        }

        _safeInsets = new SafeInsets(left, top, right, bottom);
    }

    // ---------- Vibração ----------

    public override bool CanVibrate => _vibrator?.HasVibrator == true;

    public override void Vibrate(Haptic kind)
    {
        if (!CanVibrate)
        {
            return;
        }

        // Batidas seguidas não viram um zumbido contínuo: no máximo uma vibração a cada 70 ms.
        long now = SystemClock.ElapsedRealtime();
        if (kind is Haptic.Tap or Haptic.Collision or Haptic.Checkpoint && now - _lastVibrationTicks < 70)
        {
            return;
        }

        _lastVibrationTicks = now;
        (long[] timings, int[] amplitudes) = kind switch
        {
            Haptic.Tap => (new long[] { 0, 10 }, new[] { 0, 70 }),
            Haptic.Checkpoint => (new long[] { 0, 18 }, new[] { 0, 110 }),
            Haptic.Collision => (new long[] { 0, 35 }, new[] { 0, 210 }),
            Haptic.Elimination => (new long[] { 0, 45 }, new[] { 0, 150 }),
            Haptic.PlayerEliminated => (new long[] { 0, 70, 60, 110 }, new[] { 0, 255, 0, 200 }),
            Haptic.Victory => (new long[] { 0, 40, 70, 40, 70, 110 }, new[] { 0, 170, 0, 210, 0, 255 }),
            Haptic.Unlock => (new long[] { 0, 25, 50, 40 }, new[] { 0, 150, 0, 230 }),
            _ => (new long[] { 0, 30, 60, 30 }, new[] { 0, 130, 0, 190 }),
        };

        try
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                _vibrator.Vibrate(_vibrator.HasAmplitudeControl
                    ? VibrationEffect.CreateWaveform(timings, amplitudes, -1)
                    : VibrationEffect.CreateWaveform(timings, -1));
            }
            else
            {
                _vibrator.Vibrate(timings, -1);
            }
        }
        catch (Java.Lang.Exception)
        {
            // Aparelho que recusa a vibração: o jogo segue sem ela.
        }
    }

    private static Vibrator GetVibrator(Context context)
    {
        try
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                return (context.GetSystemService(Context.VibratorManagerService) as VibratorManager)?.DefaultVibrator;
            }

            return context.GetSystemService(Context.VibratorService) as Vibrator;
        }
        catch (Java.Lang.Exception)
        {
            return null;
        }
    }

    // ---------- Inclinação ----------

    public override bool HasTiltSensor => _tilt.Available;

    public override float? ReadTilt() => _tilt.Read();

    public override void SetTiltSensorEnabled(bool enabled) => _tilt.SetEnabled(enabled);

    // ---------- Desempenho e bateria ----------

    /// <summary>Estimativa da potência do aparelho: pouca memória, poucos núcleos ou Android antigo = BAIXA;
    /// muita memória, 8 núcleos e Android recente = ALTA; o resto = MÉDIA.</summary>
    private static DeviceTier DetectDeviceTier(Context context)
    {
        try
        {
            var manager = context.GetSystemService(Context.ActivityService) as ActivityManager;
            var memory = new ActivityManager.MemoryInfo();
            manager?.GetMemoryInfo(memory);
            long totalMegabytes = memory.TotalMem / (1024 * 1024);
            int cores = System.Environment.ProcessorCount;
            int api = (int)Build.VERSION.SdkInt;
            bool is64Bit = Build.SupportedAbis?.Any(abi => abi.Contains("64", StringComparison.Ordinal)) == true;

            if (manager?.IsLowRamDevice == true || totalMegabytes < 2600 || cores <= 4 || api < 26 || !is64Bit)
            {
                return DeviceTier.Low;
            }

            return totalMegabytes >= 5500 && cores >= 8 && api >= 29 ? DeviceTier.High : DeviceTier.Medium;
        }
        catch (Java.Lang.Exception)
        {
            return DeviceTier.Medium;
        }
    }

    /// <summary>O jogo não desenha mais de 60 quadros por segundo: pedir 60 Hz a telas de 90/120 Hz evita trepidação
    /// e economiza bateria.</summary>
    public override void SetPreferredFrameRate(int framesPerSecond)
    {
        int rate = framesPerSecond <= 30 ? 60 : Math.Min(framesPerSecond, 60);
        if (rate == _preferredFrameRate)
        {
            return;
        }

        _preferredFrameRate = rate;
        _activity.RunOnUiThread(() =>
        {
            try
            {
                if (_activity.Window is { } window)
                {
                    WindowManagerLayoutParams attributes = window.Attributes;
                    attributes.PreferredRefreshRate = rate;
                    window.Attributes = attributes;
                }
            }
            catch (Java.Lang.Exception)
            {
            }
        });
    }

    /// <summary>Durante a corrida a tela não apaga sozinha (quem joga com controle ou inclinação não toca na tela).</summary>
    public override void GameplayStart() => SetKeepScreenOn(true);

    public override void GameplayStop() => SetKeepScreenOn(false);

    private void SetKeepScreenOn(bool on) => _activity.RunOnUiThread(() =>
    {
        if (on)
        {
            _activity.Window?.AddFlags(WindowManagerFlags.KeepScreenOn);
        }
        else
        {
            _activity.Window?.ClearFlags(WindowManagerFlags.KeepScreenOn);
        }
    });

    /// <summary>Sensor de gravidade (ou acelerômetro, suavizado) lido como um volante: girar o aparelho deitado no
    /// sentido horário vira pra direita. Só fica ligado enquanto o esquema INCLINAR está em uso.</summary>
    private sealed class TiltSensor : Java.Lang.Object, ISensorEventListener
    {
        private const float DeadZone = 0.05f;
        private static readonly float FullLock = MathF.Sin(25f * MathF.PI / 180f);

        private readonly Activity _activity;
        private readonly SensorManager _manager;
        private readonly Sensor _sensor;
        private readonly bool _smooth;
        private bool _enabled;
        private bool _registered;
        private float _x;
        private float _y;
        private bool _hasReading;

        public TiltSensor(Activity activity)
        {
            _activity = activity;
            _manager = activity.GetSystemService(Context.SensorService) as SensorManager;
            _sensor = _manager?.GetDefaultSensor(SensorType.Gravity);
            if (_sensor is null)
            {
                _sensor = _manager?.GetDefaultSensor(SensorType.Accelerometer);
                _smooth = true;
            }
        }

        public bool Available => _sensor is not null;

        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            if (enabled)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        public void Pause()
        {
            if (_registered)
            {
                _manager.UnregisterListener(this);
                _registered = false;
                _hasReading = false;
            }
        }

        public void Resume()
        {
            if (_enabled && !_registered && _sensor is not null)
            {
                _registered = _manager.RegisterListener(this, _sensor, SensorDelay.Game);
            }
        }

        public float? Read()
        {
            if (!_hasReading)
            {
                return null;
            }

            // Componente da gravidade no eixo "direita" da tela (depende de como o aparelho está deitado).
            SurfaceOrientation rotation = CurrentRotation();
            float right = rotation switch
            {
                SurfaceOrientation.Rotation90 => -_y,
                SurfaceOrientation.Rotation270 => _y,
                SurfaceOrientation.Rotation180 => -_x,
                _ => _x,
            };

            float steer = -right / SensorManager.GravityEarth / FullLock;
            if (MathF.Abs(steer) < DeadZone)
            {
                return 0f;
            }

            return Math.Clamp((steer - (MathF.Sign(steer) * DeadZone)) / (1f - DeadZone), -1f, 1f);
        }

        private SurfaceOrientation CurrentRotation()
        {
            try
            {
                if (OperatingSystem.IsAndroidVersionAtLeast(30))
                {
                    return _activity.Display?.Rotation ?? SurfaceOrientation.Rotation90;
                }

                return _activity.WindowManager?.DefaultDisplay?.Rotation ?? SurfaceOrientation.Rotation90;
            }
            catch (Java.Lang.Exception)
            {
                return SurfaceOrientation.Rotation90;
            }
        }

        public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
        {
        }

        public void OnSensorChanged(SensorEvent e)
        {
            if (e?.Values is not { Count: >= 2 } values)
            {
                return;
            }

            float x = values[0];
            float y = values[1];
            if (_smooth && _hasReading)
            {
                const float alpha = 0.2f;
                x = _x + ((x - _x) * alpha);
                y = _y + ((y - _y) * alpha);
            }

            _x = x;
            _y = y;
            _hasReading = true;
        }
    }
}
