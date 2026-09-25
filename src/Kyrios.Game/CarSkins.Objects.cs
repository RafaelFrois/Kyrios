using Microsoft.Xna.Framework;

namespace Kyrios.Game;

/// <summary>Skins de objetos e coisas de casa (vista de cima).</summary>
public static partial class CarSkins
{
    private static void PaintCardboardBox(CarPainter p)
    {
        var cardboard = new Color(195, 150, 95);
        var dark = new Color(150, 110, 65);
        var tape = new Color(215, 190, 140);

        p.Shadow(1.2f, 0.9f);
        p.Rect(0f, 0f, 1.16f, 0.86f, dark);
        p.Rect(0f, 0f, 1.1f, 0.8f, cardboard);
        p.Rect(0f, 0f, 1.1f, 0.03f, dark);

        // Abas abrindo e fechando com o vento da corrida.
        float flap = 0.04f + (MathF.Abs(MathF.Sin(p.Time * 7f)) * 0.06f);
        p.Rect(0f, 0.43f + flap, 1f, 0.12f, new Color(210, 170, 115));
        p.Rect(0f, -0.43f - flap, 1f, 0.12f, new Color(210, 170, 115));
        p.Rect(0f, 0f, 0.18f, 0.8f, tape);
        p.Rect(0f, 0f, 0.18f, 0.04f, Color.White * 0.3f);
        p.Rect(-0.38f, 0.22f, 0.12f, 0.03f, dark);
        p.Rect(-0.38f, 0.28f, 0.08f, 0.03f, dark);
        p.Rect(0.36f, -0.24f, 0.1f, 0.1f, new Color(60, 60, 70), MathF.PI / 4f);
        p.Rect(0.36f, -0.24f, 0.06f, 0.06f, cardboard, MathF.PI / 4f);
    }

    private static void PaintIceCube(CarPainter p)
    {
        var ice = new Color(185, 230, 250);
        var edge = new Color(130, 195, 230);
        var shine = new Color(245, 252, 255);

        p.Shadow(1.05f, 0.95f);

        // Poça derretendo atrás (pinga um pouco a cada volta do relógio).
        float drip = (p.Time * 0.7f) % 1f;
        p.Ellipse(-0.55f - (drip * 0.2f), 0f, 0.08f + (drip * 0.05f), 0.06f, new Color(140, 200, 240) * (0.6f * (1f - drip)));

        p.Rect(0f, 0f, 0.95f, 0.88f, edge);
        p.Rect(0.02f, -0.02f, 0.85f, 0.78f, ice);
        p.Rect(-0.2f, -0.25f, 0.32f, 0.07f, shine, -0.2f);
        p.Rect(-0.3f, -0.12f, 0.12f, 0.05f, shine, -0.2f);
        p.Rect(0.25f, 0.2f, 0.2f, 0.04f, Color.White * 0.6f, 0.3f);
        p.Circle(0.3f, -0.25f, 0.05f, Color.White * 0.5f);
    }

    private static void PaintSlipper(CarPainter p)
    {
        var sole = new Color(60, 120, 210);
        var soleEdge = new Color(240, 240, 235);
        var strap = new Color(250, 210, 40);

        p.Shadow(1.4f, 0.7f);

        // Sola em formato de pé (calcanhar atrás, dedos na frente).
        p.Capsule(0f, 0f, 1.35f, 0.62f, soleEdge);
        p.Capsule(0.02f, 0f, 1.27f, 0.54f, sole);
        p.Circle(0.45f, 0.06f, 0.26f, soleEdge);
        p.Circle(0.45f, 0.06f, 0.22f, sole);
        p.Line(0.36f, 0f, -0.05f, 0.26f, 0.07f, strap);
        p.Line(0.36f, 0f, -0.05f, -0.26f, 0.07f, strap);
        p.Circle(0.36f, 0f, 0.05f, strap);
        for (int i = 0; i < 5; i++)
        {
            p.Rect(-0.4f + (i * 0.08f), 0f, 0.02f, 0.3f, CarPainter.Darken(sole, 0.85f));
        }
    }

