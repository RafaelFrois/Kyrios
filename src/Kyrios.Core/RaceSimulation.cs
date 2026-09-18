namespace Kyrios.Core;

/// <summary>Orquestra uma corrida: física de todos os carros (incluindo colisões entre eles), progresso de voltas, "rubber-banding" da IA e classificação.</summary>
public sealed class RaceSimulation
{
    private const float RubberBandStrength = 0.16f;
    private const float MinRubberBandMultiplier = 0.8f;
    private const float MaxRubberBandMultiplier = 1.15f;

    public const float TimeAttackStartSeconds = 20f;
    private const float TimeAttackStartGraceSeconds = 3f;
    private const float TimeBonusPerCheckpoint = 3f;
    private const float TimeBonusPerLap = 6f;

    /// <summary>A cada volta já completada, o bônus de tempo (checkpoint e volta) encolhe por este fator —
    /// as primeiras voltas recuperam bastante relógio, as tardias recuperam bem menos, então o jogo fica
    /// mais difícil de sustentar com o tempo em vez de dar sempre o mesmo bônus fixo pra sempre.</summary>
    private const float TimeBonusDecayPerLap = 0.8f;

    /// <summary>Piso do fator de decaimento: o bônus nunca cai abaixo desta fração do valor base.</summary>
    private const float MinTimeBonusFactor = 0.35f;

    private const float ScorePerCheckpoint = 10f;
    private const float ScorePerLap = 100f;
    private const float WallCrashTimePenalty = 2f;
    private const float CarCrashTimePenalty = 1f;
    private const float HazardCrashTimePenalty = 1.5f;

    public Track Track { get; }
    public IReadOnlyList<RaceEntrant> Entrants { get; }
    public int TargetLaps { get; }
    public RaceMode Mode { get; }
    public bool IsRaceOver { get; private set; }
    public float ElapsedTime { get; private set; }

    /// <summary>Tempo restante no modo <see cref="RaceMode.TimeAttack"/>; null nos demais modos.</summary>
    public float? TimeRemaining { get; private set; }

    /// <summary>Obstáculos móveis do modo <see cref="RaceMode.TimeAttack"/>; lista vazia nos demais modos.</summary>
    public IReadOnlyList<Hazard> Hazards { get; }

    /// <summary>O participante "principal" (o jogador, ou o primeiro carro se não houver humano) — quem pontua no modo Contrarrelógio.</summary>
    public RaceEntrant ScoredEntrant => Entrants[0];

    private int _finishersCount;
    private bool _scoredWasWallCollidingLastTick;
    private bool _scoredWasCarCollidingLastTick;
    private bool _scoredWasHazardCollidingLastTick;

    /// <param name="hazards">Obstáculos móveis a usar (tipicamente só no modo Contrarrelógio). Fica vazio se
    /// omitido — quem decide se/quais obstáculos existem é quem monta a corrida (ver <see cref="RaceFactory"/>),
    /// não a pista em si, pra não acoplar posições fixas de obstáculo a uma pista arbitrária.</param>
    public RaceSimulation(
        Track track,
        IReadOnlyList<RaceEntrant> entrants,
        int targetLaps,
        RaceMode mode = RaceMode.Sprint,
        IReadOnlyList<Hazard>? hazards = null)
    {
        if (entrants.Count == 0)
        {
            throw new ArgumentException("A corrida precisa de pelo menos um participante.", nameof(entrants));
        }

        Track = track;
        Entrants = entrants;
        TargetLaps = targetLaps;
        Mode = mode;
        Hazards = hazards ?? [];

        if (mode == RaceMode.TimeAttack)
        {
            TimeRemaining = TimeAttackStartSeconds;
        }
    }

