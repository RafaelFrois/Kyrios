using Microsoft.Xna.Framework;

namespace Kyrios.Game;

/// <summary>Cenários "máquina": fábrica, lixão, placa-mãe, parque de diversões e o mundo 8 bits.</summary>
public static partial class TrackSceneries
{
    // ---------- FÁBRICA ----------

    private static readonly (float X, float Y, float R, float Speed)[] FactoryGears = [(300f, 180f, 30f, 1.2f), (344f, 214f, 18f, -2f), (880f, 300f, 34f, -0.9f), (920f, 170f, 20f, 1.6f)];

    private const float BeltLeft = 420f;
    private const float BeltRight = 820f;
    private const float BeltY = 232f;

    private static void Gear(SceneCanvas c, float x, float y, float r, float angle, Color color)
    {
        for (int i = 0; i < 10; i++)
        {
            float a = angle + (i * MathF.Tau / 10f);
            c.RectC(x + (MathF.Cos(a) * r), y + (MathF.Sin(a) * r), r * 0.35f, r * 0.3f, color, a);
        }

        c.Circle(x, y, r * 0.92f, color);
        c.Circle(x, y, r * 0.55f, CarPainter.Darken(color, 0.75f));
        c.Circle(x, y, r * 0.2f, new Color(40, 40, 44));
        for (int i = 0; i < 4; i++)
        {
            float a = angle + (i * MathF.PI / 2f);
            c.RectC(x + (MathF.Cos(a) * r * 0.38f), y + (MathF.Sin(a) * r * 0.38f), r * 0.35f, r * 0.12f, color, a);
        }
    }