    private static void PaintCone(CarPainter p)
    {
        var orange = new Color(250, 120, 30);
        var white = new Color(250, 250, 245);
        var baseColor = new Color(40, 40, 45);

        p.Shadow(1f, 1f);
        p.Rect(0f, 0f, 0.95f, 0.95f, baseColor);
        p.Rect(0f, 0f, 0.95f, 0.95f, Color.White * 0.08f);
        p.Circle(0f, 0f, 0.42f, orange);
        p.Circle(0f, 0f, 0.32f, white);
        p.Circle(0f, 0f, 0.24f, orange);
        p.Circle(0f, 0f, 0.14f, white);
        p.Circle(0f, 0f, 0.07f, orange);
        p.Circle(-0.05f, -0.05f, 0.03f, Color.White * 0.7f);
    }

    private static void PaintWashingMachine(CarPainter p)
    {
        var body = new Color(235, 238, 242);
        var edge = new Color(180, 185, 195);
        var glass = new Color(120, 170, 220);
        Color[] clothes = [new(230, 70, 70), new(250, 210, 50), new(80, 190, 110), new(255, 255, 255)];
        float spin = p.Time * 9f;
        float rattle = MathF.Sin(p.Time * 40f) * 0.012f;

        p.Shadow(1.1f, 0.95f);
        p.Rect(rattle, 0f, 1.05f, 0.95f, edge);
        p.Rect(rattle, 0f, 0.99f, 0.89f, body);
        p.Rect(-0.4f + rattle, 0f, 0.14f, 0.85f, new Color(210, 214, 222));
        p.Circle(-0.4f + rattle, 0.28f, 0.04f, new Color(80, 200, 110));
        p.Circle(-0.4f + rattle, -0.25f, 0.05f, new Color(90, 90, 100));

        p.Circle(0.12f + rattle, 0f, 0.36f, edge);
        p.Circle(0.12f + rattle, 0f, 0.3f, glass);
        for (int i = 0; i < 4; i++)
        {
            float a = spin + (i * MathF.Tau / 4f);
            p.Rect(0.12f + rattle + (MathF.Cos(a) * 0.15f), MathF.Sin(a) * 0.15f, 0.14f, 0.08f, clothes[i], a);
        }

        p.Ellipse(0.04f + rattle, -0.12f, 0.1f, 0.05f, Color.White * 0.5f, -0.6f);
    }

    private static void PaintRobotVacuum(CarPainter p)
    {
        var shell = new Color(55, 58, 66);
        var top = new Color(85, 90, 102);
        var bumper = new Color(35, 36, 40);
        var brush = new Color(170, 175, 185);
        float spin = p.Time * 14f;

        p.ShadowCircle(0.5f);
        foreach (float side in new[] { 1f, -1f })
        {
            var center = new Vector2(0.3f, side * 0.3f);
            for (int i = 0; i < 3; i++)
            {
                float a = (spin * side) + (i * MathF.Tau / 3f);
                p.Line(center.X, center.Y, center.X + (MathF.Cos(a) * 0.2f), center.Y + (MathF.Sin(a) * 0.2f), 0.025f, brush);
            }
        }

        p.Circle(0f, 0f, 0.48f, bumper);
        p.Circle(-0.02f, 0f, 0.44f, shell);
        p.Circle(-0.04f, 0f, 0.3f, top);
        p.Circle(-0.04f, 0f, 0.1f, bumper);
        p.Circle(-0.04f, 0f, 0.05f, new Color(120, 230, 255) * (0.6f + (0.4f * MathF.Sin(p.Time * 4f))));
        p.Rect(0.36f, 0f, 0.06f, 0.4f, bumper);
    }

    private static void PaintTire(CarPainter p)
    {
        var rubber = new Color(35, 35, 38);
        var tread = new Color(60, 60, 66);
        var rim = new Color(170, 175, 185);
        float roll = p.Time * 8f;

        p.ShadowCircle(0.5f);
        p.Circle(0f, 0f, 0.48f, rubber);
        for (int i = 0; i < 16; i++)
        {
            float a = roll + (i * MathF.Tau / 16f);
            p.Rect(MathF.Cos(a) * 0.4f, MathF.Sin(a) * 0.4f, 0.12f, 0.05f, tread, a);
        }

        p.Circle(0f, 0f, 0.28f, rubber);
        p.Circle(0f, 0f, 0.22f, rim);
        for (int i = 0; i < 5; i++)
        {
            float a = roll + (i * MathF.Tau / 5f);
            p.Circle(MathF.Cos(a) * 0.13f, MathF.Sin(a) * 0.13f, 0.035f, new Color(110, 115, 125));
        }

        p.Circle(0f, 0f, 0.06f, new Color(90, 94, 104));
    }

