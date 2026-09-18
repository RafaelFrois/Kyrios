using Kyrios.Core;
using Xunit;

namespace Kyrios.Core.Tests;

public class TimeAttackModeTests
{
    private static Track BuildStraightTrack(char[] markers, int height = 5)
    {
        int width = markers.Length;
        var cells = new char[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y] = markers[x];
            }
        }

        return new Track(cells, [], new Vector2D(1f, height / 2f), 0f);
    }

    private static RaceSimulation CreateSoloRace(Track track)
    {
        var car = new Car("Você", new Vector2D(1f, track.Height / 2f), 0f);
        var entrant = new RaceEntrant(car, DriverKind.Human);
        return new RaceSimulation(track, [entrant], targetLaps: 3, mode: RaceMode.TimeAttack);
    }

    [Fact]
    public void TimeAttack_StartsWithFullClock()
    {
        var markers = new char[40];
        Array.Fill(markers, '.');
        RaceSimulation race = CreateSoloRace(BuildStraightTrack(markers));

        Assert.Equal(RaceSimulation.TimeAttackStartSeconds, race.TimeRemaining);
        Assert.False(race.IsRaceOver);
    }

    [Fact]
    public void TimeAttack_ClockCountsDownAndEndsRaceAtZero()
    {
        var markers = new char[4000];
        Array.Fill(markers, '.');
        RaceSimulation race = CreateSoloRace(BuildStraightTrack(markers));

        const float dt = 0.05f;
        float elapsed = 0f;
        while (!race.IsRaceOver && elapsed < 60f)
        {
            race.Update(dt, CarInput.None); // parado, sem checkpoints: o relógio só desce.
            elapsed += dt;
        }

        Assert.True(race.IsRaceOver);
        Assert.Equal(0f, race.TimeRemaining);
        Assert.InRange(elapsed, RaceSimulation.TimeAttackStartSeconds - 0.2f, RaceSimulation.TimeAttackStartSeconds + 0.2f);
    }

    [Fact]
    public void TimeAttack_RaceOver_MarksScoredEntrantFinishedInFirstPlace()
    {
        var markers = new char[40];
        Array.Fill(markers, '.');
        RaceSimulation race = CreateSoloRace(BuildStraightTrack(markers));

        const float dt = 0.05f;
        for (int i = 0; i < 1000 && !race.IsRaceOver; i++)
        {
            race.Update(dt, CarInput.None);
        }

        Assert.True(race.IsRaceOver);
        Assert.True(race.ScoredEntrant.Finished);
        Assert.Equal(1, race.ScoredEntrant.FinishPlace);
    }

    [Fact]
    public void TimeAttack_CrossingCheckpoint_AddsScoreAndTime()
    {
        var markers = new char[40];
        Array.Fill(markers, '.');
        markers[10] = '1';
        RaceSimulation race = CreateSoloRace(BuildStraightTrack(markers));

        float timeBeforeCrossing = race.TimeRemaining!.Value;

        const float dt = 0.05f;
        for (int i = 0; i < 400 && race.ScoredEntrant.Score == 0f; i++)
        {
            race.Update(dt, new CarInput(1f, 0f));
        }

        Assert.Equal(10f, race.ScoredEntrant.Score);
        // Mesmo com o relógio descontando dt a cada passo, o bônus de checkpoint deveria deixar
        // o tempo maior do que estava antes de cruzar (ganhou mais do que perdeu no caminho).
        Assert.True(race.TimeRemaining > timeBeforeCrossing);
    }

    [Fact]
    public void TimeAttack_CompletingLap_AddsBiggerScoreBonus()
    {
        var markers = new char[40];
        Array.Fill(markers, '.');
        markers[10] = '1';
        markers[25] = 'S';
        RaceSimulation race = CreateSoloRace(BuildStraightTrack(markers));

        const float dt = 0.05f;
        for (int i = 0; i < 400 && race.ScoredEntrant.Car.LapsCompleted == 0; i++)
        {
            race.Update(dt, new CarInput(1f, 0f));
        }

        Assert.Equal(1, race.ScoredEntrant.Car.LapsCompleted);
        Assert.Equal(110f, race.ScoredEntrant.Score); // 10 do checkpoint + 100 da volta
    }

    [Fact]
    public void TimeAttack_HeadOnWallCrash_CostsExtraTime()
    {
        int height = 5;
        // A parede fica longe o bastante pra só ser alcançada depois do período de carência inicial
        // do Contrarrelógio (que não cobra penalidade de batida logo na largada).
        const int wallX = 45;
        var cells = new char[wallX + 5, height];
        for (int x = 0; x < wallX + 5; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y] = x == wallX ? '#' : '.';
            }
        }

        var track = new Track(cells, [], new Vector2D(2f, 2f), 0f);
        var car = new Car("Você", new Vector2D(2f, 2f), 0f);
        var entrant = new RaceEntrant(car, DriverKind.Human);
        var race = new RaceSimulation(track, [entrant], targetLaps: 3, mode: RaceMode.TimeAttack);

        // Acelera até bater de frente na parede. Recaptura o tempo a cada volta do laço pra medir só
        // a queda causada exatamente pelo tick da batida, não o desgaste acumulado.
        float timeBeforeCrash = race.TimeRemaining!.Value;
        bool crashed = false;
        for (int i = 0; i < 150 && !crashed; i++)
        {
            timeBeforeCrash = race.TimeRemaining!.Value;
            race.Update(0.05f, new CarInput(1f, 0f));
            crashed = car.HadHeadOnCollisionThisTick;
        }

        Assert.True(crashed, "O carro deveria ter batido de frente na parede.");

        float dropOnCrashTick = timeBeforeCrash - race.TimeRemaining!.Value;
        Assert.True(dropOnCrashTick > 1f, $"Esperava uma queda de tempo bem maior que um dt normal (caiu {dropOnCrashTick:0.000}).");
    }

    [Fact]
    public void CreateDefaultRace_TimeAttack_ScoredEntrantIsTheHuman()
    {
        RaceSimulation race = RaceFactory.CreateDefaultRace(aiOpponents: 2, includeHuman: true, randomSeed: 1, mode: RaceMode.TimeAttack);

        Assert.Equal(DriverKind.Human, race.ScoredEntrant.Kind);
        Assert.NotNull(race.TimeRemaining);
    }
}
