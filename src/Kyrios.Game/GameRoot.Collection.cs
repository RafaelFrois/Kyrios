using Microsoft.Xna.Framework;

namespace Kyrios.Game;

/// <summary>
/// A tela de coleção — o mesmo componente pra skins e pistas. Esquerda: a vitrine do item olhado (visual grande,
/// nome, categoria, estado e, se bloqueado, a lista do que falta, parte por parte). Direita: a grade com a coleção
/// inteira, pra ver de relance o que já tem e o que falta. Olhar um item nunca troca a escolha: só ENTER (ou clicar
/// de novo no item, ou em EQUIPAR) equipa — e só se estiver liberado.
/// </summary>
public sealed partial class GameRoot
{
    private static readonly Rectangle CollectionStage = new(20, 66, 540, 218);
    private static readonly Rectangle CollectionGridArea = new(590, 62, 578, 384);
    private static readonly Rectangle CollectionEquipButton = new(190, 400, 200, 40);
    private static readonly Color LockedTileFill = new(24, 26, 36);
    private static readonly Color UnlockedTileFill = new(40, 46, 64);
    private static readonly Color MutedLabelColor = new(170, 176, 192);

    private int _collectionHover = -1;

    private bool CollectionIsTracks => _state == State.TrackSelect;

    private int CollectionCount => CollectionIsTracks ? TrackThemes.All.Count : CarSkins.All.Count;

    private int CollectionColumns => CollectionIsTracks ? 7 : 11;

    private int CollectionTileSize => CollectionIsTracks ? 70 : 46;

    private int CollectionTileGap => CollectionIsTracks ? 8 : 6;

    private int CollectionIndex
    {
        get => CollectionIsTracks ? _previewTrackIndex : _previewSkinIndex;
        set
        {
            if (CollectionIsTracks)
            {
                _previewTrackIndex = value;
            }
            else
            {
                _previewSkinIndex = value;
            }
        }
    }

    private bool CollectionUnlocked(int i) => CollectionIsTracks
        ? TrackThemes.IsUnlocked(TrackThemes.All[i], _saveData)
        : SkinUnlocks.IsUnlocked(CarSkins.All[i], _saveData);

    private bool CollectionEquipped(int i) => i == (CollectionIsTracks ? _selectedTrackIndex : _selectedSkinIndex);

    /// <summary>Itens secretos ainda bloqueados não mostram nem nome, nem visual, nem requisito.</summary>
    private bool CollectionHidden(int i) => !CollectionUnlocked(i) && (CollectionIsTracks ? TrackThemes.All[i].IsSecret : CarSkins.All[i].IsSecret);

    private IUnlockable CollectionItem(int i) => CollectionIsTracks ? TrackThemes.All[i] : CarSkins.All[i];

    private void OpenCollection(State state)
    {
        _state = state;
        _previewTrackIndex = _selectedTrackIndex;
        _previewSkinIndex = _selectedSkinIndex;
        _collectionHover = -1;
        _audio.PlayMenuConfirm();
    }

    private Rectangle CollectionTileRect(int i)
    {
        int columns = CollectionColumns;
        int size = CollectionTileSize;
        int gap = CollectionTileGap;
        int rows = (CollectionCount + columns - 1) / columns;
        int gridWidth = (columns * (size + gap)) - gap;
        int gridHeight = (rows * (size + gap)) - gap;
        int left = CollectionGridArea.X + ((CollectionGridArea.Width - gridWidth) / 2);
        int top = CollectionGridArea.Y + Math.Max(0, (CollectionGridArea.Height - 26 - gridHeight) / 2);
        return new Rectangle(left + ((i % columns) * (size + gap)), top + ((i / columns) * (size + gap)), size, size);
    }