    private static void PaintFloppy(CarPainter p)
    {
        var plastic = new Color(45, 70, 140);
        var edge = new Color(30, 45, 95);
        var metal = new Color(185, 190, 200);
        var label = new Color(245, 245, 235);

        p.Shadow(1.05f, 1.05f);
        p.Rect(0f, 0f, 1f, 1f, edge);
        p.Rect(0f, 0f, 0.94f, 0.94f, plastic);
        p.Rect(0.47f, 0.43f, 0.08f, 0.08f, edge);
        p.Rect(0.32f, 0f, 0.3f, 0.52f, metal);
        p.Rect(0.32f, 0.1f, 0.2f, 0.12f, new Color(60, 62, 70));
        p.Rect(-0.2f, 0f, 0.5f, 0.74f, label);
        for (int i = 0; i < 4; i++)
        {
            p.Rect(-0.2f + (i * 0.1f) - 0.12f, 0f, 0.02f, 0.6f, new Color(120, 150, 210));
        }

        p.Rect(-0.38f, -0.36f, 0.08f, 0.08f, new Color(20, 20, 26));
    }

    private static void PaintLightBulb(CarPainter p)
    {
        var glass = new Color(255, 245, 170);
        var glassEdge = new Color(230, 210, 120);
        var metal = new Color(170, 175, 185);
        var metalDark = new Color(120, 125, 135);
        float glow = (MathF.Sin(p.Time * 5f) + 1f) / 2f;

        p.Shadow(1.3f, 0.8f);
        if (!p.Eliminated)
        {
            p.Circle(0.18f, 0f, 0.55f, new Color(255, 240, 150) * (0.12f + (0.1f * glow)));
        }

        for (int i = 0; i < 4; i++)
        {
            p.Rect(-0.44f + (i * 0.07f), 0f, 0.05f, 0.36f, i % 2 == 0 ? metal : metalDark);
        }

        p.Rect(-0.6f, 0f, 0.08f, 0.16f, new Color(60, 60, 64));
        p.Capsule(-0.18f, 0f, 0.3f, 0.36f, glassEdge);
        p.Circle(0.2f, 0f, 0.4f, glassEdge);
        p.Circle(0.2f, 0f, 0.36f, glass);
        p.Capsule(-0.18f, 0f, 0.26f, 0.3f, glass);
        p.Line(-0.15f, 0.06f, 0.18f, 0.06f, 0.02f, new Color(200, 140, 40));
        p.Line(-0.15f, -0.06f, 0.18f, -0.06f, 0.02f, new Color(200, 140, 40));
        for (int i = 0; i < 4; i++)
        {
            p.Line(0.18f, -0.06f + (i * 0.04f), 0.18f + 0.06f, -0.04f + (i * 0.04f), 0.02f, new Color(255, 170, 40));
        }

        p.Ellipse(0.12f, -0.18f, 0.12f, 0.06f, Color.White * 0.6f, -0.5f);
    }

