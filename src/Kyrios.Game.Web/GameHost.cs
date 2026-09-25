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
            RegisterPlatformFactories();
            GamePlatform.Current = new WebPlatform((IJSInProcessRuntime)JsRuntime);
            _game = new GameRoot();
            _game.Run();
        }

        _game.Tick();
    }

    /// <summary>Registra a implementação web de cada parte do framework de uma vez, sem a busca por reflexão
    /// (que escreve avisos no console do navegador).</summary>
    private static void RegisterPlatformFactories()
    {
        Microsoft.Xna.Platform.GameFactory.RegisterGameFactory(new Microsoft.Xna.Platform.ConcreteGameFactory());
        Microsoft.Xna.Platform.TitleContainerFactory.RegisterTitleContainerFactory(new Microsoft.Xna.Platform.ConcreteTitleContainerFactory());
        Microsoft.Xna.Platform.Graphics.GraphicsFactory.RegisterGraphicsFactory(new Microsoft.Xna.Platform.Graphics.ConcreteGraphicsFactory());
        Microsoft.Xna.Platform.Input.InputFactory.RegisterInputFactory(new Microsoft.Xna.Platform.Input.ConcreteInputFactory());
        Microsoft.Xna.Platform.Audio.AudioFactory.RegisterAudioFactory(new Microsoft.Xna.Platform.Audio.ConcreteAudioFactory());
        Microsoft.Xna.Platform.Media.MediaFactory.RegisterMediaFactory(new Microsoft.Xna.Platform.Media.ConcreteMediaFactory());
        Microsoft.Xna.Platform.Devices.DevicesFactory.RegisterDevicesFactory(new Microsoft.Xna.Platform.Devices.ConcreteDevicesFactory());
    }
}