    public static SceneryStyle Factory { get; } = new()
    {
        Background = new Color(40, 40, 44),
        GroundA = new Color(86, 88, 92),
        GroundB = new Color(82, 84, 88),
        Track = new Color(54, 54, 58),
        TrackSpeck = new Color(66, 66, 70),
        Line = new Color(230, 190, 40),
        Edge = EdgeStyle.Blocks,
        EdgeBase = new Color(30, 30, 30),
        EdgeA = new Color(240, 190, 30),
        EdgeB = new Color(34, 34, 38),
        Checkpoint = new Color(255, 160, 40),
        Darkness = 0.22f,
        DarknessTint = new Color(20, 10, 0),
        Ambient = AmbientKind.Embers,
        AmbientCount = 22,
        AmbientColor = new Color(255, 200, 90),
        PaintStatic = c =>
        {
            foreach (Rectangle area in MarginAreas().Append(InfieldArea))
            {
                GroundSpeckles(c, area, area.Width * area.Height / 200, new Color(100, 102, 106), new Color(70, 72, 76));
                for (int i = 0; i < Math.Max(1, area.Width * area.Height / 25000); i++)
                {
                    c.GlowEllipse(c.R(area.Left, area.Right), c.R(area.Top, area.Bottom), c.R(12f, 26f), c.R(6f, 12f), Color.Black * 0.3f);
                }
            }

            // Canos com válvulas contornando a margem.
            foreach (Rectangle band in MarginAreas())
            {
                bool horizontal = band.Width > band.Height;
                for (int k = 0; k < 2; k++)
                {
                    float offset = 10f + (k * 16f);
                    Color pipe = k == 0 ? new Color(150, 156, 166) : new Color(170, 90, 60);
                    if (horizontal)
                    {
                        c.Rect(band.X, band.Y + offset, band.Width, 8f, pipe);
                        c.Rect(band.X, band.Y + offset, band.Width, 2f, Color.White * 0.25f);
                        for (float x = band.X + 40f; x < band.Right; x += 120f)
                        {
                            c.Rect(x, band.Y + offset - 2f, 6f, 12f, CarPainter.Darken(pipe, 0.7f));
                        }
                    }
                    else
                    {
                        c.Rect(band.X + offset, band.Y, 8f, band.Height, pipe);
                        c.Rect(band.X + offset, band.Y, 2f, band.Height, Color.White * 0.25f);
                        for (float y = band.Y + 40f; y < band.Bottom; y += 120f)
                        {
                            c.Rect(band.X + offset - 2f, y, 12f, 6f, CarPainter.Darken(pipe, 0.7f));
                        }
                    }
                }
            }

            // Esteira no meio, com faixas de segurança em volta.
            c.Rect(BeltLeft - 16f, BeltY - 18f, BeltRight - BeltLeft + 32f, 56f, new Color(240, 190, 30));
            for (float x = BeltLeft - 16f; x < BeltRight + 16f; x += 16f)
            {
                c.Rect(x, BeltY - 18f, 8f, 56f, new Color(30, 30, 34));
            }

            c.Rect(BeltLeft - 10f, BeltY - 12f, BeltRight - BeltLeft + 20f, 44f, new Color(80, 82, 88));
            c.Rect(BeltLeft, BeltY - 6f, BeltRight - BeltLeft, 32f, new Color(36, 36, 40));

            // Fornalha, barris, pilhas de caixotes, painel de controle e a base do braço robótico.
            c.Rect(862f, 214f, 64f, 60f, new Color(70, 60, 58));
            c.Rect(872f, 226f, 44f, 36f, new Color(30, 16, 10));
            c.Rect(876f, 232f, 36f, 24f, new Color(230, 110, 30));
            foreach ((float x, float y, Color color) in new[] { (270f, 300f, new Color(50, 100, 190)), (292f, 318f, new Color(190, 60, 50)), (262f, 330f, new Color(50, 100, 190)), (560f, 320f, new Color(190, 60, 50)) })
            {
                c.Circle(x + 2f, y + 3f, 10f, Color.Black * 0.3f);
                c.Circle(x, y, 10f, color);
                c.Circle(x, y, 7f, CarPainter.Darken(color, 0.8f));
                c.Circle(x, y, 2f, new Color(30, 30, 34));
            }

            foreach ((float x, float y) in new[] { (640f, 150f), (670f, 150f), (655f, 170f), (460f, 300f), (490f, 310f) })
            {
                c.Rect(x + 3f, y + 4f, 26f, 22f, Color.Black * 0.3f);
                c.Rect(x, y, 26f, 22f, new Color(170, 120, 70));
                c.Line(x, y, x + 26f, y + 22f, 2f, new Color(130, 88, 50));
                c.Line(x + 26f, y, x, y + 22f, 2f, new Color(130, 88, 50));
                c.Rect(x, y, 26f, 2f, new Color(200, 150, 100));
            }

            c.Rect(740f, 300f, 60f, 34f, new Color(120, 126, 136));
            foreach ((float bx, Color color) in new[] { (752f, new Color(220, 50, 50)), (768f, new Color(60, 200, 90)), (784f, new Color(250, 200, 50)) })
            {
                c.Circle(bx, 316f, 4f, color);
            }

            c.Circle(560f, 170f, 16f, new Color(90, 94, 104));
            c.Circle(560f, 170f, 10f, new Color(240, 190, 30));
            foreach ((float x, float y, float r, float _) in FactoryGears)
            {
                c.Circle(x + 3f, y + 4f, r, Color.Black * 0.3f);
            }
        },
        PaintAnimated = c =>
        {
            foreach ((float x, float y, float r, float speed) in FactoryGears)
            {
                Gear(c, x, y, r, c.Time * speed, new Color(150, 156, 168));
            }

            float shift = (c.Time * 60f) % 20f;
            for (float x = BeltLeft + shift; x < BeltRight; x += 20f)
            {
                c.Rect(x, BeltY - 6f, 2f, 32f, new Color(60, 60, 66));
            }

            for (int i = 0; i < 4; i++)
            {
                float x = BeltLeft + ((c.Time * 60f) + (i * 100f)) % (BeltRight - BeltLeft - 24f);
                c.Rect(x + 2f, BeltY - 1f, 24f, 22f, Color.Black * 0.3f);
                c.Rect(x, BeltY - 3f, 24f, 22f, i % 2 == 0 ? new Color(180, 130, 80) : new Color(80, 140, 200));
                c.Rect(x, BeltY - 3f, 24f, 3f, Color.White * 0.25f);
            }

            float arm = MathF.Sin(c.Time * 1.3f) * 1.2f;
            float ex = 560f + (MathF.Cos(arm + 1.2f) * 44f);
            float ey = 170f + (MathF.Sin(arm + 1.2f) * 44f);
            c.Line(560f, 170f, ex, ey, 8f, new Color(240, 190, 30));
            c.Line(ex, ey, ex + (MathF.Cos(arm * 2f) * 22f), ey + (MathF.Sin(arm * 2f) * 22f) + 10f, 5f, new Color(200, 160, 30));

            for (int i = 0; i < 3; i++)
            {
                float phase = ((c.Time * 0.6f) + (i * 0.33f)) % 1f;
                c.Glow(400f + (i * 6f), 150f - (phase * 30f), 6f + (phase * 12f), Color.White * (0.35f * (1f - phase)));
            }
        },
        PaintLights = c =>
        {
            float pulse = 0.85f + (MathF.Sin(c.Time * 3f) * 0.15f);
            c.Glow(894f, 244f, 70f, new Color(255, 120, 30) * (0.35f * pulse));
            float arm = MathF.Sin(c.Time * 1.3f) * 1.2f;
            float ex = 560f + (MathF.Cos(arm + 1.2f) * 44f) + (MathF.Cos(arm * 2f) * 22f);
            float ey = 170f + (MathF.Sin(arm + 1.2f) * 44f) + (MathF.Sin(arm * 2f) * 22f) + 10f;
            if ((c.Time * 7f % 1f) < 0.5f)
            {
                c.Glow(ex, ey, 18f, new Color(200, 230, 255) * 0.7f);
            }

            float sweep = c.Time * 4f;
            c.GlowEllipse(740f + (MathF.Cos(sweep) * 40f), 150f + (MathF.Sin(sweep) * 16f), 60f, 20f, new Color(255, 150, 30) * 0.3f, sweep);
        },
        PaintSecret = (c, p) =>
        {
            c.Circle(p.X, p.Y, 5f, new Color(250, 205, 60));
            c.Circle(p.X, p.Y, 3.5f, new Color(220, 170, 40));
            c.RectC(p.X, p.Y, 7f, 1.5f, new Color(140, 100, 20), 0.6f);
            Sparkle(c, p + new Vector2(5f, -5f), Color.White * 0.8f);
        },
    };

    // ---------- LIXÃO ----------

    private static readonly Vector2[] JunkPuddles = [new(460f, 300f), new(760f, 180f), new(640f, 330f)];