    private void UpdateCollection()
    {
        if (_input.Back || WasBackButtonClicked())
        {
            _audio.PlayMenuConfirm();
            _state = State.MainMenu;
            return;
        }

        Point mouse = LogicalMousePoint();
        int hovered = -1;
        for (int i = 0; i < CollectionCount; i++)
        {
            if (CollectionTileRect(i).Contains(mouse))
            {
                hovered = i;
            }
        }

        if (hovered != _collectionHover && hovered >= 0 && _input.MouseMoved)
        {
            _audio.PlayHover();
        }

        _collectionHover = hovered;

        int columns = CollectionColumns;
        int count = CollectionCount;
        if (_input.MenuLeft)
        {
            MoveCollection((CollectionIndex - 1 + count) % count, left: true);
        }
        else if (_input.MenuRight)
        {
            MoveCollection((CollectionIndex + 1) % count, left: false);
        }
        else if (_input.MenuUp && CollectionIndex - columns >= 0)
        {
            MoveCollection(CollectionIndex - columns, left: true);
        }
        else if (_input.MenuDown && CollectionIndex < count - 1)
        {
            MoveCollection(Math.Min(count - 1, CollectionIndex + columns), left: false);
        }

        if (MouseClicked)
        {
            if (hovered >= 0)
            {
                if (hovered == CollectionIndex)
                {
                    ConfirmCollection();
                }
                else
                {
                    CollectionIndex = hovered;
                    _audio.PlayMenuMove();
                }

                return;
            }

            if (CollectionEquipButton.Contains(mouse) || CollectionStage.Contains(mouse))
            {
                ConfirmCollection();
                return;
            }
        }

        if (_input.Confirm)
        {
            ConfirmCollection();
        }
    }

    private void MoveCollection(int index, bool left)
    {
        CollectionIndex = index;
        _audio.PlayMenuMove();
        if (left)
        {
            _arrowFlashLeft = 0.15f;
        }
        else
        {
            _arrowFlashRight = 0.15f;
        }
    }

    /// <summary>Equipa o item olhado (e salva). Bloqueado: balança, avisa e conta a tentativa (tem conquista pra
    /// quem insiste...).</summary>
    private void ConfirmCollection()
    {
        int index = CollectionIndex;
        if (!CollectionUnlocked(index))
        {
            _audio.PlayDeny();
            _denyShake = 0.35f;
            _saveData.DeniedEquips++;
            _saveData.Save();
            CheckUnlocks();
            return;
        }

        if (CollectionEquipped(index))
        {
            _audio.PlayMenuMove();
            return;
        }

        if (CollectionIsTracks)
        {
            _selectedTrackIndex = index;
            _saveData.SelectedTrackId = SelectedTrack.Id;
        }
        else
        {
            _selectedSkinIndex = index;
            _saveData.SelectedSkinId = SelectedSkin.Id;
            _saveData.SkinChanges++;
        }

        _saveData.Save();
        CheckUnlocks();
        _equipFlash = 0.6f;
        _audio.PlayMenuConfirm();
    }

    private void DrawCollection()
    {
        DimScreen(CollectionIsTracks ? Color.Black * 0.5f : MenuBackgroundDim);
        (int earned, int earnable) = CollectionIsTracks
            ? Unlockables.Count(TrackThemes.All, _saveData.UnlockedTrackIds)
            : Unlockables.Count(CarSkins.All, _saveData.UnlockedSkinIds);
        string noun = CollectionIsTracks ? "PISTAS" : "SKINS";
        DrawScreenHeader(noun, $"{earned + 1}/{earnable + 1} {noun} DESBLOQUEADAS");

        DrawCollectionShowcase();
        DrawCollectionGrid();
        DrawKeyHints(("SETAS", "NAVEGAR"), ("ENTER", "EQUIPAR"), ("MOUSE", "CLIQUE 2X PRA EQUIPAR"), ("ESC", "VOLTAR"));
    }

