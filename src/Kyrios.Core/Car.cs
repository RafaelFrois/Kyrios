namespace Kyrios.Core;

/// <summary>Um carro na pista: física de movimento + progresso de volta (checkpoints/tempos) + turbo.</summary>
public sealed class Car
{
    public string Name { get; }
    public CarPhysicsSettings Settings { get; }

    public Vector2D Position { get; private set; }
    public float Angle { get; private set; }
    public float Speed { get; private set; }

    /// <summary>Combustível de turbo disponível, de 0 até <see cref="CarPhysicsSettings.BoostMaxFuel"/>.</summary>
    public float BoostFuel { get; private set; }

    /// <summary>true se o turbo está ativo neste instante (consumindo combustível).</summary>
    public bool IsBoosting { get; private set; }

    /// <summary>Próximo checkpoint (1-based) que o carro precisa cruzar antes da linha de chegada.</summary>
    public int NextCheckpointIndex { get; private set; } = 1;

    public int LapsCompleted { get; private set; }
    public float CurrentLapTime { get; private set; }
    public float TotalRaceTime { get; private set; }
    public float? BestLapTime { get; private set; }
    public List<float> LapTimes { get; } = [];

    public Car(string name, Vector2D startPosition, float startAngle, CarPhysicsSettings? settings = null)
    {
        Name = name;
        Position = startPosition;
        Angle = startAngle;
        Settings = settings ?? CarPhysicsSettings.Default;
        BoostFuel = Settings.BoostStartingFuel;
    }

    public bool HasClearedAllCheckpoints(Track track) => NextCheckpointIndex > track.CheckpointCount;

    /// <summary>Avança a simulação em <paramref name="dt"/> segundos aplicando o input informado.</summary>
    /// <returns>true se o carro cruzou a linha de chegada completando uma volta neste passo.</returns>
    public bool Update(float dt, CarInput input, Track track)
    {
        int startCellX = (int)MathF.Floor(Position.X);
        int startCellY = (int)MathF.Floor(Position.Y);
        bool onTrack = !track.IsOffTrack(startCellX, startCellY);

        ApplyThrottleAndFriction(dt, input, onTrack);
        ApplySteering(dt, input);
        MoveAndResolveCollisions(dt, track);

        CurrentLapTime += dt;
        TotalRaceTime += dt;

        return ProcessCheckpoints(track);
    }

    /// <summary>
    /// Usado pela <see cref="RaceSimulation"/> para separar carros que se sobrepuseram e aplicar uma
    /// pequena penalidade de velocidade na batida — colisão carro-com-carro não é resolvida aqui dentro
    /// porque um carro não conhece os outros, só a pista.
    /// </summary>
    internal void ResolveCarCollision(Vector2D positionCorrection, float speedMultiplier)
    {
        Position += positionCorrection;
        Speed *= speedMultiplier;
    }

    private void ApplyThrottleAndFriction(float dt, CarInput input, bool onTrack)
    {
        bool wantsBoost = input.Boost && BoostFuel > 0f && input.Throttle > 0.01f;
        IsBoosting = wantsBoost;

        float offTrackFactor = onTrack ? 1f : Settings.OffTrackMaxSpeedMultiplier;
        float extraFriction = onTrack ? 0f : Settings.OffTrackExtraFriction;
        float boostSpeedFactor = wantsBoost ? Settings.BoostSpeedMultiplier : 1f;
        float boostAccelFactor = wantsBoost ? Settings.BoostAccelerationMultiplier : 1f;

        float maxForward = Settings.MaxForwardSpeed * offTrackFactor * boostSpeedFactor;
        float maxReverse = Settings.MaxReverseSpeed * offTrackFactor;

        bool braking = input.Brake
            || (input.Throttle < -0.01f && Speed > 0.05f)
            || (input.Throttle > 0.01f && Speed < -0.05f);

        if (braking)
        {
            Speed = MoveToward(Speed, 0f, (Settings.BrakeDeceleration + extraFriction) * dt);
        }
        else if (MathF.Abs(input.Throttle) > 0.01f)
        {
            float target = input.Throttle > 0f ? maxForward : -maxReverse;
            Speed = MoveToward(Speed, target, Settings.Acceleration * boostAccelFactor * dt);
        }
        else
        {
            Speed = MoveToward(Speed, 0f, (Settings.CoastFriction + extraFriction) * dt);
        }

        Speed = Math.Clamp(Speed, -maxReverse, maxForward);

        BoostFuel = wantsBoost
            ? Math.Max(0f, BoostFuel - (Settings.BoostConsumptionPerSecond * dt))
            : Math.Min(Settings.BoostMaxFuel, BoostFuel + (Settings.BoostPassiveFillPerSecond * dt));
    }