    private static void PaintFridge(CarPainter p)
    {
        var body = new Color(240, 243, 247);
        var edge = new Color(180, 186, 196);
        var handle = new Color(150, 155, 165);
        Color[] magnets = [new(230, 70, 70), new(80, 170, 240), new(250, 210, 50), new(120, 210, 110)];

        p.Shadow(1.4f, 0.85f);
        p.Rect(0f, 0f, 1.4f, 0.84f, edge);
        p.Rect(0f, 0f, 1.34f, 0.78f, body);
        p.Rect(0.2f, 0f, 0.03f, 0.78f, edge);
        p.Rect(0.08f, 0.3f, 0.14f, 0.05f, handle);
        p.Rect(0.34f, 0.3f, 0.14f, 0.05f, handle);
        for (int i = 0; i < magnets.Length; i++)
        {
            p.Circle(-0.45f + (i * 0.13f), -0.2f + ((i % 2) * 0.12f), 0.05f, magnets[i]);
        }

        p.Rect(-0.3f, 0.12f, 0.24f, 0.2f, new Color(255, 250, 200), 0.1f);
        p.Rect(-0.3f, 0.1f, 0.16f, 0.02f, new Color(120, 120, 140), 0.1f);
        p.Rect(-0.3f, 0.16f, 0.12f, 0.02f, new Color(120, 120, 140), 0.1f);
        p.Rect(0.5f, -0.2f, 0.12f, 0.12f, new Color(120, 200, 240), 0.4f);
    }

    private static void PaintBathtub(CarPainter p)
    {
        var tub = new Color(245, 245, 250);
        var edge = new Color(190, 195, 205);
        var water = new Color(120, 195, 235);
        var chrome = new Color(185, 190, 200);
        float bob = MathF.Sin(p.Time * 3f) * 0.05f;

        p.Shadow(1.45f, 0.85f);
        p.Capsule(0f, 0f, 1.45f, 0.84f, edge);
        p.Capsule(0f, 0f, 1.38f, 0.78f, tub);
        p.Capsule(0.02f, 0f, 1.15f, 0.56f, water);
        for (int i = 0; i < 5; i++)
        {
            p.Circle(-0.3f + (i * 0.14f), (i % 2 == 0 ? 0.15f : -0.12f) + (bob * 0.5f), 0.05f, Color.White * 0.7f);
        }

        // Patinho de borracha passeando na água.
        p.Circle(0.18f, 0.06f + bob, 0.1f, new Color(250, 215, 50));
        p.Circle(0.28f, 0.06f + bob, 0.06f, new Color(250, 215, 50));
        p.Rect(0.35f, 0.06f + bob, 0.05f, 0.04f, new Color(250, 130, 30));

        p.Rect(-0.62f, 0f, 0.1f, 0.2f, chrome);
        p.Circle(-0.62f, 0.14f, 0.04f, new Color(220, 70, 70));
        p.Circle(-0.62f, -0.14f, 0.04f, new Color(70, 130, 220));
    }

    private static void PaintSock(CarPainter p)
    {
        var red = new Color(215, 60, 60);
        var white = new Color(245, 245, 240);
        float wiggle = MathF.Sin(p.Time * 5f) * 0.05f;

        p.Shadow(1.45f, 0.6f);
        p.Capsule(-0.18f, 0f, 1f, 0.5f, white);
        for (int i = 0; i < 5; i++)
        {
            p.Rect(-0.6f + (i * 0.16f), 0f, 0.08f, 0.5f, red);
        }

        p.Rect(-0.66f, 0f, 0.1f, 0.52f, red);
        p.Circle(0.38f, 0.1f + wiggle, 0.28f, white);
        p.Circle(0.46f, 0.1f + wiggle, 0.22f, white);
        p.Circle(0.56f, 0.12f + wiggle, 0.14f, red);
        p.Circle(0.08f, -0.14f, 0.12f, red);
    }

    private static void PaintSoccerBall(CarPainter p)
    {
        var white = new Color(248, 248, 248);
        var black = new Color(30, 30, 34);
        float roll = p.Time * 6f;

        p.ShadowCircle(0.47f);
        p.Circle(0f, 0f, 0.46f, new Color(200, 200, 205));
        p.Circle(-0.02f, -0.02f, 0.43f, white);
        p.Circle(0f, 0f, 0.13f, black);
        for (int i = 0; i < 5; i++)
        {
            float a = roll + (i * MathF.Tau / 5f);
            p.Circle(MathF.Cos(a) * 0.3f, MathF.Sin(a) * 0.3f, 0.1f, black);
            p.Line(MathF.Cos(a) * 0.12f, MathF.Sin(a) * 0.12f, MathF.Cos(a) * 0.22f, MathF.Sin(a) * 0.22f, 0.03f, black);
        }

        p.Ellipse(-0.14f, -0.16f, 0.1f, 0.05f, Color.White * 0.8f, -0.7f);
    }

