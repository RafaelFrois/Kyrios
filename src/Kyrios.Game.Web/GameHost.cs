using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.Xna.Framework;

namespace Kyrios.Game.Web;

/// <summary>Liga o jogo ao navegador: a cada quadro do requestAnimationFrame (megrace.js) roda um passo do laço
/// do jogo. O jogo em si é o mesmo <see cref="GameRoot"/> do desktop; só a plataforma é trocada.</summary>
public sealed class GameHost : ComponentBase
{
    private Microsoft.Xna.Framework.Game _game;

    [Inject]
    private IJSRuntime JsRuntime { get; set; }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            ((IJSInProcessRuntime)JsRuntime).InvokeVoid("megrace.startLoop", DotNetObjectReference.Create(this));
        }
    }

    [JSInvokable]
    public void TickDotNet()
    {
        if (_game is null)
        {
            GamePlatform.Current = new WebPlatform((IJSInProcessRuntime)JsRuntime);
            _game = new GameRoot();
            _game.Run();
        }

        _game.Tick();
    }
}
