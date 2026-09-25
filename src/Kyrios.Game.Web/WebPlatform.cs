using Microsoft.JSInterop;

namespace Kyrios.Game.Web;

/// <summary>
/// A plataforma "navegador": save no armazenamento local (que a Poki sincroniza na nuvem sozinha), hora local do
/// navegador, eventos e intervalos comerciais do SDK da Poki e o tamanho real do canvas. Toda chamada passa por
/// megrace.js, que já trata a ausência do SDK (bloqueador de anúncios, teste local) e falhas de armazenamento.
/// </summary>
public sealed class WebPlatform : GamePlatform
{
    private readonly IJSInProcessRuntime _js;
    private readonly DotNetObjectReference<WebPlatform> _self;
    private readonly LocalStorageSaveStore _saveStore;
    private Action _commercialBreakFinished;

    public WebPlatform(IJSInProcessRuntime js)
    {
        _js = js;
        _self = DotNetObjectReference.Create(this);
        _saveStore = new LocalStorageSaveStore(js);
    }

    public override bool IsWeb => true;

    public override bool CanQuit => false;

    public override bool ControlsFullscreen => false;

    public override bool UsesFrameworkFixedTimeStep => false;

    public override ISaveStore SaveStore => _saveStore;

    /// <summary>Sem dados de fuso horário no WebAssembly, o relógio local vem do deslocamento informado pelo navegador.</summary>
    public override DateTime LocalNow
    {
        get
        {
            int offsetMinutes = _js.Invoke<int>("megrace.timezoneOffset");
            return DateTime.SpecifyKind(DateTime.UtcNow.AddMinutes(-offsetMinutes), DateTimeKind.Unspecified);
        }
    }

    private float _pointerScale = 1f;

    /// <summary>Tamanho real do canvas (página × densidade da tela). Consultado uma vez por quadro; a mesma chamada
    /// devolve a escala do ponteiro, já que mouse e toque chegam em pixels da página.</summary>
    public override (int Width, int Height)? DesiredBackBufferSize
    {
        get
        {
            int[] size = _js.Invoke<int[]>("megrace.canvasPixelSize");
            if (size is not { Length: 3 })
            {
                return null;
            }

            _pointerScale = size[2] / 1000f;
            return (size[0], size[1]);
        }
    }

    public override float PointerScale => _pointerScale;

    public override bool PrefersTouch => _js.Invoke<bool>("megrace.prefersTouch");

    public override void LoadingFinished() => _js.InvokeVoid("megrace.loadingFinished");

    public override void GameplayStart() => _js.InvokeVoid("megrace.gameplayStart");

    public override void GameplayStop() => _js.InvokeVoid("megrace.gameplayStop");

    public override void CommercialBreak(Action onFinished)
    {
        _commercialBreakFinished = onFinished;
        _js.InvokeVoid("megrace.commercialBreak", _self);
    }

    [JSInvokable]
    public void OnCommercialBreakFinished()
    {
        Action callback = _commercialBreakFinished;
        _commercialBreakFinished = null;
        callback?.Invoke();
    }
}

/// <summary>Save no localStorage: chave principal + cópia da gravação anterior (recupera um save corrompido).
/// Se o navegador bloquear o armazenamento (aba anônima, cota cheia), megrace.js devolve vazio/falso e o jogo segue
/// com o progresso em memória, sem travar.</summary>
public sealed class LocalStorageSaveStore(IJSInProcessRuntime js) : ISaveStore
{
    public const string MainKey = "megrace.save";
    public const string BackupKey = "megrace.save.backup";

    public IEnumerable<string> ReadCandidates()
    {
        foreach (string key in new[] { MainKey, BackupKey })
        {
            string text = js.Invoke<string>("megrace.storageGet", key);
            if (!string.IsNullOrEmpty(text))
            {
                yield return text;
            }
        }
    }

    public void Write(string json)
    {
        string previous = js.Invoke<string>("megrace.storageGet", MainKey);
        if (!string.IsNullOrEmpty(previous) && previous != json)
        {
            js.Invoke<bool>("megrace.storageSet", BackupKey, previous);
        }

        js.Invoke<bool>("megrace.storageSet", MainKey, json);
    }
}
