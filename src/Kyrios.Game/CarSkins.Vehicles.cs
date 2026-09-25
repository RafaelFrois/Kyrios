using Microsoft.Xna.Framework;

namespace Kyrios.Game;

/// <summary>Skins de veículos (que também não deveriam estar numa corrida) — vista de cima.</summary>
public static partial class CarSkins
{
    private static void PaintSkateboard(CarPainter p)
    {
        var deck = new Color(220, 80, 60);
        var grip = new Color(40, 40, 46);
        var truck = new Color(170, 175, 185);
        var wheel = new Color(250, 230, 150);

        p.Shadow(1.35f, 0.55f);
        foreach (float x in new[] { 0.38f, -0.38f })
        {
            p.Rect(x, 0f, 0.08f, 0.5f, truck);
            p.Rect(x, 0.27f, 0.12f, 0.08f, wheel);
            p.Rect(x, -0.27f, 0.12f, 0.08f, wheel);
        }

        p.Capsule(0f, 0f, 1.35f, 0.44f, deck);
        p.Capsule(0f, 0f, 1.22f, 0.34f, grip);
        p.Rect(0f, 0f, 0.3f, 0.2f, deck);
        p.Circle(0f, 0f, 0.07f, new Color(250, 220, 60));
        foreach (float x in new[] { 0.38f, -0.38f })
        {
            p.Circle(x, 0.08f, 0.02f, truck);
            p.Circle(x, -0.08f, 0.02f, truck);
        }
    }

    private static void PaintHelicopter(CarPainter p)
    {
        var body = new Color(210, 60, 55);
        var dark = new Color(150, 40, 40);
        var glass = new Color(140, 200, 240);
        var blade = new Color(50, 52, 58);
        float rotor = p.Time * 22f;

        p.ShadowCircle(0.55f);
        p.Rect(-0.5f, 0f, 0.6f, 0.08f, dark);
        p.Rect(-0.78f, 0.06f, 0.06f, 0.18f, dark);
        float tail = p.Time * 30f;
        p.Rect(-0.78f, 0.06f, 0.2f, 0.03f, blade, tail);
        foreach (float side in new[] { 1f, -1f })
        {
            p.Rect(0.05f, side * 0.34f, 0.8f, 0.05f, new Color(90, 95, 105));
        }

        p.Capsule(0.05f, 0f, 0.95f, 0.56f, body);
        p.Capsule(0.32f, 0f, 0.4f, 0.42f, glass);
        p.Ellipse(0.36f, -0.08f, 0.1f, 0.05f, Color.White * 0.6f);
        p.Circle(0f, 0f, 0.08f, dark);

        // Hélice principal girando rápido (com um "rastro" apagado).
        p.Circle(0f, 0f, 0.62f, blade * 0.12f);
        p.Rect(0f, 0f, 1.3f, 0.06f, blade, rotor);
        p.Rect(0f, 0f, 1.3f, 0.06f, blade, rotor + (MathF.PI / 2f));
    }

    private static void PaintTractor(CarPainter p)
    {
        var green = new Color(60, 150, 70);
        var dark = new Color(40, 105, 50);
        var yellow = new Color(250, 205, 40);
        var tire = new Color(35, 35, 38);
        float puff = (p.Time * 1.5f) % 1f;

        p.Shadow(1.4f, 1f);
        p.Rect(-0.3f, 0.42f, 0.5f, 0.2f, tire);
        p.Rect(-0.3f, -0.42f, 0.5f, 0.2f, tire);
        for (int i = 0; i < 4; i++)
        {
            p.Rect(-0.5f + (i * 0.13f), 0.42f, 0.03f, 0.2f, new Color(60, 60, 64), 0.5f);
            p.Rect(-0.5f + (i * 0.13f), -0.42f, 0.03f, 0.2f, new Color(60, 60, 64), -0.5f);
        }

        p.Rect(0.45f, 0.3f, 0.24f, 0.12f, tire);
        p.Rect(0.45f, -0.3f, 0.24f, 0.12f, tire);
        p.Rect(0.25f, 0f, 0.8f, 0.44f, green);
        p.Rect(0.25f, 0f, 0.8f, 0.08f, dark);
        p.Rect(0.62f, 0f, 0.08f, 0.4f, new Color(90, 94, 104));
        p.Rect(-0.3f, 0f, 0.55f, 0.6f, yellow);
        p.Rect(-0.3f, 0f, 0.4f, 0.46f, CarPainter.Darken(yellow, 0.85f));
        p.Circle(0.3f, 0.12f, 0.06f, new Color(60, 60, 66));
        if (!p.Eliminated)
        {
            p.Circle(0.3f - (puff * 0.4f), 0.12f + (puff * 0.1f), 0.05f + (puff * 0.08f), new Color(200, 200, 205) * (0.8f * (1f - puff)));
        }
    }

