using Microsoft.Xna.Framework;

namespace Kyrios.Game;

/// <summary>
/// Componentes visuais compartilhados por todas as telas — painel, botão, cabeçalho, dicas de tecla, etapas,
/// barra de progresso, selo de dificuldade, cadeado... Toda tela usa os mesmos, então o jogo inteiro tem a
/// mesma cara (mesmas bordas arredondadas, mesmas cores, mesmos tamanhos de texto).
/// </summary>
public sealed partial class GameRoot
{
    // Tamanhos de texto padrão (nunca menores que 1.4 pra continuar legível).
    private const float TitleSize = 3.5f;
    private const float BodySize = 1.8f;
    private const float SmallSize = 1.5f;

    private static readonly Rectangle BackButtonRect = new(20, 14, 34, 34);

    private float _arrowFlashLeft;
    private float _arrowFlashRight;
    private float _denyShake;
    private float _equipFlash;

    private void UpdateUiAnimations(float dt)
    {
        _arrowFlashLeft = MathF.Max(0f, _arrowFlashLeft - dt);
        _arrowFlashRight = MathF.Max(0f, _arrowFlashRight - dt);
        _denyShake = MathF.Max(0f, _denyShake - dt);
        _equipFlash = MathF.Max(0f, _equipFlash - dt);
    }

    private void DimScreen(Color color) =>
        _spriteBatch.Draw(_pixel, new Rectangle(-TrackMargin, -TrackMargin, _windowWidth, _windowHeight), color);

    private void DrawPanel(Rectangle rect)
    {
        DrawRoundedRect(rect, PanelBorderColor, 10f);
        DrawRoundedRect(new Rectangle(rect.X + 3, rect.Y + 3, rect.Width - 6, rect.Height - 6), PanelFillColor, 8f);
    }

