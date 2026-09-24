using Kyrios.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Kyrios.Game;

/// <summary>Menu principal, seleção de modo, seletores de pista e skin (mesmo componente) e configurações.</summary>
public sealed partial class GameRoot
{
    private enum MainItem
    {
        Play,
        Skins,
        Tracks,
        Achievements,
        Settings,
        Quit,
    }

    private static readonly MainItem[] MainItems = Enum.GetValues<MainItem>();

    private int _mainFocus;
    private int _modeFocus;

    private State _settingsReturnState = State.MainMenu;
    private SettingsRow _settingsSelection = SettingsRow.Music;
    private SettingsRow? _draggingSettingsRow;

    // ---------- Menu principal ----------

    private Rectangle MainButtonRect(MainItem item) => item switch
    {
        MainItem.Play => new Rectangle(50, 112, 320, 58),
        MainItem.Quit => new Rectangle(50, 382, 150, 32),
        _ => new Rectangle(50, 184 + (((int)item - 1) * 48), 320, 40),
    };

    private void UpdateMainMenu()
    {
        if (_input.MouseMoved)
        {
            Point mouse = LogicalMousePoint();
            for (int i = 0; i < MainItems.Length; i++)
            {
                if (MainButtonRect(MainItems[i]).Contains(mouse))
                {
                    _mainFocus = i;
                }
            }
        }

        if (_input.MenuDown)
        {
            _mainFocus = (_mainFocus + 1) % MainItems.Length;
            _audio.PlayMenuMove();
        }
        else if (_input.MenuUp)
        {
            _mainFocus = (_mainFocus - 1 + MainItems.Length) % MainItems.Length;
            _audio.PlayMenuMove();
        }

        if (_input.WasJustPressed(Keys.C))
        {
            OpenAchievements(State.MainMenu);
            return;
        }

        if (_input.WasJustPressed(Keys.Q))
        {
            OpenSettings(State.MainMenu);
            return;
        }

        if (_input.Back)
        {
            // ESC leva o foco pro SAIR; um segundo ESC (ou ENTER) sai de verdade.
            if (MainItems[_mainFocus] == MainItem.Quit)
            {
                Exit();
                return;
            }

            _mainFocus = Array.IndexOf(MainItems, MainItem.Quit);
            _audio.PlayMenuMove();
            return;
        }

        bool clicked = _input.WasMouseLeftJustPressed && MainButtonRect(MainItems[_mainFocus]).Contains(LogicalMousePoint());
        if (_input.Confirm || clicked)
        {
            ActivateMainItem(MainItems[_mainFocus]);
        }
    }

    private void ActivateMainItem(MainItem item)
    {
        switch (item)
        {
            case MainItem.Play:
                _modeFocus = _selectedMode == RaceMode.TimeAttack ? 1 : 0;
                _audio.PlayMenuConfirm();
                _state = State.ModeSelect;
                break;
            case MainItem.Skins:
                OpenCarousel(State.SkinSelect);
                break;
            case MainItem.Tracks:
                OpenCarousel(State.TrackSelect);
                break;
            case MainItem.Achievements:
                OpenAchievements(State.MainMenu);
                break;
            case MainItem.Settings:
                OpenSettings(State.MainMenu);
                break;
            case MainItem.Quit:
                Exit();
                break;
        }
    }

    private void DrawMainMenu()
    {
        DimScreen(Color.Black * 0.35f);
        _spriteBatch.Draw(_pixel, new Rectangle(-TrackMargin, -TrackMargin, 460, _windowHeight), MenuBackground * 0.75f);

        DrawGameTitle(new Vector2(50f, 36f), 6.5f);

        (int skinsEarned, int skinsTotal) = Unlockables.Count(CarSkins.All, _saveData.UnlockedSkinIds);
        (int tracksEarned, int tracksTotal) = Unlockables.Count(TrackThemes.All, _saveData.UnlockedTrackIds);
        for (int i = 0; i < MainItems.Length; i++)
        {
            MainItem item = MainItems[i];
            (string label, string badge) = item switch
            {
                MainItem.Play => ("JOGAR", null),
                MainItem.Skins => ("SKINS", $"{skinsEarned + 1}/{skinsTotal + 1}"),
                MainItem.Tracks => ("PISTAS", $"{tracksEarned + 1}/{tracksTotal + 1}"),
                MainItem.Achievements => ("CONQUISTAS", $"{Achievements.UnlockedCount(_saveData)}/{Achievements.All.Count}"),
                MainItem.Settings => ("CONFIGURACOES", null),
                _ => ("SAIR", null),
            };

            DrawButton(MainButtonRect(item), label, _mainFocus == i, primary: item == MainItem.Play, badge, textSize: item == MainItem.Play ? 3.2f : 2f);
        }

        DrawMenuShowcase(new Rectangle(620, 112, 530, 220));
        DrawStudioLogo(new Vector2(20f, AreaHeight - 8f));
        DrawKeyHints(("SETAS", "NAVEGAR"), ("ENTER", "CONFIRMAR"), ("C", "CONQUISTAS"), ("ESC", "SAIR"));
    }

