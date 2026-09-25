using Microsoft.Xna.Framework;

namespace Kyrios.Game;

/// <summary>Skins absurdas — coisas que nem veículo, nem bicho, nem objeto são direito.</summary>
public static partial class CarSkins
{
    private static void PaintPetRock(CarPainter p)
    {
        var stone = new Color(135, 135, 140);
        var edge = new Color(100, 100, 106);
        var hat = new Color(30, 30, 36);
        (float X, float Y, float R)[] lumps = [(0f, 0f, 0.38f), (0.25f, 0.08f, 0.28f), (-0.26f, -0.06f, 0.3f), (0.02f, -0.22f, 0.24f)];

        p.Shadow(1.1f, 0.85f);
        foreach ((float x, float y, float r) in lumps)
        {
            p.Circle(x, y, r + 0.04f, edge);
        }

        foreach ((float x, float y, float r) in lumps)
        {
            p.Circle(x, y, r, stone);
        }

        p.Circle(-0.15f, 0.15f, 0.04f, edge);
        p.Circle(0.1f, -0.28f, 0.03f, edge);
        p.Ellipse(-0.12f, -0.12f, 0.14f, 0.06f, Color.White * 0.15f);

        // Cartola — pedra de estimação tem classe.
        p.Circle(-0.12f, 0.02f, 0.2f, hat);
        p.Circle(-0.12f, 0.02f, 0.13f, new Color(50, 50, 58));
        p.Circle(-0.12f, 0.02f, 0.13f, new Color(170, 40, 50) * 0.6f);
        p.Circle(-0.12f, 0.02f, 0.1f, hat);

        float jiggle = MathF.Sin(p.Time * 8f) * 0.025f;
        p.Circle(0.36f, 0.13f, 0.09f, Color.White);
        p.Circle(0.36f, -0.1f, 0.09f, Color.White);
        p.Circle(0.38f + jiggle, 0.13f, 0.045f, Color.Black);
        p.Circle(0.38f - jiggle, -0.1f + jiggle, 0.045f, Color.Black);
    }

    /// <summary>O obstáculo laranja do Contra o Relógio — agora do lado de cá, e bravo.</summary>
    private static void PaintHazardBall(CarPainter p)
    {
        var orange = new Color(230, 122, 40);
        var cap = new Color(35, 34, 38);
        float pulse = MathF.Sin(p.Time * 6f) * 0.02f;

        p.ShadowCircle(0.5f);
        p.Circle(0f, 0f, 0.48f + pulse, orange);
        p.Circle(-0.08f, -0.1f, 0.18f, new Color(250, 165, 90));
        p.Circle(0f, 0f, 0.24f, cap);
        p.Rect(0.26f, 0.13f, 0.1f, 0.03f, cap, -0.5f);
        p.Rect(0.26f, -0.13f, 0.1f, 0.03f, cap, 0.5f);
        p.Circle(0.3f, 0.12f, 0.05f, Color.White);
        p.Circle(0.3f, -0.12f, 0.05f, Color.White);
        p.Circle(0.32f, 0.12f, 0.025f, Color.Black);
        p.Circle(0.32f, -0.12f, 0.025f, Color.Black);
    }

    private static void PaintMagicCarpet(CarPainter p)
    {
        var purple = new Color(120, 50, 150);
        var gold = new Color(240, 190, 60);
        var red = new Color(200, 50, 70);

        p.Shadow(1.4f, 0.85f);
        float wave = p.Time * 6f;

        // O tapete ondula em faixas; franjas balançando nas pontas.
        for (int i = 0; i < 8; i++)
        {
            float x = -0.56f + (i * 0.16f);
            float y = MathF.Sin(wave + i) * 0.03f;
            p.Rect(x, y, 0.17f, 0.8f, gold);
            p.Rect(x, y, 0.17f, 0.68f, purple);
            p.Rect(x, y, 0.17f, 0.12f, red);
        }

        for (int i = 0; i < 6; i++)
        {
            float y = -0.3f + (i * 0.12f);
            p.Line(-0.64f, y, -0.74f, y + (MathF.Sin(wave + i) * 0.04f), 0.025f, gold);
            p.Line(0.64f, y, 0.72f, y + (MathF.Sin(wave + i + 1f) * 0.04f), 0.025f, gold);
        }

        p.Rect(0f, 0f, 0.2f, 0.2f, gold, MathF.PI / 4f);
        p.Rect(0f, 0f, 0.12f, 0.12f, red, MathF.PI / 4f);
    }

    private static void PaintSaturn(CarPainter p)
    {
        var planet = new Color(225, 190, 120);
        var band = new Color(195, 150, 90);
        var ring = new Color(210, 200, 170);
        float tilt = 0.3f + (MathF.Sin(p.Time * 0.8f) * 0.1f);

        p.ShadowCircle(0.45f);
        p.Ellipse(0f, 0f, 0.72f, 0.2f, ring * 0.9f, tilt);
        p.Ellipse(0f, 0f, 0.6f, 0.14f, new Color(0, 0, 0, 120), tilt);
        p.Circle(0f, 0f, 0.36f, planet);
        p.Ellipse(0f, 0.12f, 0.34f, 0.05f, band, 0.1f);
        p.Ellipse(0f, -0.1f, 0.33f, 0.04f, band, 0.1f);
        p.Circle(-0.1f, -0.12f, 0.1f, Color.White * 0.2f);

        // A metade da frente do anel passa por cima do planeta.
        for (int i = 0; i < 12; i++)
        {
            float a = (i / 11f * MathF.PI) - (MathF.PI / 2f);
            float x = MathF.Cos(a) * 0.66f;
            float y = MathF.Sin(a) * 0.17f;
            float rx = (x * MathF.Cos(tilt)) - (y * MathF.Sin(tilt));
            float ry = (x * MathF.Sin(tilt)) + (y * MathF.Cos(tilt));
            p.Circle(rx, ry, 0.04f, ring);
        }
    }

