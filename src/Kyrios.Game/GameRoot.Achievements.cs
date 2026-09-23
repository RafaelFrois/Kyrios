using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Kyrios.Game;

/// <summary>Página de conquistas, botão de acesso na tela inicial e notificações de desbloqueio.</summary>
public sealed partial class GameRoot
{
    private const int AchievementColumns = 3;
    private const int AchievementVisibleRows = 5;
    private const float AchievementCardHeight = 62f;
    private const float AchievementCardGap = 8f;
    private const float AchievementListTop = 100f;

    private static readonly Color AllTabColor = new(225, 228, 238);
    private static readonly Color LockedCardFill = new(22, 25, 34);
    private static readonly Color LockedCardBorder = new(44, 48, 62);
    private static readonly Color LockedTileFill = new(32, 34, 44);
    private static readonly Color LockedTileBorder = new(60, 63, 76);
    private static readonly Color LockedTextColor = new(120, 126, 142);
    private static readonly Color MutedTextColor = new(196, 201, 214);
    private static readonly Color ProgressTrackColor = new(38, 42, 56);

    /// <summary>Retângulos da página que o desenho e o mouse precisam concordar.</summary>
    private readonly record struct AchievementsLayout(Rectangle BackButton, Rectangle[] Tabs, Rectangle List, Rectangle ScrollTrack);

    private void OpenAchievements(State returnState)
    {
        _achievementsReturnState = returnState;
        _achievementScrollRow = 0;
        _state = State.Achievements;
        _audio.PlayMenuConfirm();
    }

    private void CloseAchievements()
    {
        _state = _achievementsReturnState;
        _audio.PlayMenuConfirm();
    }

    private static int AchievementTabCount => ProgressionStyle.CategoryOrder.Length + 1;

    private static List<Achievement> AchievementsInTab(int tab) => tab == 0
        ? [.. Achievements.All]
        : [.. Achievements.All.Where(achievement => achievement.Category == ProgressionStyle.CategoryOrder[tab - 1])];

    private static int MaxAchievementScroll(int tab)
    {
        int rows = (AchievementsInTab(tab).Count + AchievementColumns - 1) / AchievementColumns;
        return Math.Max(0, rows - AchievementVisibleRows);
    }

    private AchievementsLayout ComputeAchievementsLayout()
    {
        float areaWidth = _windowWidth - (2f * TrackMargin);

        var back = new Rectangle(20, 12, 34, 34);

        int tabCount = AchievementTabCount;
        const float tabGap = 6f;
        const float tabTop = 56f;
        const float tabHeight = 34f;
        float tabWidth = (areaWidth - 40f - ((tabCount - 1) * tabGap)) / tabCount;
        var tabs = new Rectangle[tabCount];
        for (int i = 0; i < tabCount; i++)
        {
            tabs[i] = new Rectangle((int)(20f + (i * (tabWidth + tabGap))), (int)tabTop, (int)tabWidth, (int)tabHeight);
        }

        float listHeight = (AchievementVisibleRows * (AchievementCardHeight + AchievementCardGap)) - AchievementCardGap;
        var list = new Rectangle(20, (int)AchievementListTop, (int)(areaWidth - 40f - 16f), (int)listHeight);
        var scrollTrack = new Rectangle(list.Right + 8, list.Y, 6, list.Height);

        return new AchievementsLayout(back, tabs, list, scrollTrack);
    }