    /// <summary>Painel "pronto pra correr": a skin equipada, a pista escolhida e o último modo jogado.</summary>
    private void DrawMenuShowcase(Rectangle panel)
    {
        DrawAccentPanel(panel, AccentColor);
        PixelFont.Draw(_spriteBatch, _pixel, "PRONTO PARA CORRER", new Vector2(panel.X + 20f, panel.Y + 18f), BodySize, AccentColor);

        var carCenter = new Vector2(panel.X + 120f, panel.Y + 110f);
        DrawCircle(carCenter + new Vector2(0f, 6f), 58f, AccentColor * 0.07f);
        _carPainter.Begin(carCenter, -0.3f + (MathF.Sin(_visualTime * 1.4f) * 0.12f), 62f, AccentColor, eliminated: false, _visualTime);
        SelectedSkin.Paint(_carPainter);
        DrawCenteredText(new Rectangle(panel.X, 0, 240, 0), SelectedSkin.Name, panel.Y + 170f, FitTextSize(SelectedSkin.Name, 220f, 2f), TextColor, shadow: true);
        DrawCenteredText(new Rectangle(panel.X, 0, 240, 0), "SEU CARRO", panel.Y + 192f, 1.4f, StatBadgeLabelColor);

        var tile = new Rectangle(panel.X + 270, panel.Y + 60, 64, 64);
        DrawIconTile(tile, AchievementIcons.Art(SelectedTrack.Icon), new Color(120, 230, 130), colored: true);
        PixelFont.DrawShadowed(_spriteBatch, _pixel, SelectedTrack.Name, new Vector2(tile.Right + 14f, tile.Y + 8f), FitTextSize(SelectedTrack.Name, panel.Right - tile.Right - 30f, 2.2f), TextColor);
        PixelFont.Draw(_spriteBatch, _pixel, "PISTA", new Vector2(tile.Right + 14f, tile.Y + 34f), 1.4f, StatBadgeLabelColor);
        string modeName = _selectedMode == RaceMode.TimeAttack ? "CONTRA O RELOGIO" : "CORRIDA MORTAL";
        PixelFont.Draw(_spriteBatch, _pixel, "ULTIMO MODO", new Vector2(panel.X + 270f, panel.Y + 140f), 1.4f, StatBadgeLabelColor);
        PixelFont.Draw(_spriteBatch, _pixel, modeName, new Vector2(panel.X + 270f, panel.Y + 156f), BodySize, TextColor);
    }

    // ---------- Seleção de modo ----------

    private static readonly Color DeathRaceTint = new(120, 24, 30);
    private static readonly Color TimeAttackTint = new(18, 60, 110);

    /// <summary>A tela é cortada ao meio por uma diagonal: esquerda = Corrida Mortal, direita = Contra o Relógio.</summary>
    private float ModeSplitX(float y) => (AreaWidth / 2f) + ((y - (AreaHeight / 2f)) * -0.22f);

    private int ModeUnderMouse()
    {
        Point mouse = LogicalMousePoint();
        return mouse.Y > 60 && mouse.X < ModeSplitX(mouse.Y) ? 0 : mouse.Y > 60 ? 1 : -1;
    }

    private void UpdateModeSelect()
    {
        if (_input.Back || WasBackButtonClicked())
        {
            _audio.PlayMenuConfirm();
            _state = State.MainMenu;
            return;
        }

        if (_input.MouseMoved && ModeUnderMouse() is >= 0 and var hovered && hovered != _modeFocus)
        {
            _modeFocus = hovered;
            _audio.PlayMenuMove();
        }

        if (_input.MenuLeft || _input.MenuRight || _input.MenuUp || _input.MenuDown)
        {
            _modeFocus = 1 - _modeFocus;
            _audio.PlayMenuMove();
        }

        bool clicked = _input.WasMouseLeftJustPressed && ModeUnderMouse() == _modeFocus;
        if (_input.Confirm || clicked)
        {
            // A pista e a skin já vêm escolhidas do menu: escolheu o modo, a partida começa.
            BeginRace(_modeFocus == 1 ? RaceMode.TimeAttack : RaceMode.Elimination);
        }
    }

