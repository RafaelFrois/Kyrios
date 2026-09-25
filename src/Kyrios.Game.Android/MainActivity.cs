using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Window;
using Microsoft.Xna.Framework;

namespace Kyrios.Game.Android;

/// <summary>
/// A tela única do app. Cria a plataforma Android, o mesmo <see cref="GameRoot"/> das outras versões e repassa ao
/// jogo o que o sistema avisa: voltar, perda de foco (ligação, notificação, bloqueio, troca de app), memória baixa e
/// área segura. Mantém o jogo em tela cheia imersiva (barras do sistema escondidas, aparecem só com um deslize).
/// </summary>
[Activity(
    Label = "@string/app_name",
    MainLauncher = true,
    Exported = true,
    Theme = "@style/Theme.MegRace",
    AlwaysRetainTaskState = true,
    LaunchMode = LaunchMode.SingleTask,
    ScreenOrientation = ScreenOrientation.SensorLandscape,
    ResizeableActivity = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden
        | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize
        | ConfigChanges.UiMode | ConfigChanges.Navigation | ConfigChanges.Density)]
public sealed class MainActivity : AndroidGameActivity
{
    private AndroidPlatform _platform;
    private GameRoot _game;
    private View _view;
    private BackCallback _backCallback;

    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        _platform = new AndroidPlatform(this);
        GamePlatform.Current = _platform;

        _game = new GameRoot();
        _view = (View)_game.Services.GetService(typeof(View));
        _view.SetOnApplyWindowInsetsListener(new InsetsListener(_platform));
        SetContentView(_view);
        EnterImmersiveMode();

        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            _backCallback = new BackCallback(_platform);
            OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(IOnBackInvokedDispatcher.PriorityDefault, _backCallback);
        }

        _game.Run();
    }

    protected override void OnPause()
    {
        _platform?.NotifySuspending();
        base.OnPause();
    }

    protected override void OnResume()
    {
        base.OnResume();
        _platform?.NotifyResumed();
        EnterImmersiveMode();
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        if (hasFocus)
        {
            EnterImmersiveMode();
        }
        else
        {
            // Barra de notificações puxada, diálogo do sistema, ligação chegando...
            _platform?.MarkInterrupted();
        }
    }

    public override void OnTrimMemory(TrimMemory level)
    {
        base.OnTrimMemory(level);
        if (level >= TrimMemory.RunningLow)
        {
            _platform?.MarkLowMemory();
        }
    }

    public override void OnLowMemory()
    {
        base.OnLowMemory();
        _platform?.MarkLowMemory();
    }

    /// <summary>Até o Android 12 o "voltar" chega como tecla; do 13 em diante, pelo <see cref="BackCallback"/>.</summary>
    public override bool DispatchKeyEvent(KeyEvent e)
    {
        if (e.KeyCode == Keycode.Back && !OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            if (e.Action == KeyEventActions.Up && !e.IsCanceled)
            {
                _platform?.RequestBack();
            }

            return true;
        }

        return base.DispatchKeyEvent(e);
    }

    protected override void OnDestroy()
    {
        if (_backCallback is not null && OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            OnBackInvokedDispatcher.UnregisterOnBackInvokedCallback(_backCallback);
        }

        base.OnDestroy();
    }

    /// <summary>Tela cheia imersiva: sem barra de status nem de navegação; um deslize na borda mostra as barras por
    /// um instante, por cima do jogo, sem tirar o app da tela.</summary>
    private void EnterImmersiveMode()
    {
        if (Window is not { } window)
        {
            return;
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            // Do Android 15 em diante a tela já vai de ponta a ponta sozinha.
            if (!OperatingSystem.IsAndroidVersionAtLeast(35))
            {
                window.SetDecorFitsSystemWindows(false);
            }

            if (window.InsetsController is { } controller)
            {
                controller.Hide(WindowInsets.Type.SystemBars());
                controller.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
            }
        }
        else
        {
            window.DecorView.SystemUiFlags = SystemUiFlags.ImmersiveSticky | SystemUiFlags.HideNavigation | SystemUiFlags.Fullscreen
                | SystemUiFlags.LayoutStable | SystemUiFlags.LayoutHideNavigation | SystemUiFlags.LayoutFullscreen;
        }
    }

    private sealed class InsetsListener(AndroidPlatform platform) : Java.Lang.Object, View.IOnApplyWindowInsetsListener
    {
        public WindowInsets OnApplyWindowInsets(View view, WindowInsets insets)
        {
            platform.UpdateSafeInsets(insets);
            return view.OnApplyWindowInsets(insets);
        }
    }

    private sealed class BackCallback(AndroidPlatform platform) : Java.Lang.Object, IOnBackInvokedCallback
    {
        public void OnBackInvoked() => platform.RequestBack();
    }
}
