using Kyrios.Core;
using Microsoft.Xna.Framework;

namespace Kyrios.Game;

/// <summary>Como a corrida é pilotada na tela de toque.</summary>
public enum ControlScheme
{
    /// <summary>◄ ► à esquerda; acelerar, frear/ré e turbo à direita — igual ao teclado.</summary>
    Buttons,

    /// <summary>Joystick à esquerda: o carro vira pra onde o dedo aponta e acelera conforme o quanto é empurrado.
    /// À direita: frear/ré e turbo.</summary>
    Joystick,

    /// <summary>Inclinar o aparelho como um volante; frear à esquerda, acelerar e turbo à direita.</summary>
    Tilt,
}

/// <summary>Tamanho dos botões de toque (acessibilidade: dedos grandes, telas pequenas).</summary>
public enum ControlSize
{
    Small,
    Medium,
    Large,
}

/// <summary>Um botão redondo da corrida: centro e raio em pixels da tela.</summary>
public readonly record struct TouchButton(Vector2 Center, float Radius)
{
    /// <summary>Distância "normalizada" do dedo: menos de 1 = dentro do desenho.</summary>
    public float Reach(Vector2 point) => Radius <= 0f ? float.MaxValue : Vector2.Distance(point, Center) / Radius;
}

/// <summary>Onde cada controle fica na tela (pixels), calculado a partir da área segura e da densidade — os botões
/// têm o mesmo tamanho físico em qualquer aparelho.</summary>
public sealed record TouchLayout(
    ControlScheme Scheme,
    Rectangle Safe,
    float PixelsPerDp,
    TouchButton Left,
    TouchButton Right,
    TouchButton Gas,
    TouchButton Brake,
    TouchButton Boost,
    TouchButton JoystickRest,
    Rectangle JoystickZone)
{
    public bool HasSteeringButtons => Scheme == ControlScheme.Buttons;

    public bool HasGas => Scheme != ControlScheme.Joystick;

    public bool HasJoystick => Scheme == ControlScheme.Joystick;

    /// <summary>Os botões que existem neste esquema (pra desenhar e pra testar sobreposição).</summary>
    public IEnumerable<TouchButton> Buttons
    {
        get
        {
            if (HasSteeringButtons)
            {
                yield return Left;
                yield return Right;
            }

            if (HasGas)
            {
                yield return Gas;
            }

            yield return Brake;
            yield return Boost;
        }
    }

    /// <summary>Retângulo que cobre os controles de um lado (pra saber o que eles tampam).</summary>
    public Rectangle Bounds(bool rightSide)
    {
        IEnumerable<TouchButton> side = Buttons.Where(button => (button.Center.X > Safe.Center.X) == rightSide);
        if (HasJoystick && !rightSide)
        {
            side = side.Append(JoystickRest);
        }

        Rectangle bounds = Rectangle.Empty;
        foreach (TouchButton button in side)
        {
            var rect = new Rectangle((int)(button.Center.X - button.Radius), (int)(button.Center.Y - button.Radius), (int)(button.Radius * 2f), (int)(button.Radius * 2f));
            bounds = bounds.IsEmpty ? rect : Rectangle.Union(bounds, rect);
        }

        return bounds;
    }

    /// <summary>Monta o layout de um esquema. <paramref name="safe"/> é a área segura em pixels.</summary>
    public static TouchLayout Build(ControlScheme scheme, ControlSize size, Rectangle safe, float pixelsPerDp)
    {
        float dp = MathF.Max(0.5f, pixelsPerDp);
        float factor = size switch
        {
            ControlSize.Small => 0.85f,
            ControlSize.Large => 1.2f,
            _ => 1f,
        };

        // Em telas baixas (pouco mais de 300 dp de altura) os botões encolhem um pouco pra não subirem até o meio.
        float heightDp = safe.Height / dp;
        float fit = Math.Clamp(heightDp / 360f, 0.8f, 1f);
        float radius = 40f * dp * factor * fit;
        float margin = 16f * dp;
        float gap = 12f * dp;

        float bottomY = safe.Bottom - margin - radius;
        float leftX = safe.X + margin + radius;
        float rightX = safe.Right - margin - radius;

        var left = new TouchButton(new Vector2(leftX, bottomY), radius);
        var right = new TouchButton(new Vector2(leftX + (2f * radius) + gap, bottomY), radius);
        var gas = new TouchButton(new Vector2(rightX, bottomY), radius);

        // Nenhum botão fica menor que 48 dp de diâmetro (o mínimo recomendado pra toque), nem no tamanho PEQUENO.
        float minimum = 24f * dp;
        radius = MathF.Max(radius, minimum);
        float brakeRadius = MathF.Max(radius * 0.82f, minimum);
        float boostRadius = MathF.Max(radius * 0.72f, minimum);
        TouchButton brake;
        TouchButton boost;
        switch (scheme)
        {
            case ControlScheme.Joystick:
                // Sem acelerador (o joystick acelera): o turbo desce pro canto e o freio fica ao lado dele.
                boost = new TouchButton(new Vector2(rightX, bottomY), radius * 0.9f);
                brake = new TouchButton(new Vector2(rightX - radius - (gap * 1.5f) - brakeRadius, safe.Bottom - margin - brakeRadius), brakeRadius);
                break;
            case ControlScheme.Tilt:
                // As duas mãos seguram o aparelho: freio embaixo à esquerda, acelerador e turbo à direita.
                brake = new TouchButton(new Vector2(leftX, bottomY), radius);
                boost = new TouchButton(new Vector2(rightX, bottomY - radius - gap - boostRadius), boostRadius);
                break;
            default:
                brake = new TouchButton(new Vector2(rightX - radius - gap - brakeRadius, safe.Bottom - margin - brakeRadius), brakeRadius);
                boost = new TouchButton(new Vector2(rightX, bottomY - radius - gap - boostRadius), boostRadius);
                break;
        }

        float stickRadius = 54f * dp * factor * fit;
        var joystickRest = new TouchButton(new Vector2(safe.X + margin + stickRadius, safe.Bottom - margin - stickRadius), stickRadius);
        int zoneTop = safe.Y + (int)(safe.Height * 0.25f);
        var joystickZone = new Rectangle(safe.X, zoneTop, (int)(safe.Width * 0.45f), safe.Bottom - zoneTop);

        return new TouchLayout(scheme, safe, dp, left, right, gas, brake, boost, joystickRest, joystickZone);
    }
}