    private void DrawModeSelect()
    {
        DimScreen(Color.Black * 0.55f);

        // Metades coloridas separadas por uma faixa diagonal, com listras de velocidade correndo por trás.
        float stripeShift = (_visualTime * 60f) % 36f;
        for (int y = -TrackMargin; y < AreaHeight + TrackMargin; y += 2)
        {
            int split = (int)ModeSplitX(y);
            _spriteBatch.Draw(_pixel, new Rectangle(-TrackMargin, y, split - 4 + TrackMargin, 2), DeathRaceTint * 0.9f);
            _spriteBatch.Draw(_pixel, new Rectangle(split + 4, y, (int)AreaWidth + TrackMargin - split, 2), TimeAttackTint * 0.9f);
            for (float x = -TrackMargin - stripeShift + ((y * 0.22f) % 36f); x < AreaWidth + TrackMargin; x += 36f)
            {
                if (MathF.Abs(x - split) > 10f)
                {
                    _spriteBatch.Draw(_pixel, new Rectangle((int)x, y, 3, 2), Color.White * 0.03f);
                }
            }
        }

        float leftX = AreaWidth * 0.26f;
        float rightX = AreaWidth * 0.74f;
        DrawDeathRaceArt(new Vector2(leftX, 196f));
        DrawTimeAttackArt(new Vector2(rightX, 190f));

        string best = _saveData.BestScoreTimeAttack is > 0f and { } score ? $"RECORDE: {score:0} PTS" : "SEM RECORDE AINDA";
        string wins = _saveData.EliminationWins == 1 ? "1 VITORIA" : $"{_saveData.EliminationWins} VITORIAS";
        DrawModeTitle(leftX, "CORRIDA MORTAL", "10 CARROS, SO O ULTIMO DE PE VENCE", wins, DangerColor, _modeFocus == 0);
        DrawModeTitle(rightX, "CONTRA O RELOGIO", "CHECKPOINTS DAO TEMPO E PONTOS", best, BoostFillColor, _modeFocus == 1);

        // O lado não escolhido apaga; a faixa do meio acende na cor do escolhido.
        Color focusColor = _modeFocus == 0 ? DangerColor : BoostFillColor;
        for (int y = -TrackMargin; y < AreaHeight + TrackMargin; y += 2)
        {
            int split = (int)ModeSplitX(y);
            Rectangle dark = _modeFocus == 0
                ? new Rectangle(split + 4, y, (int)AreaWidth + TrackMargin - split, 2)
                : new Rectangle(-TrackMargin, y, split - 4 + TrackMargin, 2);
            _spriteBatch.Draw(_pixel, dark, Color.Black * 0.55f);
            _spriteBatch.Draw(_pixel, new Rectangle(split - 4, y, 8, 2), focusColor);
        }

        DrawArrowButton(BackButtonRect, pointRight: false, BackButtonRect.Contains(LogicalMousePoint()), flashing: false);
        DrawKeyHints(("SETAS", "ESCOLHER"), ("ENTER", "CORRER"), ("ESC", "VOLTAR"));
    }

    private void DrawModeTitle(float centerX, string title, string line, string stat, Color color, bool selected)
    {
        var area = new Rectangle((int)(centerX - 280f), 0, 560, 0);
        float bounce = selected ? MathF.Sin(_visualTime * 4f) * 2f : 0f;
        DrawCenteredText(area, title, 300f + bounce, 4.2f, selected ? Color.White : StatBadgeLabelColor, shadow: true);
        DrawCenteredText(area, line, 344f, 1.9f, color);
        DrawCenteredText(area, stat, 372f, 1.6f, StatBadgeLabelColor);
        if (selected)
        {
            float pulse = (MathF.Sin(_visualTime * 5f) + 1f) / 2f;
            const string cta = "ENTER PARA CORRER";
            float width = PixelFont.Measure(cta, 1.8f) + 28f;
            var chip = new Rectangle((int)(centerX - (width / 2f)), 402, (int)width, 26);
            DrawRoundedRect(chip, Color.Lerp(color, Color.White, pulse * 0.25f), 6f);
            DrawCenteredText(chip, cta, chip.Y + 7f, 1.8f, MenuBackground);
        }
    }

