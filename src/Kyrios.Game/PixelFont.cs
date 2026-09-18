using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Kyrios.Game;

/// <summary>
/// Fonte bitmap 3x5 desenhada como retângulos coloridos. Evita depender do pipeline de
/// conteúdo do MonoGame (mgcb/SpriteFont) só para exibir o HUD.
/// </summary>
public static class PixelFont
{
    private const int GlyphWidth = 3;
    private const int GlyphHeight = 5;

    private static readonly Dictionary<char, string[]> Glyphs = new()
    {
        ['0'] = ["111", "101", "101", "101", "111"],
        ['1'] = ["010", "110", "010", "010", "111"],
        ['2'] = ["111", "001", "111", "100", "111"],
        ['3'] = ["111", "001", "111", "001", "111"],
        ['4'] = ["101", "101", "111", "001", "001"],
        ['5'] = ["111", "100", "111", "001", "111"],
        ['6'] = ["111", "100", "111", "101", "111"],
        ['7'] = ["111", "001", "010", "010", "010"],
        ['8'] = ["111", "101", "111", "101", "111"],
        ['9'] = ["111", "101", "111", "001", "111"],
        ['A'] = ["010", "101", "111", "101", "101"],
        ['B'] = ["110", "101", "110", "101", "110"],
        ['C'] = ["011", "100", "100", "100", "011"],
        ['D'] = ["110", "101", "101", "101", "110"],
        ['E'] = ["111", "100", "111", "100", "111"],
        ['F'] = ["111", "100", "111", "100", "100"],
        ['G'] = ["011", "100", "101", "101", "011"],
        ['H'] = ["101", "101", "111", "101", "101"],
        ['I'] = ["111", "010", "010", "010", "111"],
        ['J'] = ["001", "001", "001", "101", "010"],
        ['K'] = ["101", "101", "110", "101", "101"],
        ['L'] = ["100", "100", "100", "100", "111"],
        ['M'] = ["101", "111", "111", "101", "101"],
        ['N'] = ["101", "111", "111", "111", "101"],
        ['O'] = ["111", "101", "101", "101", "111"],
        ['P'] = ["111", "101", "111", "100", "100"],
        ['Q'] = ["111", "101", "101", "111", "011"],
        ['R'] = ["111", "101", "111", "110", "101"],
        ['S'] = ["011", "100", "111", "001", "110"],
        ['T'] = ["111", "010", "010", "010", "010"],
        ['U'] = ["101", "101", "101", "101", "111"],
        ['V'] = ["101", "101", "101", "101", "010"],
        ['W'] = ["101", "101", "101", "111", "101"],
        ['X'] = ["101", "101", "010", "101", "101"],
        ['Y'] = ["101", "101", "010", "010", "010"],
        ['Z'] = ["111", "001", "010", "100", "111"],
        [' '] = ["000", "000", "000", "000", "000"],
        [':'] = ["000", "010", "000", "010", "000"],
        ['.'] = ["000", "000", "000", "000", "010"],
        ['/'] = ["001", "001", "010", "100", "100"],
        ['-'] = ["000", "000", "111", "000", "000"],
        ['%'] = ["101", "001", "010", "100", "101"],
        [','] = ["000", "000", "000", "010", "100"],
        ['*'] = ["101", "010", "101", "000", "000"],
        ['!'] = ["010", "010", "010", "000", "010"],
    };

    public static float Measure(string text, float pixelSize) =>
        text.Length == 0 ? 0f : ((text.Length * (GlyphWidth + 1)) - 1) * pixelSize;

    public static float LineHeight(float pixelSize) => GlyphHeight * pixelSize;

    public static void Draw(SpriteBatch spriteBatch, Texture2D pixel, string text, Vector2 position, float pixelSize, Color color)
    {
        float cursorX = position.X;
        foreach (char rawChar in text)
        {
            char c = char.ToUpperInvariant(rawChar);
            if (Glyphs.TryGetValue(c, out string[] rows))
            {
                for (int row = 0; row < rows.Length; row++)
                {
                    string bits = rows[row];
                    for (int col = 0; col < bits.Length; col++)
                    {
                        if (bits[col] == '1')
                        {
                            var rect = new Rectangle(
                                (int)(cursorX + (col * pixelSize)),
                                (int)(position.Y + (row * pixelSize)),
                                (int)MathF.Ceiling(pixelSize),
                                (int)MathF.Ceiling(pixelSize));
                            spriteBatch.Draw(pixel, rect, color);
                        }
                    }
                }
            }

            cursorX += (GlyphWidth + 1) * pixelSize;
        }
    }

    /// <summary>Como <see cref="Draw"/>, mas cada linha do glifo (de cima pra baixo) usa uma cor diferente — dá um efeito de gradiente vertical no texto, tipo os títulos de jogo retrô.</summary>
    public static void DrawGradient(SpriteBatch spriteBatch, Texture2D pixel, string text, Vector2 position, float pixelSize, IReadOnlyList<Color> rowColors)
    {
        float cursorX = position.X;
        foreach (char rawChar in text)
        {
            char c = char.ToUpperInvariant(rawChar);
            if (Glyphs.TryGetValue(c, out string[] rows))
            {
                for (int row = 0; row < rows.Length; row++)
                {
                    string bits = rows[row];
                    Color rowColor = rowColors[Math.Min(row, rowColors.Count - 1)];
                    for (int col = 0; col < bits.Length; col++)
                    {
                        if (bits[col] == '1')
                        {
                            var rect = new Rectangle(
                                (int)(cursorX + (col * pixelSize)),
                                (int)(position.Y + (row * pixelSize)),
                                (int)MathF.Ceiling(pixelSize),
                                (int)MathF.Ceiling(pixelSize));
                            spriteBatch.Draw(pixel, rect, rowColor);
                        }
                    }
                }
            }

            cursorX += (GlyphWidth + 1) * pixelSize;
        }
    }
}
