using Kyrios.Core;
using Xunit;

namespace Kyrios.Core.Tests;

public class CarPhysicsTests
{
    // Grande o suficiente para nenhum teste de aceleração/frenagem "bater" nas bordas
    // (tratadas como parede) antes do fim do cenário.
    private const int OpenWidth = 4000;
    private const int OpenHeight = 400;
    private static readonly Vector2D StartPos = new(OpenWidth / 2f, OpenHeight / 2f);

    private static Track OpenTrack(char surface = '.')
    {
        var cells = new char[OpenWidth, OpenHeight];
        for (int x = 0; x < OpenWidth; x++)
        {
            for (int y = 0; y < OpenHeight; y++)
            {
                cells[x, y] = surface;
            }
        }

        return new Track(cells, [], StartPos, 0f);
    }

    [Fact]
    public void Update_FullThrottle_AcceleratesTowardsMaxForwardSpeed()
    {
        Track track = OpenTrack();
        var car = new Car("Test", StartPos, 0f);

        for (int i = 0; i < 200; i++)
        {
            car.Update(0.05f, new CarInput(1f, 0f), track);
        }

        Assert.InRange(car.Speed, car.Settings.MaxForwardSpeed - 0.1f, car.Settings.MaxForwardSpeed + 0.1f);
    }

    [Fact]
    public void Update_NoInput_CoastsToStopEventually()
    {
        Track track = OpenTrack();
        var car = new Car("Test", StartPos, 0f);

        for (int i = 0; i < 40; i++)
        {
            car.Update(0.05f, new CarInput(1f, 0f), track);
        }

        float speedBeforeCoast = car.Speed;
        Assert.True(speedBeforeCoast > 0f);

        for (int i = 0; i < 200; i++)
        {
            car.Update(0.05f, CarInput.None, track);
        }

        Assert.Equal(0f, car.Speed, precision: 3);
    }

    [Fact]
    public void Update_BrakingIsFasterThanCoasting()
    {
        Track track = OpenTrack();
        var accelerating = new Car("A", StartPos, 0f);
        var braking = new Car("B", StartPos, 0f);

        for (int i = 0; i < 40; i++)
        {
            accelerating.Update(0.05f, new CarInput(1f, 0f), track);
            braking.Update(0.05f, new CarInput(1f, 0f), track);
        }

        // Um passo de frenagem explícita vs. um passo de desaceleração livre (motor solto).
        accelerating.Update(0.1f, CarInput.None, track);
        braking.Update(0.1f, new CarInput(0f, 0f, brake: true), track);

        Assert.True(braking.Speed < accelerating.Speed);
    }

    [Fact]
    public void Update_NegativeThrottleFromRest_AcceleratesInReverse()
    {
        Track track = OpenTrack();
        var car = new Car("Test", StartPos, 0f);

        for (int i = 0; i < 60; i++)
        {
            car.Update(0.05f, new CarInput(-1f, 0f), track);
        }

        Assert.True(car.Speed < 0f);
        Assert.True(car.Speed >= -car.Settings.MaxReverseSpeed - 0.01f);
    }

    [Fact]
    public void Update_OffTrack_ReachesLowerTopSpeedThanOnTrack()
    {
        Track onTrack = OpenTrack(surface: '.');
        Track offTrack = OpenTrack(surface: ',');

        var carOnTrack = new Car("On", StartPos, 0f);
        var carOffTrack = new Car("Off", StartPos, 0f);

        for (int i = 0; i < 150; i++)
        {
            carOnTrack.Update(0.05f, new CarInput(1f, 0f), onTrack);
            carOffTrack.Update(0.05f, new CarInput(1f, 0f), offTrack);
        }

        Assert.True(carOffTrack.Speed < carOnTrack.Speed);
    }

    [Fact]
    public void Update_Steering_ChangesAngleTowardsInput()
    {
        Track track = OpenTrack();
        var car = new Car("Test", StartPos, 0f);

        // Precisa de alguma velocidade para ter aderência de curva.
        for (int i = 0; i < 20; i++)
        {
            car.Update(0.05f, new CarInput(1f, 0f), track);
        }

        float angleBefore = car.Angle;
        for (int i = 0; i < 20; i++)
        {
            car.Update(0.05f, new CarInput(1f, 1f), track);
        }

        Assert.True(car.Angle > angleBefore);
    }

    [Fact]
    public void Update_DrivingIntoWall_StopsCarFromPassingThrough()
    {
        int width = 10;
        int height = 5;
        var cells = new char[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y] = x == 5 ? '#' : '.';
            }
        }

        var track = new Track(cells, [], new Vector2D(2f, 2f), 0f);
        var car = new Car("Test", new Vector2D(2f, 2f), 0f);

        for (int i = 0; i < 100; i++)
        {
            car.Update(0.05f, new CarInput(1f, 0f), track);
        }

        Assert.True(car.Position.X < 5f);
    }
}