/// <summary>
/// Os controles de toque da corrida: recebe os dedos na tela e diz o que cada controle está fazendo. Só lógica (sem
/// desenho) — as telas de corrida de todas as plataformas usam a mesma. As áreas de toque são maiores que os desenhos
/// (dedo não é mira), o dedo pode deslizar de um botão pro vizinho sem soltar e vários dedos funcionam juntos.
/// </summary>
public sealed class TouchControls
{
    /// <summary>Folga de toque: até 1,5x o raio desenhado ainda conta.</summary>
    private const float HitReach = 1.5f;
    private const float JoystickDeadZone = 0.18f;

    /// <summary>Diferença de direção (radianos) a partir da qual o joystick vira com tudo.</summary>
    private const float JoystickFullLockAngle = MathF.PI / 5f;

    private int? _stickFinger;

    public bool LeftPressed { get; private set; }

    public bool RightPressed { get; private set; }

    public bool GasPressed { get; private set; }

    public bool BrakePressed { get; private set; }

    public bool BoostPressed { get; private set; }

    /// <summary>O joystick está sendo usado; centro e posição do dedo (já limitada ao raio), em pixels.</summary>
    public bool StickActive { get; private set; }

    public Vector2 StickCenter { get; private set; }

    public Vector2 StickKnob { get; private set; }

    /// <summary>O quanto o joystick está empurrado (0 a 1, já sem a zona morta) e pra onde (radianos, tela).</summary>
    public float StickAmount { get; private set; }

    public float StickAngle { get; private set; }

    /// <summary>Algum controle está sendo usado neste quadro.</summary>
    public bool AnyActive => LeftPressed || RightPressed || GasPressed || BrakePressed || BoostPressed || StickActive;

    public void Reset()
    {
        LeftPressed = RightPressed = GasPressed = BrakePressed = BoostPressed = StickActive = false;
        StickAmount = 0f;
        _stickFinger = null;
    }

