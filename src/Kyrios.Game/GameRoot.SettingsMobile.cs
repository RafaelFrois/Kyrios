using Microsoft.Xna.Framework;

namespace Kyrios.Game;

/// <summary>
/// Configurações no celular: uma página inteira de toque em duas colunas — som, idioma e vibração à esquerda;
/// controles (esquema, tamanho dos botões, aceleração automática) e qualidade gráfica à direita. Cada opção é um
/// botão grande (chips, interruptores, barra de volume arrastável). Teclado/controle continuam navegando
/// (cima/baixo escolhe a linha, esquerda/direita muda o valor, ENTER liga/desliga).
/// </summary>
public sealed partial class GameRoot
{
    private const int MobileSettingsRowHeight = 106;
    private const int MobileSettingsControlHeight = 58;

    private Rectangle MobileSettingsPanel => new(8, -30, (int)AreaWidth - 16, (int)AreaHeight + 56);

    private Rectangle MobileSettingsClose => new(MobileSettingsPanel.Right - 92, MobileSettingsPanel.Y + 14, 78, 62);

    private static readonly SettingsRow[] MobileLeftColumn = [SettingsRow.Music, SettingsRow.Sfx, SettingsRow.Language, SettingsRow.Vibration];
    private static readonly SettingsRow[] MobileRightColumn = [SettingsRow.Controls, SettingsRow.ButtonSize, SettingsRow.AutoAccelerate, SettingsRow.Quality];

    private int MobileColumnWidth => (MobileSettingsPanel.Width - 100) / 2;

    /// <summary>Onde fica a linha (rótulo em cima, controle embaixo) de cada opção.</summary>
    private Rectangle MobileSettingsRowRect(SettingsRow row)
    {
        Rectangle panel = MobileSettingsPanel;
        bool right = Array.IndexOf(MobileRightColumn, row) >= 0;
        SettingsRow[] column = right ? MobileRightColumn : MobileLeftColumn;
        int index = column.Where(IsSettingsRowAvailable).ToList().IndexOf(row);
        int x = panel.X + 30 + (right ? MobileColumnWidth + 40 : 0);
        return new Rectangle(x, panel.Y + 100 + (index * MobileSettingsRowHeight), MobileColumnWidth, MobileSettingsRowHeight - 12);
    }

    private static Rectangle ControlArea(Rectangle row) => new(row.X, row.Y + 32, row.Width, MobileSettingsControlHeight);

    /// <summary>Os botões de escolha de uma linha (idioma, esquema, tamanho, qualidade), com o valor de cada um.</summary>
    private List<(Rectangle Rect, int Value, string Label, bool Enabled)> MobileSettingChips(SettingsRow row)
    {
        (string Label, bool Enabled)[] options = row switch
        {
            SettingsRow.Language => [("PORTUGUES", true), ("ENGLISH", true)],
            SettingsRow.Controls => [(L.T("BOTOES", "BUTTONS"), true), ("JOYSTICK", true), (L.T("INCLINAR", "TILT"), GamePlatform.Current.HasTiltSensor)],
            SettingsRow.ButtonSize => [(L.T("PEQUENO", "SMALL"), true), (L.T("MEDIO", "MEDIUM"), true), (L.T("GRANDE", "LARGE"), true)],
            SettingsRow.Quality => [("AUTO", true), (L.T("BAIXA", "LOW"), true), (L.T("MEDIA", "MEDIUM"), true), (L.T("ALTA", "HIGH"), true)],
            _ => [],
        };

        var chips = new List<(Rectangle, int, string, bool)>();
        if (options.Length == 0)
        {
            return chips;
        }

        Rectangle area = ControlArea(MobileSettingsRowRect(row));
        const int gap = 10;
        int width = (area.Width - ((options.Length - 1) * gap)) / options.Length;
        for (int i = 0; i < options.Length; i++)
        {
            chips.Add((new Rectangle(area.X + (i * (width + gap)), area.Y, width, area.Height), i, options[i].Label, options[i].Enabled));
        }

        return chips;
    }

    private int MobileSettingValue(SettingsRow row) => row switch
    {
        SettingsRow.Language => L.English ? 1 : 0,
        SettingsRow.Controls => (int)MobileSettings.Scheme(_saveData.ControlScheme),
        SettingsRow.ButtonSize => (int)MobileSettings.Size(_saveData.ControlSize),
        SettingsRow.Quality => (int)MobileSettings.Quality(_saveData.GraphicsQuality),
        _ => 0,
    };

    private Rectangle MobileVolumeBar(SettingsRow row)
    {
        Rectangle area = ControlArea(MobileSettingsRowRect(row));
        return new Rectangle(area.X + 14, area.Center.Y - 8, area.Width - 200, 16);
    }

