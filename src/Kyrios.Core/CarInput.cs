namespace Kyrios.Core;

/// <summary>
/// Comando de controle de um carro em um instante: acelerador (-1 ré .. 1 acelerar)
/// e direção (-1 esquerda .. 1 direita). Independe de vir do teclado ou de uma IA.
/// </summary>
public readonly struct CarInput
{
    public float Throttle { get; }
    public float Steering { get; }
    public bool Brake { get; }

    public CarInput(float throttle, float steering, bool brake = false)
    {
        Throttle = Math.Clamp(throttle, -1f, 1f);
        Steering = Math.Clamp(steering, -1f, 1f);
        Brake = brake;
    }

    public static CarInput None => new(0f, 0f);
}