    private static void PaintBeetle(CarPainter p)
    {
        Color paint = new(110, 180, 220);
        Color dark = CarPainter.Darken(paint, 0.7f);
        var glass = new Color(40, 60, 80);

        p.Shadow(1.3f, 0.9f);
        foreach (float x in new[] { 0.36f, -0.4f })
        {
            p.Ellipse(x, 0.36f, 0.16f, 0.09f, dark);
            p.Ellipse(x, -0.36f, 0.16f, 0.09f, dark);
        }

        p.Ellipse(0f, 0f, 0.66f, 0.4f, paint);
        p.Ellipse(-0.06f, 0f, 0.4f, 0.3f, dark);
        p.Ellipse(-0.06f, 0f, 0.36f, 0.26f, paint);
        p.Ellipse(0.18f, 0f, 0.09f, 0.24f, glass);
        p.Ellipse(-0.36f, 0f, 0.06f, 0.2f, glass);
        p.Rect(0.64f, 0f, 0.05f, 0.62f, new Color(200, 205, 215));
        if (!p.Eliminated)
        {
            p.Circle(0.58f, 0.24f, 0.07f, new Color(255, 250, 210));
            p.Circle(0.58f, -0.24f, 0.07f, new Color(255, 250, 210));
        }

        p.Ellipse(-0.1f, -0.18f, 0.2f, 0.05f, Color.White * 0.25f);
    }

    private static void PaintBumperCar(CarPainter p)
    {
        var body = new Color(60, 180, 230);
        var bumper = new Color(35, 35, 40);
        var seat = new Color(230, 70, 70);
        var pole = new Color(190, 195, 205);
        bool spark = (p.Time * 3f % 1f) < 0.25f;

        p.ShadowCircle(0.55f);
        p.Capsule(0f, 0f, 1.15f, 0.95f, bumper);
        p.Capsule(0f, 0f, 1.02f, 0.82f, body);
        p.Capsule(0.3f, 0f, 0.3f, 0.6f, CarPainter.Darken(body, 0.8f));
        p.Rect(-0.08f, 0f, 0.36f, 0.46f, seat);
        p.Rect(0.18f, 0f, 0.08f, 0.2f, new Color(40, 40, 46));
        p.Circle(-0.36f, 0f, 0.08f, pole);
        if (spark && !p.Eliminated)
        {
            p.Rect(-0.36f, 0f, 0.26f, 0.04f, new Color(255, 250, 150), p.Time * 5f);
            p.Rect(-0.36f, 0f, 0.26f, 0.04f, new Color(150, 220, 255), (p.Time * 5f) + 1.4f);
        }

        p.Ellipse(0f, -0.28f, 0.3f, 0.05f, Color.White * 0.3f);
    }

