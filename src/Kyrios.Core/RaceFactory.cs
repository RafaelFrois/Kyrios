namespace Kyrios.Core;

/// <summary>Monta uma <see cref="RaceSimulation"/> pronta para jogar: pista padrão, 1 humano + N adversários de IA.</summary>
public static class RaceFactory
{
    public static readonly string[] AiNames =
        ["Rex", "Nina", "Hugo", "Vale", "Zara", "Tico", "Dara", "Kai", "Duke", "Mia"];

    public static RaceSimulation CreateDefaultRace(
        int aiOpponents = 3,
        int targetLaps = 3,
        bool includeHuman = true,
        int? randomSeed = null,
        RaceMode mode = RaceMode.Sprint)
    {
        Track track = TrackFactory.CreateRingTrack();
        int totalCars = aiOpponents + (includeHuman ? 1 : 0);
        IReadOnlyList<Vector2D> startPositions = TrackFactory.GridStartPositions(track, totalCars);

        var entrants = new List<RaceEntrant>(totalCars);
        int carIndex = 0;
        var random = randomSeed is null ? new Random() : new Random(randomSeed.Value);

        if (includeHuman)
        {
            Vector2D pos = startPositions[carIndex++];
            var playerCar = new Car("Você", pos, track.StartFacingAngle);
            entrants.Add(new RaceEntrant(playerCar, DriverKind.Human));
        }

        for (int i = 0; i < aiOpponents; i++)
        {
            Vector2D pos = startPositions[carIndex++];
            string name = AiNames[i % AiNames.Length];
            var settings = new CarPhysicsSettings
            {
                MaxForwardSpeed = CarPhysicsSettings.Default.MaxForwardSpeed * (0.9f + (0.12f * (float)random.NextDouble())),
            };
            var aiCar = new Car(name, pos, track.StartFacingAngle, settings);
            int? driverSeed = randomSeed is null ? null : random.Next();
            var driver = new AIDriver(track, skill: 0.82f + (0.18f * (float)random.NextDouble()), randomSeed: driverSeed);
            entrants.Add(new RaceEntrant(aiCar, DriverKind.Ai, driver));
        }

        IReadOnlyList<Hazard>? hazards = mode == RaceMode.TimeAttack ? TrackFactory.CreateTimeAttackHazards() : null;
        return new RaceSimulation(track, entrants, targetLaps, mode, hazards);
    }
}
