namespace Kyrios.Core;

/// <summary>
/// IA simples de "seguir waypoints": mira no próximo ponto da linha central da pista,
/// vira proporcionalmente ao ângulo até ele e reduz o acelerador em curvas fechadas.
/// </summary>
public sealed class AIDriver
{
    private readonly IReadOnlyList<Vector2D> _waypoints;
    private readonly float _waypointReachRadius;
    private readonly float _skill;

    private int _targetIndex;

    public AIDriver(Track track, int startWaypointIndex = 0, float waypointReachRadius = 1.6f, float skill = 1f)
    {
        if (track.CenterLine.Count == 0)
        {
            throw new ArgumentException("A pista precisa de uma linha central (waypoints) para ter IA.", nameof(track));
        }

        _waypoints = track.CenterLine;
        _targetIndex = ((startWaypointIndex % _waypoints.Count) + _waypoints.Count) % _waypoints.Count;
        _waypointReachRadius = waypointReachRadius;
        _skill = Math.Clamp(skill, 0.4f, 1f);
    }

    public CarInput GetInput(Car car)
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

        float steering = Math.Clamp(angleDiff / (MathF.PI / 4f), -1f, 1f);

        float turnSharpness = Math.Clamp(MathF.Abs(angleDiff) / (MathF.PI / 2f), 0f, 1f);
        float throttle = (1f - (turnSharpness * 0.75f)) * _skill;

        return new CarInput(throttle, steering);
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
