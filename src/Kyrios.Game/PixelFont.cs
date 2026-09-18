using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Kyrios.Game;

/// <summary>
/// Fonte bitmap 5x7 desenhada como retângulos coloridos. Evita depender do pipeline de
/// conteúdo do MonoGame (mgcb/SpriteFont) só para exibir texto — a grade 5x7 (em vez de uma
/// 3x5 antiga, bem mais rústica) dá letras com curvas e proporções legíveis de verdade.
/// </summary>
public static class PixelFont
{
    private const int GlyphWidth = 5;
    private const int GlyphHeight = 7;

    private static readonly Dictionary<char, string[]> Glyphs = new()
    {
        ['0'] = ["01110", "10001", "10011", "10101", "11001", "10001", "01110"],
        ['1'] = ["00100", "01100", "00100", "00100", "00100", "00100", "01110"],
        ['2'] = ["01110", "10001", "00001", "00010", "00100", "01000", "11111"],
        ['3'] = ["11111", "00010", "00100", "00010", "00001", "10001", "01110"],
        ['4'] = ["00010", "00110", "01010", "10010", "11111", "00010", "00010"],
        ['5'] = ["11111", "10000", "11110", "00001", "00001", "10001", "01110"],
        ['6'] = ["00110", "01000", "10000", "11110", "10001", "10001", "01110"],
        ['7'] = ["11111", "00001", "00010", "00100", "01000", "01000", "01000"],
        ['8'] = ["01110", "10001", "10001", "01110", "10001", "10001", "01110"],
        ['9'] = ["01110", "10001", "10001", "01111", "00001", "00010", "01100"],
        ['A'] = ["01110", "10001", "10001", "11111", "10001", "10001", "10001"],
        ['B'] = ["11110", "10001", "10001", "11110", "10001", "10001", "11110"],
        ['C'] = ["01111", "10000", "10000", "10000", "10000", "10000", "01111"],
        ['D'] = ["11100", "10010", "10001", "10001", "10001", "10010", "11100"],
        ['E'] = ["11111", "10000", "10000", "11110", "10000", "10000", "11111"],
        ['F'] = ["11111", "10000", "10000", "11110", "10000", "10000", "10000"],
        ['G'] = ["01111", "10000", "10000", "10011", "10001", "10001", "01111"],
        ['H'] = ["10001", "10001", "10001", "11111", "10001", "10001", "10001"],
        ['I'] = ["11111", "00100", "00100", "00100", "00100", "00100", "11111"],
        ['J'] = ["00111", "00010", "00010", "00010", "00010", "10010", "01100"],
        ['K'] = ["10001", "10010", "10100", "11000", "10100", "10010", "10001"],
        ['L'] = ["10000", "10000", "10000", "10000", "10000", "10000", "11111"],
        ['M'] = ["10001", "11011", "10101", "10101", "10001", "10001", "10001"],
        ['N'] = ["10001", "11001", "10101", "10101", "10011", "10001", "10001"],
        ['O'] = ["01110", "10001", "10001", "10001", "10001", "10001", "01110"],
        ['P'] = ["11110", "10001", "10001", "11110", "10000", "10000", "10000"],
        ['Q'] = ["01110", "10001", "10001", "10001", "10101", "10010", "01101"],
        ['R'] = ["11110", "10001", "10001", "11110", "10100", "10010", "10001"],
        ['S'] = ["01111", "10000", "10000", "01110", "00001", "00001", "11110"],
        ['T'] = ["11111", "00100", "00100", "00100", "00100", "00100", "00100"],
        ['U'] = ["10001", "10001", "10001", "10001", "10001", "10001", "01110"],
        ['V'] = ["10001", "10001", "10001", "10001", "10001", "01010", "00100"],
        ['W'] = ["10001", "10001", "10001", "10101", "10101", "10101", "01010"],
        ['X'] = ["10001", "10001", "01010", "00100", "01010", "10001", "10001"],
        ['Y'] = ["10001", "10001", "01010", "00100", "00100", "00100", "00100"],
        ['Z'] = ["11111", "00001", "00010", "00100", "01000", "10000", "11111"],
        [' '] = ["00000", "00000", "00000", "00000", "00000", "00000", "00000"],
        [':'] = ["00000", "00100", "00100", "00000", "00100", "00100", "00000"],
        ['.'] = ["00000", "00000", "00000", "00000", "00000", "01100", "01100"],
        ['/'] = ["00001", "00001", "00010", "00100", "01000", "10000", "10000"],
        ['-'] = ["00000", "00000", "00000", "11111", "00000", "00000", "00000"],
        ['%'] = ["11001", "11010", "00010", "00100", "01000", "01011", "10011"],
        [','] = ["00000", "00000", "00000", "00000", "01100", "01100", "01000"],
        ['*'] = ["00000", "10101", "01110", "11111", "01110", "10101", "00000"],
        ['!'] = ["00100", "00100", "00100", "00100", "00100", "00000", "00100"],
        ['('] = ["00010", "00100", "01000", "01000", "01000", "00100", "00010"],
        [')'] = ["01000", "00100", "00010", "00010", "00010", "00100", "01000"],
        ['º'] = ["01110", "10001", "10001", "01110", "00000", "11111", "00000"],
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

    /// <summary>Como <see cref="Draw"/>, mas com uma sombra projetada por baixo (deslocada e escurecida) —
    /// dá profundidade e mantém o texto legível mesmo em cima de fundos claros ou barulhentos.</summary>
    public static void DrawShadowed(SpriteBatch spriteBatch, Texture2D pixel, string text, Vector2 position, float pixelSize, Color color, Color? shadowColor = null)
    {
        float shadowOffset = MathF.Max(1f, pixelSize * 0.22f);
        Draw(spriteBatch, pixel, text, position + new Vector2(shadowOffset, shadowOffset), pixelSize, shadowColor ?? new Color(0, 0, 0, 130));
        Draw(spriteBatch, pixel, text, position, pixelSize, color);
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