    private void UpdateAchievementsPage()
    {
        if (_input.WasJustPressed(Keys.Escape) || _input.WasJustPressed(Keys.C) || _input.WasJustPressed(Keys.Back))
        {
            CloseAchievements();
            return;
        }

        int tab = _achievementTab;
        if (_input.WasJustPressed(Keys.Right) || _input.WasJustPressed(Keys.D) || _input.WasJustPressed(Keys.Tab))
        {
            tab = (tab + 1) % AchievementTabCount;
        }
        else if (_input.WasJustPressed(Keys.Left) || _input.WasJustPressed(Keys.A))
        {
            tab = (tab - 1 + AchievementTabCount) % AchievementTabCount;
        }

        int scroll = _achievementScrollRow;
        if (_input.WasJustPressed(Keys.Down) || _input.WasJustPressed(Keys.S))
        {
            scroll++;
        }
        else if (_input.WasJustPressed(Keys.Up) || _input.WasJustPressed(Keys.W))
        {
            scroll--;
        }
        else if (_input.WasJustPressed(Keys.PageDown))
        {
            scroll += AchievementVisibleRows;
        }
        else if (_input.WasJustPressed(Keys.PageUp))
        {
            scroll -= AchievementVisibleRows;
        }

        scroll -= _input.ScrollWheelSteps;

        if (_input.WasMouseLeftJustPressed)
        {
            AchievementsLayout layout = ComputeAchievementsLayout();
            Point mouse = LogicalMousePoint();
            if (layout.BackButton.Contains(mouse))
            {
                CloseAchievements();
                return;
            }

            for (int i = 0; i < layout.Tabs.Length; i++)
            {
                if (layout.Tabs[i].Contains(mouse))
                {
                    tab = i;
                }
            }
        }

        if (tab != _achievementTab)
        {
            _achievementTab = tab;
            scroll = 0;
            _audio.PlayMenuMove();
        }

        scroll = Math.Clamp(scroll, 0, MaxAchievementScroll(_achievementTab));
        if (scroll != _achievementScrollRow)
        {
            _achievementScrollRow = scroll;
            _audio.PlayMenuMove();
        }
    }

    private Point LogicalMousePoint()
    {
        Vector2 mouse = ScreenToLogicalPosition(_input.MousePosition);
        return new Point((int)mouse.X, (int)mouse.Y);
    }

    // ---------- Desenho da página ----------

