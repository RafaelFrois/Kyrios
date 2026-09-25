namespace Kyrios.Core;

/// <summary>
/// Obstáculo móvel do modo <see cref="RaceMode.TimeAttack"/>: oscila para frente e para trás ao longo de
/// um eixo fixo, forçando o piloto a cronometrar a passagem em vez de só desviar de algo parado.
/// </summary>
public sealed class Hazard
{
    public Vector2D Center { get; }
    public Vector2D Axis { get; }
    public float Amplitude { get; }
    public float Period { get; }
    public float Radius { get; }

    public Vector2D Position { get; private set; }

    private float _elapsed;

    public Hazard(Vector2D center, Vector2D axis, float amplitude, float period, float radius = 0.4f)
    {
        Center = center;
        Axis = axis;
        Amplitude = amplitude;
        Period = period;
        Radius = radius;
        Position = center;
    }

    public void Update(float dt)
    {
        _elapsed += dt;
        float offset = MathF.Sin((_elapsed / Period) * 2f * MathF.PI) * Amplitude;
        Position = Center + (Axis * offset);
    }
}
