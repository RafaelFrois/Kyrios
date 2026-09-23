using Kyrios.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Kyrios.Game;

/// <summary>Wrapper fino sobre o teclado do MonoGame: estado real de tecla pressionada/solta (sem gambiarra de expiração).</summary>
public sealed class GameInput
{
    private KeyboardState _current;
    private KeyboardState _previous;
    private MouseState _currentMouse;
    private MouseState _previousMouse;

    public void Update()
    {
        _previous = _current;
        _current = Keyboard.GetState();
        _previousMouse = _currentMouse;
        _currentMouse = Mouse.GetState();
    }

    /// <summary>Posição do mouse em pixels de tela (relativa à janela do jogo) — quem usa precisa converter
    /// pro espaço de coordenadas "lógico" do jogo, já que a cena é escalada/centralizada (ver BuildScreenTransform).</summary>
    public Point MousePosition => _currentMouse.Position;

    public bool IsMouseLeftDown => _currentMouse.LeftButton == ButtonState.Pressed;

    public bool WasMouseLeftJustPressed =>
        _currentMouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;

    /// <summary>Quantos "dentes" a roda do mouse girou desde o quadro anterior (positivo = pra cima).</summary>
    public int ScrollWheelSteps => (_currentMouse.ScrollWheelValue - _previousMouse.ScrollWheelValue) / 120;

    public bool IsDown(Keys key) => _current.IsKeyDown(key);

    public bool WasJustPressed(Keys key) => _current.IsKeyDown(key) && !_previous.IsKeyDown(key);

    public bool AnyKeyJustPressed() => _current.GetPressedKeys().Any(k => !_previous.IsKeyDown(k));

    /// <summary>Como <see cref="AnyKeyJustPressed"/>, mas ignorando as teclas passadas — usado na tela
    /// inicial pra alternar tela cheia (F11) sem que isso também conte como "aperte qualquer tecla".</summary>
    public bool AnyKeyJustPressedExcept(params Keys[] excludedKeys) =>
        _current.GetPressedKeys().Any(k => !_previous.IsKeyDown(k) && !excludedKeys.Contains(k));

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

        return new CarInput(throttle, steering, brake, boost);
    }
}