    private void DrawAchievementsPage()
    {
        DrawMenuBackground();

        float areaWidth = _windowWidth - (2f * TrackMargin);
        float areaHeight = _windowHeight - (2f * TrackMargin);
        AchievementsLayout layout = ComputeAchievementsLayout();
        Point mouse = LogicalMousePoint();

        // Cabeçalho: voltar + título + skins, e o progresso geral à direita.
        DrawSkinArrow(layout.BackButton, pointRight: false, layout.BackButton.Contains(mouse), flashing: false);
        PixelFont.DrawShadowed(_spriteBatch, _pixel, "CONQUISTAS", new Vector2(66f, 12f), 3.5f, AccentColor);
        string skinsLine = $"SKINS DESBLOQUEADAS: {SkinUnlocks.EarnedCount(_saveData)}/{SkinUnlocks.EarnableCount}";
        PixelFont.Draw(_spriteBatch, _pixel, skinsLine, new Vector2(68f, 40f), 1.4f, StatBadgeLabelColor);

        int unlockedCount = Achievements.UnlockedCount(_saveData);
        int totalCount = Achievements.All.Count;
        float fraction = totalCount == 0 ? 0f : unlockedCount / (float)totalCount;

        const float progressWidth = 380f;
        float progressX = areaWidth - 20f - progressWidth;
        string countText = $"{unlockedCount} / {totalCount} CONQUISTAS";
        string percentText = $"{MathF.Floor(fraction * 100f):0}% CONCLUIDO";
        PixelFont.DrawShadowed(_spriteBatch, _pixel, countText, new Vector2(progressX, 12f), 2f, TextColor);
        PixelFont.DrawShadowed(_spriteBatch, _pixel, percentText, new Vector2(areaWidth - 20f - PixelFont.Measure(percentText, 2f), 12f), 2f, RecordColor);
        DrawProgressBar(new Rectangle((int)progressX, 32, (int)progressWidth, 12), fraction, AccentColor, 6f);

        for (int i = 0; i < layout.Tabs.Length; i++)
        {
            DrawAchievementTab(layout.Tabs[i], i, selected: i == _achievementTab, hovered: layout.Tabs[i].Contains(mouse));
        }

        List<Achievement> items = AchievementsInTab(_achievementTab);
        Rectangle list = layout.List;
        float cardWidth = (list.Width - ((AchievementColumns - 1) * AchievementCardGap)) / AchievementColumns;
        int first = _achievementScrollRow * AchievementColumns;
        int last = Math.Min(items.Count, first + (AchievementVisibleRows * AchievementColumns));
        for (int i = first; i < last; i++)
        {
            int row = (i - first) / AchievementColumns;
            int column = (i - first) % AchievementColumns;
            var card = new Rectangle(
                (int)(list.X + (column * (cardWidth + AchievementCardGap))),
                (int)(list.Y + (row * (AchievementCardHeight + AchievementCardGap))),
                (int)cardWidth,
                (int)AchievementCardHeight);
            DrawAchievementCard(items[i], card);
        }

        int maxScroll = MaxAchievementScroll(_achievementTab);
        if (maxScroll > 0)
        {
            Rectangle track = layout.ScrollTrack;
            DrawRoundedRect(track, ProgressTrackColor, 3f);
            int totalRows = maxScroll + AchievementVisibleRows;
            int thumbHeight = Math.Max(24, track.Height * AchievementVisibleRows / totalRows);
            int thumbY = track.Y + ((track.Height - thumbHeight) * _achievementScrollRow / maxScroll);
            DrawRoundedRect(new Rectangle(track.X, thumbY, track.Width, thumbHeight), AccentColor, 3f);
        }

        const string footer = "A/D: CATEGORIA    W/S OU RODA DO MOUSE: ROLAR    ESC: VOLTAR";
        const float footerSize = 1.6f;
        float footerY = MathF.Min(areaHeight - PixelFont.LineHeight(footerSize) - 8f, list.Bottom + 12f);
        PixelFont.Draw(_spriteBatch, _pixel, footer, new Vector2((areaWidth - PixelFont.Measure(footer, footerSize)) / 2f, footerY), footerSize, StatBadgeLabelColor);
    }

    private void DrawAchievementTab(Rectangle rect, int tab, bool selected, bool hovered)
    {
        List<Achievement> items = AchievementsInTab(tab);
        string label = tab == 0 ? "TODAS" : ProgressionStyle.CategoryName(ProgressionStyle.CategoryOrder[tab - 1]);
        Color color = tab == 0 ? AllTabColor : ProgressionStyle.CategoryColor(ProgressionStyle.CategoryOrder[tab - 1]);
        string count = $"{items.Count(achievement => Achievements.IsUnlocked(achievement, _saveData))}/{items.Count}";

        if (selected)
        {
            DrawRoundedRect(rect, color, 7f);
        }
        else
        {
            DrawRoundedRect(rect, hovered ? color : PanelBorderColor, 7f);
            DrawRoundedRect(new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4), PanelFillColor, 6f);
        }