    /// <summary>Quatro carros correndo lado a lado; de tempos em tempos o último é eliminado.</summary>
    private void DrawDeathRaceArt(Vector2 center)
    {
        float cycle = (_visualTime % 2.6f) / 2.6f;
        bool eliminated = cycle > 0.55f;
        Color[] colors = [AccentColor, AiLooks[0].Color, AiLooks[1].Color, AiLooks[2].Color];
        for (int i = 0; i < 4; i++)
        {
            var carCenter = center + new Vector2(105f - (i * 70f), MathF.Sin((_visualTime * 6f) + i) * 3f);
            bool out_ = i == 3 && eliminated;
            if (!out_)
            {
                for (int s = 0; s < 3; s++)
                {
                    float trail = ((_visualTime * 180f) + (s * 14f) + (i * 9f)) % 40f;
                    _spriteBatch.Draw(_pixel, new Rectangle((int)(carCenter.X - 34f - trail), (int)carCenter.Y - 6 + (s * 6), 12, 2), Color.White * 0.25f);
                }
            }

            CarSkin skin = i == 0 ? SelectedSkin : CarSkins.Default;
            _carPainter.Begin(carCenter + (out_ ? new Vector2(-(cycle - 0.55f) * 60f, 0f) : Vector2.Zero), 0f, 40f, colors[i], out_, _visualTime);
            skin.Paint(_carPainter);
        }

        if (eliminated)
        {
            float pop = MathF.Min(1f, (cycle - 0.55f) / 0.08f);
            var x = center + new Vector2(105f - 210f - ((cycle - 0.55f) * 60f), 0f);
            DrawFilledRectRotated(x, 44f * pop, 7f, 0.8f, DangerColor);
            DrawFilledRectRotated(x, 44f * pop, 7f, -0.8f, DangerColor);
        }
    }

    /// <summary>Cronômetro grande com o ponteiro girando e o "+3.0 S" de checkpoint subindo.</summary>
    private void DrawTimeAttackArt(Vector2 center)
    {
        _spriteBatch.Draw(_pixel, new Rectangle((int)center.X - 9, (int)center.Y - 76, 18, 14), new Color(190, 195, 205));
        _spriteBatch.Draw(_pixel, new Rectangle((int)center.X - 14, (int)center.Y - 82, 28, 7), new Color(150, 155, 168));
        DrawCircle(center, 64f, new Color(150, 155, 168));
        DrawCircle(center, 57f, new Color(240, 242, 248));
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.Tau / 12f;
            DrawFilledRectRotated(center + (new Vector2(MathF.Cos(a), MathF.Sin(a)) * 47f), i % 3 == 0 ? 10f : 6f, 3f, a, new Color(60, 64, 78));
        }

        float hand = (_visualTime * 2.2f) - (MathF.PI / 2f);
        DrawFilledRectRotated(center + (new Vector2(MathF.Cos(hand), MathF.Sin(hand)) * 20f), 42f, 4f, hand, DangerColor);
        DrawCircle(center, 5f, new Color(60, 64, 78));

