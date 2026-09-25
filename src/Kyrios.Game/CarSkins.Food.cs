using Microsoft.Xna.Framework;

namespace Kyrios.Game;

/// <summary>Skins de comida (vista de cima).</summary>
public static partial class CarSkins
{
    private static void PaintDonut(CarPainter p)
    {
        var dough = new Color(205, 145, 80);
        var icing = new Color(245, 130, 180);
        var icingLight = new Color(250, 175, 210);
        Color[] sprinkles = [new(255, 255, 255), new(90, 200, 250), new(250, 220, 60), new(120, 220, 110)];
        float spin = p.Time * 1.5f;

        p.ShadowCircle(0.5f);
        p.Circle(0f, 0f, 0.5f, dough);
        for (int i = 0; i < 9; i++)
        {
            float a = i * MathF.Tau / 9f;
            p.Circle(MathF.Cos(a) * 0.3f, MathF.Sin(a) * 0.3f, 0.17f, icing);
        }

        p.Circle(0f, 0f, 0.38f, icing);
        p.Circle(-0.1f, -0.12f, 0.12f, icingLight);
        for (int i = 0; i < 12; i++)
        {
            float a = (i * MathF.Tau / 12f) + spin;
            float r = i % 2 == 0 ? 0.3f : 0.22f;
            p.Rect(MathF.Cos(a) * r, MathF.Sin(a) * r, 0.08f, 0.025f, sprinkles[i % sprinkles.Length], a * 2f);
        }

        // O furo: o chão aparecendo (sombra) com a borda da massa.
        p.Circle(0f, 0f, 0.14f, dough);
        p.Circle(0f, 0f, 0.11f, new Color(0, 0, 0, 150));
    }

    private static void PaintBurger(CarPainter p)
    {
        var bun = new Color(215, 140, 60);
        var bunLight = new Color(240, 180, 95);
        var lettuce = new Color(110, 190, 70);
        var cheese = new Color(250, 200, 50);
        var patty = new Color(95, 55, 35);
        var seed = new Color(250, 240, 210);

        p.ShadowCircle(0.52f);
        p.Circle(0f, 0f, 0.5f, patty);
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.Tau / 12f;
            p.Circle(MathF.Cos(a) * 0.4f, MathF.Sin(a) * 0.4f, 0.1f, lettuce);
        }

        p.Rect(0f, 0f, 0.78f, 0.78f, cheese, MathF.PI / 4f);
        p.Circle(0f, 0f, 0.43f, bun);
        p.Circle(-0.08f, -0.1f, 0.26f, bunLight);
        foreach ((float x, float y, float r) in new[] { (0.2f, 0.1f, 0.4f), (-0.1f, 0.22f, -0.3f), (0.05f, -0.2f, 1f), (-0.25f, -0.02f, 0.2f), (0.25f, -0.15f, -0.8f), (-0.05f, 0.02f, 0.6f) })
        {
            p.Ellipse(x, y, 0.045f, 0.025f, seed, r);
        }
    }

    private static void PaintHotDog(CarPainter p)
    {
        var bun = new Color(225, 160, 80);
        var bunLight = new Color(245, 195, 120);
        var sausage = new Color(185, 70, 50);
        var mustard = new Color(250, 210, 40);

        p.Shadow(1.4f, 0.6f);
        p.Capsule(0f, 0f, 1.25f, 0.58f, bun);
        p.Capsule(0f, -0.14f, 1.1f, 0.18f, bunLight);
        p.Capsule(0f, 0.02f, 1.45f, 0.3f, sausage);
        p.Ellipse(0.2f, -0.03f, 0.3f, 0.04f, Color.White * 0.25f);

        // Mostarda em zigue-zague.
        for (int i = 0; i < 8; i++)
        {
            float x1 = -0.5f + (i * 0.13f);
            float x2 = x1 + 0.13f;
            p.Line(x1, i % 2 == 0 ? -0.07f : 0.1f, x2, i % 2 == 0 ? 0.1f : -0.07f, 0.045f, mustard);
        }
    }

    private static void PaintFriedEgg(CarPainter p)
    {
        var white = new Color(250, 250, 245);
        var edge = new Color(230, 220, 200);
        var yolk = new Color(255, 185, 30);

        p.Shadow(1.3f, 1f);
        (float X, float Y, float R)[] blobs = [(0f, 0f, 0.42f), (0.28f, 0.14f, 0.28f), (-0.3f, 0.12f, 0.3f), (-0.1f, -0.28f, 0.28f), (0.25f, -0.2f, 0.24f)];
        foreach ((float x, float y, float r) in blobs)
        {
            p.Circle(x, y, r + 0.03f, edge);
        }

        foreach ((float x, float y, float r) in blobs)
        {
            p.Circle(x, y, r, white);
        }

        float wobble = MathF.Sin(p.Time * 6f) * 0.015f;
        p.Circle(0.08f, 0f, 0.2f + wobble, new Color(235, 150, 20));
        p.Circle(0.08f, 0f, 0.17f + wobble, yolk);
        p.Circle(0.03f, -0.06f, 0.05f, new Color(255, 235, 160));
    }

    private static void PaintSushi(CarPainter p)
    {
        var rice = new Color(248, 248, 240);
        var riceShade = new Color(220, 220, 210);
        var salmon = new Color(250, 130, 90);
        var salmonLine = new Color(255, 205, 180);
        var nori = new Color(30, 45, 35);

        p.Shadow(1.3f, 0.75f);
        p.Capsule(0f, 0f, 1.22f, 0.66f, riceShade);
        p.Capsule(0f, 0f, 1.16f, 0.6f, rice);
        for (int i = 0; i < 14; i++)
        {
            p.Ellipse(-0.5f + (i * 0.075f), (i % 3 - 1) * 0.22f, 0.03f, 0.018f, riceShade, i);
        }

        p.Capsule(0.02f, 0f, 1.1f, 0.5f, salmon);
        for (int i = 0; i < 6; i++)
        {
            float x = -0.36f + (i * 0.15f);
            p.Rect(x, 0f, 0.035f, 0.44f, salmonLine, 0.35f);
        }

        p.Rect(0f, 0f, 0.2f, 0.7f, nori);
    }

    private static void PaintCoxinha(CarPainter p)
    {
        var crust = new Color(215, 140, 55);
        var dark = new Color(170, 100, 35);
        var crumb = new Color(240, 185, 100);
        const int steps = 9;

        p.Shadow(1.25f, 0.85f);

        // Gota: base redonda atrás, afinando até o bico na frente.
        foreach (bool outline in new[] { true, false })
        {
            for (int i = 0; i < steps; i++)
            {
                float t = i / (float)(steps - 1);
                float x = -0.3f + (t * 0.9f);
                float r = (0.4f * (1f - t)) + 0.05f;
                p.Circle(x, 0f, outline ? r + 0.03f : r, outline ? dark : crust);
            }
        }

        p.Circle(0.62f, 0f, 0.05f, dark);
        foreach ((float x, float y) in new[] { (-0.35f, 0.15f), (-0.2f, -0.22f), (0f, 0.12f), (0.15f, -0.08f), (-0.45f, -0.05f), (0.3f, 0.05f), (-0.1f, -0.02f) })
        {
            p.Rect(x, y, 0.05f, 0.05f, crumb, x * 7f);
        }

        p.Ellipse(-0.25f, -0.15f, 0.14f, 0.07f, Color.White * 0.2f);
    }
}
