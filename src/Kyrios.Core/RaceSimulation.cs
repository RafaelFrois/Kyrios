namespace Kyrios.Core;

/// <summary>Orquestra uma corrida: física de todos os carros (incluindo colisões entre eles), progresso de voltas, "rubber-banding" da IA e classificação.</summary>
public sealed class RaceSimulation
{
    private const float RubberBandStrength = 0.16f;
    private const float MinRubberBandMultiplier = 0.8f;
    private const float MaxRubberBandMultiplier = 1.15f;

    public Track Track { get; }
    public IReadOnlyList<RaceEntrant> Entrants { get; }
    public int TargetLaps { get; }
    public RaceMode Mode { get; }
    public bool IsRaceOver { get; private set; }
    public float ElapsedTime { get; private set; }

    private int _finishersCount;

    public RaceSimulation(Track track, IReadOnlyList<RaceEntrant> entrants, int targetLaps, RaceMode mode = RaceMode.Sprint)
    {
        if (entrants.Count == 0)
        {
            throw new ArgumentException("A corrida precisa de pelo menos um participante.", nameof(entrants));
        }

        Track = track;
        Entrants = entrants;
        TargetLaps = targetLaps;
        Mode = mode;
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

        foreach (RaceEntrant entrant in Entrants)
        {
            if (entrant.Finished || entrant.Eliminated)
            {
                continue;
            }

            CarInput input = entrant.Kind == DriverKind.Human ? humanInput : GetAiInput(entrant);
            bool completedLap = entrant.Car.Update(dt, input, Track);

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
            else
            {
                anyLapCompletedThisTick = true;
            }
        }

        ResolveCarCollisions();

        if (Mode == RaceMode.Sprint)
        {
            if (_finishersCount >= Entrants.Count)
            {
                IsRaceOver = true;
            }
        }
        else
        {
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
        }
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

                // Nunca empurra um carro pra dentro de uma parede: se um lado não tem pra onde ir,
                // o outro absorve a separação inteira; se os dois estão travados, só freia sem mover.
                Vector2D halfPushA = normal * (-overlap / 2f);
                Vector2D halfPushB = normal * (overlap / 2f);
                bool aBlockedHalf = Track.CollidesWithWall(a.Position + halfPushA, a.Settings.Radius);
                bool bBlockedHalf = Track.CollidesWithWall(b.Position + halfPushB, b.Settings.Radius);

                if (!aBlockedHalf && !bBlockedHalf)
                {
                    a.ResolveCarCollision(halfPushA, a.Settings.CarCollisionSpeedFactor);
                    b.ResolveCarCollision(halfPushB, b.Settings.CarCollisionSpeedFactor);
                    continue;
                }

                if (aBlockedHalf && !bBlockedHalf)
                {
                    Vector2D fullPushB = normal * overlap;
                    if (!Track.CollidesWithWall(b.Position + fullPushB, b.Settings.Radius))
                    {
                        b.ResolveCarCollision(fullPushB, b.Settings.CarCollisionSpeedFactor);
                    }

                    a.ResolveCarCollision(Vector2D.Zero, a.Settings.CarCollisionSpeedFactor);
                    continue;
                }

                if (bBlockedHalf && !aBlockedHalf)
                {
                    Vector2D fullPushA = normal * -overlap;
                    if (!Track.CollidesWithWall(a.Position + fullPushA, a.Settings.Radius))
                    {
                        a.ResolveCarCollision(fullPushA, a.Settings.CarCollisionSpeedFactor);
                    }

                    b.ResolveCarCollision(Vector2D.Zero, b.Settings.CarCollisionSpeedFactor);
                    continue;
                }

                // Os dois travados entre si e a parede: sem espaço pra separar, só perde velocidade.
                a.ResolveCarCollision(Vector2D.Zero, a.Settings.CarCollisionSpeedFactor);
                b.ResolveCarCollision(Vector2D.Zero, b.Settings.CarCollisionSpeedFactor);
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
