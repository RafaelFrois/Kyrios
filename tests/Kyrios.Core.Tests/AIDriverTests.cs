using Kyrios.Core;
using Xunit;

namespace Kyrios.Core.Tests;

public class AIDriverTests
{
    private static Track MinimalTrackWithWaypoint(Vector2D waypoint)
    {
        var cells = new char[4, 4];
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                cells[x, y] = '.';
            }
        }

        return new Track(cells, [waypoint], new Vector2D(0f, 0f), 0f);
    }

    [Fact]
    public void GetInput_TargetDirectlyAhead_SteersStraight()
    {
        Track track = MinimalTrackWithWaypoint(new Vector2D(10f, 0f));
        var driver = new AIDriver(track, randomSeed: 1);
        var car = new Car("AI", new Vector2D(0f, 0f), 0f);

        CarInput input = driver.GetInput(car);

        // Há uma leve variação orgânica (jitter) na direção, então não é exatamente zero.
        Assert.InRange(input.Steering, -0.15f, 0.15f);
        Assert.True(input.Throttle > 0.5f);
    }

    [Fact]
    public void GetInput_TargetToPositiveY_SteersRight()
    {
        Track track = MinimalTrackWithWaypoint(new Vector2D(0f, 10f));
        var driver = new AIDriver(track);
        var car = new Car("AI", new Vector2D(0f, 0f), 0f);

        CarInput input = driver.GetInput(car);

        Assert.True(input.Steering > 0f);
    }

    [Fact]
    public void GetInput_TargetToNegativeY_SteersLeft()
    {
        Track track = MinimalTrackWithWaypoint(new Vector2D(0f, -10f));
        var driver = new AIDriver(track);
        var car = new Car("AI", new Vector2D(0f, 0f), 0f);

        CarInput input = driver.GetInput(car);

        Assert.True(input.Steering < 0f);
    }

    [Fact]
    public void GetInput_SharpTurnAhead_ReducesThrottle()
    {
        Track track = MinimalTrackWithWaypoint(new Vector2D(0f, 10f)); // 90 graus à direita
        var driver = new AIDriver(track);
        var car = new Car("AI", new Vector2D(0f, 0f), 0f);

        CarInput input = driver.GetInput(car);

        Assert.True(input.Throttle < 1f);
    }

    [Fact]
    public void Constructor_EmptyCenterLine_Throws()
    {
        var cells = new char[2, 2];
        var track = new Track(cells, [], new Vector2D(0f, 0f), 0f);

        Assert.Throws<ArgumentException>(() => new AIDriver(track));
    }
}
