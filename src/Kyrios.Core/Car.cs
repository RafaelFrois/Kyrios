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

    /// <summary>true se cruzou um checkpoint válido neste passo de simulação (reseta a cada <see cref="Update"/>).</summary>
    public bool CheckpointCrossedThisTick { get; private set; }

    /// <summary>true se bateu de frente numa parede (ricocheteou) neste passo — usado por modos que penalizam batidas.</summary>
    public bool HadHeadOnCollisionThisTick { get; private set; }

    /// <summary>true se colidiu com outro carro neste passo (marcado externamente pela <see cref="RaceSimulation"/>).</summary>
    public bool HadCarCollisionThisTick { get; private set; }

    /// <summary>Próximo checkpoint (1-based) que o carro precisa cruzar antes da linha de chegada.</summary>
    public int NextCheckpointIndex { get; private set; } = 1;

    public int LapsCompleted { get; private set; }
    public float CurrentLapTime { get; private set; }
    public float TotalRaceTime { get; private set; }
    public float? BestLapTime { get; private set; }
    public List<float> LapTimes { get; } = [];

    private float _stuckCheckTimer;
    private Vector2D _stuckCheckPosition;

    public Car(string name, Vector2D startPosition, float startAngle, CarPhysicsSettings? settings = null)
    {
        Name = name;
        Position = startPosition;
        Angle = startAngle;
        Settings = settings ?? CarPhysicsSettings.Default;
        BoostFuel = Settings.BoostStartingFuel;
        _stuckCheckPosition = startPosition;
    }

    public bool HasClearedAllCheckpoints(Track track) => NextCheckpointIndex > track.CheckpointCount;

    /// <summary>Avança a simulação em <paramref name="dt"/> segundos aplicando o input informado.</summary>
    /// <returns>true se o carro cruzou a linha de chegada completando uma volta neste passo.</returns>
    public bool Update(float dt, CarInput input, Track track)
    {
        CheckpointCrossedThisTick = false;
        HadHeadOnCollisionThisTick = false;
        HadCarCollisionThisTick = false;

        int startCellX = (int)MathF.Floor(Position.X);
        int startCellY = (int)MathF.Floor(Position.Y);
        bool onTrack = !track.IsOffTrack(startCellX, startCellY);

        ApplyThrottleAndFriction(dt, input, onTrack);
        ApplySteering(dt, input);
        MoveAndResolveCollisions(dt, track);

        CurrentLapTime += dt;
        TotalRaceTime += dt;

        bool completedLap = ProcessCheckpoints(track);
        RecoverIfStuck(dt, track);

        return completedLap;
    }

    /// <summary>
    /// Rede de segurança: se o carro está claramente tentando andar (velocidade relevante) mas não sai
    /// do lugar por um tempo — encravado num canto côncavo da pista, espremido contra outro carro, etc —
    /// dá um pequeno "empurrão" pra alguma direção livre. Sem isso, um carro pode ficar preso pra sempre.
    /// </summary>
    private void RecoverIfStuck(float dt, Track track)
    {
        const float sampleInterval = 0.5f;
        const float minMovementToResetTimer = 0.15f;
        const float stuckSpeedThreshold = 0.4f;
        const float escapeStepDistance = 0.6f;

        _stuckCheckTimer += dt;
        if (_stuckCheckTimer < sampleInterval)
        {
            return;
        }

        float movedSinceLastCheck = Position.DistanceTo(_stuckCheckPosition);
        bool wantsToMove = MathF.Abs(Speed) > stuckSpeedThreshold;

        if (wantsToMove && movedSinceLastCheck < minMovementToResetTimer)
        {
            Vector2D[] escapeDirections =
            [
                Vector2D.FromAngle(Angle) * -escapeStepDistance,
                Vector2D.FromAngle(Angle + (MathF.PI / 2f)) * escapeStepDistance,
                Vector2D.FromAngle(Angle - (MathF.PI / 2f)) * escapeStepDistance,
                Vector2D.FromAngle(Angle) * escapeStepDistance,
                new Vector2D(0f, -escapeStepDistance),
                new Vector2D(0f, escapeStepDistance),
                new Vector2D(-escapeStepDistance, 0f),
                new Vector2D(escapeStepDistance, 0f),
            ];

            foreach (Vector2D offset in escapeDirections)
            {
                Vector2D candidate = Position + offset;
                if (!track.CollidesWithWall(candidate, Settings.Radius))
                {
                    Position = candidate;
                    Speed *= 0.3f;
                    break;
                }
            }
        }

        _stuckCheckPosition = Position;
        _stuckCheckTimer = 0f;
    }

    /// <summary>
    /// Usado pela <see cref="RaceSimulation"/> para separar carros que se sobrepuseram e ricochetear a
    /// velocidade igual uma superfície rígida — colisão carro-com-carro não é resolvida aqui dentro
    /// porque um carro não conhece os outros, só a pista.
    /// </summary>
    /// <param name="positionCorrection">Deslocamento pra separar os carros sobrepostos.</param>
    /// <param name="outwardNormal">Direção "pra fora", se afastando do outro carro — o eixo em que a
    /// velocidade ricocheteia, como se fosse o normal de uma parede na hora do impacto.</param>
    internal void ResolveCarCollision(Vector2D positionCorrection, Vector2D outwardNormal)
    {
        Position += positionCorrection;

        Vector2D velocity = Vector2D.FromAngle(Angle) * Speed;
        float velocityAlongNormal = Vector2D.Dot(velocity, outwardNormal);

        // Só ricocheteia se o carro estava mesmo avançando pra dentro do outro (não bate duas vezes
        // num carro que já está se afastando). O ricochete rígido soma de volta o componente de
        // velocidade que ia "pra dentro", na proporção da elasticidade configurada — isso realoca a
        // velocidade numa direção que o motorista não escolheu, exatamente a perda de controle esperada
        // de uma batida contra uma superfície dura, em vez de só frear na mesma direção de antes.
        if (velocityAlongNormal < 0f)
        {
            velocity -= outwardNormal * (velocityAlongNormal * (1f + Settings.CarCollisionRestitution));
        }

        velocity *= Settings.CarCollisionSpeedFactor;

        float newSpeed = velocity.Length();
        if (newSpeed > 0.05f)
        {
            Angle = MathF.Atan2(velocity.Y, velocity.X);
            Speed = newSpeed;
        }
        else
        {
            Speed = 0f;
        }

        HadCarCollisionThisTick = true;
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
        bool collided = false;

        Vector2D stepX = new(target.X, Position.Y);
        if (!track.CollidesWithWall(stepX, Settings.Radius))
        {
            candidate = stepX;
        }
        else
        {
            collided = true;
        }

        Vector2D stepY = new(candidate.X, target.Y);
        if (!track.CollidesWithWall(stepY, Settings.Radius))
        {
            candidate = stepY;
        }
        else
        {
            collided = true;
        }

        if (collided)
        {
            // Compara o quanto o carro conseguiu avançar de fato com o que pretendia: se a parede
            // barrou quase todo o avanço (bateu de frente, ou ficou preso num canto), ricocheteia
            // com força; se só cortou uma fatia pequena (raspou de lado contornando a curva), a perda
            // de velocidade é bem mais leve. Isso funciona tanto pra batidas retas quanto de canto,
            // ao contrário de simplesmente checar "os dois eixos bateram" (o que nunca acontece numa
            // batida reta sem nenhum componente lateral de movimento).
            float intendedDistance = (target - Position).Length();
            float actualDistance = (candidate - Position).Length();
            float progressFraction = intendedDistance > 0.0001f ? actualDistance / intendedDistance : 0f;

            if (progressFraction < 0.4f)
            {
                Speed *= -Settings.WallBounceSpeedFactor;
                HadHeadOnCollisionThisTick = true;
            }
            else
            {
                Speed *= Settings.WallScrapeSpeedFactor;
            }
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
            CheckpointCrossedThisTick = true;
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
