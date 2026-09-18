using Kyrios.Core;

namespace Kyrios.ConsoleGame;

/// <summary>
/// Lê o teclado do console sem bloquear o loop do jogo. Como o Console não expõe eventos de
/// "tecla solta", cada tecla é considerada "pressionada" enquanto for vista dentro de uma janela
/// curta de tempo — o repeat automático do terminal renova essa janela enquanto a tecla está
/// realmente segurada.
/// </summary>
public sealed class InputManager
{
    private static readonly TimeSpan HoldWindow = TimeSpan.FromMilliseconds(220);

    private readonly Dictionary<ConsoleKey, DateTime> _lastSeen = new();

    public bool QuitRequested { get; private set; }
    public bool RestartRequested { get; private set; }

    public void Poll()
    {
        while (Console.KeyAvailable)
        {
            ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
            _lastSeen[keyInfo.Key] = DateTime.UtcNow;

            if (keyInfo.Key == ConsoleKey.Escape)
            {
                QuitRequested = true;
            }
            else if (keyInfo.Key == ConsoleKey.R)
            {
                RestartRequested = true;
            }
        }
    }

    public void ConsumeRestartRequest() => RestartRequested = false;

    public CarInput BuildCarInput()
    {
        float throttle = 0f;
        float steering = 0f;

        if (IsHeld(ConsoleKey.UpArrow) || IsHeld(ConsoleKey.W))
        {
            throttle += 1f;
        }

        if (IsHeld(ConsoleKey.DownArrow) || IsHeld(ConsoleKey.S))
        {
            throttle -= 1f;
        }

        if (IsHeld(ConsoleKey.LeftArrow) || IsHeld(ConsoleKey.A))
        {
            steering -= 1f;
        }

        if (IsHeld(ConsoleKey.RightArrow) || IsHeld(ConsoleKey.D))
        {
            steering += 1f;
        }

        bool brake = IsHeld(ConsoleKey.Spacebar);

        return new CarInput(throttle, steering, brake);
    }

    private bool IsHeld(ConsoleKey key) =>
        _lastSeen.TryGetValue(key, out DateTime lastSeen) && (DateTime.UtcNow - lastSeen) < HoldWindow;
}
