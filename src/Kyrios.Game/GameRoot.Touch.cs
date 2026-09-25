using Kyrios.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Kyrios.Game;

/// <summary>
/// Controles de toque da corrida (celular/tablet). Aparecem só quando o jogador está usando a tela de toque e fazem
/// o mesmo que o teclado: virar, acelerar, frear/dar ré e turbo (a lógica fica em <see cref="TouchControls"/>).
/// São desenhados por cima da cena, em pixels da tela e com tamanho físico (dp) — o mesmo tamanho de dedo em
/// qualquer aparelho — dentro da área segura. A pausa fica na faixa de cima, fora do HUD.
/// </summary>
public sealed partial class GameRoot
{
    private const int SoftTextureSize = 128;

    private readonly TouchControls _touchControls = new();
    private TouchLayout _touchLayout;
    private bool _tiltSensorOn;

    /// <summary>Círculo e anel com borda suave (anti-serrilhado), pros controles grandes desenhados em pixels da tela.</summary>
    private Texture2D _softCircle;
    private Texture2D _softRing;

    private bool ShowTouchControls => _input.UsingTouch;

    /// <summary>No celular o jogador escolhe o esquema e o tamanho; na web ficam os botões de sempre. Inclinação só
    /// se o aparelho tiver o sensor.</summary>
    private ControlScheme ActiveScheme
    {
        get
        {
            if (!GamePlatform.Current.IsMobile)
            {
                return ControlScheme.Buttons;
            }

            ControlScheme scheme = MobileSettings.Scheme(_saveData.ControlScheme);
            return scheme == ControlScheme.Tilt && !GamePlatform.Current.HasTiltSensor ? ControlScheme.Buttons : scheme;
        }
    }

    private ControlSize ActiveControlSize => GamePlatform.Current.IsMobile ? MobileSettings.Size(_saveData.ControlSize) : ControlSize.Medium;

    private bool AutoAccelerateOn => GamePlatform.Current.IsMobile && _saveData.AutoAccelerate;

    private TouchLayout CurrentTouchLayout() =>
        TouchLayout.Build(ActiveScheme, ActiveControlSize, SafeScreenRect, GamePlatform.Current.PixelsPerDp);

    /// <summary>Botão de pausa, no canto direito da faixa de cima (espaço lógico, como o HUD).</summary>
    private Rectangle TouchPauseRect => GamePlatform.Current.IsMobile
        ? new Rectangle((int)AreaWidth - 58, HudTop - 2, 70, HudPanelHeight + 4)
        : new Rectangle((int)AreaWidth - 44, HudTop, 56, HudPanelHeight);

    /// <summary>Teclado/controle + toque (e inclinação) → o comando do carro neste quadro.</summary>
    private CarInput ReadCarInput()
    {
        CarInput devices = _input.BuildCarInput();
        if (!ShowTouchControls)
        {
            _touchControls.Reset();
            return devices;
        }

        _touchLayout = CurrentTouchLayout();
        _touchControls.Update(_input.TouchPoints, _touchLayout);
        ControlScheme scheme = ActiveScheme;
        float? tilt = scheme == ControlScheme.Tilt ? GamePlatform.Current.ReadTilt() : null;
        CarInput touch = _touchControls.ToCarInput(scheme, _player.Car.Angle, tilt, AutoAccelerateOn);
        return TouchControls.Combine(devices, touch);
    }

    /// <summary>O sensor de inclinação só fica ligado na corrida com o esquema INCLINAR (economiza bateria).</summary>
    private void UpdateTiltSensor()
    {
        bool wanted = _state == State.Racing && ShowTouchControls && ActiveScheme == ControlScheme.Tilt;
        if (wanted != _tiltSensorOn)
        {
            _tiltSensorOn = wanted;
            GamePlatform.Current.SetTiltSensorEnabled(wanted);
        }
    }

    /// <summary>O toque na pausa (só no quadro em que o dedo encosta).</summary>
    private bool TouchPausePressed()
    {
        if (!ShowTouchControls)
        {
            return false;
        }

        foreach (Vector2 screen in _input.TouchesStarted)
        {
            if (InflateRect(TouchPauseRect, 14f, 14f).Contains(ScreenToLogicalPosition(screen.ToPoint()).ToPoint()))
            {
                return true;
            }
        }

        return false;
    }