    /// <summary>Avança a simulação em <paramref name="dt"/> segundos. <paramref name="humanInput"/> é aplicado a qualquer participante humano.</summary>
    public void Update(float dt, CarInput humanInput)
    {
        if (IsRaceOver)
        {
            return;
        }

        ElapsedTime += dt;
        bool anyLapCompletedThisTick = false;
        RaceEntrant scored = ScoredEntrant;
        bool scoredCheckpointThisTick = false;
        bool scoredLapThisTick = false;

        foreach (RaceEntrant entrant in Entrants)
        {
            if (entrant.Finished || entrant.Eliminated)
            {
                continue;
            }

            CarInput input = entrant.Kind == DriverKind.Human ? humanInput : GetAiInput(entrant);
            bool completedLap = entrant.Car.Update(dt, input, Track);

            if (ReferenceEquals(entrant, scored))
            {
                scoredCheckpointThisTick = entrant.Car.CheckpointCrossedThisTick;
                scoredLapThisTick = completedLap;
            }

            if (!completedLap)
            {
                continue;
            }

            if (Mode == RaceMode.Sprint)
            {
                if (entrant.Car.LapsCompleted >= TargetLaps)
                {
                    entrant.Finished = true;
                    entrant.FinishTime = entrant.Car.TotalRaceTime;
                    _finishersCount++;
                    entrant.FinishPlace = _finishersCount;
                }
            }
            else if (Mode == RaceMode.Elimination)
            {
                anyLapCompletedThisTick = true;
            }
        }

        ResolveCarCollisions();

        if (Hazards.Count > 0)
        {
            foreach (Hazard hazard in Hazards)
            {
                hazard.Update(dt);
            }

            ResolveHazardCollisions();
        }

        switch (Mode)
        {
            case RaceMode.Sprint:
                if (_finishersCount >= Entrants.Count)
                {
                    IsRaceOver = true;
                }

                break;

            case RaceMode.Elimination:
                if (anyLapCompletedThisTick)
                {
                    EliminateLastPlace();
                }

                int active = Entrants.Count(e => !e.Eliminated);
                if (active <= 1)
                {
                    RaceEntrant? winner = Entrants.FirstOrDefault(e => !e.Eliminated);
                    if (winner is not null && winner.FinishPlace is null)
                    {
                        winner.Finished = true;
                        winner.FinishPlace = 1;
                        winner.FinishTime = winner.Car.TotalRaceTime;
                    }

                    IsRaceOver = true;
                }

                break;

            case RaceMode.TimeAttack:
                UpdateTimeAttack(dt, scored, scoredCheckpointThisTick, scoredLapThisTick);
                break;
        }
    }

    private void UpdateTimeAttack(float dt, RaceEntrant scored, bool checkpointCrossed, bool lapCompleted)
    {
        float time = TimeRemaining ?? TimeAttackStartSeconds;

        // Quantas voltas já estavam completas antes deste checkpoint/volta — usado pra encolher o bônus
        // de tempo progressivamente. Se a volta acabou de ser completada, Car.LapsCompleted já foi
        // incrementado, então subtrai 1 pra pegar o índice da volta que acabou de fechar (0-based).
        int priorLaps = lapCompleted ? scored.Car.LapsCompleted - 1 : scored.Car.LapsCompleted;
        float bonusFactor = MathF.Max(MinTimeBonusFactor, MathF.Pow(TimeBonusDecayPerLap, priorLaps));

        if (checkpointCrossed)
        {
            scored.Score += ScorePerCheckpoint;
            time += TimeBonusPerCheckpoint * bonusFactor;
        }

        if (lapCompleted)
        {
            scored.Score += ScorePerLap;
            time += TimeBonusPerLap * bonusFactor;
        }

        // Sem penalidade de batida logo no início: a largada em grade fica naturalmente apertada
        // e o jogador não deveria perder o cronômetro inteiro antes de conseguir sair do lugar.
        // A penalidade só é cobrada no instante em que a colisão começa (borda de subida), não em
        // cada tick que os carros continuam encostados/deslizando — do contrário, ficar alguns
        // segundos raspando/empurrado contra outro carro sozinho já zeraria o cronômetro inteiro.
        bool wallCollidingNow = scored.Car.HadHeadOnCollisionThisTick;
        bool carCollidingNow = scored.Car.HadCarCollisionThisTick;
        bool hazardCollidingNow = scored.Car.HadHazardCollisionThisTick;

        if (ElapsedTime > TimeAttackStartGraceSeconds)
        {
            if (wallCollidingNow && !_scoredWasWallCollidingLastTick)
            {
                time -= WallCrashTimePenalty;
            }

            if (carCollidingNow && !_scoredWasCarCollidingLastTick)
            {
                time -= CarCrashTimePenalty;
            }

            if (hazardCollidingNow && !_scoredWasHazardCollidingLastTick)
            {
                time -= HazardCrashTimePenalty;
            }
        }

        _scoredWasWallCollidingLastTick = wallCollidingNow;
        _scoredWasCarCollidingLastTick = carCollidingNow;
        _scoredWasHazardCollidingLastTick = hazardCollidingNow;

        time -= dt;

        if (time <= 0f)
        {
            TimeRemaining = 0f;
            scored.Finished = true;
            scored.FinishPlace = 1;
            scored.FinishTime = scored.Car.TotalRaceTime;
            IsRaceOver = true;
            return;
        }

        TimeRemaining = time;
    }