    private static void TrashItem(SceneCanvas c, float x, float y)
    {
        switch (c.Rng.Next(9))
        {
            case 0:
                c.Circle(x, y, 6f, new Color(230, 230, 225));
                c.Line(x - 4f, y - 2f, x + 3f, y + 2f, 1f, new Color(170, 170, 170));
                c.Line(x - 2f, y + 3f, x + 4f, y - 3f, 1f, new Color(170, 170, 170));
                break;
            case 1:
                Color can = c.Pick(new Color(220, 50, 50), new Color(60, 120, 220), new Color(190, 196, 206));
                c.RectC(x, y, 14f, 8f, can, c.R(0f, 3f));
                c.Circle(x, y, 2f, new Color(200, 204, 214));
                break;
            case 2:
                c.Ellipse(x - 3f, y, 7f, 2.5f, new Color(240, 210, 60), 0.6f);
                c.Ellipse(x + 3f, y, 7f, 2.5f, new Color(240, 210, 60), -0.6f);
                c.Circle(x, y - 3f, 2f, new Color(120, 90, 40));
                break;
            case 3:
                float a = c.R(0f, 3f);
                c.Ellipse(x, y, 11f, 4f, new Color(90, 170, 120) * 0.8f, a);
                c.Ellipse(x + (MathF.Cos(a) * 10f), y + (MathF.Sin(a) * 10f), 3f, 2f, new Color(230, 230, 230), a);
                break;
            case 4:
                c.Circle(x, y, 9f, new Color(30, 30, 32));
                c.Circle(x, y, 5f, new Color(60, 60, 64));
                break;
            case 5:
                c.Circle(x + 2f, y + 2f, 10f, Color.Black * 0.3f);
                c.Circle(x, y, 10f, new Color(28, 28, 32));
                c.Circle(x - 3f, y - 3f, 4f, new Color(60, 60, 68));
                c.Rect(x - 1f, y - 12f, 2f, 5f, new Color(200, 180, 60));
                break;
            case 6:
                c.Circle(x, y, 4f, new Color(240, 220, 160));
                c.Rect(x - 1f, y - 7f, 2f, 3f, new Color(110, 80, 40));
                c.Circle(x + 3f, y, 2f, new Color(200, 60, 50));
                break;
            case 7:
                c.Line(x - 8f, y, x + 8f, y, 1.5f, new Color(230, 230, 220));
                for (int i = -2; i <= 2; i++)
                {
                    c.Line(x + (i * 3f), y - 4f, x + (i * 3f), y + 4f, 1f, new Color(230, 230, 220));
                }

                c.Circle(x + 9f, y, 3f, new Color(230, 230, 220));
                break;
            default:
                c.Rect(x - 7f, y - 6f, 14f, 12f, new Color(170, 130, 80));
                c.Rect(x - 7f, y - 1f, 14f, 2f, new Color(210, 190, 140));
                break;
        }
    }