    private void DrawTouchPauseButton()
    {
        if (!ShowTouchControls)
        {
            return;
        }

        Rectangle pause = TouchPauseRect;
        DrawRoundedRect(pause, HudFrameFill, 7f);
        int bar = Math.Max(4, pause.Width / 11);
        int barHeight = pause.Height - 16;
        _spriteBatch.Draw(_pixel, new Rectangle(pause.Center.X - bar - 3, pause.Y + 8, bar, barHeight), TextColor);
        _spriteBatch.Draw(_pixel, new Rectangle(pause.Center.X + 3, pause.Y + 8, bar, barHeight), TextColor);
    }

    // ---------- Desenho (pixels da tela) ----------

    private void CreateSoftTextures()
    {
        var circle = new Color[SoftTextureSize * SoftTextureSize];
        var ring = new Color[SoftTextureSize * SoftTextureSize];
        float center = SoftTextureSize / 2f;
        float radius = center - 1f;
        const float ringWidth = 0.075f * SoftTextureSize;
        for (int y = 0; y < SoftTextureSize; y++)
        {
            for (int x = 0; x < SoftTextureSize; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center));
                float outer = Math.Clamp(radius - d + 0.5f, 0f, 1f);
                float inner = Math.Clamp(d - (radius - ringWidth) + 0.5f, 0f, 1f);
                circle[(y * SoftTextureSize) + x] = Color.White * outer;
                ring[(y * SoftTextureSize) + x] = Color.White * (outer * inner);
            }
        }

        _softCircle = new Texture2D(GraphicsDevice, SoftTextureSize, SoftTextureSize);
        _softCircle.SetData(circle);
        _softRing = new Texture2D(GraphicsDevice, SoftTextureSize, SoftTextureSize);
        _softRing.SetData(ring);
    }

    private void DrawSoft(Texture2D texture, Vector2 center, float radius, Color color) =>
        _spriteBatch.Draw(texture, center, null, color, 0f, new Vector2(SoftTextureSize / 2f), radius * 2f / SoftTextureSize, SpriteEffects.None, 0f);

    /// <summary>Os controles da corrida, por cima de tudo (menos o aviso de girar o aparelho).</summary>
    private void DrawTouchControlsOverlay()
    {
        if (_state != State.Racing || !ShowTouchControls || _softCircle is null)
        {
            return;
        }

        TouchLayout layout = _touchLayout ?? CurrentTouchLayout();
        _touchLayout = layout;
        float dp = layout.PixelsPerDp;

        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        switch (layout.Scheme)
        {
            case ControlScheme.Joystick:
                DrawJoystick(layout);
                DrawControlButton(layout.Brake, _touchControls.BrakePressed, dp, (center, color, scale) => DrawControlLabel(center, L.T("FREIO", "BRAKE"), color, scale, dp));
                break;
            case ControlScheme.Tilt:
                DrawTiltWheel(layout, dp);
                DrawControlButton(layout.Brake, _touchControls.BrakePressed, dp, (center, color, scale) => DrawControlLabel(center, L.T("FREIO", "BRAKE"), color, scale, dp));
                DrawControlButton(layout.Gas, _touchControls.GasPressed, dp, (center, color, scale) => DrawControlLabel(center, L.T("ACEL", "GAS"), color, scale, dp));
                break;
            default:
                DrawControlButton(layout.Left, _touchControls.LeftPressed, dp, (center, color, scale) => DrawTriangle(center + new Vector2(-3f * dp, 0f), pointRight: false, 11f * dp * scale, color));
                DrawControlButton(layout.Right, _touchControls.RightPressed, dp, (center, color, scale) => DrawTriangle(center + new Vector2(3f * dp, 0f), pointRight: true, 11f * dp * scale, color));
                DrawControlButton(layout.Brake, _touchControls.BrakePressed, dp, (center, color, scale) => DrawControlLabel(center, L.T("FREIO", "BRAKE"), color, scale, dp));
                DrawControlButton(layout.Gas, _touchControls.GasPressed, dp, (center, color, scale) => DrawControlLabel(center, L.T("ACEL", "GAS"), color, scale, dp));
                break;
        }

        DrawBoostButton(layout.Boost, dp);
        _spriteBatch.End();
    }

    /// <summary>Botão redondo translúcido; apertado, afunda um pouco, acende na cor de destaque e o ícone fica cheio.</summary>
    private void DrawControlButton(TouchButton button, bool pressed, float dp, Action<Vector2, Color, float> drawIcon, Color? accent = null)
    {
        float scale = pressed ? 0.94f : 1f;
        float radius = button.Radius * scale;
        Color ring = accent ?? AccentColor;
        DrawSoft(_softCircle, button.Center + new Vector2(0f, 2f * dp), radius, Color.Black * 0.22f);
        DrawSoft(_softCircle, button.Center, radius, Color.Black * (pressed ? 0.42f : 0.3f));
        DrawSoft(_softCircle, button.Center, radius * 0.9f, pressed ? ring * 0.32f : Color.White * 0.08f);
        DrawSoft(_softRing, button.Center, radius, pressed ? ring * 0.95f : Color.White * 0.38f);
        drawIcon(button.Center, Color.White * (pressed ? 1f : 0.82f), scale);
    }

    private void DrawControlLabel(Vector2 center, string text, Color color, float scale, float dp)
    {
        float size = 1.55f * dp * scale;
        PixelFont.DrawShadowed(_spriteBatch, _pixel, text, center - new Vector2(PixelFont.Measure(text, size) / 2f, PixelFont.LineHeight(size) / 2f), size, color);
    }

    /// <summary>Turbo: o anel em volta mostra o combustível (acende amarelo enquanto queima).</summary>
    private void DrawBoostButton(TouchButton button, float dp)
    {
        DrawControlButton(button, _touchControls.BoostPressed, dp, (center, color, scale) => DrawControlLabel(center, "TURBO", color, scale * 0.9f, dp), BoostFillColor);

        float fuel = Math.Clamp(_player.Car.BoostFuel / MathF.Max(0.01f, _player.Car.Settings.BoostMaxFuel), 0f, 1f);
        const int dots = 32;
        float ringRadius = button.Radius + (6f * dp);
        Color lit = _player.Car.IsBoosting ? BoostActiveColor : BoostFillColor;
        for (int i = 0; i < dots; i++)
        {
            float angle = (i * MathF.Tau / dots) - (MathF.PI / 2f);
            bool on = (i + 0.5f) / dots <= fuel;
            DrawSoft(_softCircle, button.Center + (new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * ringRadius), 2.2f * dp, on ? lit : Color.White * 0.18f);
        }
    }

    /// <summary>Joystick: a base aparece onde o polegar encostou; sem dedo, fica apagada no canto como dica.</summary>
    private void DrawJoystick(TouchLayout layout)
    {
        float radius = layout.JoystickRest.Radius;
        bool active = _touchControls.StickActive;
        Vector2 center = active ? _touchControls.StickCenter : layout.JoystickRest.Center;
        Vector2 knob = active ? _touchControls.StickKnob : center;
        float alpha = active ? 1f : 0.6f;
        float dp = layout.PixelsPerDp;

        DrawSoft(_softCircle, center, radius, Color.Black * (0.28f * alpha));
        DrawSoft(_softRing, center, radius, Color.White * (0.35f * alpha));
        for (int i = 0; i < 4; i++)
        {
            float angle = i * MathF.PI / 2f;
            Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
            DrawFilledRectRotated(center + (direction * radius * 0.78f), 7f * dp, 3f * dp, angle, Color.White * (0.3f * alpha));
        }

        DrawSoft(_softCircle, knob + new Vector2(0f, 2f * dp), radius * 0.46f, Color.Black * 0.25f);
        DrawSoft(_softCircle, knob, radius * 0.44f, (active ? AccentColor : Color.White) * (active ? 0.9f : 0.45f));
        DrawSoft(_softRing, knob, radius * 0.44f, Color.White * (0.6f * alpha));
    }

    /// <summary>Volantinho no meio de baixo que gira junto com a inclinação — mostra que o sensor está respondendo.</summary>
    private void DrawTiltWheel(TouchLayout layout, float dp)
    {
        float tilt = GamePlatform.Current.ReadTilt() ?? 0f;
        var center = new Vector2(layout.Safe.Center.X, layout.Safe.Bottom - (40f * dp));
        float radius = 24f * dp;
        float angle = tilt * MathF.PI / 3f;
        DrawSoft(_softCircle, center, radius, Color.Black * 0.25f);
        DrawSoft(_softRing, center, radius, Color.White * 0.55f);
        DrawFilledRectRotated(center, radius * 1.8f, 3f * dp, angle, Color.White * 0.55f);
        DrawFilledRectRotated(center + new Vector2(MathF.Sin(-angle), MathF.Cos(angle)) * (radius * 0.45f), radius * 0.9f, 3f * dp, angle + (MathF.PI / 2f), Color.White * 0.55f);
        DrawSoft(_softCircle, center, 5f * dp, AccentColor);
    }
}
