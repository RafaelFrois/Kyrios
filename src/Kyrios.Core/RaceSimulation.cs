namespace Kyrios.Core;

/// <summary>Orquestra uma corrida: física de todos os carros, progresso de voltas e classificação.</summary>
public sealed class RaceSimulation
{
    public Track Track { get; }
    public IReadOnlyList<RaceEntrant> Entrants { get; }
    public int TargetLaps { get; }
    public bool IsRaceOver { get; private set; }
    public float ElapsedTime { get; private set; }

    private int _finishersCount;

    public RaceSimulation(Track track, IReadOnlyList<RaceEntrant> entrants, int targetLaps)
    {
        if (entrants.Count == 0)
        {
            throw new ArgumentException("A corrida precisa de pelo menos um participante.", nameof(entrants));
        }

        Track = track;
        Entrants = entrants;
        TargetLaps = targetLaps;
    }

    /// <summary>Avança a simulação em <paramref name="dt"/> segundos. <paramref name="humanInput"/> é aplicado a qualquer participante humano.</summary>
    public void Update(float dt, CarInput humanInput)
    {
        if (IsRaceOver)
        {
            return;
        }

        ElapsedTime += dt;

        foreach (RaceEntrant entrant in Entrants)
        {
            if (entrant.Finished)
            {
                continue;
            }

            CarInput input = entrant.Kind == DriverKind.Human
                ? humanInput
                : entrant.Driver!.GetInput(entrant.Car);

            bool completedLap = entrant.Car.Update(dt, input, Track);

            if (completedLap && entrant.Car.LapsCompleted >= TargetLaps)
            {
                entrant.Finished = true;
                entrant.FinishTime = entrant.Car.TotalRaceTime;
                _finishersCount++;
                entrant.FinishPlace = _finishersCount;
            }
        }

        if (_finishersCount >= Entrants.Count)
        {
            IsRaceOver = true;
        }
    }

    /// <summary>Classificação atual: quem já terminou primeiro (na ordem de chegada), depois os demais por progresso.</summary>
    public IReadOnlyList<RaceEntrant> GetStandings()
    {
        return [.. Entrants
            .OrderBy(e => e.FinishPlace ?? int.MaxValue)
            .ThenByDescending(e => e.Car.LapsCompleted)
            .ThenByDescending(e => e.Car.NextCheckpointIndex)
            .ThenBy(e => DistanceToNextTarget(e))];
    }

    private float DistanceToNextTarget(RaceEntrant entrant)
    {
        Vector2D target = entrant.Car.NextCheckpointIndex > Track.CheckpointCount
            ? Track.FinishLineCenter
            : Track.CheckpointCenters.GetValueOrDefault(entrant.Car.NextCheckpointIndex, Track.FinishLineCenter);

        return entrant.Car.Position.DistanceTo(target);
    }
}
