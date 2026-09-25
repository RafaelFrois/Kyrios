using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Kyrios.Game;

/// <summary>Desenha um <see cref="AchievementIcon"/> dentro de um quadrado: o pixel-art ou a skin, colorido
/// (desbloqueada) ou em tons de cinza (bloqueada), com o selo de texto no canto se houver.</summary>
public sealed class AchievementIconRenderer(SpriteBatch spriteBatch, Texture2D pixel, CarPainter carPainter)
{
    private static readonly Color BadgeFill = new(16, 18, 26);

    public void Draw(AchievementIcon icon, Rectangle area, bool colored, float time)
    {
        if (icon.Pixels is { Length: > 0 } rows)
        {
            DrawPixels(rows, area, colored);
        }
        else if (icon.SkinId is not null && CarSkins.Find(icon.SkinId) is { } skin)
        {
            float unit = area.Width * 0.62f;
            float sway = colored ? MathF.Sin(time * 2f) * 0.1f : 0f;
            carPainter.Begin(area.Center.ToVector2(), -0.35f + sway, unit, new Color(255, 200, 40), eliminated: false, time, monochrome: !colored);
            skin.Paint(carPainter);
        }

        if (icon.Badge is { Length: > 0 } badge)
        {
            float size = area.Width >= 44 ? 1.25f : 1f;
            float width = PixelFont.Measure(badge, size);
            float height = PixelFont.LineHeight(size);
            var badgeRect = new Rectangle((int)(area.Right - width - 5f), (int)(area.Bottom - height - 5f), (int)width + 4, (int)height + 4);
            spriteBatch.Draw(pixel, badgeRect, BadgeFill);
            PixelFont.Draw(spriteBatch, pixel, badge, new Vector2(badgeRect.X + 2, badgeRect.Y + 2), size, colored ? Color.White : new Color(150, 155, 168));
        }
    }

    private void DrawPixels(string[] rows, Rectangle area, bool colored)
    {
        int columns = rows.Max(row => row.Length);
        int cell = Math.Max(1, (int)(MathF.Min(area.Width, area.Height) * 0.86f / Math.Max(columns, rows.Length)));
        int originX = area.X + ((area.Width - (cell * columns)) / 2);
        int originY = area.Y + ((area.Height - (cell * rows.Length)) / 2);

        for (int y = 0; y < rows.Length; y++)
        {
            string row = rows[y];
            for (int x = 0; x < row.Length; x++)
            {
                if (!AchievementIcons.Palette.TryGetValue(row[x], out Color color))
                {
                    continue;
                }

                spriteBatch.Draw(pixel, new Rectangle(originX + (x * cell), originY + (y * cell), cell, cell), colored ? color : CarPainter.Grayscale(color));
            }
        }
    }
}