    private static void PaintBlackHole(CarPainter p)
    {
        float spin = p.Time * 3f;

        p.ShadowCircle(0.5f);
        p.Circle(0f, 0f, 0.55f, new Color(120, 60, 200) * 0.25f);
        for (int arm = 0; arm < 3; arm++)
        {
            for (int s = 0; s < 10; s++)
            {
                float t = s / 9f;
                float a = spin + (arm * MathF.Tau / 3f) + (t * 2.6f);
                float r = 0.5f - (t * 0.32f);
                Color color = Color.Lerp(new Color(255, 170, 60), new Color(255, 240, 200), t);
                p.Circle(MathF.Cos(a) * r, MathF.Sin(a) * r, 0.07f - (t * 0.03f), color);
            }
        }

        p.Circle(0f, 0f, 0.2f, new Color(255, 230, 180));
        p.Circle(0f, 0f, 0.17f, new Color(5, 5, 10));
    }

    private static void PaintTrophy(CarPainter p)
    {
        var gold = new Color(250, 200, 50);
        var goldDark = new Color(200, 140, 30);
        var goldLight = new Color(255, 235, 140);
        var baseColor = new Color(90, 60, 40);

        p.Shadow(1.1f, 1f);
        p.Rect(-0.35f, 0f, 0.4f, 0.8f, baseColor);
        p.Rect(-0.35f, 0f, 0.3f, 0.7f, CarPainter.Darken(baseColor, 1.3f));
        p.Rect(-0.35f, 0f, 0.12f, 0.4f, goldLight);
        p.Circle(0.28f, 0.38f, 0.12f, goldDark);
        p.Circle(0.28f, 0.38f, 0.06f, new Color(0, 0, 0, 120));
        p.Circle(0.28f, -0.38f, 0.12f, goldDark);
        p.Circle(0.28f, -0.38f, 0.06f, new Color(0, 0, 0, 120));
        p.Circle(0.2f, 0f, 0.4f, goldDark);
        p.Circle(0.2f, 0f, 0.35f, gold);
        p.Circle(0.2f, 0f, 0.22f, goldDark);
        p.Circle(0.2f, 0f, 0.19f, new Color(170, 110, 20));
        p.Ellipse(0.1f, -0.2f, 0.12f, 0.05f, goldLight, -0.5f);

        // Brilho passando de tempos em tempos.
        float sweep = (p.Time * 0.7f) % 1f;
        if (!p.Eliminated && sweep < 0.3f)
        {
            float t = sweep / 0.3f;
            p.Rect(-0.2f + (t * 0.8f), 0f, 0.06f, 0.8f, Color.White * 0.5f, 0.5f);
        }
    }

    private static void PaintGhost(CarPainter p)
    {
        float bob = MathF.Sin(p.Time * 3f) * 0.05f;
        Color sheet = Color.White * 0.78f;

        p.Shadow(1.1f, 0.8f);

        // Barra ondulada atrás (o "lençol" esvoaçando) e a cabeça redonda na frente.
        for (int i = 0; i < 5; i++)
        {
            float y = -0.32f + (i * 0.16f);
            p.Circle(-0.42f + (MathF.Sin((p.Time * 8f) + i) * 0.05f), y, 0.12f, sheet);
        }

        p.Rect(-0.15f + bob, 0f, 0.5f, 0.8f, sheet);
        p.Circle(0.12f + bob, 0f, 0.42f, sheet);
        p.Ellipse(0.28f + bob, 0.14f, 0.06f, 0.08f, new Color(20, 20, 30));
        p.Ellipse(0.28f + bob, -0.14f, 0.06f, 0.08f, new Color(20, 20, 30));
        p.Ellipse(0.42f + bob, 0f, 0.05f, 0.07f, new Color(20, 20, 30));
    }

    /// <summary>Um pixel que fugiu de algum outro jogo: um quadrado com o sinal "falhando" de vez em quando.</summary>
    private static void PaintLostPixel(CarPainter p)
    {
        var magenta = new Color(255, 0, 200);
        bool glitch = (p.Time * 2.3f % 1f) < 0.18f;
        float jitter = glitch ? 0.08f : 0f;

        p.Shadow(0.9f, 0.9f);
        if (glitch)
        {
            p.Rect(-jitter, jitter, 0.86f, 0.86f, new Color(0, 255, 255) * 0.8f);
            p.Rect(jitter, -jitter, 0.86f, 0.86f, new Color(255, 255, 0) * 0.8f);
        }

        p.Rect(0f, 0f, 0.86f, 0.86f, magenta);
        p.Rect(-0.2f, -0.2f, 0.22f, 0.22f, new Color(255, 120, 230));
        if (glitch)
        {
            p.Rect(0f, 0.1f, 1.1f, 0.08f, Color.White);
        }
    }
}
