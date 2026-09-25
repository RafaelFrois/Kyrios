using Kyrios.Core;
using Microsoft.Xna.Framework;

namespace Kyrios.Game;

/// <summary>
/// Controles de toque da corrida (celular/tablet). Aparecem só quando o jogador está usando a tela de toque e fazem
/// exatamente o que o teclado faz: virar (◄ ►), acelerar, frear/dar ré e turbo — mais um botão de pausa na faixa de
/// cima, fora do HUD. As áreas de toque são maiores que os desenhos (dedo não é mira) e o dedo pode deslizar de um
/// lado pro outro no volante sem soltar.
/// </summary>
public sealed partial class GameRoot
{
    private const float TouchButtonRadius = 50f;
    private const float TouchBoostRadius = 40f;

    // Posições no espaço da pista (mesmo sistema do HUD), nos cantos de baixo.
    private Vector2 TouchLeftCenter => new(58f, AreaHeight - 44f);
    private Vector2 TouchRightCenter => new(178f, AreaHeight - 44f);
    private Vector2 TouchGasCenter => new(AreaWidth - 58f, AreaHeight - 44f);
    private Vector2 TouchBrakeCenter => new(AreaWidth - 178f, AreaHeight - 34f);
    private Vector2 TouchBoostCenter => new(AreaWidth - 58f, AreaHeight - 166f);
    private Rectangle TouchPauseRect => new((int)AreaWidth - 44, HudTop, 56, HudPanelHeight);

    private bool ShowTouchControls => _input.UsingTouch;

    private bool _touchLeft;
    private bool _touchRight;
    private bool _touchGas;
    private bool _touchBrake;
    private bool _touchBoost;

    /// <summary>Lê os dedos na tela e diz o que cada controle está fazendo neste quadro.</summary>
    private void ReadTouchControls()
    {
        _touchLeft = _touchRight = _touchGas = _touchBrake = _touchBoost = false;
        foreach (Vector2 screen in _input.Touches)
        {
            Vector2 point = ScreenToLogicalPosition(screen.ToPoint());

            // Volante: a metade de baixo do canto esquerdo inteira; o lado do toque decide a direção.
            if (point.X < 250f && point.Y > AreaHeight * 0.5f)
            {
                if (point.X < (TouchLeftCenter.X + TouchRightCenter.X) / 2f)
                {
                    _touchLeft = true;
                }
                else
                {
                    _touchRight = true;
                }

                continue;
            }

            if (Vector2.Distance(point, TouchBoostCenter) < TouchBoostRadius * 1.4f)
            {
                _touchBoost = true;
                _touchGas = true;
            }
            else if (point.X > AreaWidth - 250f && point.Y > AreaHeight * 0.5f)
            {
                if (point.X > (TouchGasCenter.X + TouchBrakeCenter.X) / 2f)
                {
                    _touchGas = true;
                }
                else
                {
                    _touchBrake = true;
                }
            }
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

    /// <summary>Soma os controles de toque ao que veio do teclado/controle (os dois funcionam juntos).</summary>
    private CarInput WithTouchControls(CarInput keyboard)
    {
        ReadTouchControls();
        if (!_touchLeft && !_touchRight && !_touchGas && !_touchBrake && !_touchBoost)
        {
            return keyboard;
        }

        float throttle = keyboard.Throttle + (_touchGas ? 1f : 0f) - (_touchBrake ? 1f : 0f);
        float steering = keyboard.Steering + (_touchRight ? 1f : 0f) - (_touchLeft ? 1f : 0f);
        return new CarInput(throttle, steering, keyboard.Brake, keyboard.Boost || _touchBoost);
    }

    private void DrawTouchControls()
    {
        if (!ShowTouchControls)
        {
            return;
        }

        DrawTouchButton(TouchLeftCenter, TouchButtonRadius, _touchLeft, (center, color) => DrawTriangle(center + new Vector2(-4f, 0f), pointRight: false, 16f, color));
        DrawTouchButton(TouchRightCenter, TouchButtonRadius, _touchRight, (center, color) => DrawTriangle(center + new Vector2(4f, 0f), pointRight: true, 16f, color));
        DrawTouchButton(TouchGasCenter, TouchButtonRadius, _touchGas, (center, color) => DrawTouchLabel(center, L.T("ACEL", "GAS"), color));
        DrawTouchButton(TouchBrakeCenter, TouchButtonRadius * 0.8f, _touchBrake, (center, color) => DrawTouchLabel(center, L.T("FREIO", "BRAKE"), color));
        DrawTouchButton(TouchBoostCenter, TouchBoostRadius, _touchBoost, (center, color) => DrawTouchLabel(center, "TURBO", color), BoostFillColor);

        Rectangle pause = TouchPauseRect;
        DrawRoundedRect(pause, HudFrameFill, 7f);
        _spriteBatch.Draw(_pixel, new Rectangle(pause.Center.X - 8, pause.Y + 8, 5, pause.Height - 16), TextColor);
        _spriteBatch.Draw(_pixel, new Rectangle(pause.Center.X + 3, pause.Y + 8, 5, pause.Height - 16), TextColor);
    }

    private void DrawTouchButton(Vector2 center, float radius, bool pressed, Action<Vector2, Color> drawIcon, Color? accent = null)
    {
        Color ring = accent ?? Color.White;
        DrawCircle(center, radius, Color.Black * (pressed ? 0.45f : 0.28f));
        DrawCircle(center, radius - 3f, (pressed ? ring : Color.White) * (pressed ? 0.35f : 0.12f));
        drawIcon(center, Color.White * (pressed ? 1f : 0.8f));
    }

    private void DrawTouchLabel(Vector2 center, string text, Color color)
    {
        const float size = 1.6f;
        PixelFont.DrawShadowed(_spriteBatch, _pixel, text, center - new Vector2(PixelFont.Measure(text, size) / 2f, PixelFont.LineHeight(size) / 2f), size, color);
    }
}