        float rise = (_visualTime % 1.4f) / 1.4f;
        const string bonus = "+3.0 S";
        PixelFont.DrawShadowed(_spriteBatch, _pixel, bonus, center + new Vector2(66f, -10f - (rise * 40f)), 2.4f, RecordColor * (1f - rise));
    }

    // ---------- Seletor de pista / skin (o mesmo componente) ----------

    private bool CarouselIsTracks => _state == State.TrackSelect;

    private int CarouselCount => CarouselIsTracks ? TrackThemes.All.Count : CarSkins.All.Count;

    private int CarouselIndex
    {
        get => CarouselIsTracks ? _previewTrackIndex : _previewSkinIndex;
        set
        {
            if (CarouselIsTracks)
            {
                _previewTrackIndex = value;
            }
            else
            {
                _previewSkinIndex = value;
            }
        }
    }

    private bool CarouselUnlocked(int i) => CarouselIsTracks
        ? TrackThemes.IsUnlocked(TrackThemes.All[i], _saveData)
        : SkinUnlocks.IsUnlocked(CarSkins.All[i], _saveData);

    private bool CarouselEquipped(int i) => i == (CarouselIsTracks ? _selectedTrackIndex : _selectedSkinIndex);

    private bool CarouselHidden(int i) => !CarouselIsTracks && CarSkins.All[i].IsSecret && !CarouselUnlocked(i);

    private IUnlockable CarouselItem(int i) => CarouselIsTracks ? TrackThemes.All[i] : CarSkins.All[i];

    private static readonly Rectangle CarouselPreview = new(344, 72, 500, 222);
    private static readonly Rectangle CarouselLeftArrow = new(272, 153, 48, 60);
    private static readonly Rectangle CarouselRightArrow = new(868, 153, 48, 60);
    private static readonly Rectangle CarouselConfirmButton = new(930, 356, 220, 40);

    private void OpenCarousel(State state)
    {
        _state = state;
        _previewTrackIndex = _selectedTrackIndex;
        _previewSkinIndex = _selectedSkinIndex;
        _audio.PlayMenuConfirm();
    }

    private Rectangle ThumbnailRect(int i)
    {
        const int size = 30;
        const int gap = 5;
        int total = (CarouselCount * (size + gap)) - gap;
        return new Rectangle((int)((AreaWidth - total) / 2f) + (i * (size + gap)), 410, size, size);
    }

    private void UpdateCarousel()
    {
        if (_input.Back || WasBackButtonClicked())
        {
            _audio.PlayMenuConfirm();
            _state = State.MainMenu;
            return;
        }

        Point mouse = LogicalMousePoint();
        bool click = _input.WasMouseLeftJustPressed;
        if (_input.MenuLeft || (click && CarouselLeftArrow.Contains(mouse)))
        {
            MoveCarousel(-1);
            return;
        }

        if (_input.MenuRight || (click && CarouselRightArrow.Contains(mouse)))
        {
            MoveCarousel(1);
            return;
        }

        if (click)
        {
            for (int i = 0; i < CarouselCount; i++)
            {
                if (ThumbnailRect(i).Contains(mouse))
                {
                    CarouselIndex = i;
                    _audio.PlayMenuMove();
                    return;
                }
            }
        }

        if (_input.Confirm || (click && (CarouselConfirmButton.Contains(mouse) || CarouselPreview.Contains(mouse))))
        {
            ConfirmCarousel();
        }
    }

    private void MoveCarousel(int direction)
    {
        CarouselIndex = (CarouselIndex + direction + CarouselCount) % CarouselCount;
        _audio.PlayMenuMove();
        if (direction < 0)
        {
            _arrowFlashLeft = 0.15f;
        }
        else
        {
            _arrowFlashRight = 0.15f;
        }
    }

    /// <summary>ENTER no seletor: se o item estiver liberado, equipa (e salva). Bloqueado: balança e avisa, sem
    /// trocar nada.</summary>
    private void ConfirmCarousel()
    {
        int index = CarouselIndex;
        if (!CarouselUnlocked(index))
        {
            _audio.PlayDeny();
            _denyShake = 0.35f;
            return;
        }

        if (CarouselIsTracks)
        {
            _selectedTrackIndex = index;
            _saveData.SelectedTrackId = SelectedTrack.Id;
        }
        else
        {
            _selectedSkinIndex = index;
            _saveData.SelectedSkinId = SelectedSkin.Id;
        }

        _saveData.Save();
        _equipFlash = 0.6f;
        _audio.PlayMenuConfirm();
    }

    private void DrawCarousel()
    {
        DimScreen(CarouselIsTracks ? Color.Black * 0.55f : MenuBackgroundDim);
        (int earned, int earnable) = CarouselIsTracks
            ? Unlockables.Count(TrackThemes.All, _saveData.UnlockedTrackIds)
            : Unlockables.Count(CarSkins.All, _saveData.UnlockedSkinIds);
        string noun = CarouselIsTracks ? "PISTAS" : "SKINS";
        DrawScreenHeader(noun, $"{earned + 1}/{earnable + 1} {noun} DESBLOQUEADAS");

        int index = CarouselIndex;
        bool unlocked = CarouselUnlocked(index);
        bool hidden = CarouselHidden(index);
        IUnlockable item = CarouselItem(index);
        float shake = _denyShake > 0f ? MathF.Sin(_denyShake * 60f) * 6f * (_denyShake / 0.35f) : 0f;
        Rectangle preview = CarouselPreview;
        preview.X += (int)shake;

        Color frame = _equipFlash > 0f ? Color.Lerp(AccentColor, Color.White, _equipFlash) : CarouselEquipped(index) ? AccentColor : unlocked ? PanelBorderColor : new Color(80, 84, 100);
        DrawRoundedRect(InflateRect(preview, 4f, 4f), frame, 8f);
        if (CarouselIsTracks)
        {
            TrackTheme track = TrackThemes.All[index];
            _scenery.DrawPreview(track, preview, unlocked ? Color.White : new Color(70, 70, 82));
        }
        else
        {
            DrawSkinStage(preview, CarSkins.All[index], unlocked, hidden);
        }

        if (!unlocked)
        {
            DrawLock(preview.Center.ToVector2() + new Vector2(0f, 6f), 2.2f, AccentColor);
        }

        Point mouse = LogicalMousePoint();
        DrawArrowButton(CarouselLeftArrow, pointRight: false, CarouselLeftArrow.Contains(mouse), _arrowFlashLeft > 0f);
        DrawArrowButton(CarouselRightArrow, pointRight: true, CarouselRightArrow.Contains(mouse), _arrowFlashRight > 0f);
        string counter = $"{index + 1}/{CarouselCount}";
        PixelFont.Draw(_spriteBatch, _pixel, counter, new Vector2(preview.Right - PixelFont.Measure(counter, 1.4f), preview.Bottom + 8f), 1.4f, StatBadgeLabelColor);

        var area = new Rectangle(0, 0, (int)AreaWidth, 0);
        string name = hidden ? "SKIN SECRETA" : CarouselIsTracks ? TrackThemes.All[index].Name : CarSkins.All[index].Name;
        DrawCenteredText(area, name, 304f, 3.2f, unlocked ? TextColor : new Color(170, 176, 192), shadow: true);

        DrawStatusChip(new Vector2(AreaWidth / 2f, 342f), unlocked, CarouselEquipped(index), CarouselIsTracks ? "PISTA" : "SKIN");

        if (unlocked)
        {
            string line = CarouselIsTracks ? TrackThemes.All[index].Tagline : CarouselEquipped(index) ? "PRONTA PARA CORRER" : "ENTER PARA EQUIPAR";
            DrawCenteredText(area, line, 364f, 1.7f, StatBadgeLabelColor);
        }
        else if (hidden)
        {
            DrawCenteredText(area, "\"???\"", 364f, 1.7f, AccentColor);
        }
        else
        {
            DrawCenteredText(area, $"\"{item.Requirement.Description}\"", 362f, FitTextSize($"\"{item.Requirement.Description}\"", 560f, 1.7f), AccentColor);
            if (item.Requirement.ProgressFraction(_saveData) is { } fraction)
            {
                var bar = new Rectangle((int)(AreaWidth / 2f) - 150, 382, 300, 8);
                DrawProgressBar(bar, fraction, AccentColor, 4f);
            }

            if (ProgressLabel(item.Requirement) is { } progress)
            {
                DrawCenteredText(area, progress, 394f, 1.4f, StatBadgeLabelColor);
            }
        }

        if (unlocked)
        {
            DrawButton(CarouselConfirmButton, CarouselEquipped(index) ? "EQUIPADA" : "EQUIPAR", focused: !CarouselEquipped(index));
        }

        DrawThumbnails();
        DrawKeyHints(("SETAS", "TROCAR"), ("ENTER", "EQUIPAR"), ("ESC", "VOLTAR"));
    }

    /// <summary>Vitrine da skin: holofote, "chão" e o carro grande girando devagar. Skin secreta bloqueada não
    /// mostra nem o vulto — só um "?".</summary>
    private void DrawSkinStage(Rectangle rect, CarSkin skin, bool unlocked, bool hidden)
    {
        DrawRoundedRect(rect, new Color(18, 20, 30), 6f);
        var center = rect.Center.ToVector2();
        DrawCircle(center + new Vector2(0f, 30f), 90f, AccentColor * 0.05f);
        DrawCircle(center + new Vector2(0f, 20f), 70f, (unlocked ? AccentColor : StatBadgeLabelColor) * 0.07f);
        if (hidden)
        {
            PixelFont.Draw(_spriteBatch, _pixel, "?", center - new Vector2(PixelFont.Measure("?", 9f) / 2f, PixelFont.LineHeight(9f) / 2f), 9f, new Color(80, 86, 104));
            return;
        }

        _carPainter.Begin(center, _visualTime * 0.6f, 84f, AccentColor, eliminated: false, _visualTime, silhouette: !unlocked);
        skin.Paint(_carPainter);
    }

    /// <summary>Faixa com todos os itens do catálogo: dá pra ver de relance o que já tem e o que falta.</summary>
    private void DrawThumbnails()
    {
        Point mouse = LogicalMousePoint();
        for (int i = 0; i < CarouselCount; i++)
        {
            Rectangle rect = ThumbnailRect(i);
            bool unlocked = CarouselUnlocked(i);
            bool current = i == CarouselIndex;
            DrawRoundedRect(InflateRect(rect, 2f, 2f), current ? AccentColor : rect.Contains(mouse) ? StatBadgeLabelColor : PanelBorderColor, 5f);
            DrawRoundedRect(rect, unlocked ? new Color(40, 46, 64) : new Color(24, 26, 36), 4f);

            if (CarouselIsTracks)
            {
                _iconRenderer.Draw(AchievementIcons.Art(TrackThemes.All[i].Icon), rect, colored: unlocked, _visualTime);
            }
            else if (!CarouselHidden(i))
            {
                _carPainter.Begin(rect.Center.ToVector2(), -0.5f, 17f, AccentColor, eliminated: false, _visualTime, silhouette: !unlocked);
                CarSkins.All[i].Paint(_carPainter);
            }
            else
            {
                PixelFont.Draw(_spriteBatch, _pixel, "?", new Vector2(rect.X + 11f, rect.Y + 8f), 2f, new Color(80, 86, 104));
            }

            if (!unlocked)
            {
                DrawLock(new Vector2(rect.Right - 5f, rect.Bottom - 4f), 0.35f, new Color(170, 176, 192));
            }

            if (CarouselEquipped(i))
            {
                DrawCircle(new Vector2(rect.Right - 3f, rect.Y + 3f), 4f, RecordColor);
            }
        }
    }

    // ---------- Configurações ----------

    private const float SettingsPanelWidth = 480f;
    private const float SettingsPanelPaddingV = 26f;
    private const float SettingsHeaderSize = 3f;
    private const float SettingsRowSpacing = 46f;
    private const float SettingsBarWidth = 130f;
    private const float SettingsBarHeight = 14f;
    private const float SettingsPaddingH = 30f;
    private const float SettingsBarValueGap = 10f;
    private const float SettingsValueSlotWidth = 50f;

    /// <summary>Abre as configurações lembrando de onde vieram (menu ou pausa), pra voltar exatamente pra lá.</summary>
    private void OpenSettings(State returnState)
    {
        _settingsReturnState = returnState;
        _state = State.Settings;
        _audio.PlayMenuConfirm();
    }

    private void UpdateSettings()
    {
        if (_input.Back)
        {
            _audio.PlayMenuConfirm();
            _state = _settingsReturnState;
            _draggingSettingsRow = null;
            return;
        }

        UpdateSettingsMouseDrag();

        if (_input.MenuUp || _input.MenuDown)
        {
            _settingsSelection = _settingsSelection == SettingsRow.Music ? SettingsRow.Sfx : SettingsRow.Music;
            _audio.PlayMenuMove();
        }
        else if (_input.MenuRight)
        {
            AdjustSelectedVolume(0.1f);
        }
        else if (_input.MenuLeft)
        {
            AdjustSelectedVolume(-0.1f);
        }

        if (_input.Confirm)
        {
            ToggleSelectedMute();
        }
    }

    /// <summary>Retângulos do painel e das barras de volume — usados pra desenhar e pro mouse, então os dois
    /// lados sempre concordam sobre onde cada barra está.</summary>
    private (Rectangle Panel, Rectangle MusicBar, Rectangle SfxBar) ComputeSettingsLayout()
    {
        const float panelHeight = 230f;
        var panelRect = new Rectangle((int)((AreaWidth - SettingsPanelWidth) / 2f), (int)((AreaHeight - panelHeight) / 2f), (int)SettingsPanelWidth, (int)panelHeight);
        float rowY = panelRect.Y + SettingsPanelPaddingV + PixelFont.LineHeight(SettingsHeaderSize) + 34f;
        return (panelRect, ComputeVolumeBarRect(panelRect, rowY), ComputeVolumeBarRect(panelRect, rowY + SettingsRowSpacing));
    }

    private static Rectangle ComputeVolumeBarRect(Rectangle panel, float y)
    {
        float barRight = panel.Right - SettingsPaddingH - SettingsValueSlotWidth - SettingsBarValueGap;
        return new Rectangle((int)(barRight - SettingsBarWidth), (int)y, (int)SettingsBarWidth, (int)SettingsBarHeight);
    }

    private void DrawSettingsBackdrop()
    {
        if (_settingsReturnState == State.Paused)
        {
            DrawLiveHud();
        }

        DimScreen(OverlayDimColor);
    }

    private void DrawSettingsPopup()
    {
        (Rectangle panelRect, Rectangle musicBar, Rectangle sfxBar) = ComputeSettingsLayout();
        DrawAccentPanel(panelRect, AccentColor);
        DrawCenteredText(panelRect, "CONFIGURACOES", panelRect.Y + SettingsPanelPaddingV, SettingsHeaderSize, AccentColor, shadow: true);

        DrawSettingsRow(panelRect, musicBar, "TRILHA SONORA", _audio.MusicVolume, _audio.MusicMuted, _settingsSelection == SettingsRow.Music);
        DrawSettingsRow(panelRect, sfxBar, "EFEITOS SONOROS", _audio.SfxVolume, _audio.SfxMuted, _settingsSelection == SettingsRow.Sfx);

        DrawCenteredText(panelRect, "SETAS OU MOUSE: AJUSTAR    ENTER: MUDO", sfxBar.Y + 38f, SmallSize, StatBadgeLabelColor);
        DrawCenteredText(panelRect, "F11: TELA CHEIA    ESC: VOLTAR", sfxBar.Y + 58f, SmallSize, StatBadgeLabelColor);
    }

    /// <summary>Uma linha das configurações: rótulo + barra (com manípulo pra arrastar) + porcentagem ou "MUDO".</summary>
    private void DrawSettingsRow(Rectangle panel, Rectangle barRect, string label, float volume, bool muted, bool selected)
    {
        Color labelColor = selected ? AccentColor : TextColor;
        if (selected)
        {
            DrawTriangle(new Vector2(panel.X + SettingsPaddingH - 8f, barRect.Y + 7f), pointRight: true, 5f, AccentColor);
        }

        PixelFont.Draw(_spriteBatch, _pixel, label, new Vector2(panel.X + SettingsPaddingH + 4f, barRect.Y), 2f, labelColor);

        DrawRoundedRect(InflateRect(barRect, 2f, 2f), new Color(10, 12, 18), 4f);
        float fraction = muted ? 0f : Math.Clamp(volume, 0f, 1f);
        _spriteBatch.Draw(_pixel, new Rectangle(barRect.X, barRect.Y, (int)(barRect.Width * fraction), barRect.Height), muted ? new Color(90, 94, 104) : BoostFillColor);
        if (!muted)
        {
            DrawCircle(new Vector2(barRect.X + (barRect.Width * fraction), barRect.Y + (barRect.Height / 2f)), barRect.Height * 0.6f, Color.White);
        }

        string valueText = muted ? "MUDO" : $"{(int)MathF.Round(volume * 100f)}%";
        float valueWidth = PixelFont.Measure(valueText, 1.75f);
        PixelFont.Draw(_spriteBatch, _pixel, valueText, new Vector2(panel.Right - SettingsPaddingH - valueWidth, barRect.Y), 1.75f, muted ? StatBadgeLabelColor : TextColor);
    }

    /// <summary>Clicar/arrastar numa barra de volume ajusta direto pra posição apontada (e tira do mudo).</summary>
    private void UpdateSettingsMouseDrag()
    {
        (Rectangle _, Rectangle musicBar, Rectangle sfxBar) = ComputeSettingsLayout();
        Vector2 mouseLogical = ScreenToLogicalPosition(_input.MousePosition);
        var mousePoint = new Point((int)mouseLogical.X, (int)mouseLogical.Y);

        if (_input.IsMouseLeftDown && _draggingSettingsRow is null)
        {
            if (InflateRect(musicBar, 6f, 8f).Contains(mousePoint))
            {
                _draggingSettingsRow = SettingsRow.Music;
                _settingsSelection = SettingsRow.Music;
            }
            else if (InflateRect(sfxBar, 6f, 8f).Contains(mousePoint))
            {
                _draggingSettingsRow = SettingsRow.Sfx;
                _settingsSelection = SettingsRow.Sfx;
            }
        }

        if (_draggingSettingsRow is not { } draggingRow)
        {
            return;
        }

        if (!_input.IsMouseLeftDown)
        {
            _draggingSettingsRow = null;
            _saveData.Save();
            return;
        }

        Rectangle bar = draggingRow == SettingsRow.Music ? musicBar : sfxBar;
        float fraction = bar.Width > 0 ? Math.Clamp((mouseLogical.X - bar.X) / bar.Width, 0f, 1f) : 0f;
        if (draggingRow == SettingsRow.Music)
        {
            _audio.SetMusicMuted(false);
            _audio.SetMusicVolume(fraction);
            _saveData.MusicMuted = false;
            _saveData.MusicVolume = fraction;
        }
        else
        {
            _audio.SetSfxMuted(false);
            _audio.SetSfxVolume(fraction);
            _saveData.SfxMuted = false;
            _saveData.SfxVolume = fraction;
        }
    }

    private void AdjustSelectedVolume(float delta)
    {
        if (_settingsSelection == SettingsRow.Music)
        {
            _audio.SetMusicVolume(_audio.MusicVolume + delta);
            _saveData.MusicVolume = _audio.MusicVolume;
        }
        else
        {
            _audio.SetSfxVolume(_audio.SfxVolume + delta);
            _saveData.SfxVolume = _audio.SfxVolume;
        }

        _saveData.Save();
        _audio.PlayMenuMove();
    }

    private void ToggleSelectedMute()
    {
        if (_settingsSelection == SettingsRow.Music)
        {
            _audio.SetMusicMuted(!_audio.MusicMuted);
            _saveData.MusicMuted = _audio.MusicMuted;
        }
        else
        {
            _audio.SetSfxMuted(!_audio.SfxMuted);
            _saveData.SfxMuted = _audio.SfxMuted;
        }

        _saveData.Save();
        _audio.PlayMenuConfirm();
    }
}