    /// <summary>Lê os dedos (pixels da tela) contra o layout.</summary>
    public void Update(IReadOnlyList<TouchPoint> touches, TouchLayout layout)
    {
        LeftPressed = RightPressed = GasPressed = BrakePressed = BoostPressed = false;

        // O dedo do joystick é o mesmo do começo ao fim, mesmo que escorregue pra fora da zona.
        TouchPoint? stickTouch = null;
        if (layout.HasJoystick)
        {
            if (_stickFinger is { } id)
            {
                foreach (TouchPoint touch in touches)
                {
                    if (touch.Id == id)
                    {
                        stickTouch = touch;
                    }
                }
            }

            if (stickTouch is null)
            {
                _stickFinger = null;
                foreach (TouchPoint touch in touches)
                {
                    if (layout.JoystickZone.Contains(touch.Position.ToPoint()) && !IsOnRightButtons(touch.Position, layout))
                    {
                        _stickFinger = touch.Id;
                        stickTouch = touch;
                        StickCenter = ClampStickCenter(touch.Position, layout);
                        break;
                    }
                }
            }
        }
        else
        {
            _stickFinger = null;
        }

        UpdateStick(stickTouch, layout);

        foreach (TouchPoint touch in touches)
        {
            if (stickTouch is { } stick && touch.Id == stick.Id)
            {
                continue;
            }

            Press(Nearest(touch.Position, layout));
        }
    }

    private void UpdateStick(TouchPoint? stickTouch, TouchLayout layout)
    {
        if (stickTouch is not { } touch)
        {
            StickActive = false;
            StickAmount = 0f;
            StickCenter = layout.JoystickRest.Center;
            StickKnob = StickCenter;
            return;
        }

        float radius = layout.JoystickRest.Radius;
        Vector2 offset = touch.Position - StickCenter;
        float length = offset.Length();
        if (length > radius && length > 0f)
        {
            offset *= radius / length;
            length = radius;
        }

        StickActive = true;
        StickKnob = StickCenter + offset;
        float amount = radius > 0f ? length / radius : 0f;
        StickAmount = amount <= JoystickDeadZone ? 0f : (amount - JoystickDeadZone) / (1f - JoystickDeadZone);
        StickAngle = MathF.Atan2(offset.Y, offset.X);
    }

    /// <summary>O círculo do joystick nasce onde o dedo encostou, mas sempre inteiro dentro da área segura.</summary>
    private static Vector2 ClampStickCenter(Vector2 point, TouchLayout layout)
    {
        float r = layout.JoystickRest.Radius;
        Rectangle safe = layout.Safe;
        return new Vector2(
            Math.Clamp(point.X, safe.X + r, MathF.Max(safe.X + r, safe.Right - r)),
            Math.Clamp(point.Y, safe.Y + r, MathF.Max(safe.Y + r, safe.Bottom - r)));
    }

    private static bool IsOnRightButtons(Vector2 point, TouchLayout layout) =>
        layout.Brake.Reach(point) < 1f || layout.Boost.Reach(point) < 1f;

    private enum Control
    {
        None,
        Left,
        Right,
        Gas,
        Brake,
        Boost,
    }

    private void Press(Control control)
    {
        switch (control)
        {
            case Control.Left:
                LeftPressed = true;
                break;
            case Control.Right:
                RightPressed = true;
                break;
            case Control.Gas:
                GasPressed = true;
                break;
            case Control.Brake:
                BrakePressed = true;
                break;
            case Control.Boost:
                BoostPressed = true;
                break;
        }
    }

