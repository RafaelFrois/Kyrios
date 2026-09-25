using Kyrios.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Kyrios.Game;

/// <summary>
/// Teclado, mouse e controle (gamepad) num lugar só. As telas usam as ações de menu (<see cref="MenuUp"/>,
/// <see cref="Confirm"/>, <see cref="Back"/>...) em vez de teclas específicas, então teclado e controle navegam
/// igual em todo o jogo. Na tela de toque (celular/tablet), o dedo principal funciona como o mouse — todos os menus
/// respondem ao toque sem código extra — e todos os dedos ficam disponíveis pros botões da corrida (<see cref="Touches"/>).
/// </summary>
public sealed class GameInput
{
    private KeyboardState _current;
    private KeyboardState _previous;
    private MouseState _currentMouse;
    private MouseState _previousMouse;
    private GamePadState _pad;
    private GamePadState _previousPad;
    private readonly List<TouchPoint> _rawTouches = [];
    private readonly List<TouchPoint> _touchPoints = [];
    private readonly List<Vector2> _touches = [];
    private readonly List<Vector2> _touchesStarted = [];
    private bool _systemBack;
    private bool _touchDown;
    private bool _previousTouchDown;
    private Point _touchPosition;
    private int _framesSinceTouch = int.MaxValue;
    private Point _pointerPosition;
    private Point _previousPointerPosition;

    /// <summary>Quantos pixels da área de desenho valem um pixel informado pelo mouse/toque. No navegador o mouse
    /// vem em pixels da página e a área de desenho usa a densidade real da tela (2x, 3x no celular).</summary>
    public float PointerScale { get; set; } = 1f;

    /// <summary>O jogador está usando a tela de toque (mostra os botões da corrida e esconde as dicas de teclado).
    /// Volta a falso quando ele usa o teclado ou mexe um mouse de verdade.</summary>
    public bool UsingTouch { get; set; }

    /// <summary>Dedos na tela, em pixels da área de desenho.</summary>
    public IReadOnlyList<Vector2> Touches => _touches;

    /// <summary>Dedos que encostaram na tela neste quadro.</summary>
    public IReadOnlyList<Vector2> TouchesStarted => _touchesStarted;

    /// <summary>Dedos na tela com identificador (acompanhar o mesmo dedo entre quadros), em pixels da área de desenho.</summary>
    public IReadOnlyList<TouchPoint> TouchPoints => _touchPoints;

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

        UpdateTouches();
        _systemBack = GamePlatform.Current.ConsumeBackRequest();

        // Modo toque: liga com qualquer dedo na tela; desliga com o teclado ou com um mouse de verdade (alguns
        // navegadores ainda mandam um "mouse" sintético logo depois do toque, então ele só conta um tempo depois).
        bool realMouseMoved = _currentMouse.Position != _previousMouse.Position && _framesSinceTouch > 30;
        if (_touchDown)
        {
            UsingTouch = true;
        }
        else if (_current.GetPressedKeyCount() > 0 || realMouseMoved)
        {
            UsingTouch = false;
        }

        _previousPointerPosition = _pointerPosition;
        _pointerPosition = UsingTouch ? _touchPosition : Scale(_currentMouse.Position);
    }

    private void UpdateTouches()
    {
        _rawTouches.Clear();
        _touchPoints.Clear();
        _touches.Clear();
        _touchesStarted.Clear();
        _previousTouchDown = _touchDown;
        try
        {
            GamePlatform.Current.ReadTouches(_rawTouches);
        }
        catch (Exception)
        {
            // Plataforma sem tela de toque: segue só com teclado/mouse.
            _rawTouches.Clear();
        }

        foreach (TouchPoint touch in _rawTouches)
        {
            Vector2 position = touch.Position * PointerScale;
            _touchPoints.Add(touch with { Position = position });
            _touches.Add(position);
            if (touch.JustPressed)
            {
                _touchesStarted.Add(position);
            }
        }

        _touchDown = _touches.Count > 0;
        if (_touchDown)
        {
            _touchPosition = _touches[0].ToPoint();
            _framesSinceTouch = 0;
        }
        else if (_framesSinceTouch < int.MaxValue)
        {
            _framesSinceTouch++;
        }
    }

    private Point Scale(Point point) => PointerScale == 1f ? point : new Point((int)(point.X * PointerScale), (int)(point.Y * PointerScale));

    /// <summary>Posição do ponteiro (mouse ou dedo principal) em pixels da área de desenho — quem usa precisa
    /// converter pro espaço de coordenadas "lógico" do jogo, já que a cena é escalada/centralizada.</summary>
    public Point MousePosition => _pointerPosition;

    public bool MouseMoved => _pointerPosition != _previousPointerPosition;

    public bool IsMouseLeftDown => _currentMouse.LeftButton == ButtonState.Pressed || _touchDown;

    public bool WasMouseLeftJustPressed =>
        (_currentMouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released)
        || (_touchDown && !_previousTouchDown);

    /// <summary>Quanto o ponteiro foi arrastado na vertical neste quadro com o botão/dedo pressionado (pixels da área
    /// de desenho) — rolar listas com o dedo.</summary>
    public float DragDeltaY => IsMouseLeftDown && !WasMouseLeftJustPressed ? _pointerPosition.Y - _previousPointerPosition.Y : 0f;

    /// <summary>Quantos "dentes" a roda do mouse girou desde o quadro anterior (positivo = pra cima).</summary>
    public int ScrollWheelSteps => (_currentMouse.ScrollWheelValue - _previousMouse.ScrollWheelValue) / 120;

    /// <summary>Qualquer sinal de vida do jogador neste quadro (tecla, mouse ou controle).</summary>
    public bool AnyActivity =>
        _current.GetPressedKeyCount() > 0 || MouseMoved || IsMouseLeftDown || ScrollWheelSteps != 0 || _touchDown
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

    /// <summary>Voltar: ESC, BACKSPACE, B/BACK do controle ou o botão/gesto "voltar" do celular.</summary>
    public bool Back => Any(Keys.Escape, Keys.Back) || PadPressed(Buttons.B) || PadPressed(Buttons.Back) || _systemBack;

    /// <summary>Pausar/retomar a corrida (ESC, P, START ou o "voltar" do celular).</summary>
    public bool Pause => Any(Keys.Escape, Keys.P) || PadPressed(Buttons.Start) || _systemBack;

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
