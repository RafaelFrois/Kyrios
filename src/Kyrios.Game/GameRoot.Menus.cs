using Kyrios.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Kyrios.Game;

/// <summary>Menu principal, seleção de modo e configurações (a coleção de skins/pistas fica em
/// <c>GameRoot.Collection</c>).</summary>
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

    /// <summary>Itens do menu principal. No navegador não existe "fechar o jogo", então o SAIR some.</summary>
    private readonly MainItem[] MainItems = [.. Enum.GetValues<MainItem>().Where(item => item != MainItem.Quit || GamePlatform.Current.CanQuit)];

    /// <summary>Onde fica a galinha de kart no menu principal (clicável...).</summary>
    private static readonly Vector2 MenuLogoCenter = new(885f, 92f);
    private const float MenuLogoScale = 2f;

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

    private static Rectangle MenuLogoRect => new((int)(MenuLogoCenter.X - (48f * MenuLogoScale)), (int)(MenuLogoCenter.Y - (40f * MenuLogoScale)), (int)(96f * MenuLogoScale), (int)(80f * MenuLogoScale));

    private void UpdateMainMenu(float frameSeconds)
    {
        UpdateMenuSecrets(frameSeconds, MenuLogoRect);

        if (_input.MouseMoved)
        {
            Point mouse = LogicalMousePoint();
            for (int i = 0; i < MainItems.Length; i++)
            {
                if (MainButtonRect(MainItems[i]).Contains(mouse) && _mainFocus != i)
                {
                    _mainFocus = i;
                    _audio.PlayHover();
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

        if (_input.Back && GamePlatform.Current.CanQuit)
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

        bool clicked = MouseClicked && MainButtonRect(MainItems[_mainFocus]).Contains(LogicalMousePoint());
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
                OpenCollection(State.SkinSelect);
                break;
            case MainItem.Tracks:
                OpenCollection(State.TrackSelect);
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
                MainItem.Play => (L.T("JOGAR", "PLAY"), null),
                MainItem.Skins => ("SKINS", $"{skinsEarned + 1}/{skinsTotal + 1}"),
                MainItem.Tracks => (L.T("PISTAS", "TRACKS"), $"{tracksEarned + 1}/{tracksTotal + 1}"),
                MainItem.Achievements => (L.T("CONQUISTAS", "ACHIEVEMENTS"), $"{Achievements.UnlockedCount(_saveData)}/{Achievements.All.Count}"),
                MainItem.Settings => (L.T("CONFIGURACOES", "SETTINGS"), null),
                _ => (L.T("SAIR", "QUIT"), null),
            };

            DrawButton(MainButtonRect(item), label, _mainFocus == i, primary: item == MainItem.Play, badge, textSize: item == MainItem.Play ? 3.2f : 2f);
        }

        // O mascote: flutua de leve, pula quando é cutucado.
        float bob = MathF.Sin(_visualTime * 2.2f) * 3f;
        DrawCircle(MenuLogoCenter + new Vector2(0f, 64f), 80f, Color.Black * 0.12f);
        DrawLogo(MenuLogoCenter + new Vector2(0f, bob - (_logoBounce * 14f)), MenuLogoScale, _logoBounce);

        DrawMenuShowcase(new Rectangle(620, 196, 530, 226));
        DrawStudioLogo(new Vector2(20f, AreaHeight - 8f));
        DrawKeyHints((L.T("SETAS", "ARROWS"), L.T("NAVEGAR", "NAVIGATE")), ("ENTER", L.T("CONFIRMAR", "CONFIRM")), ("C", L.T("CONQUISTAS", "ACHIEVEMENTS")), ("Q", L.T("OPCOES", "OPTIONS")), ("ESC", L.T("SAIR", "QUIT")));
    }

    /// <summary>Painel "pronto pra correr": a skin equipada, a pista escolhida e o último modo jogado.</summary>
    private void DrawMenuShowcase(Rectangle panel)
    {
        DrawAccentPanel(panel, AccentColor);
        PixelFont.Draw(_spriteBatch, _pixel, L.T("PRONTO PARA CORRER", "READY TO RACE"), new Vector2(panel.X + 20f, panel.Y + 18f), BodySize, AccentColor);

        var carCenter = new Vector2(panel.X + 120f, panel.Y + 106f);
        DrawCircle(carCenter + new Vector2(0f, 6f), 58f, SkinCategories.Color(SelectedSkin.Category) * 0.08f);
        _carPainter.Begin(carCenter, -0.3f + (MathF.Sin(_visualTime * 1.4f) * 0.12f), 62f, AccentColor, eliminated: false, _visualTime);
        SelectedSkin.Paint(_carPainter);
        var carColumn = new Rectangle(panel.X, 0, 240, 0);
        DrawCenteredText(carColumn, SelectedSkin.Name, panel.Y + 172f, FitTextSize(SelectedSkin.Name, 220f, 2f), TextColor, shadow: true);
        DrawCenteredText(carColumn, SkinCategories.Singular(SelectedSkin.Category), panel.Y + 196f, 1.4f, SkinCategories.Color(SelectedSkin.Category));

        var tile = new Rectangle(panel.X + 270, panel.Y + 58, 64, 64);
        DrawIconTile(tile, AchievementIcons.Art(SelectedTrack.Icon), new Color(120, 230, 130), colored: true);
        PixelFont.DrawShadowed(_spriteBatch, _pixel, SelectedTrack.Name, new Vector2(tile.Right + 14f, tile.Y + 8f), FitTextSize(SelectedTrack.Name, panel.Right - tile.Right - 30f, 2.2f), TextColor);
        PixelFont.Draw(_spriteBatch, _pixel, L.T("PISTA", "TRACK"), new Vector2(tile.Right + 14f, tile.Y + 34f), 1.4f, StatBadgeLabelColor);
        string modeName = _selectedMode == RaceMode.TimeAttack ? L.T("CONTRA O RELOGIO", "TIME ATTACK") : L.T("CORRIDA MORTAL", "DEATH RACE");
        PixelFont.Draw(_spriteBatch, _pixel, L.T("ULTIMO MODO", "LAST MODE"), new Vector2(panel.X + 270f, panel.Y + 140f), 1.4f, StatBadgeLabelColor);
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

        bool clicked = MouseClicked && ModeUnderMouse() == _modeFocus;
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

        string best = _saveData.BestScoreTimeAttack is > 0f and { } score ? L.T($"RECORDE: {score:0} PTS", $"RECORD: {score:0} PTS") : L.T("SEM RECORDE AINDA", "NO RECORD YET");
        string wins = _saveData.EliminationWins == 1 ? L.T("1 VITORIA", "1 WIN") : L.T($"{_saveData.EliminationWins} VITORIAS", $"{_saveData.EliminationWins} WINS");
        DrawModeTitle(leftX, L.T("CORRIDA MORTAL", "DEATH RACE"), L.T("10 CARROS, SO O ULTIMO DE PE VENCE", "10 CARS, ONLY THE LAST ONE STANDING WINS"), wins, DangerColor, _modeFocus == 0);
        DrawModeTitle(rightX, L.T("CONTRA O RELOGIO", "TIME ATTACK"), L.T("CHECKPOINTS DAO TEMPO E PONTOS", "CHECKPOINTS GIVE TIME AND POINTS"), best, BoostFillColor, _modeFocus == 1);

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
        DrawKeyHints((L.T("SETAS", "ARROWS"), L.T("ESCOLHER", "CHOOSE")), ("ENTER", L.T("CORRER", "RACE")), ("ESC", L.T("VOLTAR", "BACK")));
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
            string cta = L.T("ENTER PARA CORRER", "ENTER TO RACE");
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


    // ---------- Configurações ----------

    private const float SettingsPanelWidth = 480f;
    /// <summary>Altura do painel: uma linha a menos quando a tela cheia é do site (versão web).</summary>
    private static float SettingsPanelHeight => GamePlatform.Current.ControlsFullscreen ? 326f : 280f;
    private const float SettingsPanelPaddingV = 26f;
    private const float SettingsHeaderSize = 3f;
    private const float SettingsRowSpacing = 46f;
    private const float SettingsBarWidth = 130f;
    private const float SettingsBarHeight = 14f;
    private const float SettingsPaddingH = 30f;
    private const float SettingsBarValueGap = 10f;
    private const float SettingsValueSlotWidth = 50f;

    private readonly SettingsRow[] SettingsRows = [.. Enum.GetValues<SettingsRow>().Where(row => row != SettingsRow.Fullscreen || GamePlatform.Current.ControlsFullscreen)];

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

        UpdateSettingsMouse();

        int row = Array.IndexOf(SettingsRows, _settingsSelection);
        if (_input.MenuDown)
        {
            _settingsSelection = SettingsRows[(row + 1) % SettingsRows.Length];
            _audio.PlayMenuMove();
        }
        else if (_input.MenuUp)
        {
            _settingsSelection = SettingsRows[(row - 1 + SettingsRows.Length) % SettingsRows.Length];
            _audio.PlayMenuMove();
        }
        else if (_input.MenuRight || _input.MenuLeft)
        {
            if (_settingsSelection == SettingsRow.Fullscreen)
            {
                ToggleFullscreenSetting();
            }
            else if (_settingsSelection == SettingsRow.Language)
            {
                SetLanguage(_input.MenuRight ? Language.English : Language.Portuguese);
            }
            else
            {
                AdjustSelectedVolume(_input.MenuRight ? 0.1f : -0.1f);
            }
        }

        if (_input.Confirm)
        {
            if (_settingsSelection == SettingsRow.Fullscreen)
            {
                ToggleFullscreenSetting();
            }
            else if (_settingsSelection == SettingsRow.Language)
            {
                SetLanguage(L.English ? Language.Portuguese : Language.English);
            }
            else
            {
                ToggleSelectedMute();
            }
        }
    }

    private void ToggleFullscreenSetting()
    {
        ToggleFullscreen();
        _saveData.Windowed = !_isFullscreen;
        _saveData.Save();
        _audio.PlayMenuConfirm();
    }

    /// <summary>Troca o idioma do jogo inteiro na hora (telas, catálogos e placas dos cenários) e salva a escolha.</summary>
    private void SetLanguage(Language language)
    {
        if (L.Current == language)
        {
            return;
        }

        L.Current = language;
        _saveData.Language = L.Code(language);
        _saveData.Save();
        _audio.PlayMenuConfirm();
    }

    /// <summary>Retângulos do painel, das barras de volume e do botão de tela cheia — usados pra desenhar e pro
    /// mouse, então os dois lados sempre concordam sobre onde cada coisa está.</summary>
    private (Rectangle Panel, Rectangle MusicBar, Rectangle SfxBar, Rectangle FullscreenSwitch, Rectangle Portuguese, Rectangle English) ComputeSettingsLayout()
    {
        var panelRect = new Rectangle((int)((AreaWidth - SettingsPanelWidth) / 2f), (int)((AreaHeight - SettingsPanelHeight) / 2f), (int)SettingsPanelWidth, (int)SettingsPanelHeight);
        float rowY = panelRect.Y + SettingsPanelPaddingV + PixelFont.LineHeight(SettingsHeaderSize) + 34f;
        Rectangle music = ComputeVolumeBarRect(panelRect, rowY);
        Rectangle sfx = ComputeVolumeBarRect(panelRect, rowY + SettingsRowSpacing);
        var fullscreen = new Rectangle(music.X, (int)(rowY + (2f * SettingsRowSpacing)) - 3, 60, 20);
        int languageY = (int)(rowY + ((GamePlatform.Current.ControlsFullscreen ? 3f : 2f) * SettingsRowSpacing)) - 4;
        int englishWidth = (int)PixelFont.Measure("ENGLISH", LanguageChipSize) + 18;
        int portugueseWidth = (int)PixelFont.Measure("PORTUGUES", LanguageChipSize) + 18;
        var english = new Rectangle(panelRect.Right - (int)SettingsPaddingH - englishWidth, languageY, englishWidth, 22);
        var portuguese = new Rectangle(english.X - 6 - portugueseWidth, languageY, portugueseWidth, 22);
        return (panelRect, music, sfx, fullscreen, portuguese, english);
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
        (Rectangle panelRect, Rectangle musicBar, Rectangle sfxBar, Rectangle fullscreenSwitch, Rectangle portuguese, Rectangle english) = ComputeSettingsLayout();
        DrawAccentPanel(panelRect, AccentColor);
        DrawCenteredText(panelRect, L.T("CONFIGURACOES", "SETTINGS"), panelRect.Y + SettingsPanelPaddingV, SettingsHeaderSize, AccentColor, shadow: true);

        DrawSettingsRow(panelRect, musicBar, L.T("TRILHA SONORA", "MUSIC"), _audio.MusicVolume, _audio.MusicMuted, _settingsSelection == SettingsRow.Music);
        DrawSettingsRow(panelRect, sfxBar, L.T("EFEITOS SONOROS", "SOUND EFFECTS"), _audio.SfxVolume, _audio.SfxMuted, _settingsSelection == SettingsRow.Sfx);
        if (GamePlatform.Current.ControlsFullscreen)
        {
            DrawFullscreenRow(panelRect, fullscreenSwitch, _settingsSelection == SettingsRow.Fullscreen);
        }

        DrawLanguageRow(panelRect, portuguese, english, _settingsSelection == SettingsRow.Language);

        DrawCenteredText(panelRect, L.T("SETAS OU MOUSE: AJUSTAR    ENTER: MUDO / LIGAR", "ARROWS OR MOUSE: ADJUST    ENTER: MUTE / TOGGLE"), portuguese.Y + 42f, SmallSize, StatBadgeLabelColor);
        string backHint = GamePlatform.Current.ControlsFullscreen ? L.T("F11: TELA CHEIA    ESC: VOLTAR", "F11: FULLSCREEN    ESC: BACK") : L.T("ESC: VOLTAR", "ESC: BACK");
        DrawCenteredText(panelRect, backHint, portuguese.Y + 62f, SmallSize, StatBadgeLabelColor);
    }

    private void DrawSettingsLabel(Rectangle panel, float y, string label, bool selected)
    {
        if (selected)
        {
            DrawTriangle(new Vector2(panel.X + SettingsPaddingH - 8f, y + 7f), pointRight: true, 5f, AccentColor);
        }

        PixelFont.Draw(_spriteBatch, _pixel, label, new Vector2(panel.X + SettingsPaddingH + 4f, y), 2f, selected ? AccentColor : TextColor);
    }

    /// <summary>Uma linha das configurações: rótulo + barra (com manípulo pra arrastar) + porcentagem ou "MUDO".</summary>
    private void DrawSettingsRow(Rectangle panel, Rectangle barRect, string label, float volume, bool muted, bool selected)
    {
        DrawSettingsLabel(panel, barRect.Y, label, selected);

        DrawRoundedRect(InflateRect(barRect, 2f, 2f), new Color(10, 12, 18), 4f);
        float fraction = muted ? 0f : Math.Clamp(volume, 0f, 1f);
        _spriteBatch.Draw(_pixel, new Rectangle(barRect.X, barRect.Y, (int)(barRect.Width * fraction), barRect.Height), muted ? new Color(90, 94, 104) : BoostFillColor);
        if (!muted)
        {
            DrawCircle(new Vector2(barRect.X + (barRect.Width * fraction), barRect.Y + (barRect.Height / 2f)), barRect.Height * 0.6f, Color.White);
        }

        string valueText = muted ? L.T("MUDO", "MUTED") : $"{(int)MathF.Round(volume * 100f)}%";
        float valueWidth = PixelFont.Measure(valueText, 1.75f);
        PixelFont.Draw(_spriteBatch, _pixel, valueText, new Vector2(panel.Right - SettingsPaddingH - valueWidth, barRect.Y), 1.75f, muted ? StatBadgeLabelColor : TextColor);
    }

    /// <summary>Interruptor de tela cheia (liga/desliga, com a bolinha deslizando pro lado).</summary>
    private void DrawFullscreenRow(Rectangle panel, Rectangle toggle, bool selected)
    {
        DrawSettingsLabel(panel, toggle.Y + 3f, L.T("TELA CHEIA", "FULLSCREEN"), selected);
        bool on = _isFullscreen;
        DrawRoundedRect(toggle, on ? RecordColor * 0.8f : new Color(60, 64, 76), 10f);
        DrawCircle(new Vector2(on ? toggle.Right - 10f : toggle.X + 10f, toggle.Center.Y), 8f, Color.White);
        string valueText = on ? L.T("LIGADA", "ON") : L.T("DESLIGADA", "OFF");
        float valueWidth = PixelFont.Measure(valueText, 1.5f);
        PixelFont.Draw(_spriteBatch, _pixel, valueText, new Vector2(panel.Right - SettingsPaddingH - valueWidth, toggle.Y + 4f), 1.5f, on ? RecordColor : StatBadgeLabelColor);
    }

    private const float LanguageChipSize = 1.4f;

    /// <summary>Idioma: o rótulo aparece nas duas línguas (pra achar mesmo sem entender a atual) e as duas opções
    /// ficam lado a lado, com a escolhida acesa.</summary>
    private void DrawLanguageRow(Rectangle panel, Rectangle portuguese, Rectangle english, bool selected)
    {
        DrawSettingsLabel(panel, portuguese.Y + 4f, "IDIOMA / LANGUAGE", selected);
        DrawLanguageChip(portuguese, "PORTUGUES", !L.English);
        DrawLanguageChip(english, "ENGLISH", L.English);
    }

    private void DrawLanguageChip(Rectangle rect, string label, bool active)
    {
        DrawRoundedRect(rect, active ? AccentColor : new Color(46, 50, 62), 6f);
        DrawCenteredText(rect, label, rect.Y + 5f, LanguageChipSize, active ? MenuBackground : StatBadgeLabelColor);
    }

    /// <summary>Clicar/arrastar numa barra de volume ajusta direto pra posição apontada (e tira do mudo); clicar no
    /// interruptor liga/desliga a tela cheia.</summary>
    private void UpdateSettingsMouse()
    {
        (Rectangle _, Rectangle musicBar, Rectangle sfxBar, Rectangle fullscreenSwitch, Rectangle portuguese, Rectangle english) = ComputeSettingsLayout();
        Vector2 mouseLogical = ScreenToLogicalPosition(_input.MousePosition);
        var mousePoint = new Point((int)mouseLogical.X, (int)mouseLogical.Y);

        if (MouseClicked && (portuguese.Contains(mousePoint) || english.Contains(mousePoint)))
        {
            _settingsSelection = SettingsRow.Language;
            SetLanguage(english.Contains(mousePoint) ? Language.English : Language.Portuguese);
            return;
        }

        if (GamePlatform.Current.ControlsFullscreen && MouseClicked && InflateRect(fullscreenSwitch, 6f, 6f).Contains(mousePoint))
        {
            _settingsSelection = SettingsRow.Fullscreen;
            ToggleFullscreenSetting();
            return;
        }

        if (_input.IsMouseLeftDown && _draggingSettingsRow is null && _stateTime > ClickGuardSeconds)
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
