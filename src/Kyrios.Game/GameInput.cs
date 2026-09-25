using Kyrios.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Kyrios.Game;

/// <summary>
/// Teclado, mouse e controle (gamepad) num lugar só. As telas usam as ações de menu (<see cref="MenuUp"/>,
/// <see cref="Confirm"/>, <see cref="Back"/>...) em vez de teclas específicas, então teclado e controle navegam
/// igual em todo o jogo.
/// </summary>
public sealed class GameInput
{
    private KeyboardState _current;
    private KeyboardState _previous;
    private MouseState _currentMouse;
    private MouseState _previousMouse;
    private GamePadState _pad;
    private GamePadState _previousPad;

    public void Update()
    {
        _previous = _current;
        _current = Keyboard.GetState();
        _previousMouse = _currentMouse;
        _currentMouse = Mouse.GetState();
        _previousPad = _pad;
        try
        {
            _pad = GamePad.GetState(PlayerIndex.One);
        }
        catch (Exception)
        {
            _pad = default;
        }
    }

    /// <summary>Posição do mouse em pixels de tela (relativa à janela do jogo) — quem usa precisa converter
    /// pro espaço de coordenadas "lógico" do jogo, já que a cena é escalada/centralizada.</summary>
    public Point MousePosition => _currentMouse.Position;

    public bool MouseMoved => _currentMouse.Position != _previousMouse.Position;

    public bool IsMouseLeftDown => _currentMouse.LeftButton == ButtonState.Pressed;

    public bool WasMouseLeftJustPressed =>
        _currentMouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;

    /// <summary>Quantos "dentes" a roda do mouse girou desde o quadro anterior (positivo = pra cima).</summary>
    public int ScrollWheelSteps => (_currentMouse.ScrollWheelValue - _previousMouse.ScrollWheelValue) / 120;

    /// <summary>Qualquer sinal de vida do jogador neste quadro (tecla, mouse ou controle).</summary>
    public bool AnyActivity =>
        _current.GetPressedKeyCount() > 0 || MouseMoved || IsMouseLeftDown || ScrollWheelSteps != 0
        || (_pad.IsConnected && (_pad.Buttons != _previousPad.Buttons || _pad.ThumbSticks.Left.LengthSquared() > 0.05f));

    /// <summary>Teclas que acabaram de ser apertadas neste quadro (pra reconhecer sequências, como um código).</summary>
    public IEnumerable<Keys> JustPressedKeys => _current.GetPressedKeys().Where(key => !_previous.IsKeyDown(key));

    public bool IsDown(Keys key) => _current.IsKeyDown(key);

    public bool WasJustPressed(Keys key) => _current.IsKeyDown(key) && !_previous.IsKeyDown(key);

    private bool PadPressed(Buttons button) => _pad.IsConnected && _pad.IsButtonDown(button) && !_previousPad.IsButtonDown(button);

    private bool Any(params Keys[] keys) => keys.Any(WasJustPressed);

    // ----- Ações de menu -----
    public bool MenuUp => Any(Keys.Up, Keys.W) || PadPressed(Buttons.DPadUp) || PadPressed(Buttons.LeftThumbstickUp);

    public bool MenuDown => Any(Keys.Down, Keys.S) || PadPressed(Buttons.DPadDown) || PadPressed(Buttons.LeftThumbstickDown);

    public bool MenuLeft => Any(Keys.Left, Keys.A) || PadPressed(Buttons.DPadLeft) || PadPressed(Buttons.LeftThumbstickLeft) || PadPressed(Buttons.LeftShoulder);

    public bool MenuRight => Any(Keys.Right, Keys.D) || PadPressed(Buttons.DPadRight) || PadPressed(Buttons.LeftThumbstickRight) || PadPressed(Buttons.RightShoulder);

    public bool Confirm => Any(Keys.Enter, Keys.Space) || PadPressed(Buttons.A);

    public bool Back => Any(Keys.Escape, Keys.Back) || PadPressed(Buttons.B) || PadPressed(Buttons.Back);

    /// <summary>Pausar/retomar a corrida (ESC, P ou START).</summary>
    public bool Pause => Any(Keys.Escape, Keys.P) || PadPressed(Buttons.Start);

    public CarInput BuildCarInput()
    {
        float throttle = 0f;
        float steering = 0f;

        if (IsDown(Keys.Up) || IsDown(Keys.W))
        {
            throttle += 1f;
        }

        if (IsDown(Keys.Down) || IsDown(Keys.S))
        {
            throttle -= 1f;
        }

        if (IsDown(Keys.Left) || IsDown(Keys.A))
        {
            steering -= 1f;
        }

        if (IsDown(Keys.Right) || IsDown(Keys.D))
        {
            steering += 1f;
        }

        bool brake = IsDown(Keys.Space);
        bool boost = IsDown(Keys.LeftShift) || IsDown(Keys.RightShift);

        if (_pad.IsConnected)
        {
            // Gatilho direito acelera, esquerdo dá ré; analógico/direcional esquerdo vira; A = freio de mão,
            // RB ou B = turbo.
            throttle += _pad.Triggers.Right - _pad.Triggers.Left;
            float stick = _pad.ThumbSticks.Left.X;
            steering += MathF.Abs(stick) > 0.15f ? stick : 0f;
            steering += _pad.IsButtonDown(Buttons.DPadLeft) ? -1f : _pad.IsButtonDown(Buttons.DPadRight) ? 1f : 0f;
            brake |= _pad.IsButtonDown(Buttons.A);
            boost |= _pad.IsButtonDown(Buttons.RightShoulder) || _pad.IsButtonDown(Buttons.B);
        }

        return new CarInput(Math.Clamp(throttle, -1f, 1f), Math.Clamp(steering, -1f, 1f), brake, boost);
    }
}
