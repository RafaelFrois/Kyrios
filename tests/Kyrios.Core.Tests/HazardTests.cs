using Kyrios.Core;
using Xunit;

namespace Kyrios.Core.Tests;

public class HazardTests
{
    [Fact]
    public void Update_OscillatesAroundCenterAlongAxis()
    {
        var hazard = new Hazard(new Vector2D(10f, 5f), new Vector2D(0f, 1f), amplitude: 2f, period: 4f);

        Assert.Equal(10f, hazard.Position.X, precision: 3);
        Assert.Equal(5f, hazard.Position.Y, precision: 3);

        hazard.Update(1f); // um quarto do período -> pico da senoide (offset = amplitude)
        Assert.Equal(7f, hazard.Position.Y, precision: 2);

        hazard.Update(1f); // mais um quarto -> de volta ao centro
        Assert.Equal(5f, hazard.Position.Y, precision: 1);
    }

    [Fact]
    public void DeathRace_HasNoHazards()
    {
        RaceSimulation elimination = RaceFactory.CreateDefaultRace(RaceMode.Elimination, aiOpponents: 1, includeHuman: true);
        Assert.Empty(elimination.Hazards);
    }

    [Fact]
    public void TimeAttack_HasHazardsPlacedOnTrack()
    {
        RaceSimulation race = RaceFactory.CreateDefaultRace(RaceMode.TimeAttack, aiOpponents: 1, includeHuman: true);

        Assert.NotEmpty(race.Hazards);
        Assert.All(race.Hazards, hazard => Assert.False(race.Track.CollidesWithWall(hazard.Position, 0.05f)));
    }

    [Fact]
    public void TimeAttack_HittingHazard_CostsExtraTimeOnce()
    {
        // Deixa a IA guiar sozinha (ela segue a centerline, incluindo as curvas) por várias voltas —
        // os obstáculos ficam bem em cima da centerline em cada reta, então dá bastante chance de bater
        // em algum sem precisar cronometrar manualmente um único encontro.
        RaceSimulation race = RaceFactory.CreateDefaultRace(RaceMode.TimeAttack, aiOpponents: 1, includeHuman: false, randomSeed: 3);
        RaceEntrant scored = race.ScoredEntrant;

        const float dt = 0.05f;
        bool hit = false;
        float timeBeforeHit = race.TimeRemaining!.Value;

        for (int i = 0; i < 3000 && !hit && !race.IsRaceOver; i++)
        {
            timeBeforeHit = race.TimeRemaining!.Value;
            race.Update(dt, CarInput.None);
            hit = scored.Car.HadHazardCollisionThisTick && race.ElapsedTime > 3f;
        }

        Assert.True(hit, "O carro deveria ter batido em algum obstáculo ao longo de várias voltas.");

        float dropOnHitTick = timeBeforeHit - race.TimeRemaining!.Value;
        Assert.True(dropOnHitTick > 1f, $"Esperava uma queda de tempo bem maior que um dt normal (caiu {dropOnHitTick:0.000}).");
    }
}
