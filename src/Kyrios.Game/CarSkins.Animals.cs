using Microsoft.Xna.Framework;

namespace Kyrios.Game;

/// <summary>Skins de bichos (vista de cima, cabeça pra frente = +x).</summary>
public static partial class CarSkins
{
    private static void PaintSheep(CarPainter p)
    {
        var wool = new Color(242, 242, 236);
        var woolShade = new Color(208, 208, 200);
        var face = new Color(44, 40, 44);
        float trot = MathF.Sin(p.Time * 12f) * 0.04f;

        p.Shadow(1.25f, 0.9f);
        foreach (float x in new[] { 0.28f, -0.3f })
        {
            p.Rect(x + trot, 0.36f, 0.12f, 0.1f, face);
            p.Rect(x - trot, -0.36f, 0.12f, 0.1f, face);
        }

        for (int i = 0; i < 10; i++)
        {
            float a = i * MathF.Tau / 10f;
            p.Circle(-0.06f + (MathF.Cos(a) * 0.36f), MathF.Sin(a) * 0.25f, 0.18f, woolShade);
        }

        p.Ellipse(-0.06f, 0f, 0.44f, 0.32f, wool);
        p.Circle(-0.2f, -0.1f, 0.12f, Color.White);
        p.Circle(0.06f, 0.12f, 0.1f, Color.White);

        p.Capsule(0.5f, 0f, 0.34f, 0.26f, face);
        p.Rect(0.42f, 0.18f, 0.14f, 0.07f, face, 0.7f);
        p.Rect(0.42f, -0.18f, 0.14f, 0.07f, face, -0.7f);
        p.Circle(0.4f, 0f, 0.1f, wool);
        p.Circle(0.58f, 0.07f, 0.03f, Color.White);
        p.Circle(0.58f, -0.07f, 0.03f, Color.White);
    }

    private static void PaintPig(CarPainter p)
    {
        var pink = new Color(245, 165, 175);
        var dark = new Color(215, 120, 135);
        var snout = new Color(235, 130, 145);

        p.Shadow(1.3f, 0.82f);
        foreach (float x in new[] { 0.28f, -0.3f })
        {
            p.Rect(x, 0.36f, 0.14f, 0.1f, dark);
            p.Rect(x, -0.36f, 0.14f, 0.1f, dark);
        }

        // Rabinho enrolado balançando.
        float wag = MathF.Sin(p.Time * 9f) * 0.05f;
        for (int i = 0; i < 6; i++)
        {
            float a = i * 1.1f;
            p.Circle(-0.66f - (i * 0.012f) + (MathF.Cos(a) * 0.05f), wag + (MathF.Sin(a) * 0.05f), 0.03f, dark);
        }

        p.Capsule(-0.02f, 0f, 1.2f, 0.72f, pink);
        p.Ellipse(-0.1f, -0.12f, 0.3f, 0.12f, Color.White * 0.25f);
        p.Rect(0.34f, 0.24f, 0.18f, 0.12f, dark, 0.6f);
        p.Rect(0.34f, -0.24f, 0.18f, 0.12f, dark, -0.6f);
        p.Circle(0.46f, 0.15f, 0.04f, Color.Black);
        p.Circle(0.46f, -0.15f, 0.04f, Color.Black);
        p.Ellipse(0.6f, 0f, 0.1f, 0.16f, snout);
        p.Circle(0.62f, 0.06f, 0.03f, dark);
        p.Circle(0.62f, -0.06f, 0.03f, dark);
    }