    private void DrawCollectionShowcase()
    {
        int index = CollectionIndex;
        bool unlocked = CollectionUnlocked(index);
        bool hidden = CollectionHidden(index);
        bool equipped = CollectionEquipped(index);
        float shake = _denyShake > 0f ? MathF.Sin(_denyShake * 60f) * 6f * (_denyShake / 0.35f) : 0f;
        Rectangle stage = CollectionStage;
        stage.X += (int)shake;

        Color frame = _equipFlash > 0f ? Color.Lerp(AccentColor, Color.White, _equipFlash) : equipped ? AccentColor : unlocked ? PanelBorderColor : new Color(80, 84, 100);
        DrawRoundedRect(InflateRect(stage, 4f, 4f), frame, 8f);
        if (CollectionIsTracks)
        {
            if (hidden)
            {
                DrawRoundedRect(stage, new Color(18, 20, 30), 6f);
                DrawBigQuestionMark(stage);
            }
            else
            {
                _scenery.DrawPreview(TrackThemes.All[index], stage, unlocked ? Color.White : new Color(70, 70, 82));
            }
        }
        else
        {
            DrawSkinStage(stage, CarSkins.All[index], unlocked, hidden);
        }

        if (!unlocked)
        {
            DrawLock(stage.Center.ToVector2() + new Vector2(0f, 6f), 2.2f, AccentColor);
        }

        string counter = $"{index + 1}/{CollectionCount}";
        PixelFont.Draw(_spriteBatch, _pixel, counter, new Vector2(stage.Right - PixelFont.Measure(counter, 1.4f) - 8f, stage.Bottom - 16f), 1.4f, StatBadgeLabelColor);

        var column = new Rectangle(20, 0, 540, 0);
        string name = hidden ? (CollectionIsTracks ? "PISTA SECRETA" : "SKIN SECRETA") : CollectionIsTracks ? TrackThemes.All[index].Name : CarSkins.All[index].Name;
        DrawCenteredText(column, name, 294f, FitTextSize(name, 520f, 3.2f), unlocked ? TextColor : MutedLabelColor, shadow: true);

        DrawCollectionChips(new Vector2(290f, 336f), index, unlocked, equipped, hidden);

        if (unlocked)
        {
            string line = CollectionIsTracks ? TrackThemes.All[index].Tagline : equipped ? "PRONTA PARA CORRER" : "ENTER PARA EQUIPAR";
            DrawCenteredText(column, line, 364f, FitTextSize(line, 520f, 1.7f), StatBadgeLabelColor);
            DrawButton(CollectionEquipButton, equipped ? "EQUIPADA" : "EQUIPAR", focused: !equipped, primary: !equipped);
        }
        else if (hidden)
        {
            DrawCenteredText(column, "\"???\"", 366f, 1.8f, AccentColor);
            DrawCenteredText(column, "ALGUNS SEGREDOS SO APARECEM PRA QUEM PROCURA", 392f, 1.4f, StatBadgeLabelColor);
        }
        else
        {
            DrawRequirementChecklist(new Rectangle(30, 356, 520, 92), CollectionItem(index).Requirement);
        }
    }

    private void DrawBigQuestionMark(Rectangle area)
    {
        const float size = 9f;
        PixelFont.Draw(_spriteBatch, _pixel, "?", area.Center.ToVector2() - new Vector2(PixelFont.Measure("?", size) / 2f, PixelFont.LineHeight(size) / 2f), size, new Color(80, 86, 104));
    }

    /// <summary>Selos do item: a categoria (skins) e o estado (bloqueada / desbloqueada / equipada).</summary>
    private void DrawCollectionChips(Vector2 center, int index, bool unlocked, bool equipped, bool hidden)
    {
        const float size = 1.6f;
        string status = equipped ? "EQUIPADA" : unlocked ? "DESBLOQUEADA" : "BLOQUEADA";
        Color statusColor = equipped ? AccentColor : unlocked ? RecordColor : MutedLabelColor;
        float statusWidth = PixelFont.Measure(status, size) + 34f;

        string category = null;
        Color categoryColor = Color.White;
        float categoryWidth = 0f;
        if (!CollectionIsTracks && !hidden)
        {
            SkinCategory skinCategory = CarSkins.All[index].Category;
            category = SkinCategories.Singular(skinCategory);
            categoryColor = SkinCategories.Color(skinCategory);
            categoryWidth = PixelFont.Measure(category, size) + 20f;
        }

        const float gap = 10f;
        float total = statusWidth + (category is null ? 0f : categoryWidth + gap);
        float x = center.X - (total / 2f);
        if (category is not null)
        {
            var chip = new Rectangle((int)x, (int)center.Y - 10, (int)categoryWidth, 20);
            DrawRoundedRect(chip, categoryColor * 0.25f, 6f);
            PixelFont.Draw(_spriteBatch, _pixel, category, new Vector2(chip.X + 10f, chip.Y + 5f), size, categoryColor);
            x += categoryWidth + gap;
        }

        var statusChip = new Rectangle((int)x, (int)center.Y - 10, (int)statusWidth, 20);
        DrawRoundedRect(statusChip, statusColor * 0.25f, 6f);
        DrawLock(new Vector2(statusChip.X + 13f, statusChip.Y + 12f), 0.5f, statusColor, open: unlocked);
        PixelFont.Draw(_spriteBatch, _pixel, status, new Vector2(statusChip.X + 26f, statusChip.Y + 5f), size, statusColor);
    }

