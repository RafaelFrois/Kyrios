using Kyrios.Core;
using Xunit;

namespace Kyrios.Core.Tests;

public class CarLapTests
{
    private static Track BuildStraightTrack(char[] markers)
    {
        // Pista reta de 40 células: cada índice de markers[x] define o caractere na coluna x
        // (linha y=1), permitindo testar a ordem em que checkpoints/linha de chegada são cruzados.
        int width = 40;
        int height = 3;
        var cells = new char[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y] = x < markers.Length ? markers[x] : '.';
            }
        }

        return new Track(cells, [], new Vector2D(1f, 1f), 0f);
    }

    [Fact]
    public void Update_PassingCheckpointThenFinishLine_CompletesLap()
    {
        var markers = new char[40];
        Array.Fill(markers, '.');
        markers[10] = '1';
        markers[25] = 'S';

        Track track = BuildStraightTrack(markers);
        var car = new Car("Test", new Vector2D(1f, 1f), 0f);

        bool lapCompleted = false;
        for (int i = 0; i < 400 && !lapCompleted; i++)
        {
            lapCompleted = car.Update(0.05f, new CarInput(1f, 0f), track);
        }

        Assert.True(lapCompleted);
        Assert.Equal(1, car.LapsCompleted);
        Assert.NotNull(car.BestLapTime);
        Assert.Single(car.LapTimes);
    }

    [Fact]
    public void Update_CrossingFinishLineBeforeCheckpoint_DoesNotCountLap()
    {
        var markers = new char[40];
        Array.Fill(markers, '.');
        markers[10] = 'S'; // linha de chegada ANTES do checkpoint no percurso
        markers[25] = '1';

        Track track = BuildStraightTrack(markers);
        var car = new Car("Test", new Vector2D(1f, 1f), 0f);

        for (int i = 0; i < 400; i++)
        {
            car.Update(0.05f, new CarInput(1f, 0f), track);
        }

        Assert.Equal(0, car.LapsCompleted);
        // O checkpoint foi cruzado (índice avançou), mas nunca houve chance de voltar à linha.
        Assert.Equal(2, car.NextCheckpointIndex);
    }

    [Fact]
    public void Update_TrackWithoutCheckpoints_NeverAutoCompletesJustBySittingOnFinish()
    {
        var markers = new char[40];
        Array.Fill(markers, '.');
        // Nenhum checkpoint definido: CheckpointCount = 0.
        Track track = BuildStraightTrack(markers);
        var car = new Car("Test", new Vector2D(1f, 1f), 0f);

        for (int i = 0; i < 100; i++)
        {
            car.Update(0.05f, new CarInput(1f, 0f), track);
        }

        Assert.Equal(0, car.LapsCompleted);
    }
}