    private void ApplySteering(float dt, CarInput input)
    {
        if (MathF.Abs(input.Steering) < 0.01f)
        {
            return;
        }

        float speedFraction = Math.Clamp(MathF.Abs(Speed) / Settings.TurnGripReferenceSpeed, 0f, 1f);
        float grip = Settings.MinTurnGrip + ((1f - Settings.MinTurnGrip) * speedFraction);
        Angle += input.Steering * Settings.MaxTurnRateRadiansPerSecond * grip * dt;
    }

    private void MoveAndResolveCollisions(float dt, Track track)
    {
        Vector2D forward = Vector2D.FromAngle(Angle);
        Vector2D target = Position + (forward * Speed * dt);

        Vector2D candidate = Position;
        bool collidedX = false;
        bool collidedY = false;

        Vector2D stepX = new(target.X, Position.Y);
        if (!track.CollidesWithWall(stepX, Settings.Radius))
        {
            candidate = stepX;
        }
        else
        {
            collidedX = true;
        }

        Vector2D stepY = new(candidate.X, target.Y);
        if (!track.CollidesWithWall(stepY, Settings.Radius))
        {
            candidate = stepY;
        }
        else
        {
            collidedY = true;
        }

        if (collidedX && collidedY)
        {
            // Bateu de frente (ou num canto): ricocheteia, perdendo boa parte da velocidade.
            Speed *= -Settings.WallBounceSpeedFactor;
        }
        else if (collidedX || collidedY)
        {
            // Só raspou de lado enquanto contornava a parede: perde pouca velocidade e segue andando.
            Speed *= Settings.WallScrapeSpeedFactor;
        }

        Position = candidate;
    }

    private bool ProcessCheckpoints(Track track)
    {
        int cellX = (int)MathF.Floor(Position.X);
        int cellY = (int)MathF.Floor(Position.Y);

        int checkpoint = track.CheckpointAt(cellX, cellY);
        if (checkpoint != 0 && checkpoint == NextCheckpointIndex)
        {
            NextCheckpointIndex++;
            BoostFuel = Math.Min(Settings.BoostMaxFuel, BoostFuel + Settings.BoostFillPerCheckpoint);
            return false;
        }

        if (track.CheckpointCount > 0
            && NextCheckpointIndex > track.CheckpointCount
            && track.IsFinishLine(cellX, cellY))
        {
            CompleteLap();
            return true;
        }

        return false;
    }

    private void CompleteLap()
    {
        LapsCompleted++;
        LapTimes.Add(CurrentLapTime);
        if (BestLapTime is null || CurrentLapTime < BestLapTime)
        {
            BestLapTime = CurrentLapTime;
        }

        CurrentLapTime = 0f;
        NextCheckpointIndex = 1;
        BoostFuel = Math.Min(Settings.BoostMaxFuel, BoostFuel + Settings.BoostFillPerLap);
    }

    private static float MoveToward(float current, float target, float maxDelta)
    {
        if (MathF.Abs(target - current) <= maxDelta)
        {
            return target;
        }

        return current + (MathF.Sign(target - current) * maxDelta);
    }
}