    private static void PaintSchoolBus(CarPainter p)
    {
        var yellow = new Color(250, 195, 30);
        var dark = new Color(200, 145, 20);
        var black = new Color(35, 35, 40);

        p.Shadow(1.55f, 0.82f);
        foreach (float x in new[] { 0.45f, -0.45f })
        {
            p.Rect(x, 0.4f, 0.2f, 0.08f, Wheel);
            p.Rect(x, -0.4f, 0.2f, 0.08f, Wheel);
        }

        p.Rect(0f, 0f, 1.52f, 0.78f, dark);
        p.Rect(0f, 0f, 1.46f, 0.72f, yellow);
        p.Rect(0f, 0.3f, 1.46f, 0.04f, black);
        p.Rect(0f, -0.3f, 1.46f, 0.04f, black);
        foreach (float x in new[] { -0.3f, 0.2f })
        {
            p.Rect(x, 0f, 0.18f, 0.3f, new Color(230, 230, 225));
            p.Rect(x, 0f, 0.14f, 0.26f, new Color(200, 205, 210));
        }

        p.Rect(0.62f, 0f, 0.1f, 0.6f, new Color(60, 70, 90));
        p.Circle(0.74f, 0.3f, 0.05f, new Color(255, 250, 210));
        p.Circle(0.74f, -0.3f, 0.05f, new Color(255, 250, 210));

        // Placa de "pare" abrindo pro lado de vez em quando.
        bool stop = (p.Time % 3f) < 0.8f;
        if (stop)
        {
            p.Circle(0.4f, 0.5f, 0.09f, new Color(220, 40, 40));
            p.Circle(0.4f, 0.5f, 0.05f, Color.White);
        }
    }

    private static void PaintTank(CarPainter p)
    {
        var olive = new Color(95, 115, 60);
        var dark = new Color(65, 80, 40);
        var tread = new Color(40, 42, 36);
        float roll = (p.Time * 5f) % 1f;
        float aim = MathF.Sin(p.Time * 0.8f) * 0.25f;

        p.Shadow(1.4f, 1f);
        foreach (float side in new[] { 1f, -1f })
        {
            p.Rect(0f, side * 0.38f, 1.35f, 0.22f, tread);
            for (int i = 0; i < 8; i++)
            {
                p.Rect(-0.6f + ((i + roll) * 0.16f), side * 0.38f, 0.05f, 0.22f, new Color(70, 72, 62));
            }
        }

        p.Rect(0f, 0f, 1.2f, 0.58f, dark);
        p.Rect(0f, 0f, 1.12f, 0.5f, olive);
        p.Rect(0.4f, 0f, 0.4f, 0.08f, dark, aim);
        p.Rect(0.18f + (MathF.Cos(aim) * 0.42f), MathF.Sin(aim) * 0.42f, 0.5f, 0.07f, dark, aim);
        p.Circle(0f, 0f, 0.24f, dark);
        p.Circle(-0.02f, 0f, 0.2f, olive);
        p.Circle(-0.08f, 0.08f, 0.06f, dark);
        p.Rect(-0.46f, 0.16f, 0.14f, 0.1f, new Color(60, 70, 40));
    }

    private static void PaintFireTruck(CarPainter p)
    {
        var red = new Color(215, 40, 35);
        var dark = new Color(150, 25, 25);
        var ladder = new Color(200, 205, 212);
        bool flash = (p.Time * 4f % 1f) < 0.5f;

        p.Shadow(1.55f, 0.82f);
        foreach (float x in new[] { 0.48f, -0.2f, -0.5f })
        {
            p.Rect(x, 0.4f, 0.18f, 0.08f, Wheel);
            p.Rect(x, -0.4f, 0.18f, 0.08f, Wheel);
        }

        p.Rect(0f, 0f, 1.52f, 0.76f, dark);
        p.Rect(0f, 0f, 1.46f, 0.7f, red);
        p.Rect(0.52f, 0f, 0.36f, 0.66f, CarPainter.Darken(red, 0.85f));
        p.Rect(0.6f, 0f, 0.12f, 0.56f, new Color(60, 75, 95));

        // A escada ocupando o teto.
        p.Rect(-0.15f, 0.12f, 1f, 0.04f, ladder);
        p.Rect(-0.15f, -0.12f, 1f, 0.04f, ladder);
        for (int i = 0; i < 8; i++)
        {
            p.Rect(-0.6f + (i * 0.13f), 0f, 0.03f, 0.24f, ladder);
        }

        p.Circle(0.46f, 0.2f, 0.06f, flash ? new Color(255, 60, 60) : new Color(110, 30, 30));
        p.Circle(0.46f, -0.2f, 0.06f, flash ? new Color(90, 110, 140) : new Color(80, 170, 255));
        p.Rect(-0.72f, 0.28f, 0.06f, 0.1f, new Color(250, 200, 40));
    }