    /// <summary>Classificação atual: quem ainda está correndo (por progresso), depois quem já terminou/foi eliminado (por posição).</summary>
    public IReadOnlyList<RaceEntrant> GetStandings()
    {
        return [.. Entrants
            .OrderBy(e => e.Eliminated ? 1 : 0)
            .ThenBy(e => e.FinishPlace ?? int.MaxValue)
            .ThenByDescending(RaceProgressValue)];
    }

    private CarInput GetAiInput(RaceEntrant entrant)
    {
        RaceEntrant? reference = Entrants.FirstOrDefault(e => e.Kind == DriverKind.Human && !e.Finished && !e.Eliminated);
        reference ??= Entrants
            .Where(e => !e.Finished && !e.Eliminated)
            .OrderByDescending(RaceProgressValue)
            .FirstOrDefault();

        float throttleMultiplier = 1f;
        if (reference is not null && !ReferenceEquals(reference, entrant))
        {
            float gap = RaceProgressValue(reference) - RaceProgressValue(entrant);
            throttleMultiplier = Math.Clamp(1f + (gap * RubberBandStrength), MinRubberBandMultiplier, MaxRubberBandMultiplier);
        }

        return entrant.Driver!.GetInput(entrant.Car, throttleMultiplier);
    }

    private void ResolveCarCollisions()
    {
        List<RaceEntrant> active = Entrants.Where(e => !e.Finished && !e.Eliminated).ToList();

        for (int i = 0; i < active.Count; i++)
        {
            for (int j = i + 1; j < active.Count; j++)
            {
                Car a = active[i].Car;
                Car b = active[j].Car;

                float minDistance = a.Settings.Radius + b.Settings.Radius;
                Vector2D delta = b.Position - a.Position;
                float distance = delta.Length();

                if (distance >= minDistance)
                {
                    continue;
                }

                if (distance < 0.0001f)
                {
                    delta = new Vector2D(0.05f, 0f);
                    distance = delta.Length();
                }

                float overlap = minDistance - distance;
                Vector2D normal = delta * (1f / distance);

                // Direção "pra fora" de cada carro — a que o ricochete rígido deve empurrar a velocidade.
                Vector2D outwardA = normal * -1f;
                Vector2D outwardB = normal;

                // Nunca empurra um carro pra dentro de uma parede: se um lado não tem pra onde ir,
                // o outro absorve a separação inteira; se os dois estão travados, só o ricochete de
                // velocidade acontece (sem empurrão de posição).
                Vector2D halfPushA = normal * (-overlap / 2f);
                Vector2D halfPushB = normal * (overlap / 2f);
                bool aBlockedHalf = Track.CollidesWithWall(a.Position + halfPushA, a.Settings.Radius);
                bool bBlockedHalf = Track.CollidesWithWall(b.Position + halfPushB, b.Settings.Radius);

                if (!aBlockedHalf && !bBlockedHalf)
                {
                    a.ResolveCarCollision(halfPushA, outwardA);
                    b.ResolveCarCollision(halfPushB, outwardB);
                    continue;
                }

                if (aBlockedHalf && !bBlockedHalf)
                {
                    Vector2D fullPushB = normal * overlap;
                    if (!Track.CollidesWithWall(b.Position + fullPushB, b.Settings.Radius))
                    {
                        b.ResolveCarCollision(fullPushB, outwardB);
                    }
                    else
                    {
                        b.ResolveCarCollision(Vector2D.Zero, outwardB);
                    }

                    a.ResolveCarCollision(Vector2D.Zero, outwardA);
                    continue;
                }

                if (bBlockedHalf && !aBlockedHalf)
                {
                    Vector2D fullPushA = normal * -overlap;
                    if (!Track.CollidesWithWall(a.Position + fullPushA, a.Settings.Radius))
                    {
                        a.ResolveCarCollision(fullPushA, outwardA);
                    }
                    else
                    {
                        a.ResolveCarCollision(Vector2D.Zero, outwardA);
                    }

                    b.ResolveCarCollision(Vector2D.Zero, outwardB);
                    continue;
                }

                // Os dois travados entre si e a parede: sem espaço pra separar, mas a velocidade ainda
                // ricocheteia — é assim que dá pra perder o controle mesmo encostado numa parede.
                a.ResolveCarCollision(Vector2D.Zero, outwardA);
                b.ResolveCarCollision(Vector2D.Zero, outwardB);
            }
        }
    }

