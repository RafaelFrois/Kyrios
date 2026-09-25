using Microsoft.Xna.Framework;

namespace Kyrios.Game;

/// <summary>Cenários fora da Terra: estação espacial e planeta alienígena.</summary>
public static partial class TrackSceneries
{
    // ---------- ESTAÇÃO ESPACIAL ----------

    private static readonly Vector2 EarthCenter = new(330f, 244f);
    private static readonly Vector2 StationCenter = new(700f, 244f);

    private static void Starfield(SceneCanvas c, Rectangle area, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float size = c.Chance(0.12) ? 2f : 1f;
            c.Rect(area.X + c.R(0f, area.Width), area.Y + c.R(0f, area.Height), size, size, c.Pick(Color.White, new Color(200, 220, 255), new Color(255, 240, 200)) * c.R(0.4f, 1f));
        }
    }

    private static void Asteroid(SceneCanvas c, float x, float y, float r)
    {
        var rock = new Color(96, 92, 100);
        c.Circle(x + 2f, y + 3f, r, Color.Black * 0.3f);
        c.Circle(x, y, r, rock);
        c.Circle(x + (r * 0.4f), y - (r * 0.2f), r * 0.6f, rock);
        c.Circle(x - (r * 0.3f), y + (r * 0.3f), r * 0.28f, new Color(70, 66, 74));
        c.Circle(x + (r * 0.3f), y - (r * 0.35f), r * 0.18f, new Color(70, 66, 74));
        c.Circle(x - (r * 0.35f), y - (r * 0.35f), r * 0.25f, Color.White * 0.12f);
    }

    public static SceneryStyle Space { get; } = new()
    {
        Background = new Color(4, 4, 14),
        GroundA = new Color(10, 10, 24),
        GroundB = new Color(8, 8, 20),
        Track = new Color(62, 68, 86),
        TrackSpeck = new Color(82, 88, 108),
        Line = new Color(80, 220, 255),
        Edge = EdgeStyle.Glow,
        EdgeBase = new Color(18, 20, 34),
        EdgeA = new Color(80, 220, 255),
        EdgeB = new Color(230, 240, 255),
        Checkpoint = new Color(80, 220, 255),
        Darkness = 0.12f,
        Headlights = true,
        PaintStatic = c =>
        {
            foreach (Rectangle area in MarginAreas().Append(InfieldArea))
            {
                Starfield(c, area, area.Width * area.Height / 350);
            }

            c.GlowEllipse(560f, 200f, 200f, 80f, new Color(140, 60, 200) * 0.25f);
            c.GlowEllipse(860f, 300f, 160f, 60f, new Color(40, 120, 220) * 0.25f);

            // A Terra lá embaixo: oceano, continentes, nuvens e atmosfera.
            c.Circle(EarthCenter.X, EarthCenter.Y, 86f, new Color(120, 190, 255) * 0.5f);
            c.Circle(EarthCenter.X, EarthCenter.Y, 80f, new Color(30, 90, 180));
            foreach ((float dx, float dy, float r) in new[] { (-30f, -20f, 26f), (-10f, -34f, 18f), (22f, 20f, 30f), (40f, -10f, 16f), (-40f, 30f, 14f) })
            {
                c.Circle(EarthCenter.X + dx, EarthCenter.Y + dy, r, new Color(70, 150, 80));
                c.Circle(EarthCenter.X + dx + 4f, EarthCenter.Y + dy - 3f, r * 0.5f, new Color(150, 170, 90));
            }

            for (int i = 0; i < 6; i++)
            {
                float a = i * 1.1f;
                c.Ellipse(EarthCenter.X + (MathF.Cos(a) * 45f), EarthCenter.Y + (MathF.Sin(a) * 40f), 26f, 7f, Color.White * 0.55f, a + 0.6f);
            }

            c.GlowEllipse(EarthCenter.X + 34f, EarthCenter.Y + 34f, 90f, 90f, Color.Black * 0.45f);

            // Lua com crateras.
            c.Circle(900f, 176f, 24f, new Color(180, 180, 186));
            foreach ((float dx, float dy, float r) in new[] { (-8f, -6f, 6f), (8f, 6f, 5f), (4f, -12f, 3f), (-10f, 10f, 3.5f) })
            {
                c.Circle(900f + dx, 176f + dy, r, new Color(140, 140, 148));
            }

            // Estação: módulo central, anel de acoplamento e asas de painéis solares.
            foreach (float side in new[] { -1f, 1f })
            {
                for (int k = 0; k < 2; k++)
                {
                    float px = StationCenter.X + (side * (70f + (k * 64f)));
                    c.Rect(px - 28f, StationCenter.Y - 36f, 56f, 72f, new Color(30, 50, 110));
                    for (int gx = 0; gx < 4; gx++)
                    {
                        c.Rect(px - 28f + (gx * 14f), StationCenter.Y - 36f, 1f, 72f, new Color(90, 140, 220));
                    }

                    for (int gy = 0; gy < 5; gy++)
                    {
                        c.Rect(px - 28f, StationCenter.Y - 36f + (gy * 14.4f), 56f, 1f, new Color(90, 140, 220));
                    }
                }

                c.Rect(StationCenter.X + (side > 0f ? 20f : -150f), StationCenter.Y - 3f, 130f, 6f, new Color(160, 166, 180));
            }

            c.Rect(StationCenter.X - 40f, StationCenter.Y - 14f, 80f, 28f, new Color(200, 204, 214));
            c.Rect(StationCenter.X - 40f, StationCenter.Y - 14f, 80f, 4f, Color.White);
            c.Circle(StationCenter.X, StationCenter.Y, 20f, new Color(150, 156, 170));
            c.Circle(StationCenter.X, StationCenter.Y, 12f, new Color(40, 44, 60));
            c.Rect(StationCenter.X - 8f, StationCenter.Y - 40f, 16f, 26f, new Color(190, 194, 204));
            c.Rect(StationCenter.X - 8f, StationCenter.Y + 14f, 16f, 26f, new Color(190, 194, 204));

            foreach (Rectangle band in MarginAreas())
            {
                int count = Math.Max(2, (band.Width + band.Height) / 140);
                for (int i = 0; i < count; i++)
                {
                    Asteroid(c, c.R(band.Left + 12f, band.Right - 12f), c.R(band.Top + 12f, band.Bottom - 12f), c.R(6f, 12f));
                }
            }

            Asteroid(c, 520f, 320f, 12f);
            Asteroid(c, 560f, 160f, 8f);
            Asteroid(c, 920f, 320f, 10f);
        },
        PaintAnimated = c =>
        {
            for (int i = 0; i < 14; i++)
            {
                float x = ((i * 257f) % (InR - InL)) + InL;
                float y = ((i * 131f) % (InB - InT)) + InT;
                float twinkle = (MathF.Sin((c.Time * 3f) + (i * 1.7f)) + 1f) / 2f;
                c.RectC(x, y, 5f * twinkle, 1f, Color.White * twinkle);
                c.RectC(x, y, 1f, 5f * twinkle, Color.White * twinkle);
            }

            float a = c.Time * 0.5f;
            float sx = EarthCenter.X + (MathF.Cos(a) * 118f);
            float sy = EarthCenter.Y + (MathF.Sin(a) * 60f);
            c.RectC(sx, sy, 6f, 6f, new Color(210, 214, 224), a);
            c.RectC(sx, sy, 22f, 3f, new Color(60, 110, 200), a);

            float comet = (c.Time * 0.12f) % 1f;
            if (comet < 0.3f)
            {
                float t = comet / 0.3f;
                float cx = -40f + (t * (W + 80f));
                float cy = 40f + (t * 60f);
                c.Line(cx - 60f, cy - 5f, cx, cy, 2f, new Color(200, 230, 255) * 0.4f);
                c.Circle(cx, cy, 3f, Color.White);
            }
        },
        PaintLights = c =>
        {
            c.Glow(EarthCenter.X, EarthCenter.Y, 120f, new Color(90, 170, 255) * 0.2f);
            bool blink = (c.Time % 1.2f) < 0.3f;
            if (blink)
            {
                c.Glow(StationCenter.X - 150f, StationCenter.Y, 10f, new Color(255, 60, 60) * 0.9f);
                c.Glow(StationCenter.X + 150f, StationCenter.Y, 10f, new Color(60, 255, 120) * 0.9f);
            }

            c.Glow(StationCenter.X, StationCenter.Y, 30f, new Color(120, 220, 255) * 0.2f);
            float comet = (c.Time * 0.12f) % 1f;
            if (comet < 0.3f)
            {
                float t = comet / 0.3f;
                c.Glow(-40f + (t * (W + 80f)), 40f + (t * 60f), 16f, new Color(200, 230, 255) * 0.5f);
            }
        },
        PaintSecret = (c, p) =>
        {
            c.Circle(p.X, p.Y, 5f, new Color(245, 245, 250));
            c.Rect(p.X - 7f, p.Y - 3f, 3f, 6f, new Color(200, 204, 214));
            c.Ellipse(p.X + 1.5f, p.Y, 3f, 2.5f, new Color(250, 190, 60));
            c.Line(p.X + 3f, p.Y + 4f, p.X + 7f, p.Y + 7f, 2f, new Color(245, 245, 250));
            c.Line(p.X - 2f, p.Y + 4f, p.X - 4f, p.Y + 8f, 2f, new Color(245, 245, 250));
        },
    };

    // ---------- PLANETA ALIENÍGENA ----------

    private static readonly (float X, float Y, float R, bool Cyan)[] AlienMushrooms =
    [
        (290f, 170f, 22f, true), (360f, 300f, 16f, false), (520f, 180f, 18f, false), (880f, 170f, 20f, true), (900f, 300f, 16f, false),
        (-18f, 90f, 14f, true), (W + 18f, 380f, 14f, false), (160f, -18f, 13f, false), (700f, H + 18f, 14f, true), (1050f, -18f, 12f, true),
    ];

    private static readonly (float X, float Y)[] AlienTentacles = [(460f, 320f), (620f, 150f), (820f, 330f), (300f, 240f)];

    private static void Crystal(SceneCanvas c, float x, float y, float size, Color color)
    {
        for (int i = 0; i < 4; i++)
        {
            float a = -1.2f + (i * 0.7f) + c.R(-0.2f, 0.2f);
            float len = size * c.R(0.7f, 1.2f);
            c.RectC(x + (MathF.Cos(a) * len * 0.4f), y + (MathF.Sin(a) * len * 0.4f), len, size * 0.28f, color, a);
            c.RectC(x + (MathF.Cos(a) * len * 0.4f), y + (MathF.Sin(a) * len * 0.4f), len * 0.8f, size * 0.08f, Color.White * 0.5f, a);
        }
    }

    public static SceneryStyle AlienWorld { get; } = new()
    {
        Background = new Color(34, 10, 44),
        GroundA = new Color(86, 40, 106),
        GroundB = new Color(80, 36, 98),
        Track = new Color(40, 84, 88),
        TrackSpeck = new Color(60, 120, 116),
        Edge = EdgeStyle.Rocks,
        EdgeBase = new Color(50, 20, 62),
        EdgeA = new Color(220, 80, 200),
        EdgeB = new Color(80, 220, 210),
        Checkpoint = new Color(140, 255, 120),
        Darkness = 0.36f,
        DarknessTint = new Color(20, 0, 40),
        Headlights = true,
        Ambient = AmbientKind.Fireflies,
        AmbientCount = 40,
        AmbientColor = new Color(150, 255, 150),
        PaintStatic = c =>
        {
            foreach (Rectangle area in MarginAreas().Append(InfieldArea))
            {
                GroundSpeckles(c, area, area.Width * area.Height / 150, new Color(120, 60, 140), new Color(60, 24, 80));
            }

            // Duas luas enormes aparecendo nos cantos.
            c.Circle(-10f, H + 10f, 40f, new Color(230, 150, 90));
            c.Circle(4f, H - 4f, 12f, new Color(200, 120, 70));
            c.Circle(W + 10f, -10f, 34f, new Color(150, 220, 200));

            foreach ((float x, float y, float r) in new[] { (420f, 250f, 28f), (760f, 160f, 18f), (640f, 330f, 16f) })
            {
                c.Circle(x, y, r + 5f, new Color(110, 60, 130));
                c.Circle(x, y, r, new Color(48, 18, 62));
                c.Circle(x - (r * 0.3f), y - (r * 0.3f), r * 0.35f, new Color(70, 30, 86));
            }

            // Poças de gosma e o disco voador que caiu aqui.
            foreach ((float x, float y, float rx, float ry) in new[] { (560f, 260f, 40f, 16f), (300f, 330f, 26f, 10f), (940f, 240f, 16f, 30f) })
            {
                c.Ellipse(x, y, rx + 4f, ry + 3f, new Color(40, 90, 40));
                c.Ellipse(x, y, rx, ry, new Color(110, 230, 80));
                c.Ellipse(x - (rx * 0.3f), y - (ry * 0.3f), rx * 0.3f, ry * 0.3f, new Color(190, 255, 150));
            }

            c.Ellipse(745f, 262f, 70f, 26f, Color.Black * 0.35f, 0.3f);
            c.Ellipse(740f, 256f, 62f, 22f, new Color(150, 156, 170), 0.3f);
            c.Ellipse(740f, 256f, 50f, 16f, new Color(120, 126, 140), 0.3f);
            c.Circle(736f, 250f, 16f, new Color(120, 220, 230) * 0.8f);
            c.Circle(730f, 246f, 5f, Color.White * 0.6f);
            for (int i = 0; i < 6; i++)
            {
                c.Line(790f + (i * 6f), 272f + (i * 3f), 830f + (i * 10f), 290f + (i * 6f), 2f, new Color(30, 14, 38) * 0.6f);
            }

            Color[] crystals = [new(230, 90, 210), new(90, 230, 220), new(170, 120, 255)];
            foreach ((float x, float y) in new[] { (330f, 190f), (480f, 160f), (600f, 200f), (860f, 330f), (920f, 150f), (270f, 280f), (660f, 300f) })
            {
                Crystal(c, x, y, c.R(12f, 18f), c.Pick(crystals));
            }

            c.AlongMargin(64f, (x, y) =>
            {
                if (c.Chance(0.5))
                {
                    Crystal(c, x, y, c.R(9f, 13f), c.Pick(crystals));
                }
                else
                {
                    Rock(c, x, y, c.R(7f, 11f), new Color(80, 44, 96));
                }
            });

            foreach ((float x, float y, float r, bool cyan) in AlienMushrooms)
            {
                Color cap = cyan ? new Color(60, 200, 210) : new Color(220, 70, 190);
                c.Circle(x + 3f, y + 4f, r, Color.Black * 0.3f);
                c.Circle(x, y, r, CarPainter.Darken(cap, 0.7f));
                c.Circle(x, y, r * 0.85f, cap);
                for (int i = 0; i < 5; i++)
                {
                    float a = i * MathF.Tau / 5f;
                    c.Circle(x + (MathF.Cos(a) * r * 0.5f), y + (MathF.Sin(a) * r * 0.5f), r * 0.16f, Color.White * 0.8f);
                }
            }

            foreach ((float x, float y) in AlienTentacles)
            {
                c.Circle(x, y, 7f, new Color(70, 140, 60));
            }

            for (int i = 0; i < 5; i++)
            {
                c.Ellipse(820f + (i * 8f), 200f + ((i % 2) * 6f), 4f, 6f, new Color(220, 230, 150));
            }
        },
        PaintAnimated = c =>
        {
            foreach ((float x, float y) in AlienTentacles)
            {
                for (int s = 1; s < 6; s++)
                {
                    float sway = MathF.Sin((c.Time * 2.5f) + x + (s * 0.6f)) * s * 2.5f;
                    c.Circle(x + sway, y - (s * 5f), 5f - (s * 0.6f), s % 2 == 0 ? new Color(100, 190, 80) : new Color(80, 160, 70));
                }
            }

            float walk = MathF.Sin(c.Time * 0.5f);
            float ax = 594f + (walk * 120f);
            const float ay = 200f;
            float bob = MathF.Abs(MathF.Sin(c.Time * 6f)) * 2f;
            c.Circle(ax + 2f, ay + 3f, 6f, Color.Black * 0.3f);
            c.Circle(ax, ay - bob, 6f, new Color(120, 230, 100));
            c.Circle(ax + (walk > 0f ? 2f : -2f), ay - bob - 1f, 2.5f, Color.Black);
            c.Line(ax, ay - bob - 5f, ax + 3f, ay - bob - 10f, 1f, new Color(120, 230, 100));
            c.Circle(ax + 3f, ay - bob - 10f, 1.5f, new Color(250, 250, 120));

            float pop = (c.Time * 0.9f) % 1f;
            c.Circle(560f + (MathF.Sin(c.Time * 3f) * 20f), 260f, 2f + (pop * 5f), new Color(200, 255, 170) * (1f - pop));
        },
        PaintLights = c =>
        {
            foreach ((float x, float y, float r, bool cyan) in AlienMushrooms)
            {
                float pulse = 0.8f + (MathF.Sin((c.Time * 2f) + x) * 0.2f);
                c.Glow(x, y, r * 2.2f, (cyan ? new Color(60, 220, 230) : new Color(240, 80, 210)) * (0.28f * pulse));
            }

            foreach ((float x, float y, float rx, float ry) in new[] { (560f, 260f, 40f, 16f), (300f, 330f, 26f, 10f), (940f, 240f, 16f, 30f) })
            {
                c.GlowEllipse(x, y, rx * 1.8f, ry * 2f, new Color(120, 255, 90) * 0.25f);
            }

            if ((c.Time % 2f) < 1f)
            {
                c.Glow(736f, 250f, 26f, new Color(120, 230, 255) * 0.3f);
            }
        },
        PaintSecret = (c, p) =>
        {
            c.Circle(p.X, p.Y, 4.5f, new Color(130, 235, 110));
            c.Ellipse(p.X - 1.5f, p.Y - 0.5f, 1.5f, 2f, Color.Black);
            c.Ellipse(p.X + 1.5f, p.Y - 0.5f, 1.5f, 2f, Color.Black);
            c.Line(p.X, p.Y - 4f, p.X - 2f, p.Y - 8f, 1f, new Color(130, 235, 110));
            c.Circle(p.X - 2f, p.Y - 8f, 1.2f, new Color(255, 240, 120));
        },
    };
}