    private Rectangle MobileMuteChip(SettingsRow row)
    {
        Rectangle area = ControlArea(MobileSettingsRowRect(row));
        return new Rectangle(area.Right - 160, area.Y, 160, area.Height);
    }

    private Rectangle MobileSwitch(SettingsRow row)
    {
        Rectangle area = ControlArea(MobileSettingsRowRect(row));
        return new Rectangle(area.X, area.Y + 4, 110, area.Height - 8);
    }

    // ---------- Atualização ----------

    private void UpdateMobileSettings()
    {
        Point point = LogicalMousePoint();
        bool closeTapped = MouseClicked && (InflateRect(MobileSettingsClose, 10f, 10f).Contains(point) || !MobileSettingsPanel.Contains(point));
        if (_input.Back || closeTapped)
        {
            _audio.PlayMenuConfirm();
            _saveData.Save();
            _state = _settingsReturnState;
            _draggingSettingsRow = null;
            return;
        }

        int rowIndex = Array.IndexOf(SettingsRows, _settingsSelection);
        if (rowIndex < 0)
        {
            _settingsSelection = SettingsRows[0];
            rowIndex = 0;
        }

        if (_input.MenuDown)
        {
            _settingsSelection = SettingsRows[(rowIndex + 1) % SettingsRows.Length];
            _audio.PlayMenuMove();
        }
        else if (_input.MenuUp)
        {
            _settingsSelection = SettingsRows[(rowIndex - 1 + SettingsRows.Length) % SettingsRows.Length];
            _audio.PlayMenuMove();
        }
        else if (_input.MenuRight || _input.MenuLeft)
        {
            StepMobileSetting(_settingsSelection, _input.MenuRight ? 1 : -1);
        }
        else if (_input.Confirm)
        {
            ToggleMobileSetting(_settingsSelection);
        }

        if (MouseClicked && _draggingSettingsRow is null)
        {
            foreach (SettingsRow row in SettingsRows)
            {
                if (TryTapMobileSetting(row, point))
                {
                    _settingsSelection = row;
                    return;
                }
            }
        }

        UpdateMobileVolumeDrag(point);
    }

    private bool TryTapMobileSetting(SettingsRow row, Point point)
    {
        switch (row)
        {
            case SettingsRow.Music or SettingsRow.Sfx:
                if (InflateRect(MobileMuteChip(row), 4f, 6f).Contains(point))
                {
                    _settingsSelection = row;
                    ToggleSelectedMute();
                    Haptic(Game.Haptic.Tap);
                    return true;
                }

                return false;

            case SettingsRow.Vibration or SettingsRow.AutoAccelerate:
                Rectangle row_ = MobileSettingsRowRect(row);
                if (new Rectangle(row_.X, row_.Y + 28, 320, MobileSettingsControlHeight + 8).Contains(point))
                {
                    ToggleMobileSetting(row);
                    return true;
                }

                return false;

            default:
                foreach ((Rectangle rect, int value, _, bool enabled) in MobileSettingChips(row))
                {
                    if (InflateRect(rect, 3f, 6f).Contains(point))
                    {
                        if (!enabled)
                        {
                            _audio.PlayDeny();
                            return true;
                        }

                        SetMobileSetting(row, value);
                        return true;
                    }
                }

                return false;
        }
    }