    /// <summary>O controle mais perto do dedo (entre os que existem no esquema). Fora das áreas dos botões, as
    /// "zonas" de cada canto ainda contam: a metade de baixo do canto esquerdo vira, a do direito acelera/freia.</summary>
    private static Control Nearest(Vector2 point, TouchLayout layout)
    {
        Control best = Control.None;
        float bestReach = HitReach;

        void Consider(Control control, TouchButton button, bool exists)
        {
            if (!exists)
            {
                return;
            }

            float reach = button.Reach(point);
            if (reach < bestReach)
            {
                bestReach = reach;
                best = control;
            }
        }

        Consider(Control.Left, layout.Left, layout.HasSteeringButtons);
        Consider(Control.Right, layout.Right, layout.HasSteeringButtons);
        Consider(Control.Gas, layout.Gas, layout.HasGas);
        Consider(Control.Brake, layout.Brake, true);
        Consider(Control.Boost, layout.Boost, true);
        if (best != Control.None)
        {
            return best;
        }

        // Zonas generosas nos cantos de baixo (o polegar escorregou pra longe do desenho).
        Rectangle safe = layout.Safe;
        bool lowerHalf = point.Y > safe.Y + (safe.Height * 0.45f);
        if (!lowerHalf)
        {
            return Control.None;
        }

        if (point.X < safe.X + (safe.Width * 0.3f))
        {
            return layout.Scheme switch
            {
                ControlScheme.Buttons => layout.Left.Reach(point) <= layout.Right.Reach(point) ? Control.Left : Control.Right,
                ControlScheme.Tilt => Control.Brake,
                _ => Control.None,
            };
        }

        if (point.X > safe.Right - (safe.Width * 0.3f))
        {
            Control nearestRight = Control.Brake;
            float nearestReach = layout.Brake.Reach(point);
            if (layout.HasGas && layout.Gas.Reach(point) < nearestReach)
            {
                nearestRight = Control.Gas;
                nearestReach = layout.Gas.Reach(point);
            }

            if (layout.Boost.Reach(point) < nearestReach)
            {
                nearestRight = Control.Boost;
            }

            return layout.Scheme == ControlScheme.Tilt && nearestRight == Control.Brake ? Control.Gas : nearestRight;
        }

        return Control.None;
    }

    /// <summary>
    /// O comando do carro vindo do toque (e da inclinação). <paramref name="carAngle"/> é pra onde o carro aponta
    /// (radianos na tela) — o joystick vira o carro na direção apontada. <paramref name="tilt"/> é a inclinação
    /// (-1 a 1) no esquema INCLINAR. Com <paramref name="autoAccelerate"/>, acelera sozinho (menos freando).
    /// </summary>
    public CarInput ToCarInput(ControlScheme scheme, float carAngle, float? tilt, bool autoAccelerate)
    {
        float throttle = 0f;
        float steering = 0f;

        switch (scheme)
        {
            case ControlScheme.Joystick:
                if (StickActive && StickAmount > 0f)
                {
                    float difference = WrapAngle(StickAngle - carAngle);
                    steering = Math.Clamp(difference / JoystickFullLockAngle, -1f, 1f);

                    // Apontar pra trás: vira no lugar com meio acelerador em vez de sair reto.
                    float facing = MathF.Abs(difference) > MathF.PI * 0.6f ? 0.5f : 1f;
                    throttle = StickAmount * facing;
                }

                break;
            case ControlScheme.Tilt:
                steering = tilt ?? 0f;
                break;
            default:
                steering = (RightPressed ? 1f : 0f) - (LeftPressed ? 1f : 0f);
                break;
        }

        if (GasPressed || BoostPressed)
        {
            throttle = 1f;
        }

        if (autoAccelerate && (scheme != ControlScheme.Joystick || StickActive))
        {
            throttle = 1f;
        }

        if (BrakePressed)
        {
            throttle = -1f;
        }

        return new CarInput(throttle, steering, false, BoostPressed);
    }

    private static float WrapAngle(float angle)
    {
        angle %= MathF.Tau;
        if (angle > MathF.PI)
        {
            angle -= MathF.Tau;
        }
        else if (angle < -MathF.PI)
        {
            angle += MathF.Tau;
        }

        return angle;
    }

    /// <summary>Junta dois comandos (ex.: teclado/controle + toque): somam e ficam no limite de -1 a 1; freio de mão e
    /// turbo valem se qualquer um pedir.</summary>
    public static CarInput Combine(CarInput a, CarInput b) => new(
        Math.Clamp(a.Throttle + b.Throttle, -1f, 1f),
        Math.Clamp(a.Steering + b.Steering, -1f, 1f),
        a.Brake || b.Brake,
        a.Boost || b.Boost);
}