    public static SceneryStyle Junkyard { get; } = new()
    {
        Background = new Color(40, 44, 34),
        GroundA = new Color(72, 76, 58),
        GroundB = new Color(68, 72, 54),
        Track = new Color(98, 100, 92),
        TrackSpeck = new Color(82, 86, 72),
        Edge = EdgeStyle.Rocks,
        EdgeBase = new Color(60, 58, 46),
        EdgeA = new Color(110, 100, 80),
        EdgeB = new Color(70, 96, 64),
        Checkpoint = new Color(200, 255, 90),
        Darkness = 0.1f,
        DarknessTint = new Color(20, 40, 0),
        Ambient = AmbientKind.Flies,
        AmbientCount = 40,
        AmbientColor = new Color(20, 20, 20),
        PaintStatic = c =>
        {
            foreach (Rectangle area in MarginAreas().Append(InfieldArea))
            {
                GroundSpeckles(c, area, area.Width * area.Height / 120, new Color(90, 92, 70), new Color(56, 60, 44));
            }

            foreach (Vector2 puddle in JunkPuddles)
            {
                c.Ellipse(puddle.X, puddle.Y, 34f, 14f, new Color(60, 80, 40));
                c.Ellipse(puddle.X, puddle.Y, 28f, 10f, new Color(90, 120, 50));
                c.Ellipse(puddle.X - 8f, puddle.Y - 3f, 8f, 3f, new Color(150, 180, 90) * 0.6f);
            }

            // Montes de lixo no miolo e tralha espalhada pela margem.
            foreach ((float x, float y, float r) in new[] { (320f, 190f, 40f), (560f, 200f, 34f), (880f, 260f, 44f), (700f, 290f, 30f) })
            {
                c.Circle(x + 6f, y + 8f, r, Color.Black * 0.25f);
                c.Circle(x, y, r, new Color(84, 80, 64));
                for (int i = 0; i < 14; i++)
                {
                    float a = c.R(0f, MathF.Tau);
                    float d = c.R(0f, r * 0.85f);
                    TrashItem(c, x + (MathF.Cos(a) * d), y + (MathF.Sin(a) * d));
                }
            }

            for (int i = 0; i < 26; i++)
            {
                TrashItem(c, c.R(InL + 10f, InR - 10f), c.R(InT + 10f, InB - 10f));
            }

            foreach (Rectangle band in MarginAreas())
            {
                int count = (band.Width + band.Height) / 40;
                for (int i = 0; i < count; i++)
                {
                    TrashItem(c, c.R(band.Left + 8f, band.Right - 8f), c.R(band.Top + 8f, band.Bottom - 8f));
                }
            }

            // TV quebrada e caixa de pizza aberta.
            c.Rect(480f, 150f, 50f, 40f, new Color(90, 90, 96));
            c.Rect(486f, 156f, 38f, 28f, new Color(30, 34, 40));
            c.Line(490f, 160f, 510f, 176f, 1f, Color.White * 0.6f);
            c.Line(510f, 176f, 518f, 162f, 1f, Color.White * 0.6f);
            c.Rect(780f, 320f, 44f, 26f, new Color(190, 150, 100));
            c.Rect(780f, 300f, 44f, 22f, new Color(170, 130, 80));
            c.RectC(800f, 334f, 16f, 10f, new Color(240, 200, 80), 0.4f);
            c.Circle(798f, 334f, 2f, new Color(200, 60, 40));
        },
        PaintAnimated = c =>
        {
            foreach (Vector2 puddle in JunkPuddles)
            {
                for (int i = 0; i < 3; i++)
                {
                    float phase = ((c.Time * 0.4f) + (i * 0.33f)) % 1f;
                    float x = puddle.X - 10f + (i * 10f);
                    float y = puddle.Y - 6f - (phase * 30f);
                    for (int s = 0; s < 4; s++)
                    {
                        c.Rect(x + (MathF.Sin((phase * 10f) + s) * 3f), y - (s * 3f), 2f, 3f, new Color(160, 200, 80) * (0.6f * (1f - phase)));
                    }
                }
            }

            float run = (c.Time * 0.35f) % 1f;
            float rx = InL + (run * (InR - InL));
            float ry = 240f + (MathF.Sin(c.Time * 9f) * 3f);
            c.Ellipse(rx, ry, 7f, 4f, new Color(110, 106, 110));
            c.Circle(rx + 6f, ry, 3f, new Color(120, 116, 120));
            c.Line(rx - 7f, ry, rx - 16f, ry + (MathF.Sin(c.Time * 12f) * 3f), 1f, new Color(200, 150, 150));

            float a = c.Time * 0.6f;
            float gx = 600f + (MathF.Cos(a) * 250f);
            float gy = 242f + (MathF.Sin(a) * 70f);
            float flap = MathF.Sin(c.Time * 7f) * 3f;
            c.Line(gx - 9f, gy - flap, gx, gy, 2f, Color.White);
            c.Line(gx, gy, gx + 9f, gy - flap, 2f, Color.White);
        },
        PaintSecret = (c, p) =>
        {
            c.Rect(p.X - 7f, p.Y - 1.5f, 14f, 3f, new Color(160, 110, 40));
            c.Circle(p.X, p.Y, 4f, new Color(250, 205, 60));
            c.Circle(p.X, p.Y, 2.8f, Color.White);
            c.Line(p.X, p.Y, p.X + 1.5f, p.Y - 1.5f, 0.8f, Color.Black);
            Sparkle(c, p + new Vector2(5f, -5f), Color.White * 0.8f);
        },
    };

    // ---------- PLACA-MÃE ----------

    private static readonly (Vector2 From, Vector2 To)[] DataTraces =
    [
        (new(260f, 150f), new(520f, 150f)), (new(520f, 150f), new(520f, 330f)), (new(640f, 330f), new(930f, 330f)),
        (new(930f, 150f), new(930f, 330f)), (new(-20f, 60f), new(-20f, 420f)), (new(100f, -20f), new(1100f, -20f)),
        (new(80f, H + 20f), new(1120f, H + 20f)), (new(W + 20f, 40f), new(W + 20f, 440f)),
    ];

    private static readonly (float X, float Y, Color Color)[] BoardLeds = [(900f, 200f, new Color(80, 255, 120)), (560f, 330f, new Color(255, 60, 60)), (300f, 330f, new Color(80, 180, 255)), (-20f, 440f, new Color(255, 220, 60))];

    private static void ChipPackage(SceneCanvas c, float x, float y, float w, float h)
    {
        for (float px = x + 4f; px < x + w - 2f; px += 6f)
        {
            c.Rect(px, y - 4f, 2f, 4f, new Color(200, 204, 210));
            c.Rect(px, y + h, 2f, 4f, new Color(200, 204, 210));
        }

        c.Rect(x + 2f, y + 3f, w, h, Color.Black * 0.35f);
        c.Rect(x, y, w, h, new Color(28, 28, 32));
        c.Circle(x + 5f, y + 5f, 2f, new Color(60, 60, 66));
    }