    /// <summary>Arrastar na barra de volume (ou tocar nela) ajusta direto pra posição do dedo e tira do mudo.</summary>
    private void UpdateMobileVolumeDrag(Point point)
    {
        if (_input.IsMouseLeftDown && _draggingSettingsRow is null && _input.WasMouseLeftJustPressed && _stateTime > ClickGuardSeconds)
        {
            foreach (SettingsRow row in new[] { SettingsRow.Music, SettingsRow.Sfx })
            {
                if (InflateRect(MobileVolumeBar(row), 14f, 22f).Contains(point))
                {
                    _draggingSettingsRow = row;
                    _settingsSelection = row;
                }
            }
        }

        if (_draggingSettingsRow is not { } dragging)
        {
            return;
        }

        if (!_input.IsMouseLeftDown)
        {
            _draggingSettingsRow = null;
            _saveData.Save();
            return;
        }

        Rectangle bar = MobileVolumeBar(dragging);
        float fraction = Math.Clamp((ScreenToLogicalPosition(_input.MousePosition).X - bar.X) / MathF.Max(1f, bar.Width), 0f, 1f);
        if (dragging == SettingsRow.Music)
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

    /// <summary>Esquerda/direita no teclado/controle: volume em passos de 10%, escolhas pro lado.</summary>
    private void StepMobileSetting(SettingsRow row, int direction)
    {
        switch (row)
        {
            case SettingsRow.Music or SettingsRow.Sfx:
                AdjustSelectedVolume(direction * 0.1f);
                break;
            case SettingsRow.Vibration or SettingsRow.AutoAccelerate:
                ToggleMobileSetting(row);
                break;
            default:
                List<(Rectangle Rect, int Value, string Label, bool Enabled)> chips = MobileSettingChips(row);
                int value = MobileSettingValue(row);
                for (int i = 0; i < chips.Count; i++)
                {
                    value = (value + direction + chips.Count) % chips.Count;
                    if (chips[value].Enabled)
                    {
                        break;
                    }
                }

                SetMobileSetting(row, value);
                break;
        }
    }

    private void ToggleMobileSetting(SettingsRow row)
    {
        switch (row)
        {
            case SettingsRow.Music or SettingsRow.Sfx:
                ToggleSelectedMute();
                break;
            case SettingsRow.Vibration:
                _saveData.Vibration = !_saveData.Vibration;
                _saveData.Save();
                _audio.PlayMenuConfirm();
                Haptic(Game.Haptic.Tap);
                break;
            case SettingsRow.AutoAccelerate:
                _saveData.AutoAccelerate = !_saveData.AutoAccelerate;
                _saveData.Save();
                _audio.PlayMenuConfirm();
                Haptic(Game.Haptic.Tap);
                break;
            default:
                StepMobileSetting(row, 1);
                break;
        }
    }

    private void SetMobileSetting(SettingsRow row, int value)
    {
        switch (row)
        {
            case SettingsRow.Language:
                SetLanguage(value == 1 ? Language.English : Language.Portuguese);
                break;
            case SettingsRow.Controls:
                _saveData.ControlScheme = MobileSettings.Code((ControlScheme)value);
                break;
            case SettingsRow.ButtonSize:
                _saveData.ControlSize = MobileSettings.Code((ControlSize)value);
                break;
            case SettingsRow.Quality:
                _saveData.GraphicsQuality = MobileSettings.Code((GraphicsQuality)value);
                break;
        }

        _saveData.Save();
        _audio.PlayMenuConfirm();
        Haptic(Game.Haptic.Tap);
    }

    // ---------- Desenho ----------

    private void DrawMobileSettings()
    {
        Rectangle panel = MobileSettingsPanel;
        DrawAccentPanel(panel, AccentColor);
        PixelFont.DrawShadowed(_spriteBatch, _pixel, L.T("CONFIGURACOES", "SETTINGS"), new Vector2(panel.X + 30f, panel.Y + 28f), 4f, AccentColor);

        Rectangle close = MobileSettingsClose;
        DrawButton(close, string.Empty, focused: false);
        DrawFilledRectRotated(close.Center.ToVector2(), 30f, 6f, MathF.PI / 4f, TextColor);
        DrawFilledRectRotated(close.Center.ToVector2(), 30f, 6f, -MathF.PI / 4f, TextColor);

        // Divisória entre as duas colunas.
        int dividerX = panel.X + 30 + MobileColumnWidth + 20;
        _spriteBatch.Draw(_pixel, new Rectangle(dividerX, panel.Y + 100, 2, panel.Height - 124), PanelBorderColor);

        foreach (SettingsRow row in SettingsRows)
        {
            DrawMobileSettingsRow(row);
        }
    }

    private void DrawMobileSettingsRow(SettingsRow row)
    {
        Rectangle rect = MobileSettingsRowRect(row);
        bool selected = row == _settingsSelection && !_input.UsingTouch;
        (string label, string value) = row switch
        {
            SettingsRow.Music => (L.T("TRILHA SONORA", "MUSIC"), _audio.MusicMuted ? L.T("MUDO", "MUTED") : $"{(int)MathF.Round(_audio.MusicVolume * 100f)}%"),
            SettingsRow.Sfx => (L.T("EFEITOS SONOROS", "SOUND EFFECTS"), _audio.SfxMuted ? L.T("MUDO", "MUTED") : $"{(int)MathF.Round(_audio.SfxVolume * 100f)}%"),
            SettingsRow.Language => ("IDIOMA / LANGUAGE", null),
            SettingsRow.Vibration => (L.T("VIBRACAO", "VIBRATION"), null),
            SettingsRow.Controls => (L.T("CONTROLES NA CORRIDA", "RACE CONTROLS"), null),
            SettingsRow.ButtonSize => (L.T("TAMANHO DOS BOTOES", "BUTTON SIZE"), null),
            SettingsRow.AutoAccelerate => (L.T("ACELERAR SOZINHO", "AUTO ACCELERATE"), null),
            _ => (L.T("QUALIDADE GRAFICA", "GRAPHICS QUALITY"), ChosenQuality == GraphicsQuality.Auto ? L.T($"AGORA: {QualityName(EffectiveQuality)}", $"NOW: {QualityName(EffectiveQuality)}") : null),
        };

        if (selected)
        {
            DrawTriangle(new Vector2(rect.X - 12f, rect.Y + 8f), pointRight: true, 6f, AccentColor);
        }

        PixelFont.Draw(_spriteBatch, _pixel, label, new Vector2(rect.X, rect.Y), 2.3f, selected ? AccentColor : TextColor);
        if (value is not null)
        {
            PixelFont.Draw(_spriteBatch, _pixel, value, new Vector2(rect.Right - PixelFont.Measure(value, 2.1f), rect.Y + 2f), 2.1f, StatBadgeLabelColor);
        }

        switch (row)
        {
            case SettingsRow.Music or SettingsRow.Sfx:
                bool music = row == SettingsRow.Music;
                DrawMobileVolume(MobileVolumeBar(row), music ? _audio.MusicVolume : _audio.SfxVolume, music ? _audio.MusicMuted : _audio.SfxMuted);
                bool muted = music ? _audio.MusicMuted : _audio.SfxMuted;
                DrawMobileChip(MobileMuteChip(row), muted ? L.T("MUDO", "MUTED") : L.T("SILENCIAR", "MUTE"), muted, enabled: true, activeColor: DangerColor);
                break;

            case SettingsRow.Vibration or SettingsRow.AutoAccelerate:
                bool on = row == SettingsRow.Vibration ? _saveData.Vibration : _saveData.AutoAccelerate;
                Rectangle toggle = MobileSwitch(row);
                DrawRoundedRect(toggle, on ? RecordColor * 0.85f : new Color(60, 64, 76), toggle.Height / 2f);
                DrawCircle(new Vector2(on ? toggle.Right - (toggle.Height / 2f) : toggle.X + (toggle.Height / 2f), toggle.Center.Y), (toggle.Height / 2f) - 5f, Color.White);
                string state = on ? L.T("LIGADA", "ON") : L.T("DESLIGADA", "OFF");
                PixelFont.Draw(_spriteBatch, _pixel, state, new Vector2(toggle.Right + 18f, toggle.Center.Y - (PixelFont.LineHeight(2.3f) / 2f)), 2.3f, on ? RecordColor : StatBadgeLabelColor);
                if (row == SettingsRow.AutoAccelerate)
                {
                    string hint = L.T("SO VIRAR E FREAR", "JUST STEER AND BRAKE");
                    PixelFont.Draw(_spriteBatch, _pixel, hint, new Vector2(rect.Right - PixelFont.Measure(hint, 1.8f), toggle.Center.Y - 6f), 1.8f, StatBadgeLabelColor);
                }

                break;

            default:
                int current = MobileSettingValue(row);
                foreach ((Rectangle chip, int chipValue, string chipLabel, bool enabled) in MobileSettingChips(row))
                {
                    DrawMobileChip(chip, chipLabel, chipValue == current, enabled, AccentColor);
                }

                break;
        }
    }

    private void DrawMobileVolume(Rectangle bar, float volume, bool muted)
    {
        DrawRoundedRect(InflateRect(bar, 3f, 3f), new Color(10, 12, 18), 8f);
        float fraction = muted ? 0f : Math.Clamp(volume, 0f, 1f);
        if (fraction > 0f)
        {
            DrawRoundedRect(new Rectangle(bar.X, bar.Y, Math.Max(16, (int)(bar.Width * fraction)), bar.Height), BoostFillColor, 7f);
        }

        var knob = new Vector2(bar.X + (bar.Width * fraction), bar.Center.Y);
        DrawCircle(knob, 17f, muted ? new Color(110, 114, 126) : Color.White);
        DrawCircle(knob, 7f, muted ? new Color(60, 64, 76) : BoostFillColor);
    }

    /// <summary>Botão de escolha: aceso na cor de destaque quando escolhido; apagado (e riscado) se não existir no aparelho.</summary>
    private void DrawMobileChip(Rectangle rect, string label, bool active, bool enabled, Color activeColor)
    {
        if (active)
        {
            DrawRoundedRect(rect, activeColor, 10f);
        }
        else
        {
            DrawRoundedRect(rect, PanelBorderColor, 10f);
            DrawRoundedRect(InflateRect(rect, -2f, -2f), enabled ? new Color(40, 45, 62) : new Color(28, 30, 40), 9f);
        }

        float size = FitTextSize(label, rect.Width - 16f, 2.3f);
        Color color = active ? MenuBackground : enabled ? TextColor : new Color(90, 94, 108);
        DrawCenteredText(rect, label, rect.Center.Y - (PixelFont.LineHeight(size) / 2f), size, color);
        if (!enabled)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X + 12, rect.Center.Y, rect.Width - 24, 2), new Color(90, 94, 108));
        }
    }
}