    /// <summary>Colisão entre carros e obstáculos móveis do Contrarrelógio — mesmo ricochete rígido da
    /// colisão carro-com-carro, mas o obstáculo em si nunca se move em resposta (ele segue sua oscilação).</summary>
    private void ResolveHazardCollisions()
    {
        foreach (RaceEntrant entrant in Entrants)
        {
            if (entrant.Finished || entrant.Eliminated)
            {
                continue;
            }

            Car car = entrant.Car;

            foreach (Hazard hazard in Hazards)
            {
                float minDistance = car.Settings.Radius + hazard.Radius;
                Vector2D delta = car.Position - hazard.Position;
                float distance = delta.Length();

                if (distance >= minDistance)
                {
                    continue;
                }

                if (distance < 0.0001f)
                {
                    delta = new Vector2D(0.05f, 0f);
                    distance = delta.Length();
                }

                float overlap = minDistance - distance;
                Vector2D outward = delta * (1f / distance);
                Vector2D push = outward * overlap;

                if (!Track.CollidesWithWall(car.Position + push, car.Settings.Radius))
                {
                    car.ResolveHazardCollision(push, outward);
                }
                else
                {
                    car.ResolveHazardCollision(Vector2D.Zero, outward);
                }
            }
        }
    }

    private void EliminateLastPlace()
    {
        List<RaceEntrant> active = Entrants.Where(e => !e.Eliminated).ToList();
        if (active.Count <= 1)
        {
            return;
        }

        RaceEntrant last = active.OrderBy(RaceProgressValue).First();
        last.Eliminated = true;
        last.FinishPlace = active.Count;
    }

    /// <summary>Quanto maior, mais adiantado na corrida — combina voltas, checkpoints e proximidade do próximo alvo.</summary>
    private float RaceProgressValue(RaceEntrant entrant)
    {
        float checkpointFraction = Track.CheckpointCount > 0
            ? Math.Min(entrant.Car.NextCheckpointIndex, Track.CheckpointCount) / (float)(Track.CheckpointCount + 1)
            : 0f;
        float proximityBonus = 1f / (1f + DistanceToNextTarget(entrant));

        return (entrant.Car.LapsCompleted * 10f) + checkpointFraction + (proximityBonus * 0.5f);
    }

    private float DistanceToNextTarget(RaceEntrant entrant)
    {
        Vector2D target = entrant.Car.NextCheckpointIndex > Track.CheckpointCount
            ? Track.FinishLineCenter
            : Track.CheckpointCenters.GetValueOrDefault(entrant.Car.NextCheckpointIndex, Track.FinishLineCenter);

        return entrant.Car.Position.DistanceTo(target);
    }
}