    /// <summary>O que falta pra liberar. Requisito simples: a frase, a barra e "x/y - faltam N". Combinação: uma
    /// linha por parte, cada uma com o seu "check" ou o seu progresso — dá pra ver exatamente o que já foi feito.</summary>
    private void DrawRequirementChecklist(Rectangle area, UnlockRequirement requirement)
    {
        if (requirement is not AllOfRequirement { Parts.Count: > 1 } combination)
        {
            string text = $"\"{requirement.Description}\"";
            List<string> lines = WrapText(text, area.Width, 1.7f, maxLines: 2);
            float y = area.Y + 4f;
            foreach (string line in lines)
            {
                DrawCenteredText(area, line, y, 1.7f, AccentColor);
                y += PixelFont.LineHeight(1.7f) + 4f;
            }

            if (requirement.ProgressFraction(_saveData) is { } fraction)
            {
                DrawProgressBar(new Rectangle(area.Center.X - 150, (int)y + 6, 300, 8), fraction, AccentColor, 4f);
                y += 20f;
            }

            if (ProgressLabel(requirement) is { } progress)
            {
                DrawCenteredText(area, progress, y + 2f, 1.4f, StatBadgeLabelColor);
            }

            return;
        }

        int done = combination.Parts.Count(part => part.IsMet(_saveData));
        DrawCenteredText(area, $"COMPLETE TUDO ({done}/{combination.Parts.Count}):", area.Y, 1.5f, AccentColor);
        float rowY = area.Y + 18f;
        float rowHeight = MathF.Min(20f, (area.Height - 18f) / combination.Parts.Count);
        foreach (UnlockRequirement part in combination.Parts)
        {
            bool met = part.IsMet(_saveData);
            var row = new Rectangle(area.X, (int)rowY, area.Width, (int)rowHeight);
            if (met)
            {
                DrawCheckBadge(new Vector2(row.X + 8f, row.Y + 7f));
            }
            else
            {
                DrawCircle(new Vector2(row.X + 8f, row.Y + 7f), 6f, PanelBorderColor);
                DrawCircle(new Vector2(row.X + 8f, row.Y + 7f), 4f, PanelFillColor);
            }

            string progress = met ? null : part.ProgressText(_saveData);
            float progressWidth = progress is null ? 0f : PixelFont.Measure(progress, 1.4f) + 10f;
            float textWidth = row.Width - 22f - progressWidth;
            string description = part.Description;
            PixelFont.Draw(_spriteBatch, _pixel, description, new Vector2(row.X + 20f, row.Y + 3f), FitTextSize(description, textWidth, 1.45f), met ? RecordColor : TextColor);
            if (progress is not null)
            {
                PixelFont.Draw(_spriteBatch, _pixel, progress, new Vector2(row.Right - progressWidth + 10f, row.Y + 3f), 1.4f, StatBadgeLabelColor);
            }

            rowY += rowHeight;
        }
    }

    /// <summary>Vitrine da skin: holofote, "chão" e o carro grande girando devagar. Skin secreta bloqueada não
    /// mostra nem o vulto — só um "?".</summary>
    private void DrawSkinStage(Rectangle rect, CarSkin skin, bool unlocked, bool hidden)
    {
        DrawRoundedRect(rect, new Color(18, 20, 30), 6f);
        var center = rect.Center.ToVector2();
        DrawCircle(center + new Vector2(0f, 30f), 96f, AccentColor * 0.05f);
        DrawCircle(center + new Vector2(0f, 20f), 74f, (unlocked ? SkinCategories.Color(skin.Category) : StatBadgeLabelColor) * 0.08f);
        if (hidden)
        {
            DrawBigQuestionMark(rect);
            return;
        }

        _carPainter.Begin(center, _visualTime * 0.6f, 88f, AccentColor, eliminated: false, _visualTime, silhouette: !unlocked);
        skin.Paint(_carPainter);
    }