    private static void PaintSubmarine(CarPainter p)
    {
        var yellow = new Color(250, 200, 40);
        var dark = new Color(200, 145, 25);
        var window = new Color(120, 200, 235);
        float prop = p.Time * 18f;

        p.Shadow(1.45f, 0.7f);
        p.Rect(-0.72f, 0f, 0.05f, 0.34f, new Color(90, 95, 105), prop);
        p.Rect(-0.62f, 0f, 0.1f, 0.12f, dark);
        p.Rect(-0.5f, 0f, 0.16f, 0.5f, dark);
        p.Capsule(0.02f, 0f, 1.3f, 0.6f, yellow);
        p.Capsule(-0.05f, 0f, 0.4f, 0.26f, dark);
        p.Circle(0.05f, 0f, 0.06f, new Color(90, 95, 105));
        foreach (float x in new[] { 0.25f, 0.45f })
        {
            p.Circle(x, 0.18f, 0.07f, new Color(160, 165, 175));
            p.Circle(x, 0.18f, 0.05f, window);
            p.Circle(x, -0.18f, 0.07f, new Color(160, 165, 175));
            p.Circle(x, -0.18f, 0.05f, window);
        }

        p.Ellipse(0.2f, -0.06f, 0.3f, 0.05f, Color.White * 0.3f);
        float bubble = (p.Time * 1.2f) % 1f;
        p.Circle(-0.75f - (bubble * 0.2f), MathF.Sin(bubble * 8f) * 0.08f, 0.04f + (bubble * 0.03f), new Color(200, 235, 255) * (1f - bubble));
    }

    /// <summary>O mascote do jogo (a galinha de óculos escuros no kart vermelho da logo), visto de cima.</summary>
    private static void PaintKartChicken(CarPainter p)
    {
        var red = new Color(225, 45, 40);
        var dark = new Color(160, 30, 30);
        var stripe = new Color(250, 245, 235);
        var white = new Color(248, 245, 238);
        var comb = new Color(230, 40, 35);
        var beak = new Color(250, 165, 30);
        var shades = new Color(20, 22, 30);

        p.Shadow(1.4f, 0.95f);
        foreach (float x in new[] { 0.45f, -0.42f })
        {
            p.Rect(x, 0.42f, 0.26f, 0.16f, Wheel);
            p.Rect(x, -0.42f, 0.26f, 0.16f, Wheel);
            p.Rect(x, 0.42f, 0.08f, 0.1f, new Color(150, 155, 165));
            p.Rect(x, -0.42f, 0.08f, 0.1f, new Color(150, 155, 165));
        }

        p.Rect(-0.64f, 0f, 0.12f, 0.9f, dark);
        p.Capsule(0.05f, 0f, 1.3f, 0.62f, red);
        p.Rect(0.35f, 0.1f, 0.6f, 0.06f, stripe);
        p.Rect(0.35f, -0.1f, 0.6f, 0.06f, stripe);
        p.Rect(0.62f, 0f, 0.1f, 0.5f, dark);

        // A galinha pilotando (corpo, asas no volante, cabeça de óculos escuros e crista).
        p.Circle(-0.12f, 0f, 0.27f, white);
        p.Ellipse(0.05f, 0.2f, 0.14f, 0.07f, white, -0.5f);
        p.Ellipse(0.05f, -0.2f, 0.14f, 0.07f, white, 0.5f);
        p.Circle(0.14f, 0f, 0.09f, new Color(40, 40, 46));
        p.Circle(-0.02f, 0f, 0.18f, white);
        p.Capsule(-0.06f, 0f, 0.3f, 0.09f, comb);
        p.Circle(-0.2f, 0f, 0.06f, comb);
        p.Rect(0.08f, 0f, 0.07f, 0.3f, shades);
        p.Capsule(0.2f, 0f, 0.14f, 0.1f, beak);
    }
}
