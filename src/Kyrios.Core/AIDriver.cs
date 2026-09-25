namespace Kyrios.Core;

/// <summary>
/// IA de "seguir waypoints": mira no próximo ponto da linha central da pista, vira proporcionalmente
/// ao ângulo até ele, reduz o acelerador em curvas fechadas, usa turbo em retas e tem uma leve variação
/// orgânica na direção para não dirigir uma linha robótica idêntica toda corrida.
/// </summary>
public sealed class AIDriver
{
    private readonly IReadOnlyList<Vector2D> _waypoints;
    private readonly float _waypointReachRadius;
    private readonly float _skill;
    private readonly Random _random;

    private int _targetIndex;
    private float _steeringJitter;

    public AIDriver(Track track, int startWaypointIndex = 0, float waypointReachRadius = 1.6f, float skill = 1f, int? randomSeed = null)
    {
        if (track.CenterLine.Count == 0)
        {
            throw new ArgumentException("A pista precisa de uma linha central (waypoints) para ter IA.", nameof(track));
        }

        _waypoints = track.CenterLine;
        _targetIndex = ((startWaypointIndex % _waypoints.Count) + _waypoints.Count) % _waypoints.Count;
        _waypointReachRadius = waypointReachRadius;
        _skill = Math.Clamp(skill, 0.4f, 1f);
        _random = randomSeed is null ? new Random() : new Random(randomSeed.Value);
    }

    /// <param name="throttleMultiplier">
    /// Ajuste externo de "rubber-banding": acima de 1 acelera mais (pra colar em quem está na frente),
    /// abaixo de 1 pisa mais leve (pra não disparar de quem está atrás).
    /// </param>
    public CarInput GetInput(Car car, float throttleMultiplier = 1f)
    {
        Vector2D target = _waypoints[_targetIndex];
        if (car.Position.DistanceTo(target) < _waypointReachRadius)
        {
            _targetIndex = (_targetIndex + 1) % _waypoints.Count;
            target = _waypoints[_targetIndex];
        }

        Vector2D toTarget = target - car.Position;
        float desiredAngle = MathF.Atan2(toTarget.Y, toTarget.X);
        float angleDiff = NormalizeAngle(desiredAngle - car.Angle);

        _steeringJitter = Math.Clamp(_steeringJitter + (((float)_random.NextDouble() - 0.5f) * 0.15f), -0.12f, 0.12f);

        float steering = Math.Clamp((angleDiff / (MathF.PI / 4f)) + _steeringJitter, -1f, 1f);

        float turnSharpness = Math.Clamp(MathF.Abs(angleDiff) / (MathF.PI / 2f), 0f, 1f);
        float throttle = Math.Clamp((1f - (turnSharpness * 0.75f)) * _skill * throttleMultiplier, 0f, 1.1f);

        bool boost = car.BoostFuel > 55f && turnSharpness < 0.15f && throttle > 0.8f;

        return new CarInput(throttle, steering, boost: boost);
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > MathF.PI)
        {
            angle -= 2f * MathF.PI;
        }

        while (angle < -MathF.PI)
        {
            angle += 2f * MathF.PI;
        }

        return angle;
    }
}