    /// <summary>A coleção inteira em grade: liberadas coloridas, bloqueadas em vulto com cadeado, secretas com "?",
    /// a equipada com a bolinha verde e a olhada com a borda de destaque.</summary>
    private void DrawCollectionGrid()
    {
        Rectangle area = CollectionGridArea;
        DrawRoundedRect(InflateRect(area, 4f, 4f), PanelBorderColor * 0.6f, 10f);
        DrawRoundedRect(area, new Color(16, 18, 27, 215), 8f);

        for (int i = 0; i < CollectionCount; i++)
        {
            Rectangle rect = CollectionTileRect(i);
            bool unlocked = CollectionUnlocked(i);
            bool current = i == CollectionIndex;
            bool hidden = CollectionHidden(i);
            float pulse = current ? (MathF.Sin(_visualTime * 6f) + 1f) / 2f : 0f;
            Color border = current ? Color.Lerp(AccentColor, Color.White, pulse * 0.4f) : i == _collectionHover ? StatBadgeLabelColor : PanelBorderColor;
            DrawRoundedRect(InflateRect(rect, current ? 3f : 2f, current ? 3f : 2f), border, 6f);
            DrawRoundedRect(rect, unlocked ? UnlockedTileFill : LockedTileFill, 5f);

            if (hidden)
            {
                const float size = 2.4f;
                PixelFont.Draw(_spriteBatch, _pixel, "?", rect.Center.ToVector2() - new Vector2(PixelFont.Measure("?", size) / 2f, PixelFont.LineHeight(size) / 2f), size, new Color(90, 96, 116));
            }
            else if (CollectionIsTracks)
            {
                var inner = new Rectangle(rect.X + 8, rect.Y + 8, rect.Width - 16, rect.Height - 16);
                _iconRenderer.Draw(AchievementIcons.Art(TrackThemes.All[i].Icon), inner, colored: unlocked, _visualTime);
            }
            else
            {
                CarSkin skin = CarSkins.All[i];
                _carPainter.Begin(rect.Center.ToVector2(), -0.5f + (current ? MathF.Sin(_visualTime * 2f) * 0.2f : 0f), rect.Width * 0.36f, AccentColor, eliminated: false, _visualTime, silhouette: !unlocked);
                skin.Paint(_carPainter);
                _spriteBatch.Draw(_pixel, new Rectangle(rect.X + 4, rect.Bottom - 4, rect.Width - 8, 2), SkinCategories.Color(skin.Category) * (unlocked ? 0.8f : 0.3f));
            }

            if (!unlocked)
            {
                DrawLock(new Vector2(rect.Right - 7f, rect.Bottom - 7f), 0.4f, MutedLabelColor);
            }

            if (CollectionEquipped(i))
            {
                DrawCheckBadge(new Vector2(rect.Right - 3f, rect.Y + 3f));
            }
        }

        DrawCollectionLegend(new Rectangle(area.X, area.Bottom - 22, area.Width, 18));
    }

    /// <summary>Rodapé da grade: quanto já tem de cada categoria (skins) ou de cada tipo de desbloqueio (pistas).</summary>
    private void DrawCollectionLegend(Rectangle area)
    {
        if (CollectionIsTracks)
        {
            int found = TrackThemes.All.Count(t => _saveData.FoundTrackSecrets.Contains(t.Id));
            string text = $"SEGREDOS ENCONTRADOS NAS PISTAS: {found}/{TrackThemes.All.Count}";
            DrawCenteredText(area, text, area.Y + 4f, 1.4f, StatBadgeLabelColor);
            return;
        }

        const float size = 1.2f;
        var parts = new List<(string Text, Color Color)>();
        foreach (SkinCategory category in Enum.GetValues<SkinCategory>())
        {
            int total = CarSkins.All.Count(s => s.Category == category);
            int have = CarSkins.All.Count(s => s.Category == category && SkinUnlocks.IsUnlocked(s, _saveData));
            parts.Add(($"{SkinCategories.ShortPlural(category)} {have}/{total}", SkinCategories.Color(category)));
        }

        const float gap = 16f;
        float width = parts.Sum(p => PixelFont.Measure(p.Text, size) + 10f) + (gap * (parts.Count - 1));
        float x = area.X + ((area.Width - width) / 2f);
        foreach ((string text, Color color) in parts)
        {
            _spriteBatch.Draw(_pixel, new Rectangle((int)x, (int)area.Y + 5, 6, 6), color);
            PixelFont.Draw(_spriteBatch, _pixel, text, new Vector2(x + 10f, area.Y + 4f), size, StatBadgeLabelColor);
            x += PixelFont.Measure(text, size) + 10f + gap;
        }
    }
}
