namespace Kyrios.Core;

/// <summary>
/// Grade 2D (visão de cima) que representa uma pista de corrida.
/// Cada célula é um caractere:
///   '#' parede (sólida)      '.' pista (asfalto)
///   ',' grama (fora da pista) 'S' linha de largada/chegada
///   '1'-'9' checkpoints, na ordem em que devem ser cruzados
/// </summary>
public sealed class Track
{
    private readonly char[,] _cells;

    public int Width { get; }
    public int Height { get; }
    public int CheckpointCount { get; }
    public IReadOnlyList<Vector2D> CenterLine { get; }
    public Vector2D StartPosition { get; }
    public float StartFacingAngle { get; }

    /// <summary>Centro aproximado (em coordenadas de mundo) de cada checkpoint, indexado por 1..CheckpointCount.</summary>
    public IReadOnlyDictionary<int, Vector2D> CheckpointCenters { get; }

    public Vector2D FinishLineCenter { get; }

    public Track(char[,] cells, IReadOnlyList<Vector2D> centerLine, Vector2D startPosition, float startFacingAngle)
    {
        _cells = cells;
        Width = cells.GetLength(0);
        Height = cells.GetLength(1);
        CenterLine = centerLine;
        StartPosition = startPosition;
        StartFacingAngle = startFacingAngle;

        int maxCheckpoint = 0;
        var checkpointSums = new Dictionary<int, (float SumX, float SumY, int Count)>();
        float finishSumX = 0f, finishSumY = 0f;
        int finishCount = 0;

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                char c = cells[x, y];
                if (c is >= '1' and <= '9')
                {
                    int index = c - '0';
                    maxCheckpoint = Math.Max(maxCheckpoint, index);
                    var (sumX, sumY, count) = checkpointSums.TryGetValue(index, out var existing) ? existing : (0f, 0f, 0);
                    checkpointSums[index] = (sumX + x + 0.5f, sumY + y + 0.5f, count + 1);
                }
                else if (c == 'S')
                {
                    finishSumX += x + 0.5f;
                    finishSumY += y + 0.5f;
                    finishCount++;
                }
            }
        }

        CheckpointCount = maxCheckpoint;
        CheckpointCenters = checkpointSums.ToDictionary(
            kvp => kvp.Key,
            kvp => new Vector2D(kvp.Value.SumX / kvp.Value.Count, kvp.Value.SumY / kvp.Value.Count));
        FinishLineCenter = finishCount > 0 ? new Vector2D(finishSumX / finishCount, finishSumY / finishCount) : startPosition;
    }

    public char CellAt(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
        {
            return '#';
        }

        return _cells[x, y];
    }

    public char CellAt(Vector2D worldPosition) => CellAt((int)MathF.Floor(worldPosition.X), (int)MathF.Floor(worldPosition.Y));

    public bool IsWall(int x, int y) => CellAt(x, y) == '#';

    public bool IsOffTrack(int x, int y) => CellAt(x, y) == ',';

    /// <summary>
    /// Verifica se um círculo (carro) colide com alguma parede, varrendo as
    /// células vizinhas dentro do raio informado.
    /// </summary>
    public bool CollidesWithWall(Vector2D center, float radius)
    {
        int minX = (int)MathF.Floor(center.X - radius);
        int maxX = (int)MathF.Floor(center.X + radius);
        int minY = (int)MathF.Floor(center.Y - radius);
        int maxY = (int)MathF.Floor(center.Y + radius);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (!IsWall(x, y))
                {
                    continue;
                }

                float closestX = Math.Clamp(center.X, x, x + 1f);
                float closestY = Math.Clamp(center.Y, y, y + 1f);
                float dx = center.X - closestX;
                float dy = center.Y - closestY;
                if ((dx * dx) + (dy * dy) < radius * radius)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Checkpoint esperado (1-based) ao cruzar a célula em (x, y), ou 0 se não for checkpoint.</summary>
    public int CheckpointAt(int x, int y)
    {
        char c = CellAt(x, y);
        return c is >= '1' and <= '9' ? c - '0' : 0;
    }

    public bool IsFinishLine(int x, int y) => CellAt(x, y) == 'S';
}
