using Kyrios.Core;
using Xunit;

namespace Kyrios.Core.Tests;

public class CarCollisionAndBoostTests
{
    private static Track OpenTrack(int width = 4000, int height = 400) =>
        BuildTrack(width, height, '.');

    private static Track BuildTrack(int width, int height, char surface)
    {
        var cells = new char[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y] = surface;
            }
        }

        return new Track(cells, [], new Vector2D(width / 2f, height / 2f), 0f);
    }

    [Fact]
    public void Update_ScrapingOneAxisOnly_LosesLessSpeedThanHeadOnHit()
    {
        // Parede vertical em x=5, bem alta: um carro andando na diagonal só "raspa" ao encostar (colide só
        // no eixo X enquanto ainda avança livremente em Y); um carro andando reto bate de frente (nos dois eixos).
        const int height = 2000;
        var cells = new char[10, height];
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y] = x == 5 ? '#' : '.';
            }
        }

        var track = new Track(cells, [], new Vector2D(2f, height / 2f), 0f);

        Vector2D scrapingStart = new(2f, height / 2f);
        Vector2D headOnStart = new(2f, (height / 2f) + 3f);
        var scraping = new Car("Scrape", scrapingStart, MathF.PI / 3f); // anda na diagonal, roça na parede
        var headOn = new Car("HeadOn", headOnStart, 0f); // anda reto contra a parede

        for (int i = 0; i < 30; i++)
        {
            scraping.Update(0.05f, new CarInput(1f, 0f), track);
            headOn.Update(0.05f, new CarInput(1f, 0f), track);
        }

        // Nenhum dos dois deveria atravessar a parede.
        Assert.True(headOn.Position.X < 5f);
        Assert.True(scraping.Position.X < 5f);

        // Raspar de lado deixa o carro avançar quase normalmente; bater de frente repetidas vezes
        // trava o carro perto da parede, avançando muito menos no total.
        float scrapingProgress = scraping.Position.DistanceTo(scrapingStart);
        float headOnProgress = headOn.Position.DistanceTo(headOnStart);
        Assert.True(scrapingProgress > headOnProgress * 2f,
            $"Esperava que raspar avançasse bem mais que bater de frente (raspou {scrapingProgress:0.00}, bateu {headOnProgress:0.00}).");
    }

    [Fact]
    public void ResolveCarCollision_MovesCarByCorrectionAndDampensSpeed()
    {
        var car = new Car("Test", new Vector2D(5f, 5f), 0f);
        // Acelera um pouco pra ter velocidade > 0 antes da colisão.
        Track track = OpenTrack();
        for (int i = 0; i < 20; i++)
        {
            car.Update(0.05f, new CarInput(1f, 0f), track);
        }

        float speedBefore = car.Speed;
        Vector2D positionBefore = car.Position;
        var correction = new Vector2D(0.2f, -0.1f);

        car.ResolveCarCollision(correction, 0.5f);

        Assert.Equal(positionBefore.X + 0.2f, car.Position.X, precision: 3);
        Assert.Equal(positionBefore.Y - 0.1f, car.Position.Y, precision: 3);
        Assert.Equal(speedBefore * 0.5f, car.Speed, precision: 3);
    }

    [Fact]
    public void RaceSimulation_TwoCarsCollidingHeadOn_SeparateInsteadOfOverlapping()
    {
        Track track = TrackFactory.CreateRingTrack();

        var carA = new Car("A", track.StartPosition, track.StartFacingAngle);
        var carB = new Car("B", track.StartPosition, track.StartFacingAngle); // exatamente sobrepostos de propósito

        var entrantA = new RaceEntrant(carA, DriverKind.Human);
        var driver = new AIDriver(track, randomSeed: 42);
        var entrantB = new RaceEntrant(carB, DriverKind.Ai, driver);

        var race = new RaceSimulation(track, [entrantA, entrantB], targetLaps: 1);

        race.Update(0.05f, CarInput.None);

        float distance = carA.Position.DistanceTo(carB.Position);
        float minDistance = carA.Settings.Radius + carB.Settings.Radius;

        Assert.True(distance >= minDistance - 0.01f, "Carros sobrepostos deveriam ser separados pela colisão.");
    }

    [Fact]
    public void Update_FullThrottleWithBoost_ReachesHigherSpeedThanWithout()
    {
        Track track = OpenTrack();
        var boosted = new Car("Boost", new Vector2D(2000f, 200f), 0f);
        var normal = new Car("Normal", new Vector2D(2000f, 200f), 0f);

        for (int i = 0; i < 60; i++)
        {
            boosted.Update(0.05f, new CarInput(1f, 0f, boost: true), track);
            normal.Update(0.05f, new CarInput(1f, 0f), track);
        }

        Assert.True(boosted.Speed > normal.Speed);
        Assert.True(boosted.IsBoosting);
        Assert.False(normal.IsBoosting);
    }

    [Fact]
    public void Update_BoostConsumesFuelAndRefillsWhenNotBoosting()
    {
        Track track = OpenTrack();
        var car = new Car("Test", new Vector2D(2000f, 200f), 0f);
        float startingFuel = car.BoostFuel;

        for (int i = 0; i < 20; i++)
        {
            car.Update(0.05f, new CarInput(1f, 0f, boost: true), track);
        }

        Assert.True(car.BoostFuel < startingFuel);

        float depletedFuel = car.BoostFuel;
        for (int i = 0; i < 20; i++)
        {
            car.Update(0.05f, new CarInput(1f, 0f), track);
        }

        Assert.True(car.BoostFuel > depletedFuel);
    }

    [Fact]
    public void Update_CannotBoostWithoutFuel()
    {
        var settings = new CarPhysicsSettings { BoostStartingFuel = 0f, BoostPassiveFillPerSecond = 0f };
        Track track = OpenTrack();
        var car = new Car("Test", new Vector2D(2000f, 200f), 0f, settings);

        car.Update(0.05f, new CarInput(1f, 0f, boost: true), track);

        Assert.False(car.IsBoosting);
    }
}
