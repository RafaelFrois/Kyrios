namespace Kyrios.Core;

/// <summary>Constantes de física ajustáveis para um carro. Unidades: células de pista por segundo.</summary>
public sealed class CarPhysicsSettings
{
    public float Acceleration { get; init; } = 9.0f;
    public float BrakeDeceleration { get; init; } = 16.0f;
    public float CoastFriction { get; init; } = 5.0f;
    public float MaxForwardSpeed { get; init; } = 13.0f;
    public float MaxReverseSpeed { get; init; } = 4.5f;
    public float MaxTurnRateRadiansPerSecond { get; init; } = 3.4f;
    public float TurnGripReferenceSpeed { get; init; } = 3.0f;
    public float MinTurnGrip { get; init; } = 0.2f;
    public float Radius { get; init; } = 0.38f;
    public float OffTrackMaxSpeedMultiplier { get; init; } = 0.45f;
    public float OffTrackExtraFriction { get; init; } = 7.0f;
    public float WallBounceSpeedFactor { get; init; } = 0.25f;
    public float WallScrapeSpeedFactor { get; init; } = 0.92f;

    public float CarCollisionSpeedFactor { get; init; } = 0.7f;

    public float BoostMaxFuel { get; init; } = 100f;
    public float BoostStartingFuel { get; init; } = 40f;
    public float BoostPassiveFillPerSecond { get; init; } = 4f;
    public float BoostFillPerCheckpoint { get; init; } = 25f;
    public float BoostFillPerLap { get; init; } = 40f;
    public float BoostConsumptionPerSecond { get; init; } = 45f;
    public float BoostSpeedMultiplier { get; init; } = 1.45f;
    public float BoostAccelerationMultiplier { get; init; } = 1.6f;

    public static CarPhysicsSettings Default { get; } = new();
}