    public static SceneryStyle Motherboard { get; } = new()
    {
        Background = new Color(8, 50, 26),
        GroundA = new Color(14, 76, 40),
        GroundB = new Color(13, 72, 38),
        Track = new Color(184, 116, 56),
        TrackSpeck = new Color(210, 146, 80),
        Edge = EdgeStyle.Glow,
        EdgeBase = new Color(10, 56, 30),
        EdgeA = new Color(240, 200, 90),
        EdgeB = new Color(200, 150, 70),
        Checkpoint = new Color(80, 230, 255),
        Darkness = 0.18f,
        DarknessTint = new Color(0, 20, 10),
        PaintStatic = c =>
        {
            foreach (Rectangle area in MarginAreas().Append(InfieldArea))
            {
                c.Rect(area.X, area.Y, area.Width, area.Height, new Color(14, 76, 40));
                for (float x = area.X; x < area.Right; x += 16f)
                {
                    c.Rect(x, area.Y, 1f, area.Height, Color.Black * 0.08f);
                }
            }

            // Trilhas de cobre com vias nas curvas.
            foreach ((Vector2 from, Vector2 to) in DataTraces)
            {
                c.Line(from.X, from.Y, to.X, to.Y, 3f, new Color(190, 130, 60));
                c.Circle(from.X, from.Y, 4f, new Color(220, 170, 80));
                c.Circle(to.X, to.Y, 4f, new Color(220, 170, 80));
                c.Circle(from.X, from.Y, 1.5f, new Color(20, 30, 20));
                c.Circle(to.X, to.Y, 1.5f, new Color(20, 30, 20));
            }

            for (int i = 0; i < 26; i++)
            {
                float x = c.R(InL + 10f, InR - 10f);
                float y = c.R(InT + 10f, InB - 10f);
                bool horizontal = c.Chance(0.5);
                float len = c.R(20f, 70f);
                c.Rect(x, y, horizontal ? len : 2f, horizontal ? 2f : len, new Color(160, 110, 50) * 0.8f);
                c.Circle(x, y, 2.5f, new Color(210, 160, 80));
            }

            // Processador com dissipador e cooler, memórias, chips, capacitores e resistores.
            c.Rect(566f, 170f, 150f, 150f, new Color(20, 22, 26));
            for (float x = 572f; x < 712f; x += 7f)
            {
                c.Rect(x, 170f, 2f, 150f, new Color(150, 156, 166));
            }

            c.Rect(580f, 184f, 122f, 122f, new Color(180, 186, 196));
            c.Circle(641f, 245f, 50f, new Color(40, 42, 48));
            c.Circle(641f, 245f, 12f, new Color(90, 94, 104));

            for (int i = 0; i < 4; i++)
            {
                float x = 760f + (i * 22f);
                c.Rect(x, 160f, 14f, 170f, new Color(30, 30, 36));
                c.Rect(x + 2f, 164f, 10f, 162f, new Color(40, 110, 60));
                for (float y = 172f; y < 320f; y += 20f)
                {
                    c.Rect(x + 3f, y, 8f, 10f, new Color(28, 28, 32));
                }
            }

            ChipPackage(c, 270f, 180f, 60f, 40f);
            ChipPackage(c, 370f, 280f, 44f, 30f);
            ChipPackage(c, 430f, 176f, 30f, 30f);

            foreach ((float x, float y) in new[] { (270f, 290f), (296f, 300f), (322f, 290f), (480f, 320f), (900f, 160f), (920f, 180f), (500f, 250f) })
            {
                c.Circle(x + 2f, y + 3f, 8f, Color.Black * 0.35f);
                c.Circle(x, y, 8f, new Color(40, 60, 140));
                c.Circle(x, y, 6f, new Color(180, 186, 196));
                c.Rect(x - 6f, y - 1f, 12f, 2f, new Color(120, 126, 136));
                c.Rect(x - 1f, y - 6f, 2f, 12f, new Color(120, 126, 136));
            }

            foreach ((float x, float y) in new[] { (360f, 160f), (380f, 170f), (520f, 200f), (860f, 330f), (870f, 250f), (460f, 240f) })
            {
                c.Rect(x, y, 16f, 6f, new Color(220, 200, 160));
                c.Rect(x + 3f, y, 2f, 6f, new Color(200, 60, 40));
                c.Rect(x + 7f, y, 2f, 6f, new Color(40, 40, 40));
                c.Rect(x + 11f, y, 2f, 6f, new Color(230, 180, 40));
            }

            c.Rect(262f, 240f, 90f, 30f, Color.White * 0.08f);
            c.Rect(262f, 240f, 90f, 1f, Color.White * 0.5f);
            c.Rect(262f, 269f, 90f, 1f, Color.White * 0.5f);
            c.Rect(262f, 240f, 1f, 30f, Color.White * 0.5f);
            c.Rect(351f, 240f, 1f, 30f, Color.White * 0.5f);

            foreach ((float x, float y, Color color) in BoardLeds)
            {
                c.Rect(x - 3f, y - 3f, 6f, 6f, CarPainter.Darken(color, 0.4f));
            }
        },
        PaintAnimated = c =>
        {
            for (int i = 0; i < DataTraces.Length; i++)
            {
                (Vector2 from, Vector2 to) = DataTraces[i];
                float t = ((c.Time * 0.35f) + (i * 0.23f)) % 1f;
                Vector2 p = Vector2.Lerp(from, to, t);
                c.Circle(p.X, p.Y, 2.5f, new Color(150, 240, 255));
            }

            float spin = c.Time * 12f;
            for (int i = 0; i < 5; i++)
            {
                float a = spin + (i * MathF.Tau / 5f);
                c.RectC(641f + (MathF.Cos(a) * 28f), 245f + (MathF.Sin(a) * 28f), 34f, 12f, new Color(70, 74, 84), a + 0.4f);
            }

            c.Circle(641f, 245f, 12f, new Color(90, 94, 104));
            for (int i = 0; i < BoardLeds.Length; i++)
            {
                (float x, float y, Color color) = BoardLeds[i];
                if (((c.Time * (1.5f + i)) % 1f) < 0.5f)
                {
                    c.Rect(x - 3f, y - 3f, 6f, 6f, color);
                }
            }
        },
        PaintLights = c =>
        {
            for (int i = 0; i < DataTraces.Length; i++)
            {
                (Vector2 from, Vector2 to) = DataTraces[i];
                float t = ((c.Time * 0.35f) + (i * 0.23f)) % 1f;
                Vector2 p = Vector2.Lerp(from, to, t);
                c.Glow(p.X, p.Y, 12f, new Color(120, 230, 255) * 0.5f);
            }

            for (int i = 0; i < BoardLeds.Length; i++)
            {
                (float x, float y, Color color) = BoardLeds[i];
                if (((c.Time * (1.5f + i)) % 1f) < 0.5f)
                {
                    c.Glow(x, y, 14f, color * 0.5f);
                }
            }
        },
        PaintSecret = (c, p) =>
        {
            var shell = new Color(120, 70, 40);
            for (int i = -1; i <= 1; i++)
            {
                c.Line(p.X + (i * 2.5f), p.Y, p.X + (i * 3f), p.Y - 6f, 1f, new Color(40, 30, 20));
                c.Line(p.X + (i * 2.5f), p.Y, p.X + (i * 3f), p.Y + 6f, 1f, new Color(40, 30, 20));
            }

            c.Ellipse(p.X, p.Y, 5f, 3.5f, shell);
            c.Rect(p.X - 5f, p.Y - 0.5f, 10f, 1f, new Color(60, 30, 20));
            c.Circle(p.X + 5f, p.Y, 2f, new Color(40, 30, 20));
        },
    };