        Color labelColor = selected ? MenuBackground : color;
        Color countColor = selected ? MenuBackground * 0.75f : StatBadgeLabelColor;
        DrawCenteredText(rect, label, rect.Y + 6f, 1.6f, labelColor);
        DrawCenteredText(rect, count, rect.Y + 21f, 1.3f, countColor);
    }

    /// <summary>Cartão de uma conquista. Desbloqueada: ícone colorido, borda na cor da categoria e um "check".
    /// Bloqueada: tudo apagado, cadeado no ícone e a barrinha de progresso (quando dá pra medir). Secreta e
    /// bloqueada: ícone genérico, "CONQUISTA SECRETA" e "???".</summary>
    private void DrawAchievementCard(Achievement achievement, Rectangle rect)
    {
        bool unlocked = Achievements.IsUnlocked(achievement, _saveData);
        bool hiddenSecret = achievement.IsSecret && !unlocked;
        Color categoryColor = ProgressionStyle.CategoryColor(achievement.Category);

        if (unlocked)
        {
            DrawRoundedRect(rect, categoryColor, 8f);
            DrawRoundedRect(new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4), Color.Lerp(PanelFillColor, categoryColor, 0.12f), 7f);
        }
        else
        {
            DrawRoundedRect(rect, LockedCardBorder, 8f);
            DrawRoundedRect(new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4), LockedCardFill, 7f);
        }

        var tile = new Rectangle(rect.X + 7, rect.Y + 7, 48, 48);
        DrawAchievementIconTile(achievement, tile, unlocked);

        if (unlocked)
        {
            DrawCheckBadge(new Vector2(tile.Right - 3f, tile.Y + 3f));
        }
        else
        {
            DrawLock(new Vector2(tile.Right - 4f, tile.Y + 7f), 0.45f, hiddenSecret ? AccentColor : LockedTextColor);
        }

        float textX = rect.X + 64f;
        float textRight = rect.Right - 10f;

        float tagWidth = DrawDifficultyTag(new Vector2(textRight, rect.Y + 10f), achievement.Difficulty, 1.25f, dimmed: !unlocked, alignRight: true);

        string name = hiddenSecret ? "CONQUISTA SECRETA" : achievement.Name;
        float nameSize = FitTextSize(name, textRight - tagWidth - 10f - textX, 1.9f);
        Color nameColor = unlocked ? TextColor : hiddenSecret ? AccentColor * 0.85f : LockedTextColor;
        PixelFont.DrawShadowed(_spriteBatch, _pixel, name, new Vector2(textX, rect.Y + 9f), nameSize, nameColor);

        string description = hiddenSecret ? "???" : achievement.Description;
        const float descriptionSize = 1.35f;
        float y = rect.Y + 28f;
        foreach (string line in WrapText(description, textRight - textX, descriptionSize, maxLines: 2))
        {
            PixelFont.Draw(_spriteBatch, _pixel, line, new Vector2(textX, y), descriptionSize, unlocked ? MutedTextColor : LockedTextColor);
            y += PixelFont.LineHeight(descriptionSize) + 3f;
        }

        if (!unlocked && !hiddenSecret && achievement.Condition.ProgressFraction(_saveData) is { } progress && progress > 0f)
        {
            DrawProgressBar(new Rectangle((int)textX, rect.Bottom - 8, (int)(textRight - textX), 3), progress, categoryColor * 0.8f, 1f);
        }
    }

    /// <summary>Quadrado do ícone: fundo tingido com a cor da categoria (ou cinza, se bloqueada) + desenho.</summary>
    private void DrawAchievementIconTile(Achievement achievement, Rectangle tile, bool unlocked)
    {
        bool hiddenSecret = achievement.IsSecret && !unlocked;
        Color categoryColor = ProgressionStyle.CategoryColor(achievement.Category);

        DrawRoundedRect(tile, unlocked ? categoryColor : LockedTileBorder, 6f);
        var inner = new Rectangle(tile.X + 2, tile.Y + 2, tile.Width - 4, tile.Height - 4);
        DrawRoundedRect(inner, unlocked ? Color.Lerp(new Color(20, 22, 30), categoryColor, 0.28f) : LockedTileFill, 5f);

        AchievementIcon icon = hiddenSecret ? AchievementIcons.Secret
            : unlocked ? achievement.Icon
            : achievement.LockedIcon ?? achievement.Icon;
        _iconRenderer.Draw(icon, inner, colored: unlocked, _visualTime);
    }

    /// <summary>Bolinha verde com "check" pixelizado — marca de conquista obtida.</summary>
    private void DrawCheckBadge(Vector2 center)
    {
        DrawCircle(center, 8f, MenuBackground);
        DrawCircle(center, 6.5f, RecordColor);
        Color ink = new(20, 60, 30);
        var x = (int)center.X;
        var y = (int)center.Y;

        // "✓" em quadradinhos de 2 px: perninha curta descendo e a longa subindo.
        for (int i = 0; i < 3; i++)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(x - 4 + i, y - 1 + i, 2, 2), ink);
        }

        for (int i = 0; i < 5; i++)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(x - 2 + i, y + 1 - i, 2, 2), ink);
        }
    }

    /// <summary>Bolinha colorida + "FACIL"/"MEDIA"/"DIFICIL". Devolve a largura ocupada.</summary>
    private float DrawDifficultyTag(Vector2 anchor, Difficulty difficulty, float size, bool dimmed, bool alignRight = false)
    {
        string label = ProgressionStyle.DifficultyName(difficulty);
        Color color = ProgressionStyle.DifficultyColor(difficulty);
        if (dimmed)
        {
            color = Color.Lerp(color, LockedTextColor, 0.45f);
        }

        float dotRadius = 2.2f * size;
        float gap = 3f * size;
        float textWidth = PixelFont.Measure(label, size);
        float totalWidth = (dotRadius * 2f) + gap + textWidth;
        float x = alignRight ? anchor.X - totalWidth : anchor.X;

        DrawCircle(new Vector2(x + dotRadius, anchor.Y + (PixelFont.LineHeight(size) / 2f)), dotRadius, color);
        PixelFont.Draw(_spriteBatch, _pixel, label, new Vector2(x + (dotRadius * 2f) + gap, anchor.Y), size, color);
        return totalWidth;
    }

    private void DrawProgressBar(Rectangle rect, float fraction, Color fill, float radius)
    {
        DrawRoundedRect(rect, ProgressTrackColor, radius);
        int fillWidth = (int)(rect.Width * Math.Clamp(fraction, 0f, 1f));
        if (fillWidth > 0)
        {
            DrawRoundedRect(new Rectangle(rect.X, rect.Y, Math.Max(fillWidth, (int)(radius * 2f)), rect.Height), fill, radius);
        }
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

    // ---------- Botão na tela inicial ----------

    private Rectangle ComputeAchievementsButton()
    {
        float areaWidth = _windowWidth - (2f * TrackMargin);
        float areaHeight = _windowHeight - (2f * TrackMargin);
        const int width = 206;
        const int height = 46;
        return new Rectangle((int)(areaWidth - 16f - width), (int)(areaHeight - 16f - height), width, height);
    }

    private bool WasAchievementsButtonClicked() =>
        _input.WasMouseLeftJustPressed && ComputeAchievementsButton().Contains(LogicalMousePoint());

    /// <summary>Atalho pra página de conquistas no canto inferior direito da tela inicial (espelhando o selo do
    /// estúdio no outro canto), já mostrando quantas foram obtidas.</summary>
    private void DrawAchievementsButton()
    {
        Rectangle rect = ComputeAchievementsButton();
        bool hovered = rect.Contains(LogicalMousePoint());

        DrawRoundedRect(rect, hovered ? AccentColor : PanelBorderColor, 8f);
        DrawRoundedRect(new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4), PanelFillColor, 7f);

        _iconRenderer.Draw(AchievementIcons.Art(AchievementIcons.Trophy), new Rectangle(rect.X + 6, rect.Y + 6, 34, 34), colored: true, _visualTime);

        float textX = rect.X + 46f;
        PixelFont.Draw(_spriteBatch, _pixel, "C: CONQUISTAS", new Vector2(textX, rect.Y + 9f), 1.75f, AccentColor);

        int unlocked = Achievements.UnlockedCount(_saveData);
        int total = Achievements.All.Count;
        var bar = new Rectangle((int)textX, rect.Y + 27, 88, 8);
        DrawProgressBar(bar, total == 0 ? 0f : unlocked / (float)total, RecordColor, 4f);
        PixelFont.Draw(_spriteBatch, _pixel, $"{unlocked}/{total}", new Vector2(bar.Right + 8f, rect.Y + 26f), 1.4f, StatBadgeLabelColor);
    }

    // ---------- Notificações de desbloqueio ----------

    /// <summary>Aviso que desce do topo da tela, fica alguns segundos e sobe de volta. Conquista: ícone que
    /// "salta", nome e descrição. Skin: a skin girando pra entrar, com brilhinhos em volta, e "AGORA
    /// DISPONIVEL!". Os dois ganham um reflexo de luz passando pelo painel e uma borda que pisca ao chegar.</summary>
    private void DrawUnlockToast()
    {
        if (_activeUnlockToast is not { } toast)
        {
            return;
        }

        const float width = 540f;
        const float height = 92f;
        float elapsed = _unlockToastDuration - _unlockToastTimer;
        float slide = MathF.Min(1f, MathF.Min(elapsed, _unlockToastTimer) / UnlockToastSlideSeconds);
        float eased = 1f - ((1f - slide) * (1f - slide));

        float areaWidth = _windowWidth - (2f * TrackMargin);
        var rect = new Rectangle((int)((areaWidth - width) / 2f), (int)(-height + ((height + 10f) * eased)), (int)width, (int)height);

        Color accent = toast.IsSummary || toast.IsSkin ? AccentColor : ProgressionStyle.CategoryColor(toast.Notice.Achievement.Category);

        // Borda que acende em branco na chegada e volta pra cor de destaque.
        float flash = Math.Clamp(1f - ((elapsed - UnlockToastSlideSeconds) / 0.5f), 0f, 1f);
        DrawRoundedRect(new Rectangle(rect.X - 3, rect.Y - 3, rect.Width + 6, rect.Height + 6), Color.Lerp(accent, Color.White, flash) * (0.55f + (0.45f * flash)), 12f);
        DrawPanel(rect);
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X + 4, rect.Y + 4, rect.Width - 8, 4), accent);

        // Reflexo de luz atravessando o painel logo depois de ele chegar.
        float sweep = (elapsed - UnlockToastSlideSeconds) / 0.7f;
        if (sweep is > 0f and < 1f)
        {
            float sweepX = rect.X + 20f + ((rect.Width - 40f) * sweep);
            DrawFilledRectRotated(new Vector2(sweepX, rect.Center.Y), rect.Height - 12f, 16f, MathF.PI / 2f + 0.22f, Color.White * 0.08f);
            DrawFilledRectRotated(new Vector2(sweepX + 14f, rect.Center.Y), rect.Height - 16f, 5f, MathF.PI / 2f + 0.22f, Color.White * 0.06f);
        }

        float pop = 1f + (0.35f * (1f - Math.Clamp((elapsed - 0.15f) / 0.35f, 0f, 1f)));
        var iconCenter = new Vector2(rect.X + 50f, rect.Center.Y + 3f);
        float textX = rect.X + 96f;
        float textWidth = rect.Right - 18f - textX;

        string title;
        string name;
        string detail;
        Color detailColor;
        string[] titleIcon;

        if (toast.IsSummary)
        {
            title = "VOCE DESBLOQUEOU MUITA COISA!";
            var parts = new List<string>();
            if (toast.SummaryAchievements > 0)
            {
                parts.Add(toast.SummaryAchievements == 1 ? "1 CONQUISTA" : $"{toast.SummaryAchievements} CONQUISTAS");
            }

            if (toast.SummarySkins > 0)
            {
                parts.Add(toast.SummarySkins == 1 ? "1 SKIN" : $"{toast.SummarySkins} SKINS");
            }

            name = string.Join(" + ", parts);
            detail = "VEJA TUDO EM C: CONQUISTAS";
            detailColor = RecordColor;
            titleIcon = AchievementIcons.Star;

            int size = (int)(52f * pop);
            var tile = new Rectangle((int)(iconCenter.X - (size / 2f)), (int)(iconCenter.Y - (size / 2f)), size, size);
            DrawRoundedRect(tile, AccentColor, 6f);
            DrawRoundedRect(new Rectangle(tile.X + 2, tile.Y + 2, tile.Width - 4, tile.Height - 4), Color.Lerp(new Color(20, 22, 30), AccentColor, 0.28f), 5f);
            _iconRenderer.Draw(AchievementIcons.Art(AchievementIcons.Medal), tile, colored: true, _visualTime);
        }
        else if (toast.IsSkin)
        {
            CarSkin skin = toast.Notice.Skin;
            title = "NOVA SKIN DESBLOQUEADA!";
            name = skin.Name;
            detail = "AGORA DISPONIVEL!";
            detailColor = RecordColor;
            titleIcon = AchievementIcons.PaintPalette;

            DrawCircle(iconCenter, 34f, AccentColor * 0.10f);
            DrawToastSparkles(iconCenter, elapsed);

            // Entra girando uma volta inteira e "assenta" balançando de leve.
            float spin = MathF.Max(0f, 1f - (elapsed / 0.7f));
            float angle = (MathF.Sin(_visualTime * 3f) * 0.15f) - (spin * spin * MathF.Tau);
            _carPainter.Begin(iconCenter, angle, 34f * pop, AccentColor, eliminated: false, _visualTime);
            skin.Paint(_carPainter);
        }
        else
        {
            Achievement achievement = toast.Notice.Achievement;
            title = "CONQUISTA DESBLOQUEADA!";
            name = achievement.Name;
            detail = achievement.Description;
            detailColor = StatBadgeLabelColor;
            titleIcon = AchievementIcons.Trophy;

            int size = (int)(56f * pop);
            var tile = new Rectangle((int)(iconCenter.X - (size / 2f)), (int)(iconCenter.Y - (size / 2f)), size, size);
            DrawAchievementIconTile(achievement, tile, unlocked: true);
        }

        _iconRenderer.Draw(AchievementIcons.Art(titleIcon), new Rectangle((int)textX - 3, rect.Y + 11, 20, 20), colored: true, _visualTime);
        PixelFont.DrawShadowed(_spriteBatch, _pixel, title, new Vector2(textX + 20f, rect.Y + 16f), 1.9f, AccentColor);
        PixelFont.DrawShadowed(_spriteBatch, _pixel, name, new Vector2(textX, rect.Y + 38f), FitTextSize(name, textWidth, 2.4f), TextColor);
        PixelFont.Draw(_spriteBatch, _pixel, detail, new Vector2(textX, rect.Y + 64f), FitTextSize(detail, textWidth, 1.6f), detailColor);
    }

    /// <summary>Estrelinhas de 4 pontas girando em volta da skin recém-liberada, piscando defasadas.</summary>
    private void DrawToastSparkles(Vector2 center, float elapsed)
    {
        const int count = 6;
        float appear = Math.Clamp(elapsed / 0.4f, 0f, 1f);
        for (int i = 0; i < count; i++)
        {
            float angle = (_visualTime * 1.6f) + (i * MathF.Tau / count);
            float radius = 30f + (8f * appear) + (MathF.Sin((_visualTime * 5f) + i) * 2f);
            Vector2 position = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle) * 0.8f) * radius;
            float twinkle = (MathF.Sin((_visualTime * 7f) + (i * 1.7f)) + 1f) / 2f;
            float arm = (2f + (3f * twinkle)) * appear;
            Color color = (i % 2 == 0 ? Color.White : AccentColor) * (0.5f + (0.5f * twinkle));
            _spriteBatch.Draw(_pixel, new Rectangle((int)(position.X - arm), (int)position.Y - 1, (int)(arm * 2f), 2), color);
            _spriteBatch.Draw(_pixel, new Rectangle((int)position.X - 1, (int)(position.Y - arm), 2, (int)(arm * 2f)), color);
        }
    }
}
