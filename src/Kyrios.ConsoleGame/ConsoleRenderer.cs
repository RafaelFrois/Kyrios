using System.Text;
using Kyrios.Core;

namespace Kyrios.ConsoleGame;

/// <summary>Desenha a pista, os carros e o HUD no console, redesenhando o quadro inteiro no lugar (sem Console.Clear, para evitar piscar).</summary>
public sealed class ConsoleRenderer
{
    private const int HudLineWidth = 78;
    private const string DirectionGlyphs = "→↘↓↙←↖↑↗";

    private readonly int _width;
    private readonly int _height;
    private readonly char[,] _baseGrid;

    public ConsoleRenderer(Track track)
    {
        _width = track.Width;
        _height = track.Height;
        _baseGrid = new char[_width, _height];

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                _baseGrid[x, y] = track.CellAt(x, y) switch
                {
                    '#' => '█',
                    'S' => '=',
                    _ => ' ',
                };
            }
        }
    }

    public void Render(RaceSimulation race, IReadOnlyList<string> hudLines)
    {
        var carDots = new (int X, int Y, char Glyph, ConsoleColor Color)[race.Entrants.Count];
        for (int i = 0; i < race.Entrants.Count; i++)
        {
            Car car = race.Entrants[i].Car;
            int cx = Math.Clamp((int)MathF.Floor(car.Position.X), 0, _width - 1);
            int cy = Math.Clamp((int)MathF.Floor(car.Position.Y), 0, _height - 1);
            char glyph = DirectionGlyph(car.Angle);
            ConsoleColor color = race.Entrants[i].Kind == DriverKind.Human ? ConsoleColor.Yellow : AiColor(i);
            carDots[i] = (cx, cy, glyph, color);
        }

        var frame = new StringBuilder();
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                frame.Append(_baseGrid[x, y]);
            }

            frame.Append('\n');
        }

        foreach (string line in hudLines)
        {
            frame.Append(line.PadRight(HudLineWidth)).Append('\n');
        }

        Console.SetCursorPosition(0, 0);
        Console.ResetColor();
        Console.Write(frame.ToString());

        ConsoleColor previous = Console.ForegroundColor;
        foreach (var dot in carDots)
        {
            Console.SetCursorPosition(dot.X, dot.Y);
            Console.ForegroundColor = dot.Color;
            Console.Write(dot.Glyph);
        }

        Console.ForegroundColor = previous;
        Console.SetCursorPosition(0, _height + hudLines.Count);
    }

    private static char DirectionGlyph(float angleRadians)
    {
        const float twoPi = MathF.PI * 2f;
        float normalized = angleRadians % twoPi;
        if (normalized < 0f)
        {
            normalized += twoPi;
        }

        int sector = (int)MathF.Round(normalized / (twoPi / 8f)) % 8;
        return DirectionGlyphs[sector];
    }

    private static ConsoleColor AiColor(int index)
    {
        ConsoleColor[] palette =
        [
            ConsoleColor.Cyan, ConsoleColor.Green, ConsoleColor.Magenta,
            ConsoleColor.Red, ConsoleColor.Blue, ConsoleColor.DarkYellow,
        ];
        return palette[index % palette.Length];
    }
}
