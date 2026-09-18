using Kyrios.Core;
using Xunit;

namespace Kyrios.Core.Tests;

public class TrackTests
{
    [Fact]
    public void CreateRingTrack_HasThreeCheckpoints()
    {
        Track track = TrackFactory.CreateRingTrack();

        Assert.Equal(3, track.CheckpointCount);
    }

    [Fact]
    public void CreateRingTrack_InnerBlockIsWalled_SoRingIsHollow()
    {
        Track track = TrackFactory.CreateRingTrack();

        // Centro do "miolo" do anel deve ser parede (o carro não pode cortar caminho pelo meio).
        Assert.True(track.IsWall(27, 10));
    }

    [Fact]
    public void CreateRingTrack_StartPosition_IsOnTrackSurface()
    {
        Track track = TrackFactory.CreateRingTrack();

        char cell = track.CellAt(track.StartPosition);
        Assert.False(track.IsWall((int)track.StartPosition.X, (int)track.StartPosition.Y));
        Assert.NotEqual('#', cell);
    }

    [Fact]
    public void CellAt_OutOfBounds_IsTreatedAsWall()
    {
        Track track = TrackFactory.CreateRingTrack();

        Assert.True(track.IsWall(-1, 5));
        Assert.True(track.IsWall(track.Width + 5, 5));
        Assert.True(track.CollidesWithWall(new Vector2D(-1f, 5f), 0.3f));
    }

    [Fact]
    public void CollidesWithWall_CircleOverlappingWallCell_ReturnsTrue()
    {
        Track track = TrackFactory.CreateRingTrack();

        // (27, 10) é parede (miolo do anel); um círculo bem próximo deve colidir.
        bool collides = track.CollidesWithWall(new Vector2D(27.1f, 9.9f), 0.4f);

        Assert.True(collides);
    }

    [Fact]
    public void CollidesWithWall_CircleFarFromWalls_ReturnsFalse()
    {
        Track track = TrackFactory.CreateRingTrack();

        bool collides = track.CollidesWithWall(track.StartPosition, 0.4f);

        Assert.False(collides);
    }

    [Fact]
    public void CheckpointCenters_AreComputedForEachCheckpoint()
    {
        Track track = TrackFactory.CreateRingTrack();

        Assert.Equal(3, track.CheckpointCenters.Count);
        Assert.All(track.CheckpointCenters.Values, center =>
        {
            Assert.False(track.IsWall((int)center.X, (int)center.Y));
        });
    }
}
