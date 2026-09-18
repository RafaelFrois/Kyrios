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
        RaceSimulation race = RaceFactory.CreateDefaultRace(
            aiOpponents: 5, includeHuman: false, randomSeed: 5, mode: RaceMode.Elimination);

        RunToCompletion(race);

        Assert.True(race.IsRaceOver);
        Assert.Single(race.Entrants, e => !e.Eliminated);
        Assert.Equal(4, race.Entrants.Count(e => e.Eliminated));
    }

    [Fact]
    public void Elimination_SurvivorIsMarkedFinishedWithPlaceOne()
    {
        RaceSimulation race = RaceFactory.CreateDefaultRace(
            aiOpponents: 4, includeHuman: false, randomSeed: 8, mode: RaceMode.Elimination);

        RunToCompletion(race);

        RaceEntrant survivor = race.Entrants.Single(e => !e.Eliminated);
        Assert.True(survivor.Finished);
        Assert.Equal(1, survivor.FinishPlace);
    }

    [Fact]
    public void Elimination_EliminatedEntrantsGetDecreasingDistinctPlaces()
    {
        RaceSimulation race = RaceFactory.CreateDefaultRace(
            aiOpponents: 5, includeHuman: false, randomSeed: 13, mode: RaceMode.Elimination);

        RunToCompletion(race);

        var places = race.Entrants.Select(e => e.FinishPlace).OrderBy(p => p).ToList();
        Assert.Equal([1, 2, 3, 4, 5], places);
    }

    [Fact]
    public void Elimination_EliminatedCarStopsBeingUpdated()
    {
        RaceSimulation race = RaceFactory.CreateDefaultRace(
            aiOpponents: 3, includeHuman: false, randomSeed: 21, mode: RaceMode.Elimination);

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
    public void Sprint_DoesNotEliminateAnyone()
    {
        RaceSimulation race = RaceFactory.CreateDefaultRace(
            aiOpponents: 3, targetLaps: 1, includeHuman: false, randomSeed: 3, mode: RaceMode.Sprint);

        RunToCompletion(race);

        Assert.All(race.Entrants, e => Assert.False(e.Eliminated));
    }
}
