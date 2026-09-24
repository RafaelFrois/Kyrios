using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Kyrios.Game;

/// <summary>Página de conquistas e notificações de desbloqueio (conquista, skin e pista).</summary>
public sealed partial class GameRoot
{
    private const int AchievementColumns = 3;
    private const int AchievementVisibleRows = 5;
    private const float AchievementCardHeight = 60f;
    private const float AchievementCardGap = 8f;
    private const float AchievementListTop = 104f;

    private static readonly Color AllTabColor = new(225, 228, 238);
    private static readonly Color LockedCardFill = new(22, 25, 34);
    private static readonly Color LockedCardBorder = new(44, 48, 62);
    private static readonly Color LockedTextColor = new(120, 126, 142);
    private static readonly Color MutedTextColor = new(196, 201, 214);

    private State _achievementsReturnState = State.MainMenu;

    /// <summary>Aba da página: 0 = todas, 1.. = cada categoria em <see cref="ProgressionStyle.CategoryOrder"/>.</summary>
    private int _achievementTab;
    private int _achievementScrollRow;

    private void OpenAchievements(State returnState)
    {
        _achievementsReturnState = returnState;
        _achievementScrollRow = 0;
        _state = State.Achievements;
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

    private Rectangle AchievementTabRect(int i)
    {
        const float gap = 6f;
        float width = (AreaWidth - 40f - ((AchievementTabCount - 1) * gap)) / AchievementTabCount;
        return new Rectangle((int)(20f + (i * (width + gap))), 60, (int)width, 34);
    }

    private Rectangle AchievementListRect =>
        new(20, (int)AchievementListTop, (int)(AreaWidth - 56f), (int)((AchievementVisibleRows * (AchievementCardHeight + AchievementCardGap)) - AchievementCardGap));

    private void UpdateAchievementsPage()
    {
        if (_input.Back || _input.WasJustPressed(Keys.C) || WasBackButtonClicked())
        {
            _audio.PlayMenuConfirm();
            _state = _achievementsReturnState;
            return;
        }

        int tab = _achievementTab;
        if (_input.MenuRight || _input.WasJustPressed(Keys.Tab))
        {
            tab = (tab + 1) % AchievementTabCount;
        }
        else if (_input.MenuLeft)
        {
            tab = (tab - 1 + AchievementTabCount) % AchievementTabCount;
        }

        int scroll = _achievementScrollRow;
        if (_input.MenuDown)
        {
            scroll++;
        }
        else if (_input.MenuUp)
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
            Point mouse = LogicalMousePoint();
            for (int i = 0; i < AchievementTabCount; i++)
            {
                if (AchievementTabRect(i).Contains(mouse))
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

    private void DrawAchievementsPage()
    {
        DimScreen(MenuBackgroundDim);

        int unlockedCount = Achievements.UnlockedCount(_saveData);
        int totalCount = Achievements.All.Count;
        int secretsLeft = Achievements.All.Count(a => a.IsSecret && !Achievements.IsUnlocked(a, _saveData));
        DrawScreenHeader("CONQUISTAS", secretsLeft > 0 ? $"{secretsLeft} CONQUISTAS SECRETAS AINDA ESCONDIDAS" : "TODAS AS SECRETAS REVELADAS!");

        float fraction = totalCount == 0 ? 0f : unlockedCount / (float)totalCount;
        const float progressWidth = 380f;
        float progressX = AreaWidth - 20f - progressWidth;
        string countText = $"{unlockedCount} / {totalCount} CONQUISTAS";
        string percentText = $"{MathF.Floor(fraction * 100f):0}% CONCLUIDO";
        PixelFont.DrawShadowed(_spriteBatch, _pixel, countText, new Vector2(progressX, 16f), 1.7f, TextColor);
        PixelFont.DrawShadowed(_spriteBatch, _pixel, percentText, new Vector2(AreaWidth - 20f - PixelFont.Measure(percentText, 1.7f), 16f), 1.7f, RecordColor);
        DrawProgressBar(new Rectangle((int)progressX, 36, (int)progressWidth, 12), fraction, AccentColor, 6f);

        Point mouse = LogicalMousePoint();
        for (int i = 0; i < AchievementTabCount; i++)
        {
            DrawAchievementTab(AchievementTabRect(i), i, selected: i == _achievementTab, hovered: AchievementTabRect(i).Contains(mouse));
        }

        List<Achievement> items = AchievementsInTab(_achievementTab);
        Rectangle list = AchievementListRect;
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
            var track = new Rectangle(list.Right + 8, list.Y, 6, list.Height);
            DrawRoundedRect(track, new Color(38, 42, 56), 3f);
            int totalRows = maxScroll + AchievementVisibleRows;
            int thumbHeight = Math.Max(24, track.Height * AchievementVisibleRows / totalRows);
            int thumbY = track.Y + ((track.Height - thumbHeight) * _achievementScrollRow / maxScroll);
            DrawRoundedRect(new Rectangle(track.X, thumbY, track.Width, thumbHeight), AccentColor, 3f);
        }

        DrawKeyHints(("A/D", "CATEGORIA"), ("W/S", "ROLAR"), ("ESC", "VOLTAR"));
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

        DrawCenteredText(rect, label, rect.Y + 6f, 1.6f, selected ? MenuBackground : color);
        DrawCenteredText(rect, count, rect.Y + 21f, 1.3f, selected ? MenuBackground * 0.75f : StatBadgeLabelColor);
    }

    /// <summary>Cartão de uma conquista. Desbloqueada: ícone colorido, borda na cor da categoria e um "check".
    /// Bloqueada: tudo apagado, cadeado no ícone e barrinha de progresso (quando dá pra medir). Secreta e
    /// bloqueada: ícone "?", "CONQUISTA SECRETA" e "???".</summary>
    private void DrawAchievementCard(Achievement achievement, Rectangle rect)
    {
        bool unlocked = Achievements.IsUnlocked(achievement, _saveData);
        bool hiddenSecret = achievement.IsSecret && !unlocked;
        Color categoryColor = ProgressionStyle.CategoryColor(achievement.Category);

        DrawRoundedRect(rect, unlocked ? categoryColor : LockedCardBorder, 8f);
        DrawRoundedRect(new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4), unlocked ? Color.Lerp(PanelFillColor, categoryColor, 0.12f) : LockedCardFill, 7f);

        var tile = new Rectangle(rect.X + 7, rect.Y + 6, 48, 48);
        AchievementIcon icon = hiddenSecret ? AchievementIcons.Secret : unlocked ? achievement.Icon : achievement.LockedIcon ?? achievement.Icon;
        DrawIconTile(tile, icon, categoryColor, colored: unlocked);
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

        string name = hiddenSecret ? "CONQUISTA SECRETA" : achievement.Name;
        float nameSize = FitTextSize(name, textRight - textX, 1.9f);
        Color nameColor = unlocked ? TextColor : hiddenSecret ? AccentColor * 0.85f : LockedTextColor;
        PixelFont.DrawShadowed(_spriteBatch, _pixel, name, new Vector2(textX, rect.Y + 8f), nameSize, nameColor);

        string description = hiddenSecret ? "???" : achievement.Description;
        const float descriptionSize = 1.4f;
        float y = rect.Y + 26f;
        foreach (string line in WrapText(description, textRight - textX, descriptionSize, maxLines: 2))
        {
            PixelFont.Draw(_spriteBatch, _pixel, line, new Vector2(textX, y), descriptionSize, unlocked ? MutedTextColor : LockedTextColor);
            y += PixelFont.LineHeight(descriptionSize) + 3f;
        }

        if (!unlocked && !hiddenSecret && achievement.Condition.ProgressFraction(_saveData) is { } progress && progress > 0f)
        {
            DrawProgressBar(new Rectangle((int)textX, rect.Bottom - 7, (int)(textRight - textX), 3), progress, categoryColor * 0.8f, 1f);
        }
    }

    // ---------- Notificações de desbloqueio ----------

    /// <summary>Uma notificação na fila: um desbloqueio, ou (quando muita coisa é liberada de uma vez, como
    /// num save de versão anterior) um resumo só.</summary>
    private sealed record UnlockToast(UnlockNotice Notice, int SummaryCount = 0)
    {
        public bool IsSummary => Notice is null;
    }

    private const float ToastDuration = 3.6f;
    private const float QuickToastDuration = 2.4f;
    private const float ToastSlideSeconds = 0.3f;
    private const int ToastSummaryThreshold = 5;

    private readonly Queue<UnlockToast> _pendingUnlockToasts = new();
    private UnlockToast _activeUnlockToast;
    private float _unlockToastTimer;
    private float _unlockToastDuration;

    private void EnqueueUnlockToasts(List<UnlockNotice> notices)
    {
        if (notices.Count >= ToastSummaryThreshold)
        {
            _pendingUnlockToasts.Enqueue(new UnlockToast(null, notices.Count));
            return;
        }

        foreach (UnlockNotice notice in notices)
        {
            _pendingUnlockToasts.Enqueue(new UnlockToast(notice));
        }
    }

    private void UpdateUnlockToast(float frameSeconds)
    {
        if (_activeUnlockToast is null && _pendingUnlockToasts.Count > 0)
        {
            _activeUnlockToast = _pendingUnlockToasts.Dequeue();
            _unlockToastDuration = _pendingUnlockToasts.Count >= 2 ? QuickToastDuration : ToastDuration;
            _unlockToastTimer = _unlockToastDuration;
            _audio?.PlayUnlock(isSkin: _activeUnlockToast.Notice?.Achievement is null);
        }

        if (_activeUnlockToast is null)
        {
            return;
        }

        _unlockToastTimer -= frameSeconds;
        if (_unlockToastTimer <= 0f)
        {
            _activeUnlockToast = null;
        }
    }

    /// <summary>Aviso que desce do topo, fica alguns segundos e sobe de volta. Conquista: ícone que "salta".
    /// Skin: a skin girando pra entrar, com brilhinhos. Pista: o ícone da pista. Todos com borda que acende na
    /// chegada e um reflexo de luz passando.</summary>
    private void DrawUnlockToast()
    {
        if (_activeUnlockToast is not { } toast)
        {
            return;
        }

        const float width = 540f;
        const float height = 92f;
        float elapsed = _unlockToastDuration - _unlockToastTimer;
        float slide = MathF.Min(1f, MathF.Min(elapsed, _unlockToastTimer) / ToastSlideSeconds);
        float eased = 1f - ((1f - slide) * (1f - slide));
        var rect = new Rectangle((int)((AreaWidth - width) / 2f), (int)(-height + ((height + 10f) * eased)), (int)width, (int)height);

        UnlockNotice notice = toast.Notice;
        Color accent = notice?.Achievement is { } a ? ProgressionStyle.CategoryColor(a.Category) : notice?.Track is not null ? new Color(120, 230, 130) : AccentColor;

        float flash = Math.Clamp(1f - ((elapsed - ToastSlideSeconds) / 0.5f), 0f, 1f);
        DrawRoundedRect(InflateRect(rect, 3f, 3f), Color.Lerp(accent, Color.White, flash) * (0.55f + (0.45f * flash)), 12f);
        DrawAccentPanel(rect, accent);

        float sweep = (elapsed - ToastSlideSeconds) / 0.7f;
        if (sweep is > 0f and < 1f)
        {
            float sweepX = rect.X + 20f + ((rect.Width - 40f) * sweep);
            DrawFilledRectRotated(new Vector2(sweepX, rect.Center.Y), rect.Height - 12f, 16f, (MathF.PI / 2f) + 0.22f, Color.White * 0.08f);
        }

        float pop = 1f + (0.35f * (1f - Math.Clamp((elapsed - 0.15f) / 0.35f, 0f, 1f)));
        var iconCenter = new Vector2(rect.X + 50f, rect.Center.Y + 3f);
        float textX = rect.X + 96f;
        float textWidth = rect.Right - 18f - textX;
        int tileSize = (int)(54f * pop);
        var tile = new Rectangle((int)(iconCenter.X - (tileSize / 2f)), (int)(iconCenter.Y - (tileSize / 2f)), tileSize, tileSize);

        string title;
        string name;
        string detail;
        Color detailColor = StatBadgeLabelColor;

        switch (notice)
        {
            case null:
                title = "VOCE DESBLOQUEOU MUITA COISA!";
                name = $"{toast.SummaryCount} NOVIDADES";
                detail = "VEJA EM CONQUISTAS, SKINS E PISTAS";
                detailColor = RecordColor;
                DrawIconTile(tile, AchievementIcons.Art(AchievementIcons.Medal), AccentColor, colored: true);
                break;

            case { Skin: { } skin }:
                title = "NOVA SKIN DESBLOQUEADA!";
                name = skin.Name;
                detail = "AGORA DISPONIVEL!";
                detailColor = RecordColor;
                DrawCircle(iconCenter, 34f, AccentColor * 0.1f);
                DrawToastSparkles(iconCenter, elapsed);
                float spin = MathF.Max(0f, 1f - (elapsed / 0.7f));
                _carPainter.Begin(iconCenter, (MathF.Sin(_visualTime * 3f) * 0.15f) - (spin * spin * MathF.Tau), 34f * pop, AccentColor, eliminated: false, _visualTime);
                skin.Paint(_carPainter);
                break;

            case { Track: { } track }:
                title = "NOVA PISTA DESBLOQUEADA!";
                name = track.Name;
                detail = "AGORA DISPONIVEL!";
                detailColor = RecordColor;
                DrawToastSparkles(iconCenter, elapsed);
                DrawIconTile(tile, AchievementIcons.Art(track.Icon), accent, colored: true);
                break;

            default:
                Achievement achievement = notice.Achievement;
                title = "CONQUISTA DESBLOQUEADA!";
                name = achievement.Name;
                detail = achievement.Description;
                DrawIconTile(tile, achievement.Icon, accent, colored: true);
                break;
        }

        PixelFont.DrawShadowed(_spriteBatch, _pixel, title, new Vector2(textX, rect.Y + 16f), 1.9f, accent);
        PixelFont.DrawShadowed(_spriteBatch, _pixel, name, new Vector2(textX, rect.Y + 38f), FitTextSize(name, textWidth, 2.4f), TextColor);
        PixelFont.Draw(_spriteBatch, _pixel, detail, new Vector2(textX, rect.Y + 64f), FitTextSize(detail, textWidth, 1.6f), detailColor);
    }

    /// <summary>Estrelinhas de 4 pontas girando em volta do item recém-liberado, piscando defasadas.</summary>
    private void DrawToastSparkles(Vector2 center, float elapsed)
    {
        const int count = 6;
        float appear = Math.Clamp(elapsed / 0.4f, 0f, 1f);
        for (int i = 0; i < count; i++)
        {
            float angle = (_visualTime * 1.6f) + (i * MathF.Tau / count);
            float radius = 30f + (8f * appear) + (MathF.Sin((_visualTime * 5f) + i) * 2f);
            Vector2 position = center + (new Vector2(MathF.Cos(angle), MathF.Sin(angle) * 0.8f) * radius);
            float twinkle = (MathF.Sin((_visualTime * 7f) + (i * 1.7f)) + 1f) / 2f;
            float arm = (2f + (3f * twinkle)) * appear;
            Color color = (i % 2 == 0 ? Color.White : AccentColor) * (0.5f + (0.5f * twinkle));
            _spriteBatch.Draw(_pixel, new Rectangle((int)(position.X - arm), (int)position.Y - 1, (int)(arm * 2f), 2), color);
            _spriteBatch.Draw(_pixel, new Rectangle((int)position.X - 1, (int)(position.Y - arm), 2, (int)(arm * 2f)), color);
        }
    }
}