    // ---------- PARQUE DE DIVERSÕES ----------

    private static readonly Vector2 FerrisCenter = new(800f, 236f);
    private static readonly Vector2 CarouselCenter = new(360f, 240f);
    private const float FerrisRadius = 78f;

    private static Vector2 CoasterPoint(float t)
    {
        float a = t * MathF.Tau;
        return new Vector2(580f + (MathF.Cos(a) * 110f), 190f + (MathF.Sin(a * 2f) * 34f));
    }

    public static SceneryStyle AmusementPark { get; } = new()
    {
        Background = new Color(40, 30, 60),
        GroundA = new Color(116, 106, 98),
        GroundB = new Color(110, 100, 92),
        Track = new Color(66, 58, 86),
        TrackSpeck = new Color(80, 72, 100),
        Line = new Color(250, 220, 80),
        Edge = EdgeStyle.Stripes,
        EdgeBase = new Color(40, 30, 40),
        EdgeA = new Color(230, 60, 60),
        EdgeB = new Color(250, 210, 60),
        Checkpoint = new Color(255, 120, 220),
        Darkness = 0.3f,
        DarknessTint = new Color(30, 0, 50),
        Headlights = true,
        PaintStatic = c =>
        {
            foreach (Rectangle area in MarginAreas().Append(InfieldArea))
            {
                for (float y = area.Y; y < area.Bottom; y += 18f)
                {
                    c.Rect(area.X, y, area.Width, 1f, new Color(96, 88, 82));
                }

                for (float x = area.X; x < area.Right; x += 18f)
                {
                    c.Rect(x, area.Y, 1f, area.Height, new Color(96, 88, 82));
                }
            }

            // Montanha-russa: trilho duplo em oito, com pilares.
            for (int i = 0; i < 90; i++)
            {
                Vector2 p = CoasterPoint(i / 90f);
                Vector2 q = CoasterPoint((i + 1) / 90f);
                c.Line(p.X, p.Y - 3f, q.X, q.Y - 3f, 2f, new Color(150, 110, 70));
                c.Line(p.X, p.Y + 3f, q.X, q.Y + 3f, 2f, new Color(150, 110, 70));
                if (i % 6 == 0)
                {
                    c.Rect(p.X - 1f, p.Y - 4f, 2f, 8f, new Color(110, 80, 50));
                }
            }

            // Base da roda-gigante e plataforma do carrossel.
            c.Circle(FerrisCenter.X + 4f, FerrisCenter.Y + 6f, FerrisRadius + 6f, Color.Black * 0.2f);
            c.Line(FerrisCenter.X - 40f, FerrisCenter.Y + 90f, FerrisCenter.X, FerrisCenter.Y, 5f, new Color(90, 94, 104));
            c.Line(FerrisCenter.X + 40f, FerrisCenter.Y + 90f, FerrisCenter.X, FerrisCenter.Y, 5f, new Color(90, 94, 104));
            c.Circle(CarouselCenter.X + 4f, CarouselCenter.Y + 5f, 70f, Color.Black * 0.25f);
            c.Circle(CarouselCenter.X, CarouselCenter.Y, 68f, new Color(200, 170, 120));
            c.Circle(CarouselCenter.X, CarouselCenter.Y, 62f, new Color(240, 220, 170));

            // Barraquinhas de pipoca e algodão-doce, bilheteria e bancos.
            foreach ((float x, Color stripe) in new[] { (470f, new Color(230, 60, 60)), (560f, new Color(240, 120, 200)), (650f, new Color(60, 150, 230)) })
            {
                c.Rect(x + 3f, 304f, 60f, 40f, Color.Black * 0.25f);
                c.Rect(x, 300f, 60f, 40f, new Color(250, 250, 245));
                for (float sx = x; sx < x + 60f; sx += 12f)
                {
                    c.Rect(sx, 300f, 6f, 16f, stripe);
                }

                c.Rect(x, 316f, 60f, 3f, Color.Black * 0.2f);
            }

            c.Circle(496f, 330f, 5f, new Color(255, 240, 200));
            c.Circle(586f, 330f, 7f, new Color(250, 170, 220));
            c.Rect(900f, 150f, 36f, 30f, new Color(170, 60, 60));
            c.Rect(904f, 154f, 28f, 10f, new Color(250, 220, 120));
            foreach ((float x, float y) in new[] { (270f, 160f), (270f, 320f), (930f, 330f) })
            {
                c.Rect(x, y, 30f, 10f, new Color(120, 80, 50));
                c.Rect(x, y + 12f, 30f, 3f, new Color(80, 50, 30));
            }

            c.AlongMargin(40f, (x, y) => c.Circle(x, y, 4f, new Color(80, 70, 60)));
        },
        PaintAnimated = c =>
        {
            float rot = c.Time * 0.4f;
            for (int i = 0; i < 60; i++)
            {
                float ring = i * MathF.Tau / 60f;
                c.Circle(FerrisCenter.X + (MathF.Cos(ring) * FerrisRadius), FerrisCenter.Y + (MathF.Sin(ring) * FerrisRadius), 2.5f, new Color(220, 220, 230));
            }

            Color[] gondolas = [new(230, 60, 60), new(250, 200, 50), new(60, 170, 230), new(120, 210, 110)];
            for (int i = 0; i < 10; i++)
            {
                float a = rot + (i * MathF.Tau / 10f);
                Vector2 end = FerrisCenter + (new Vector2(MathF.Cos(a), MathF.Sin(a)) * FerrisRadius);
                c.Line(FerrisCenter.X, FerrisCenter.Y, end.X, end.Y, 2f, new Color(200, 200, 212));
                c.Rect(end.X - 8f, end.Y - 6f, 16f, 12f, gondolas[i % gondolas.Length]);
                c.Rect(end.X - 8f, end.Y - 6f, 16f, 3f, Color.White * 0.35f);
            }

            c.Circle(FerrisCenter.X, FerrisCenter.Y, 10f, new Color(90, 94, 104));

            float spin = c.Time * 1.2f;
            for (int i = 0; i < 12; i++)
            {
                float a = spin + (i * MathF.Tau / 12f);
                c.RectC(CarouselCenter.X + (MathF.Cos(a) * 30f), CarouselCenter.Y + (MathF.Sin(a) * 30f), 60f, 16f, i % 2 == 0 ? new Color(230, 60, 90) : new Color(250, 240, 220), a);
            }

            c.Circle(CarouselCenter.X, CarouselCenter.Y, 10f, new Color(250, 200, 50));

            Vector2 cart = CoasterPoint((c.Time * 0.25f) % 1f);
            Vector2 ahead = CoasterPoint(((c.Time * 0.25f) + 0.01f) % 1f);
            float angle = MathF.Atan2(ahead.Y - cart.Y, ahead.X - cart.X);
            c.RectC(cart.X, cart.Y, 18f, 10f, new Color(230, 60, 60), angle);
            c.RectC(cart.X, cart.Y, 8f, 8f, new Color(250, 220, 90), angle);
        },
        PaintLights = c =>
        {
            float rot = c.Time * 0.4f;
            for (int i = 0; i < 10; i++)
            {
                float a = rot + (i * MathF.Tau / 10f);
                Vector2 end = FerrisCenter + (new Vector2(MathF.Cos(a), MathF.Sin(a)) * FerrisRadius);
                c.Glow(end.X, end.Y, 12f, Hue((i * 0.1f) + (c.Time * 0.2f), 0.9f) * 0.35f);
            }

            c.Glow(CarouselCenter.X, CarouselCenter.Y, 90f, new Color(255, 220, 150) * 0.2f);
            int step = (int)(c.Time * 4f);
            int index = 0;
            c.AlongMargin(40f, (x, y) =>
            {
                if ((index++ + step) % 3 == 0)
                {
                    c.Glow(x, y, 14f, new Color(255, 230, 150) * 0.5f);
                }
            });
        },
        PaintSecret = (c, p) =>
        {
            c.Line(p.X, p.Y + 3f, p.X - 2f, p.Y + 10f, 1f, Color.White * 0.8f);
            c.Ellipse(p.X, p.Y - 1f, 4.5f, 5.5f, new Color(230, 40, 50));
            c.Circle(p.X - 1.5f, p.Y - 3f, 1.2f, Color.White * 0.6f);
        },
    };