    private static void PaintFlamingoFloat(CarPainter p)
    {
        var pink = new Color(250, 130, 170);
        var light = new Color(255, 185, 210);
        var beak = new Color(40, 35, 40);

        p.ShadowCircle(0.5f);
        p.Circle(-0.08f, 0f, 0.44f, pink);
        p.Circle(-0.12f, -0.06f, 0.3f, light);
        p.Circle(-0.08f, 0f, 0.2f, pink);
        p.Circle(-0.08f, 0f, 0.14f, new Color(80, 170, 220));
        p.Circle(-0.12f, -0.04f, 0.05f, Color.White * 0.5f);

        // Pescoço e cabeça pra frente.
        float bob = MathF.Sin(p.Time * 3f) * 0.03f;
        p.Capsule(0.38f, bob, 0.34f, 0.12f, pink, 0.2f);
        p.Circle(0.56f, 0.04f + bob, 0.1f, pink);
        p.Capsule(0.68f, 0.05f + bob, 0.14f, 0.06f, light);
        p.Circle(0.74f, 0.05f + bob, 0.035f, beak);
        p.Circle(0.58f, 0f + bob, 0.025f, beak);
    }

    private static void PaintPaperPlane(CarPainter p)
    {
        var paper = new Color(245, 245, 240);
        var fold = new Color(210, 212, 218);
        var line = new Color(150, 170, 210);
        float tilt = MathF.Sin(p.Time * 3f) * 0.06f;

        p.Shadow(1.3f, 0.7f);

        // Duas asas triangulares (faixas que afinam até o bico) e a dobra no meio.
        const int steps = 10;
        foreach (float side in new[] { 1f, -1f })
        {
            for (int i = 0; i < steps; i++)
            {
                float t = i / (float)(steps - 1);
                float x = -0.55f + (t * 1.2f);
                float w = (0.44f * (1f - t)) + 0.02f;
                p.Rect(x, side * ((w / 2f) + tilt), 0.14f, w, side > 0f ? paper : fold);
            }
        }

        p.Line(-0.55f, 0f, 0.65f, 0f, 0.035f, line);
        for (int i = 0; i < 3; i++)
        {
            p.Line(-0.4f + (i * 0.1f), 0.08f, -0.2f + (i * 0.1f), 0.08f, 0.015f, line);
        }
    }

    private static void PaintRobot(CarPainter p)
    {
        var metal = new Color(160, 168, 182);
        var dark = new Color(95, 102, 116);
        var tread = new Color(40, 42, 48);
        var eye = new Color(90, 235, 255);
        float roll = (p.Time * 5f) % 1f;

        p.Shadow(1.3f, 0.95f);
        foreach (float side in new[] { 1f, -1f })
        {
            p.Rect(0f, side * 0.38f, 1.2f, 0.2f, tread);
            for (int i = 0; i < 7; i++)
            {
                p.Rect(-0.54f + ((i + roll) * 0.17f), side * 0.38f, 0.04f, 0.2f, new Color(70, 72, 80));
            }
        }

        p.Rect(-0.08f, 0f, 0.9f, 0.62f, dark);
        p.Rect(-0.08f, 0f, 0.84f, 0.56f, metal);
        p.Rect(-0.3f, 0f, 0.2f, 0.3f, dark);
        p.Circle(-0.3f, 0.08f, 0.03f, new Color(250, 80, 80));
        p.Circle(-0.3f, -0.08f, 0.03f, new Color(80, 250, 120));

        p.Rect(0.42f, 0f, 0.34f, 0.42f, dark);
        p.Rect(0.42f, 0f, 0.3f, 0.38f, metal);
        p.Rect(0.5f, 0.1f, 0.08f, 0.1f, eye);
        p.Rect(0.5f, -0.1f, 0.08f, 0.1f, eye);
        p.Line(0.3f, 0f, 0.2f, 0.18f, 0.03f, dark);
        p.Circle(0.2f, 0.18f, 0.05f, (p.Time % 1f) < 0.5f ? new Color(250, 80, 80) : new Color(120, 40, 40));
    }
}