    /// <summary>Painel com a faixa colorida no topo — o "card" padrão de pop-ups e destaques.</summary>
    private void DrawAccentPanel(Rectangle rect, Color accent)
    {
        DrawPanel(rect);
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X + 6, rect.Y + 5, rect.Width - 12, 4), accent);
    }

    /// <summary>Cabeçalho das telas: seta de voltar (clicável), título grande e subtítulo discreto.</summary>
    private void DrawScreenHeader(string title, string subtitle)
    {
        DrawArrowButton(BackButtonRect, pointRight: false, BackButtonRect.Contains(LogicalMousePoint()), flashing: false);
        PixelFont.DrawShadowed(_spriteBatch, _pixel, title, new Vector2(66f, 15f), TitleSize, AccentColor);
        if (subtitle is not null)
        {
            PixelFont.Draw(_spriteBatch, _pixel, subtitle, new Vector2(68f, 44f), SmallSize, StatBadgeLabelColor);
        }
    }

    private bool WasBackButtonClicked() => _input.WasMouseLeftJustPressed && BackButtonRect.Contains(LogicalMousePoint());

    /// <summary>Botão padrão. Principal = preenchido na cor de destaque (a ação mais importante da tela);
    /// focado = borda de destaque pulsando + marcador; desativado = apagado.</summary>
    private void DrawButton(Rectangle rect, string label, bool focused, bool primary = false, string badge = null, float textSize = 2f)
    {
        float pulse = (MathF.Sin(_visualTime * 5f) + 1f) / 2f;
        Color textColor;
        if (primary)
        {
            Color fill = focused ? Color.Lerp(AccentColor, Color.White, 0.2f * pulse) : CarPainter.Darken(AccentColor, 0.8f);
            if (focused)
            {
                DrawRoundedRect(InflateRect(rect, 3f, 3f), Color.White * 0.9f, 10f);
            }

            DrawRoundedRect(rect, fill, 8f);
            textColor = MenuBackground;
        }
        else
        {
            DrawRoundedRect(rect, focused ? Color.Lerp(AccentColor, Color.White, pulse * 0.3f) : PanelBorderColor, 8f);
            DrawRoundedRect(new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4), focused ? new Color(44, 50, 70) : PanelFillColor, 7f);
            textColor = focused ? AccentColor : TextColor;
        }

        float textHeight = PixelFont.LineHeight(textSize);
        float textY = rect.Y + ((rect.Height - textHeight) / 2f);
        float textX = rect.X + 22f;
        if (focused)
        {
            DrawTriangle(new Vector2(rect.X + 11f, rect.Center.Y), pointRight: true, 5f, textColor);
        }

        PixelFont.Draw(_spriteBatch, _pixel, label, new Vector2(textX, textY), textSize, textColor);

        if (badge is not null)
        {
            float badgeWidth = PixelFont.Measure(badge, SmallSize);
            PixelFont.Draw(_spriteBatch, _pixel, badge, new Vector2(rect.Right - 14f - badgeWidth, rect.Y + ((rect.Height - PixelFont.LineHeight(SmallSize)) / 2f)), SmallSize, primary ? MenuBackground * 0.8f : StatBadgeLabelColor);
        }
    }

    /// <summary>Triângulo pixelizado (a fonte não tem "&lt;"/"&gt;") — setas e marcadores.</summary>
    private void DrawTriangle(Vector2 center, bool pointRight, float size, Color color)
    {
        int columns = (int)(size * 2f);
        for (int i = 0; i < columns; i++)
        {
            float half = size * (1f - (i / (float)columns));
            int x = pointRight ? (int)(center.X - size + i) : (int)(center.X + size - i - 1);
            _spriteBatch.Draw(_pixel, new Rectangle(x, (int)(center.Y - half), 1, (int)MathF.Max(1f, half * 2f)), color);
        }
    }

    /// <summary>Dicas de tecla no rodapé: [TECLA] AÇÃO, centralizadas — o mesmo formato em todas as telas.</summary>
    private void DrawKeyHints(params (string Key, string Label)[] hints)
    {
        const float keySize = 1.4f;
        const float labelSize = SmallSize;
        const float padding = 5f;
        const float gap = 20f;

        float total = 0f;
        foreach ((string key, string label) in hints)
        {
            total += PixelFont.Measure(key, keySize) + (padding * 2f) + 6f + PixelFont.Measure(label, labelSize) + gap;
        }

        total -= gap;
        float x = (AreaWidth - total) / 2f;
        float y = AreaHeight - 22f;
        foreach ((string key, string label) in hints)
        {
            float keyWidth = PixelFont.Measure(key, keySize) + (padding * 2f);
            DrawRoundedRect(new Rectangle((int)x, (int)y - 3, (int)keyWidth, 16), PanelBorderColor, 4f);
            PixelFont.Draw(_spriteBatch, _pixel, key, new Vector2(x + padding, y + 1f), keySize, TextColor);
            x += keyWidth + 6f;
            PixelFont.Draw(_spriteBatch, _pixel, label, new Vector2(x, y), labelSize, StatBadgeLabelColor);
            x += PixelFont.Measure(label, labelSize) + gap;
        }
    }

    private void DrawCenteredText(Rectangle area, string text, float y, float size, Color color, bool shadow = false)
    {
        float width = PixelFont.Measure(text, size);
        var position = new Vector2(area.X + ((area.Width - width) / 2f), y);
        if (shadow)
        {
            PixelFont.DrawShadowed(_spriteBatch, _pixel, text, position, size, color);
        }
        else
        {
            PixelFont.Draw(_spriteBatch, _pixel, text, position, size, color);
        }
    }

    private void DrawProgressBar(Rectangle rect, float fraction, Color fill, float radius)
    {
        DrawRoundedRect(rect, new Color(38, 42, 56), radius);
        int fillWidth = (int)(rect.Width * Math.Clamp(fraction, 0f, 1f));
        if (fillWidth > 0)
        {
            DrawRoundedRect(new Rectangle(rect.X, rect.Y, Math.Max(fillWidth, (int)(radius * 2f)), rect.Height), fill, radius);
        }
    }

    /// <summary>Cadeado pixelizado (fechado, ou aberto com a alça levantada), centrado em <paramref name="center"/>.
    /// No tamanho 1 ocupa uns 18x23 px.</summary>
    private void DrawLock(Vector2 center, float scale, Color color, bool open = false)
    {
        int Px(float value) => Math.Max(1, (int)MathF.Round(value * scale));
        int x = (int)center.X;
        int y = (int)center.Y;
        int lift = open ? Px(5) : 0;

        _spriteBatch.Draw(_pixel, new Rectangle(x - Px(7), y - Px(12) - lift, Px(3), Px(10)), color);
        if (!open)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(x + Px(4), y - Px(12), Px(3), Px(10)), color);
        }

        _spriteBatch.Draw(_pixel, new Rectangle(x - Px(7), y - Px(13) - lift, Px(14), Px(3)), color);
        _spriteBatch.Draw(_pixel, new Rectangle(x - Px(9), y - Px(3), Px(18), Px(13)), color);
        _spriteBatch.Draw(_pixel, new Rectangle(x - Px(1), y + Px(1), Px(2), Px(5)), new Color(40, 30, 10));
    }

    /// <summary>Selo de estado: cadeado aberto + "DESBLOQUEADA", fechado + "BLOQUEADA", ou estrela de equipado.</summary>
    private void DrawStatusChip(Vector2 center, bool unlocked, bool equipped, string noun)
    {
        string text = equipped ? $"{noun} EQUIPADA" : unlocked ? "DESBLOQUEADA" : "BLOQUEADA";
        Color color = equipped ? AccentColor : unlocked ? RecordColor : new Color(170, 176, 192);
        const float size = 1.6f;
        float width = PixelFont.Measure(text, size) + 34f;
        var rect = new Rectangle((int)(center.X - (width / 2f)), (int)center.Y - 10, (int)width, 20);
        DrawRoundedRect(rect, color * 0.25f, 6f);
        DrawLock(new Vector2(rect.X + 13f, rect.Y + 12f), 0.5f, color, open: unlocked);
        PixelFont.Draw(_spriteBatch, _pixel, text, new Vector2(rect.X + 26f, rect.Y + 5f), size, color);
    }

    /// <summary>Bolinha verde com "check" — marca de conquista obtida / item equipado.</summary>
    private void DrawCheckBadge(Vector2 center)
    {
        DrawCircle(center, 8f, MenuBackground);
        DrawCircle(center, 6.5f, RecordColor);
        var ink = new Color(20, 60, 30);
        var x = (int)center.X;
        var y = (int)center.Y;
        for (int i = 0; i < 3; i++)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(x - 4 + i, y - 1 + i, 2, 2), ink);
        }

        for (int i = 0; i < 5; i++)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(x - 2 + i, y + 1 - i, 2, 2), ink);
        }
    }

    /// <summary>Botão de seta: acende com o mouse em cima e dá um "flash" quando usado.</summary>
    private void DrawArrowButton(Rectangle rect, bool pointRight, bool hovered, bool flashing)
    {
        Color border = flashing ? Color.White : hovered ? AccentColor : PanelBorderColor;
        DrawRoundedRect(rect, border, 6f);
        DrawRoundedRect(new Rectangle(rect.X + 3, rect.Y + 3, rect.Width - 6, rect.Height - 6), PanelFillColor, 5f);
        DrawTriangle(rect.Center.ToVector2(), pointRight, MathF.Min(rect.Width, rect.Height) * 0.22f, flashing || hovered ? AccentColor : TextColor);
    }

    /// <summary>Quadrado de ícone (pixel-art ou skin) com fundo tingido — usado em conquistas, pistas e desbloqueios.</summary>
    private void DrawIconTile(Rectangle tile, AchievementIcon icon, Color tint, bool colored)
    {
        DrawRoundedRect(tile, colored ? tint : new Color(60, 63, 76), 6f);
        var inner = new Rectangle(tile.X + 2, tile.Y + 2, tile.Width - 4, tile.Height - 4);
        DrawRoundedRect(inner, colored ? Color.Lerp(new Color(20, 22, 30), tint, 0.28f) : new Color(32, 34, 44), 5f);
        _iconRenderer.Draw(icon, inner, colored, _visualTime);
    }

    /// <summary>Título do jogo com contorno e gradiente.</summary>
    private void DrawGameTitle(Vector2 position, float size, float alpha = 1f)
    {
        const string title = "MEGRACE";
        Vector2[] outline = [new(-1, -1), new(0, -1), new(1, -1), new(-1, 0), new(1, 0), new(-1, 1), new(0, 1), new(1, 1)];
        foreach (Vector2 offset in outline)
        {
            PixelFont.Draw(_spriteBatch, _pixel, title, position + (offset * 1.5f), size, TitleOutline * alpha);
        }

        PixelFont.DrawGradient(_spriteBatch, _pixel, title, position, size, [.. TitleGradient.Select(color => color * alpha)]);
    }

    /// <summary>Selo pixel-art da Domus Arcis (espada + nome do estúdio).</summary>
    private void DrawStudioLogo(Vector2 bottomLeft)
    {
        const float iconSize = 30f;
        const float textSize = 1.1f;
        float textBlockHeight = (PixelFont.LineHeight(textSize) * 2f) + 2f;
        Vector2 topLeft = bottomLeft - new Vector2(0f, iconSize + 4f + textBlockHeight);

        float angle = MathF.PI * 0.7f;
        Vector2 iconCenter = topLeft + new Vector2(iconSize * 0.55f, iconSize * 0.5f);
        DrawFilledRectRotated(iconCenter, iconSize * 0.85f, 3.5f, angle, StudioLogoColor);
        DrawFilledRectRotated(iconCenter + Rotate(new Vector2(iconSize * 0.3f, 0f), angle), 10f, 3f, angle + (MathF.PI / 2f), StudioLogoColor);
        DrawCircle(iconCenter + Rotate(new Vector2(iconSize * 0.42f, 0f), angle), 3f, StudioLogoColor);

        float centerX = topLeft.X + (iconSize / 2f);
        float textY = topLeft.Y + iconSize + 4f;
        PixelFont.Draw(_spriteBatch, _pixel, "DOMUS", new Vector2(centerX - (PixelFont.Measure("DOMUS", textSize) / 2f), textY), textSize, StudioLogoColor);
        PixelFont.Draw(_spriteBatch, _pixel, "ARCIS", new Vector2(centerX - (PixelFont.Measure("ARCIS", textSize) / 2f), textY + PixelFont.LineHeight(textSize) + 2f), textSize, StudioLogoColor);
    }

    /// <summary>Maior tamanho (até <paramref name="maxSize"/>) em que o texto cabe na largura dada.</summary>
    private static float FitTextSize(string text, float width, float maxSize)
    {
        float atOne = PixelFont.Measure(text, 1f);
        return atOne <= 0f ? maxSize : MathF.Min(maxSize, width / atOne);
    }

    /// <summary>Quebra o texto por palavras pra caber na largura; a última linha permitida termina em "..." se sobrar texto.</summary>
    private static List<string> WrapText(string text, float width, float size, int maxLines)
    {
        var lines = new List<string>();
        string current = string.Empty;
        foreach (string word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            string candidate = current.Length == 0 ? word : $"{current} {word}";
            if (PixelFont.Measure(candidate, size) <= width || current.Length == 0)
            {
                current = candidate;
                continue;
            }

            lines.Add(current);
            current = word;
        }

        if (current.Length > 0)
        {
            lines.Add(current);
        }

        if (lines.Count > maxLines)
        {
            lines = lines.Take(maxLines).ToList();
            lines[^1] += "...";
        }

        return lines;
    }

    /// <summary>Texto de "quanto falta" pra um requisito: "8/10 - FALTAM 2" (ou o texto do próprio requisito).</summary>
    private string ProgressLabel(UnlockRequirement requirement)
    {
        string progress = requirement.ProgressText(_saveData);
        if (progress is null)
        {
            return null;
        }

        string[] parts = progress.Split('/');
        if (parts.Length == 2 && float.TryParse(parts[0], out float have) && float.TryParse(parts[1], out float need) && need > have)
        {
            return $"{progress}  -  FALTAM {need - have:0}";
        }

        return progress;
    }
}