    // ---------- MUNDO 8 BITS ----------

    private static readonly Color Retro0 = new(15, 56, 15);
    private static readonly Color Retro1 = new(48, 98, 48);
    private static readonly Color Retro2 = new(139, 172, 15);
    private static readonly Color Retro3 = new(155, 188, 15);

    /// <summary>Converte qualquer cor pra um dos 4 tons de verde (pela luminosidade), respeitando a transparência
    /// (as cores do SpriteBatch vêm pré-multiplicadas pelo alfa).</summary>
    private static Color RetroFilter(Color color)
    {
        if (color.A == 0)
        {
            return color;
        }

        float alpha = color.A / 255f;
        float luminance = ((color.R * 0.3f) + (color.G * 0.59f) + (color.B * 0.11f)) / (255f * alpha);
        Color shade = luminance < 0.22f ? Retro0 : luminance < 0.45f ? Retro1 : luminance < 0.7f ? Retro2 : Retro3;
        return shade * alpha;
    }

    /// <summary>Desenha um "sprite" feito de quadradinhos: cada caractere diferente de '.' é um pixel de <paramref name="size"/>.</summary>
    private static void PixelSprite(SceneCanvas c, float x, float y, float size, string[] rows, Color[] palette)
    {
        for (int row = 0; row < rows.Length; row++)
        {
            for (int col = 0; col < rows[row].Length; col++)
            {
                char ch = rows[row][col];
                if (ch != '.')
                {
                    c.Rect(x + (col * size), y + (row * size), size, size, palette[ch - '0']);
                }
            }
        }
    }

