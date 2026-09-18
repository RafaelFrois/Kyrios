using Kyrios.Core;
using Microsoft.Xna.Framework.Input;

namespace Kyrios.Game;

/// <summary>Wrapper fino sobre o teclado do MonoGame: estado real de tecla pressionada/solta (sem gambiarra de expiração).</summary>
public sealed class GameInput
{
    private KeyboardState _current;
    private KeyboardState _previous;

    public void Update()
    {
        _previous = _current;
        _current = Keyboard.GetState();
    }

    public bool IsDown(Keys key) => _current.IsKeyDown(key);

    public bool WasJustPressed(Keys key) => _current.IsKeyDown(key) && !_previous.IsKeyDown(key);

    public bool AnyKeyJustPressed() => _current.GetPressedKeys().Any(k => !_previous.IsKeyDown(k));

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
