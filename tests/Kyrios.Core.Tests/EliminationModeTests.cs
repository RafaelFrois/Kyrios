using Kyrios.Core;
using Xunit;

namespace Kyrios.Core.Tests;

public class EliminationModeTests
{
    private static RaceSimulation RunToCompletion(RaceSimulation race, float maxSeconds = 90f)
    {
        const float dt = 0.05f;
        float elapsed = 0f;
        while (!race.IsRaceOver && elapsed < maxSeconds)
        {
            race.Update(dt, CarInput.None);
            elapsed += dt;
        }

        return race;
    }

    [Fact]
    public void Elimination_EndsWithExactlyOneSurvivor()
    {
        RaceSimulation race = RaceFactory.CreateDefaultRace(RaceMode.Elimination, aiOpponents: 5, includeHuman: false, randomSeed: 5);

        RunToCompletion(race);

        Assert.True(race.IsRaceOver);
        Assert.Single(race.Entrants, e => !e.Eliminated);
        Assert.Equal(4, race.Entrants.Count(e => e.Eliminated));
    }

    [Fact]
    public void Elimination_SurvivorIsMarkedFinishedWithPlaceOne()
    {
        RaceSimulation race = RaceFactory.CreateDefaultRace(RaceMode.Elimination, aiOpponents: 4, includeHuman: false, randomSeed: 8);

        RunToCompletion(race);

        RaceEntrant survivor = race.Entrants.Single(e => !e.Eliminated);
        Assert.True(survivor.Finished);
        Assert.Equal(1, survivor.FinishPlace);
    }

    [Fact]
    public void Elimination_EliminatedEntrantsGetDecreasingDistinctPlaces()
    {
        RaceSimulation race = RaceFactory.CreateDefaultRace(RaceMode.Elimination, aiOpponents: 5, includeHuman: false, randomSeed: 13);

        RunToCompletion(race);

        var places = race.Entrants.Select(e => e.FinishPlace).OrderBy(p => p).ToList();
        Assert.Equal([1, 2, 3, 4, 5], places);
    }

    [Fact]
    public void Elimination_EliminatedCarStopsBeingUpdated()
    {
        RaceSimulation race = RaceFactory.CreateDefaultRace(RaceMode.Elimination, aiOpponents: 3, includeHuman: false, randomSeed: 21);

        const float dt = 0.05f;
        float elapsed = 0f;
        RaceEntrant? eliminated = null;
        Vector2D? positionAtElimination = null;

        while (!race.IsRaceOver && elapsed < 90f)
        {
            race.Update(dt, CarInput.None);
            elapsed += dt;

            eliminated ??= race.Entrants.FirstOrDefault(e => e.Eliminated);
            if (eliminated is not null && positionAtElimination is null)
            {
                positionAtElimination = eliminated.Car.Position;
            }
        }

        Assert.NotNull(eliminated);
        Assert.NotNull(positionAtElimination);
        Assert.Equal(positionAtElimination.Value.X, eliminated!.Car.Position.X, precision: 3);
        Assert.Equal(positionAtElimination.Value.Y, eliminated.Car.Position.Y, precision: 3);
    }

    [Fact]
    public void Elimination_ReportsWhoWasEliminatedOnThatTick()
    {
        RaceSimulation race = RaceFactory.CreateDefaultRace(RaceMode.Elimination, aiOpponents: 4, includeHuman: false, randomSeed: 17);

        var reported = new List<RaceEntrant>();
        const float dt = 0.05f;
        float elapsed = 0f;
        while (!race.IsRaceOver && elapsed < 90f)
        {
            race.Update(dt, CarInput.None);
            elapsed += dt;
            if (race.EliminatedThisTick is { } eliminated)
            {
                reported.Add(eliminated);
            }
        }

        Assert.Equal(3, reported.Count);
        Assert.Equal(race.Entrants.Where(e => e.Eliminated).OrderByDescending(e => e.FinishPlace), reported);
    }

    [Fact]
    public void Elimination_TwoLapsOnTheSameTick_EliminateTwoCars()
    {
        // Corredor reto: checkpoint na coluna 20, chegada na 30. Os dois da frente (lado a lado, mesmo carro,
        // mesmo input) fecham a volta no mesmo tick; os dois de trás são os últimos.
        const int width = 40;
        const int height = 7;
        var cells = new char[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bool wall = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                cells[x, y] = wall ? '#' : x == 20 ? '1' : x == 30 ? 'S' : '.';
            }
        }

        var track = new Track(cells, [new Vector2D(2f, 3.5f), new Vector2D(38f, 3.5f)], new Vector2D(18f, 2.5f), 0f);
        RaceEntrant Human(float x, float y) => new(new Car("c", new Vector2D(x, y), 0f), DriverKind.Human);
        List<RaceEntrant> entrants = [Human(18f, 2.5f), Human(18f, 4.5f), Human(4f, 2.5f), Human(4f, 4.5f)];
        var race = new RaceSimulation(track, entrants, RaceMode.Elimination);

        var input = new CarInput(1f, 0f);
        for (int i = 0; i < 400 && race.EliminationsThisTick.Count == 0; i++)
        {
            race.Update(0.02f, input);
        }

        Assert.Equal(2, race.EliminationsThisTick.Count);
        Assert.True(entrants[2].Eliminated);
        Assert.True(entrants[3].Eliminated);
        Assert.False(entrants[0].Eliminated);
        Assert.False(entrants[1].Eliminated);
        Assert.Equal([4, 3], race.EliminationsThisTick.Select(e => e.FinishPlace!.Value));
        Assert.Same(race.EliminationsThisTick[0], race.EliminatedThisTick);
    }

    [Fact]
    public void TimeAttack_DoesNotEliminateAnyone()
    {
        RaceSimulation race = RaceFactory.CreateDefaultRace(RaceMode.TimeAttack, aiOpponents: 3, includeHuman: false, randomSeed: 3);

        RunToCompletion(race);

        Assert.All(race.Entrants, e => Assert.False(e.Eliminated));
    }
}
