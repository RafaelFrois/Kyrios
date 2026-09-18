namespace Kyrios.Core;

/// <summary>Gera pistas padrão em formato de anel (donut), vistas de cima.</summary>
public static class TrackFactory
{
    public const int Width = 54;
    public const int Height = 22;

    /// <summary>
    /// Cria a pista padrão: um anel retangular com curvas nos quatro cantos.
    /// Reta inferior = largada/chegada; sentido de prova: baixo -> direita -> cima -> esquerda -> baixo.
    /// </summary>
    public static Track CreateRingTrack()
    {
        var cells = new char[Width, Height];
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                cells[x, y] = '#';
            }
        }

        static bool IsInnerBlock(int x, int y) => x is >= 10 and <= 43 && y is >= 5 and <= 16;

        for (int x = 1; x < Width - 1; x++)
        {
            for (int y = 1; y < Height - 1; y++)
            {
                if (!IsInnerBlock(x, y))
                {
                    cells[x, y] = '.';
                }
            }
        }

        // Linha de largada/chegada: reta inferior, faixa vertical na coluna 27.
        for (int y = 17; y <= 20; y++)
        {
            cells[27, y] = 'S';
        }

        // Checkpoint 1: reta direita.
        for (int x = 44; x <= 52; x++)
        {
            cells[x, 10] = '1';
        }

        // Checkpoint 2: reta superior, mesma coluna da linha de chegada.
        for (int y = 1; y <= 4; y++)
        {
            cells[27, y] = '2';
        }

        // Checkpoint 3: reta esquerda.
        for (int x = 1; x <= 9; x++)
        {
            cells[x, 10] = '3';
        }

        List<Vector2D> centerLine =
        [
            new Vector2D(27.5f, 18.5f), // baixo (largada)
            new Vector2D(48f, 18.5f),   // canto inferior-direito
            new Vector2D(48f, 10.5f),   // direita (checkpoint 1)
            new Vector2D(48f, 2.5f),    // canto superior-direito
            new Vector2D(27.5f, 2.5f),  // cima (checkpoint 2)
            new Vector2D(5f, 2.5f),     // canto superior-esquerdo
            new Vector2D(5f, 10.5f),    // esquerda (checkpoint 3)
            new Vector2D(5f, 18.5f),    // canto inferior-esquerdo
        ];

        var startPosition = new Vector2D(27.5f, 18.5f);
        const float startAngle = 0f; // aponta para a direita, em direção ao próximo trecho

        return new Track(cells, centerLine, startPosition, startAngle);
    }

    /// <summary>
    /// Posições de largada em grade (2 colunas) atrás da linha de partida. A pista padrão parte
    /// com os carros voando na direção +X, então as filas ficam escalonadas para trás no eixo X
    /// e lado a lado no eixo Y.
    /// </summary>
    public static IReadOnlyList<Vector2D> GridStartPositions(Track track, int carCount)
    {
        var positions = new List<Vector2D>(carCount);
        for (int i = 0; i < carCount; i++)
        {
            float behind = 1.3f * (i / 2);
            float x = track.StartPosition.X - behind;
            float y = track.StartPosition.Y + ((i % 2 == 0) ? -0.9f : 0.9f);
            positions.Add(new Vector2D(x, y));
        }

        return positions;
    }
}
