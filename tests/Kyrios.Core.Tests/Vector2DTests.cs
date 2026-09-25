using Kyrios.Core;
using Xunit;

namespace Kyrios.Core.Tests;

public class Vector2DTests
{
    [Fact]
    public void Addition_SumsComponents()
    {
        var a = new Vector2D(1f, 2f);
        var b = new Vector2D(3f, 4f);

        Vector2D result = a + b;

        Assert.Equal(4f, result.X, precision: 5);
        Assert.Equal(6f, result.Y, precision: 5);
    }

    [Fact]
    public void Subtraction_SubtractsComponents()
    {
        var a = new Vector2D(5f, 5f);
        var b = new Vector2D(2f, 1f);

        Vector2D result = a - b;

        Assert.Equal(3f, result.X, precision: 5);
        Assert.Equal(4f, result.Y, precision: 5);
    }

    [Fact]
    public void DistanceTo_ComputesEuclideanDistance()
    {
        var a = new Vector2D(0f, 0f);
        var b = new Vector2D(3f, 4f);

        Assert.Equal(5f, a.DistanceTo(b), precision: 4);
    }

    [Fact]
    public void FromAngle_Zero_PointsAlongPositiveX()
    {
        Vector2D direction = Vector2D.FromAngle(0f);

        Assert.Equal(1f, direction.X, precision: 4);
        Assert.Equal(0f, direction.Y, precision: 4);
    }
}