    private static readonly string[] RetroTree = ["..111..", ".12221.", "1222221", "1222221", ".12221.", "..101..", "...0..."];
    private static readonly string[] RetroHero = ["..000..", ".0333..", ".03330.", "..000..", ".01110.", "0.111.0", "..0.0.."];
    private static readonly string[] RetroMushroom = [".0000.", "032230", "022222", "000000", ".0330.", ".0000."];

    public static SceneryStyle Retro { get; } = new()
    {
        Background = Retro0,
        GroundA = Retro3,
        GroundB = new Color(150, 182, 15),
        Track = Retro1,
        TrackSpeck = Retro0,
        Line = Retro2,
        Edge = EdgeStyle.Blocks,
        EdgeBase = Retro0,
        EdgeA = Retro2,
        EdgeB = Retro1,
        Checkpoint = Retro3,
        CarFilter = RetroFilter,
        PaintStatic = c =>
        {
            Color[] palette = [Retro0, Retro1, Retro2, Retro3];
            foreach (Rectangle area in MarginAreas().Append(InfieldArea))
            {
                c.Rect(area.X, area.Y, area.Width, area.Height, Retro3);
                for (float y = area.Y; y < area.Bottom; y += 8f)
                {
                    for (float x = area.X + ((y / 8f) % 2f * 4f); x < area.Right; x += 8f)
                    {
                        c.Rect(x, y, 2f, 2f, Retro2);
                    }
                }
            }

            c.AlongMargin(44f, (x, y) => PixelSprite(c, x - 14f, y - 14f, 4f, RetroTree, palette));

            // Castelo de blocos no miolo, canos, tijolos e nuvens quadradas.
            for (int bx = 0; bx < 10; bx++)
            {
                for (int by = 0; by < 6; by++)
                {
                    float x = 640f + (bx * 16f);
                    float y = 180f + (by * 16f);
                    c.Rect(x, y, 16f, 16f, Retro1);
                    c.Rect(x + 1f, y + 1f, 14f, 14f, by % 2 == bx % 2 ? Retro2 : Retro1);
                }
            }

            for (int bx = 0; bx < 10; bx += 2)
            {
                c.Rect(640f + (bx * 16f), 164f, 16f, 16f, Retro1);
            }

            c.Rect(712f, 244f, 16f, 32f, Retro0);
            foreach ((float x, float y) in new[] { (300f, 170f), (300f, 300f), (900f, 180f), (520f, 320f) })
            {
                c.Circle(x, y, 18f, Retro0);
                c.Circle(x, y, 14f, Retro1);
                c.Circle(x, y, 8f, Retro0);
            }

            for (int i = 0; i < 5; i++)
            {
                float x = 400f + (i * 20f);
                c.Rect(x, 200f, 18f, 18f, Retro0);
                c.Rect(x + 2f, 202f, 14f, 14f, i == 2 ? Retro3 : Retro2);
                c.Rect(x + 8f, 208f, 2f, 2f, Retro0);
            }

            foreach ((float x, float y) in new[] { (420f, 290f), (840f, 310f), (560f, 160f) })
            {
                c.Rect(x, y, 48f, 12f, Retro3);
                c.Rect(x + 8f, y - 8f, 28f, 8f, Retro3);
                c.Rect(x, y + 12f, 48f, 2f, Retro2);
            }
        },
        PaintAnimated = c =>
        {
            Color[] palette = [Retro0, Retro1, Retro2, Retro3];
            foreach ((float x, float y) in new[] { (470f, 180f), (500f, 180f), (530f, 180f), (880f, 240f), (880f, 270f) })
            {
                float width = MathF.Abs(MathF.Cos((c.Time * 4f) + x)) * 10f;
                c.Rect(x - (width / 2f), y - 6f, MathF.Max(2f, width), 12f, Retro0);
                c.Rect(x - (width / 2f) + 1f, y - 5f, MathF.Max(1f, width - 2f), 10f, Retro2);
            }

            float walk = (MathF.Sin(c.Time * 0.6f) + 1f) / 2f;
            float hx = 300f + (walk * 280f);
            float jump = MathF.Max(0f, MathF.Sin(c.Time * 3f)) * 10f;
            PixelSprite(c, hx, 250f - jump, 3f, RetroHero, palette);
        },
        PaintSecret = (c, p) => PixelSprite(c, p.X - 6f, p.Y - 6f, 2f, RetroMushroom, [Retro0, Retro1, Retro2, Retro3]),
    };
}