    private static void PaintTurtle(CarPainter p)
    {
        var shell = new Color(70, 120, 60);
        var plate = new Color(110, 160, 80);
        var rim = new Color(50, 90, 45);
        var skin = new Color(150, 190, 110);
        float paddle = MathF.Sin(p.Time * 5f) * 0.3f;
        float peek = 0.04f * MathF.Sin(p.Time * 2f);

        p.Shadow(1.2f, 0.85f);
        p.Ellipse(0.26f, 0.36f, 0.16f, 0.07f, skin, -0.6f + paddle);
        p.Ellipse(0.26f, -0.36f, 0.16f, 0.07f, skin, 0.6f - paddle);
        p.Ellipse(-0.3f, 0.3f, 0.12f, 0.06f, skin, 0.6f - paddle);
        p.Ellipse(-0.3f, -0.3f, 0.12f, 0.06f, skin, -0.6f + paddle);
        p.Rect(-0.55f, 0f, 0.12f, 0.06f, skin);

        p.Capsule(0.58f + peek, 0f, 0.3f, 0.2f, skin);
        p.Circle(0.66f + peek, 0.06f, 0.025f, Color.Black);
        p.Circle(0.66f + peek, -0.06f, 0.025f, Color.Black);

        p.Ellipse(0f, 0f, 0.5f, 0.4f, rim);
        p.Ellipse(0f, 0f, 0.45f, 0.35f, shell);
        p.Circle(0f, 0f, 0.13f, plate);
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f;
            p.Circle(MathF.Cos(a) * 0.27f, MathF.Sin(a) * 0.2f, 0.09f, plate);
        }
    }

    private static void PaintBee(CarPainter p)
    {
        var yellow = new Color(250, 205, 45);
        var black = new Color(35, 30, 30);
        float flap = 0.08f + (MathF.Abs(MathF.Sin(p.Time * 40f)) * 0.08f);

        p.Shadow(1.2f, 0.7f);
        p.Rect(-0.68f, 0f, 0.14f, 0.05f, black);
        p.Capsule(-0.02f, 0f, 1.15f, 0.62f, yellow);
        foreach (float x in new[] { -0.36f, -0.12f, 0.12f })
        {
            p.Rect(x, 0f, 0.1f, 0.6f, black);
        }

        // Asas translúcidas batendo rápido.
        p.Ellipse(-0.05f, 0.34f, 0.26f, flap + 0.08f, new Color(210, 235, 255) * 0.75f, 0.35f);
        p.Ellipse(-0.05f, -0.34f, 0.26f, flap + 0.08f, new Color(210, 235, 255) * 0.75f, -0.35f);

        p.Circle(0.46f, 0f, 0.2f, black);
        p.Circle(0.52f, 0.1f, 0.07f, new Color(80, 80, 90));
        p.Circle(0.52f, -0.1f, 0.07f, new Color(80, 80, 90));
        p.Line(0.6f, 0.05f, 0.76f, 0.16f, 0.03f, black);
        p.Line(0.6f, -0.05f, 0.76f, -0.16f, 0.03f, black);
    }

    private static void PaintOctopus(CarPainter p)
    {
        var purple = new Color(170, 90, 200);
        var dark = new Color(125, 60, 155);
        var spot = new Color(210, 150, 230);

        p.Shadow(1.3f, 0.9f);

        // Oito tentáculos ondulando pra trás (cada um uma corrente de bolinhas encostadas).
        for (int t = 0; t < 8; t++)
        {
            float baseY = -0.3f + (t * 0.086f);
            for (int s = 0; s < 9; s++)
            {
                float x = 0.1f - (s * 0.08f);
                float spread = baseY * (1f + (s * 0.1f));
                float y = spread + (MathF.Sin((p.Time * 7f) + (s * 0.8f) + t) * 0.02f * s);
                p.Circle(x, y, 0.075f - (s * 0.006f), t % 2 == 0 ? dark : purple);
            }
        }

        p.Ellipse(0.3f, 0f, 0.36f, 0.34f, purple);
        p.Circle(0.38f, -0.12f, 0.06f, spot);
        p.Circle(0.2f, 0.16f, 0.05f, spot);
        p.Circle(0.44f, 0.16f, 0.08f, Color.White);
        p.Circle(0.44f, -0.02f, 0.08f, Color.White);
        p.Circle(0.47f, 0.17f, 0.04f, Color.Black);
        p.Circle(0.47f, -0.01f, 0.04f, Color.Black);
    }

    private static void PaintCapybara(CarPainter p)
    {
        var fur = new Color(160, 115, 75);
        var dark = new Color(115, 80, 50);
        var orange = new Color(250, 150, 40);

        p.Shadow(1.3f, 0.85f);
        foreach (float x in new[] { 0.3f, -0.32f })
        {
            p.Rect(x, 0.36f, 0.12f, 0.1f, dark);
            p.Rect(x, -0.36f, 0.12f, 0.1f, dark);
        }

        p.Capsule(-0.06f, 0f, 1.12f, 0.74f, fur);
        p.Ellipse(-0.12f, -0.14f, 0.32f, 0.14f, Color.White * 0.12f);
        p.Capsule(0.5f, 0f, 0.4f, 0.46f, fur);
        p.Rect(0.34f, 0.2f, 0.08f, 0.08f, dark);
        p.Rect(0.34f, -0.2f, 0.08f, 0.08f, dark);
        p.Rect(0.68f, 0f, 0.08f, 0.2f, dark);
        p.Circle(0.54f, 0.12f, 0.03f, Color.Black);
        p.Circle(0.54f, -0.12f, 0.03f, Color.Black);

        // A laranja equilibrada na cabeça — capivara que é capivara tem.
        p.Circle(0.42f, 0f, 0.13f, orange);
        p.Circle(0.4f, -0.04f, 0.04f, Color.White * 0.35f);
        p.Rect(0.36f, 0.05f, 0.06f, 0.04f, new Color(70, 150, 60), 0.4f);
    }

    private static void PaintCow(CarPainter p)
    {
        var white = new Color(245, 245, 240);
        var black = new Color(35, 32, 34);
        var pink = new Color(240, 170, 170);
        var horn = new Color(235, 225, 190);

        p.Shadow(1.4f, 0.85f);
        foreach (float x in new[] { 0.3f, -0.34f })
        {
            p.Rect(x, 0.38f, 0.12f, 0.1f, black);
            p.Rect(x, -0.38f, 0.12f, 0.1f, black);
        }

        float swish = MathF.Sin(p.Time * 5f) * 0.25f;
        p.Rect(-0.7f, 0f, 0.2f, 0.04f, black, swish);
        p.Capsule(-0.04f, 0f, 1.25f, 0.76f, white);
        p.Ellipse(-0.3f, 0.14f, 0.18f, 0.14f, black, 0.4f);
        p.Ellipse(0.08f, -0.18f, 0.16f, 0.12f, black, -0.3f);
        p.Ellipse(-0.08f, 0.22f, 0.08f, 0.06f, black);
        p.Circle(-0.44f, -0.16f, 0.07f, black);

        p.Capsule(0.54f, 0f, 0.36f, 0.42f, white);
        p.Rect(0.5f, 0.26f, 0.08f, 0.14f, horn, -0.4f);
        p.Rect(0.5f, -0.26f, 0.08f, 0.14f, horn, 0.4f);
        p.Ellipse(0.7f, 0f, 0.09f, 0.18f, pink);
        p.Circle(0.72f, 0.06f, 0.025f, black);
        p.Circle(0.72f, -0.06f, 0.025f, black);
        p.Circle(0.55f, 0.12f, 0.035f, black);
        p.Circle(0.55f, -0.12f, 0.035f, black);
    }

    private static void PaintWhale(CarPainter p)
    {
        var blue = new Color(60, 105, 170);
        var light = new Color(120, 165, 215);
        float wag = MathF.Sin(p.Time * 4f) * 0.25f;

        p.Shadow(1.5f, 0.85f);
        p.Ellipse(-0.72f, 0.12f, 0.16f, 0.08f, blue, 0.7f + wag);
        p.Ellipse(-0.72f, -0.12f, 0.16f, 0.08f, blue, -0.7f + wag);
        p.Capsule(-0.52f, 0f, 0.4f, 0.24f, blue, wag * 0.4f);
        p.Capsule(0.04f, 0f, 1.25f, 0.8f, blue);
        p.Ellipse(0.1f, 0.18f, 0.4f, 0.12f, light);
        p.Ellipse(0.05f, 0.34f, 0.16f, 0.06f, light, -0.4f);
        p.Ellipse(0.05f, -0.34f, 0.16f, 0.06f, light, 0.4f);
        p.Circle(0.46f, 0.26f, 0.04f, Color.Black);
        p.Circle(0.46f, -0.26f, 0.04f, Color.Black);

        // Esguicho do respiradouro, de tempos em tempos.
        float phase = (p.Time * 0.8f) % 1f;
        if (phase < 0.5f)
        {
            float t = phase / 0.5f;
            for (int i = 0; i < 5; i++)
            {
                float a = i * MathF.Tau / 5f;
                p.Circle(0.24f + (MathF.Cos(a) * t * 0.22f), MathF.Sin(a) * t * 0.22f, 0.05f * (1f - t), new Color(200, 230, 255));
            }
        }

        p.Circle(0.24f, 0f, 0.04f, new Color(40, 70, 120));
    }

    private static void PaintPenguin(CarPainter p)
    {
        var black = new Color(30, 32, 40);
        var white = new Color(245, 245, 250);
        var orange = new Color(250, 150, 40);
        float flap = MathF.Sin(p.Time * 10f) * 0.2f;

        // Deslizando de barriga, como pinguim no gelo.
        p.Shadow(1.3f, 0.7f);
        p.Ellipse(-0.64f, 0.08f, 0.1f, 0.06f, orange, 0.3f);
        p.Ellipse(-0.64f, -0.08f, 0.1f, 0.06f, orange, -0.3f);
        p.Ellipse(0.02f, 0.36f, 0.24f, 0.07f, black, -0.3f + flap);
        p.Ellipse(0.02f, -0.36f, 0.24f, 0.07f, black, 0.3f - flap);
        p.Capsule(-0.02f, 0f, 1.2f, 0.62f, black);
        p.Capsule(0.02f, 0f, 0.8f, 0.36f, white);
        p.Circle(0.42f, 0f, 0.22f, black);
        p.Circle(0.47f, 0.09f, 0.05f, white);
        p.Circle(0.47f, -0.09f, 0.05f, white);
        p.Circle(0.49f, 0.09f, 0.025f, Color.Black);
        p.Circle(0.49f, -0.09f, 0.025f, Color.Black);
        p.Capsule(0.67f, 0f, 0.14f, 0.1f, orange);
    }

    private static void PaintDragon(CarPainter p)
    {
        var red = new Color(200, 50, 45);
        var dark = new Color(140, 30, 30);
        var belly = new Color(245, 190, 90);
        var wing = new Color(170, 40, 40);
        float flap = MathF.Sin(p.Time * 6f) * 0.25f;

        p.Shadow(1.5f, 1f);
        p.Capsule(-0.55f, 0f, 0.5f, 0.16f, red, MathF.Sin(p.Time * 4f) * 0.3f);
        p.Rect(-0.84f, 0f, 0.14f, 0.14f, dark, MathF.PI / 4f);

        // Asas abertas batendo.
        foreach (float side in new[] { 1f, -1f })
        {
            p.Line(0f, side * 0.18f, -0.25f, side * (0.5f + flap), 0.05f, dark);
            p.Line(-0.25f, side * (0.5f + flap), -0.42f, side * 0.2f, 0.04f, dark);
            for (int i = 0; i < 4; i++)
            {
                float t = i / 3f;
                p.Ellipse(-0.1f - (t * 0.3f), side * (0.3f + (flap * 0.6f) - (t * 0.08f)), 0.12f, 0.1f, wing);
            }
        }

        p.Capsule(0f, 0f, 0.9f, 0.46f, red);
        p.Capsule(-0.02f, 0f, 0.6f, 0.2f, belly);
        for (float x = -0.3f; x <= 0.3f; x += 0.15f)
        {
            p.Rect(x, 0.2f, 0.07f, 0.07f, dark, MathF.PI / 4f);
        }

        p.Capsule(0.52f, 0f, 0.36f, 0.3f, red);
        p.Rect(0.46f, 0.14f, 0.16f, 0.05f, belly, -0.5f);
        p.Rect(0.46f, -0.14f, 0.16f, 0.05f, belly, 0.5f);
        p.Circle(0.58f, 0.08f, 0.035f, new Color(255, 230, 80));
        p.Circle(0.58f, -0.08f, 0.035f, new Color(255, 230, 80));

        // Uma baforada de fogo de vez em quando.
        float breath = (p.Time * 0.6f) % 1f;
        if (breath < 0.25f && !p.Eliminated)
        {
            float t = breath / 0.25f;
            p.Circle(0.78f + (t * 0.15f), 0f, 0.08f + (t * 0.06f), new Color(255, 150, 40));
            p.Circle(0.76f + (t * 0.1f), 0f, 0.05f + (t * 0.03f), new Color(255, 235, 110));
        }
    }

    private static void PaintCrab(CarPainter p)
    {
        var red = new Color(225, 80, 55);
        var dark = new Color(170, 50, 35);
        float snap = MathF.Max(0f, MathF.Sin(p.Time * 6f)) * 0.25f;
        float scuttle = MathF.Sin(p.Time * 16f) * 0.05f;

        p.Shadow(1.1f, 1f);
        for (int i = 0; i < 3; i++)
        {
            float x = -0.25f + (i * 0.16f);
            p.Line(x, 0.3f, x - 0.1f + scuttle, 0.5f, 0.05f, dark);
            p.Line(x, -0.3f, x - 0.1f - scuttle, -0.5f, 0.05f, dark);
        }

        foreach (float side in new[] { 1f, -1f })
        {
            p.Line(0.25f, side * 0.22f, 0.45f, side * 0.32f, 0.07f, dark);
            p.Circle(0.52f, side * 0.33f, 0.12f, red);
            p.Rect(0.65f, side * (0.3f + (snap * 0.1f)), 0.14f, 0.05f, dark, side * snap);
            p.Rect(0.65f, side * (0.37f - (snap * 0.1f)), 0.14f, 0.05f, dark, -side * snap);
        }

        p.Ellipse(0f, 0f, 0.36f, 0.34f, red);
        p.Ellipse(-0.04f, -0.08f, 0.18f, 0.12f, Color.White * 0.15f);
        p.Line(0.25f, 0.08f, 0.4f, 0.1f, 0.03f, dark);
        p.Line(0.25f, -0.08f, 0.4f, -0.1f, 0.03f, dark);
        p.Circle(0.42f, 0.1f, 0.045f, Color.Black);
        p.Circle(0.42f, -0.1f, 0.045f, Color.Black);
    }

    private static void PaintCat(CarPainter p)
    {
        var orange = new Color(235, 150, 60);
        var stripe = new Color(190, 105, 35);
        var light = new Color(250, 215, 170);

        p.Shadow(1.35f, 0.8f);
        float tail = MathF.Sin(p.Time * 3f) * 0.5f;
        for (int i = 0; i < 6; i++)
        {
            float t = i / 5f;
            p.Circle(-0.5f - (t * 0.25f), MathF.Sin(tail + (t * 2f)) * t * 0.22f, 0.07f, i % 2 == 0 ? orange : stripe);
        }

        foreach (float x in new[] { 0.3f, -0.3f })
        {
            p.Circle(x, 0.3f, 0.08f, light);
            p.Circle(x, -0.3f, 0.08f, light);
        }

        p.Capsule(-0.04f, 0f, 1f, 0.58f, orange);
        foreach (float x in new[] { -0.3f, -0.12f, 0.06f })
        {
            p.Rect(x, 0f, 0.06f, 0.5f, stripe);
        }

        p.Circle(0.46f, 0f, 0.24f, orange);
        p.Rect(0.42f, 0.2f, 0.16f, 0.1f, orange, 0.8f);
        p.Rect(0.42f, -0.2f, 0.16f, 0.1f, orange, -0.8f);
        p.Rect(0.42f, 0.2f, 0.08f, 0.05f, new Color(240, 160, 170), 0.8f);
        p.Rect(0.42f, -0.2f, 0.08f, 0.05f, new Color(240, 160, 170), -0.8f);
        p.Ellipse(0.55f, 0.1f, 0.04f, 0.05f, new Color(120, 200, 80));
        p.Ellipse(0.55f, -0.1f, 0.04f, 0.05f, new Color(120, 200, 80));
        p.Rect(0.56f, 0.1f, 0.02f, 0.05f, Color.Black);
        p.Rect(0.56f, -0.1f, 0.02f, 0.05f, Color.Black);
        p.Circle(0.66f, 0f, 0.03f, new Color(230, 120, 140));
    }

    private static void PaintAnt(CarPainter p)
    {
        var body = new Color(50, 35, 30);
        var shine = new Color(110, 80, 70);
        float walk = MathF.Sin(p.Time * 18f) * 0.06f;

        p.Shadow(1.4f, 0.6f);
        for (int i = 0; i < 3; i++)
        {
            float x = -0.1f + (i * 0.14f);
            float w = i % 2 == 0 ? walk : -walk;
            p.Line(x, 0.05f, x - 0.12f + w, 0.42f, 0.04f, body);
            p.Line(x, -0.05f, x - 0.12f - w, -0.42f, 0.04f, body);
        }

        p.Ellipse(-0.42f, 0f, 0.26f, 0.2f, body);
        p.Ellipse(0.02f, 0f, 0.16f, 0.12f, body);
        p.Circle(0.35f, 0f, 0.17f, body);
        p.Ellipse(-0.46f, -0.07f, 0.1f, 0.05f, shine);
        p.Circle(0.32f, -0.06f, 0.04f, shine);
        p.Line(0.45f, 0.06f, 0.66f, 0.22f + walk, 0.03f, body);
        p.Line(0.45f, -0.06f, 0.66f, -0.22f - walk, 0.03f, body);
    }
}
