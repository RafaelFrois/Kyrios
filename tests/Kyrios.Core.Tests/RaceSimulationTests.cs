using Kyrios.Core;
using Xunit;

namespace Kyrios.Core.Tests;

public class RaceSimulationTests
{
    [Fact]
    public void CreateDefaultRace_AllAiOpponents_EventuallyFinishes()
    {
        RaceSimulation race = RaceFactory.CreateDefaultRace(aiOpponents: 3, targetLaps: 1, includeHuman: false, randomSeed: 123);

        const float dt = 0.05f;
        float elapsed = 0f;
        while (!race.IsRaceOver && elapsed < 60f)
        {
            race.Update(dt, CarInput.None);
            elapsed += dt;
        }

        Assert.True(race.IsRaceOver, "A corrida deveria terminar dentro do tempo limite do teste.");
        Assert.All(race.Entrants, e => Assert.True(e.Finished));
        Assert.All(race.Entrants, e => Assert.NotNull(e.FinishPlace));
    }

    [Fact]
    public void GetStandings_AssignsDistinctFinishPlacesInArrivalOrder()
    {
        RaceSimulation race = RaceFactory.CreateDefaultRace(aiOpponents: 4, targetLaps: 1, includeHuman: false, randomSeed: 99);

        const float dt = 0.05f;
        float elapsed = 0f;
        while (!race.IsRaceOver && elapsed < 60f)
        {
            race.Update(dt, CarInput.None);
            elapsed += dt;
        }

        var standings = race.GetStandings();
        var places = standings.Select(e => e.FinishPlace).ToList();

        Assert.Equal([1, 2, 3, 4], places);
        Assert.True(standings[0].FinishTime <= standings[^1].FinishTime);
    }

    [Fact]
    public void Update_HumanInputOnlyAppliesToHumanEntrant()
    {
        RaceSimulation race = RaceFactory.CreateDefaultRace(aiOpponents: 1, targetLaps: 1, includeHuman: true, randomSeed: 5);
        RaceEntrant human = race.Entrants.First(e => e.Kind == DriverKind.Human);
        RaceEntrant ai = race.Entrants.First(e => e.Kind == DriverKind.Ai);

        // Sem input humano (parado), mas a IA continua avançando sozinha.
        for (int i = 0; i < 40; i++)
        {
            race.Update(0.05f, CarInput.None);
        }

        Assert.Equal(0f, human.Car.Speed, precision: 3);
        Assert.True(ai.Car.Speed > 0f);
    }

    [Fact]
    public void Constructor_NoEntrants_Throws()
    {
        Track track = TrackFactory.CreateRingTrack();
        Assert.Throws<ArgumentException>(() => new RaceSimulation(track, [], targetLaps: 3));
    }
}
