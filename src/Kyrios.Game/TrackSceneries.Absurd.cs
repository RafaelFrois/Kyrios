using Microsoft.Xna.Framework;

namespace Kyrios.Game;

/// <summary>Cenários "exagerados": neon, vulcão e os absurdos em escala gigante (supermercado, mesa, quarto).</summary>
public static partial class TrackSceneries
{
    // ---------- SUPERMERCADO ----------

    private static void Shelf(SceneCanvas c, float x, float y, float w, float h)
    {
        c.Rect(x + 4f, y + 6f, w, h, Color.Black * 0.25f);
        c.Rect(x, y, w, h, new Color(150, 156, 168));
        c.Rect(x + 2f, y + (h / 2f) - 1f, w - 4f, 2f, new Color(110, 116, 128));
        Color[] products = [new(220, 60, 50), new(50, 110, 210), new(250, 200, 40), new(60, 170, 80), new(240, 130, 40), new(200, 80, 180), new(245, 245, 245)];
        for (int row = 0; row < 2; row++)
        {
            float py = y + 3f + (row * (h / 2f));
            float rowH = (h / 2f) - 6f;
            for (float px = x + 3f; px < x + w - 12f;)
            {
                float pw = c.R(7f, 14f);
                Color color = c.Pick(products);
                if (c.Chance(0.3))
                {
                    c.Circle(px + (pw / 2f), py + (rowH / 2f), MathF.Min(pw, rowH) / 2f, color);
                    c.Circle(px + (pw / 2f), py + (rowH / 2f), MathF.Min(pw, rowH) / 4f, Color.White * 0.5f);
                }
                else
                {
                    c.Rect(px, py, pw - 1f, rowH, color);
                    c.Rect(px + 1f, py + (rowH * 0.4f), pw - 3f, 3f, Color.White * 0.7f);
                }

                px += pw;
            }
        }

        for (float tx = x + 20f; tx < x + w; tx += 60f)
        {
            c.Rect(tx, y + h - 5f, 10f, 5f, new Color(250, 220, 60));
        }
    }

    public static SceneryStyle Supermarket { get; } = new()
    {
        Background = new Color(200, 202, 200),
        GroundA = new Color(230, 230, 226),
        GroundB = new Color(212, 212, 208),
        Track = new Color(178, 184, 190),
        TrackSpeck = new Color(198, 204, 210),
        Line = new Color(240, 200, 40),
        Edge = EdgeStyle.Blocks,
        EdgeBase = new Color(40, 40, 40),
        EdgeA = new Color(240, 196, 30),
        EdgeB = new Color(46, 46, 50),
        PaintStatic = c =>
        {
            // Freezers em cima, caixas embaixo, gôndolas nas laterais.
            for (float x = -40f; x < W + 40f; x += 90f)
            {
                c.Rect(x + 4f, -36f, 82f, 30f, new Color(160, 200, 225));
                c.Rect(x + 8f, -32f, 74f, 22f, new Color(205, 235, 250));
                for (int i = 0; i < 5; i++)
                {
                    c.Rect(x + 12f + (i * 14f), -26f, 10f, 12f, c.Pick(new Color(250, 250, 250), new Color(240, 150, 60), new Color(120, 180, 240)));
                }

                c.Rect(x + 10f, H + 6f, 70f, 12f, new Color(40, 40, 44));
                c.Rect(x + 12f, H + 8f, 20f, 8f, c.Pick(new Color(230, 70, 60), new Color(80, 170, 90), new Color(250, 210, 60)));
                c.Rect(x + 30f, H + 22f, 24f, 14f, new Color(90, 90, 96));
            }

            for (float y = 10f; y < H; y += 80f)
            {
                Shelf(c, -38f, y, 34f, 70f);
                Shelf(c, W + 4f, y, 34f, 70f);
            }

            Shelf(c, 254f, 140f, 680f, 44f);
            Shelf(c, 254f, 300f, 680f, 44f);

            // Corredor do meio: banana gigante, cereal derramado, placa de piso molhado e a OFERTA.
            for (int i = 0; i < 9; i++)
            {
                float t = i / 8f;
                float bx = 350f + (t * 120f);
                float by = 250f - (MathF.Sin(t * MathF.PI) * 22f);
                c.Circle(bx, by + 4f, 12f + (MathF.Sin(t * MathF.PI) * 5f), Color.Black * 0.15f);
                c.Circle(bx, by, 11f + (MathF.Sin(t * MathF.PI) * 5f), new Color(245, 215, 70));
            }

            c.Circle(350f, 250f, 5f, new Color(110, 80, 40));
            c.Circle(470f, 250f, 4f, new Color(110, 80, 40));
            for (int i = 0; i < 60; i++)
            {
                c.Circle(c.R(560f, 640f), c.R(220f, 280f), 2.5f, c.Pick(new Color(230, 190, 120), new Color(210, 160, 90)));
            }

            c.Rect(544f, 214f, 26f, 18f, new Color(230, 200, 150));
            c.Rect(690f, 226f, 24f, 30f, new Color(250, 210, 40));
            c.TextC("!", 702f, 240f, 2f, Color.Black);
            c.Rect(760f, 212f, 120f, 56f, new Color(120, 120, 128));
            c.Rect(764f, 216f, 112f, 48f, new Color(210, 40, 40));
            c.TextC("OFERTA", 820f, 240f, 2.5f, Color.White);

            // Luz fria de lâmpada fluorescente em faixas.
            for (float y = -20f; y < H + 40f; y += 110f)
            {
                c.Rect(-40f, y, W + 80f, 26f, Color.White * 0.05f);
            }
        },
        PaintAnimated = c =>
        {
            if ((c.Time % 1.2f) < 0.6f)
            {
                c.Rect(764f, 216f, 112f, 48f, Color.White * 0.15f);
            }
        },
        PaintSecret = (c, p) =>
        {
            var yellow = new Color(245, 215, 70);
            c.Circle(p.X, p.Y, 3f, new Color(200, 170, 50));
            c.Ellipse(p.X - 5f, p.Y + 2f, 5f, 2f, yellow, 0.6f);
            c.Ellipse(p.X + 5f, p.Y + 2f, 5f, 2f, yellow, -0.6f);
            c.Ellipse(p.X, p.Y - 5f, 2f, 5f, yellow);
        },
    };

    // ---------- CIDADE NEON ----------

    private static readonly (float X, float Y, string Text, Color Color)[] NeonSigns =
    [
        (310f, 190f, "NEON", new Color(255, 70, 200)),
        (470f, 300f, "24H", new Color(60, 230, 255)),
        (760f, 180f, "RAMEN", new Color(255, 220, 60)),
        (880f, 300f, "MEGA", new Color(90, 255, 140)),
    ];

    public static SceneryStyle NeonCity { get; } = new()
    {
        Background = new Color(12, 8, 24),
        GroundA = new Color(26, 20, 44),
        GroundB = new Color(22, 17, 38),
        Track = new Color(30, 28, 44),
        TrackSpeck = new Color(72, 42, 104),
        Line = new Color(60, 220, 255),
        Edge = EdgeStyle.Glow,
        EdgeBase = new Color(16, 12, 28),
        EdgeA = new Color(60, 230, 255),
        EdgeB = new Color(255, 70, 200),
        Checkpoint = new Color(60, 230, 255),
        Darkness = 0.55f,
        DarknessTint = new Color(5, 0, 25),
        Ambient = AmbientKind.Rain,
        AmbientCount = 150,
        AmbientColor = new Color(150, 180, 255),
        Headlights = true,
        PaintStatic = c =>
        {
            foreach (Rectangle area in MarginAreas().Append(InfieldArea))
            {
                for (float x = area.X; x < area.Right; x += 22f)
                {
                    c.Rect(x, area.Y, 1f, area.Height, new Color(60, 230, 255) * 0.08f);
                }

                for (float y = area.Y; y < area.Bottom; y += 22f)
                {
                    c.Rect(area.X, y, area.Width, 1f, new Color(60, 230, 255) * 0.08f);
                }
            }

            // Poças no asfalto refletindo o neon.
            foreach ((float x, float y) in new[] { (160f, 410f), (600f, 60f), (1040f, 240f), (110f, 180f), (900f, 420f) })
            {
                c.Ellipse(x, y, 22f, 6f, new Color(50, 44, 80));
                c.Rect(x - 14f, y - 1f, 18f, 1.5f, new Color(255, 70, 200) * 0.5f);
            }

            // Torres com contorno neon.
            (float X, float Y, float W, float H, bool Cyan)[] towers =
            [
                (252f, 142f, 110f, 90f, true), (380f, 142f, 70f, 70f, false), (470f, 150f, 90f, 60f, true), (580f, 142f, 80f, 100f, false),
                (680f, 150f, 150f, 70f, true), (850f, 142f, 86f, 110f, false), (252f, 250f, 120f, 92f, false), (390f, 262f, 170f, 80f, true),
                (580f, 262f, 110f, 80f, true), (710f, 244f, 120f, 98f, false),
            ];
            foreach ((float tx, float ty, float tw, float th, bool cyan) in towers)
            {
                Color neon = cyan ? new Color(60, 230, 255) : new Color(255, 70, 200);
                c.Rect(tx + 5f, ty + 7f, tw, th, Color.Black * 0.4f);
                c.Rect(tx, ty, tw, th, new Color(32, 26, 54));
                c.Rect(tx, ty, tw, 2f, neon);
                c.Rect(tx, ty + th - 2f, tw, 2f, neon * 0.6f);
                c.Rect(tx, ty, 2f, th, neon * 0.8f);
                c.Rect(tx + tw - 2f, ty, 2f, th, neon * 0.8f);
                for (int i = 0; i < 3; i++)
                {
                    c.Rect(tx + c.R(6f, tw - 16f), ty + c.R(6f, th - 16f), 10f, 8f, new Color(50, 44, 80));
                }
            }

            c.Circle(620f, 190f, 22f, new Color(50, 44, 80));
            c.Circle(620f, 190f, 18f, new Color(32, 26, 54));
            c.TextC("H", 620f, 190f, 3f, new Color(255, 220, 60));

            foreach ((float x, float y, string text, Color color) in NeonSigns)
            {
                float width = PixelFont.Measure(text, 2.5f) + 12f;
                c.Rect(x - (width / 2f), y - 12f, width, 24f, new Color(10, 6, 20));
                c.Rect(x - (width / 2f), y - 12f, width, 1.5f, color);
                c.Rect(x - (width / 2f), y + 10.5f, width, 1.5f, color);
                c.TextC(text, x, y, 2.5f, color);
            }

            c.AlongMargin(46f, (x, y) =>
            {
                Color neon = c.Chance(0.5) ? new Color(60, 230, 255) : new Color(255, 70, 200);
                c.Rect(x - 16f, y - 16f, 32f, 32f, new Color(30, 24, 50));
                c.Rect(x - 16f, y - 16f, 32f, 2f, neon);
                c.Rect(x - 4f, y - 4f, 8f, 8f, neon * 0.4f);
            });
        },
        PaintAnimated = c =>
        {
            for (int i = 0; i < 3; i++)
            {
                float lane = 236f + (i * 24f);
                float x = InL + (((c.Time * (90f + (i * 40f))) + (i * 250f)) % (InR - InL));
                c.Rect(x, lane, 10f, 4f, new Color(40, 40, 60));
                c.Rect(x + 8f, lane, 2f, 4f, new Color(255, 255, 220));
            }

            if ((c.Time % 3f) > 2.7f)
            {
                c.Rect(740f, 168f, 40f, 24f, Color.Black * 0.6f);
            }
        },
        PaintLights = c =>
        {
            foreach ((float x, float y, string _, Color color) in NeonSigns)
            {
                float pulse = 0.85f + (MathF.Sin((c.Time * 3f) + x) * 0.15f);
                if (x > 740f && x < 780f && (c.Time % 3f) > 2.7f)
                {
                    continue;
                }

                c.GlowEllipse(x, y, 70f, 34f, color * (0.35f * pulse));
            }

            c.Glow(620f, 190f, 40f, new Color(255, 220, 60) * 0.2f);
            for (int i = 0; i < 3; i++)
            {
                float lane = 236f + (i * 24f);
                float x = InL + (((c.Time * (90f + (i * 40f))) + (i * 250f)) % (InR - InL));
                c.Glow(x + 10f, lane + 2f, 14f, new Color(255, 250, 220) * 0.4f);
            }

            c.Glow((51.4f * 22f) + 1f, 13.5f * 22f, 6f, new Color(60, 230, 255) * 0.8f);
        },
        PaintSecret = (c, p) =>
        {
            c.Rect(p.X - 4f, p.Y - 4f, 8f, 8f, new Color(150, 156, 170));
            c.Rect(p.X - 3f, p.Y + 4f, 2f, 3f, new Color(100, 104, 116));
            c.Rect(p.X + 1f, p.Y + 4f, 2f, 3f, new Color(100, 104, 116));
            c.Rect(p.X - 2f, p.Y - 2f, 4f, 2f, new Color(60, 230, 255));
            c.Rect(p.X, p.Y - 7f, 1f, 3f, new Color(150, 156, 170));
        },
    };

    // ---------- VULCÃO ----------

    private static readonly (float X, float Y, float Rx, float Ry)[] LavaPools =
    [
        (520f, 250f, 150f, 62f),
        (680f, 236f, 90f, 48f),
        (440f, 290f, 70f, 34f),
    ];

    public static SceneryStyle Volcano { get; } = new()
    {
        Background = new Color(26, 16, 14),
        GroundA = new Color(46, 33, 29),
        GroundB = new Color(40, 29, 26),
        Track = new Color(60, 52, 52),
        TrackSpeck = new Color(92, 62, 50),
        Edge = EdgeStyle.Rocks,
        EdgeBase = new Color(32, 22, 20),
        EdgeA = new Color(72, 42, 32),
        EdgeB = new Color(24, 17, 15),
        Checkpoint = new Color(255, 170, 60),
        Darkness = 0.35f,
        DarknessTint = new Color(35, 4, 0),
        Ambient = AmbientKind.Embers,
        AmbientCount = 70,
        AmbientColor = new Color(255, 150, 50),
        PaintStatic = c =>
        {
            // Rachaduras com brasa por dentro.
            foreach (Rectangle area in MarginAreas().Append(InfieldArea))
            {
                int cracks = Math.Max(2, area.Width * area.Height / 9000);
                for (int i = 0; i < cracks; i++)
                {
                    float x = c.R(area.Left, area.Right);
                    float y = c.R(area.Top, area.Bottom);
                    for (int s = 0; s < 4; s++)
                    {
                        float nx = Math.Clamp(x + c.R(-16f, 16f), area.Left, area.Right);
                        float ny = Math.Clamp(y + c.R(-10f, 10f), area.Top, area.Bottom);
                        c.Line(x, y, nx, ny, 2.5f, new Color(20, 10, 8));
                        c.Line(x, y, nx, ny, 1f, new Color(230, 90, 20) * 0.7f);
                        x = nx;
                        y = ny;
                    }
                }
            }

            // Lago de lava com crosta.
            foreach ((float x, float y, float rx, float ry) in LavaPools)
            {
                c.Ellipse(x, y, rx + 8f, ry + 6f, new Color(30, 18, 14));
                c.Ellipse(x, y, rx, ry, new Color(200, 60, 15));
                c.Ellipse(x, y, rx * 0.8f, ry * 0.75f, new Color(240, 110, 25));
                c.Ellipse(x - (rx * 0.1f), y - (ry * 0.1f), rx * 0.45f, ry * 0.4f, new Color(255, 180, 60));
            }

            for (int i = 0; i < 10; i++)
            {
                (float x, float y, float rx, float ry) = LavaPools[i % LavaPools.Length];
                c.Ellipse(x + c.R(-rx * 0.6f, rx * 0.6f), y + c.R(-ry * 0.5f, ry * 0.5f), c.R(8f, 16f), c.R(4f, 8f), new Color(60, 30, 22), c.R(0f, 3f));
            }

            // Cratera, pedras e espinhos de obsidiana.
            c.Circle(860f, 240f, 60f, new Color(64, 46, 42));
            c.Circle(860f, 240f, 44f, new Color(46, 32, 29));
            c.Circle(860f, 240f, 24f, new Color(220, 80, 20));
            c.Circle(860f, 240f, 12f, new Color(255, 190, 70));
            foreach ((float x, float y, float r) in new[] { (290f, 170f, 14f), (330f, 320f, 11f), (620f, 330f, 9f), (740f, 160f, 12f), (270f, 250f, 10f) })
            {
                Rock(c, x, y, r, new Color(58, 44, 40));
            }

            for (int i = 0; i < 8; i++)
            {
                float x = c.R(InL + 20f, 380f);
                float y = c.R(InT + 10f, InB - 10f);
                c.RectC(x, y, 5f, 16f, new Color(30, 20, 40), c.R(-0.4f, 0.4f));
            }

            // Rio de lava na margem de baixo.
            c.Rect(-40f, H + 10f, W + 80f, 22f, new Color(210, 70, 18));
            c.Rect(-40f, H + 16f, W + 80f, 8f, new Color(250, 160, 50));
        },
        PaintAnimated = c =>
        {
            for (int i = 0; i < 6; i++)
            {
                (float x, float y, float rx, float ry) = LavaPools[i % LavaPools.Length];
                float phase = ((c.Time * 0.7f) + (i * 0.37f)) % 1f;
                float bx = x + (MathF.Sin(i * 12.3f) * rx * 0.55f);
                float by = y + (MathF.Cos(i * 7.1f) * ry * 0.45f);
                c.Circle(bx, by, 2f + (phase * 5f), new Color(255, 210, 90) * (1f - phase));
            }

            for (int i = 0; i < 5; i++)
            {
                float phase = ((c.Time * 0.25f) + (i * 0.2f)) % 1f;
                c.Glow(860f + (MathF.Sin((phase * 4f) + i) * 12f), 230f - (phase * 90f), 12f + (phase * 26f), new Color(70, 60, 60) * (0.6f * (1f - phase)));
            }

            float flow = (c.Time * 40f) % 60f;
            for (float x = -40f + flow; x < W + 40f; x += 60f)
            {
                c.Ellipse(x, H + 20f, 16f, 3f, new Color(255, 210, 90) * 0.7f);
            }
        },
        PaintLights = c =>
        {
            float pulse = 0.85f + (MathF.Sin(c.Time * 2f) * 0.15f);
            foreach ((float x, float y, float rx, float ry) in LavaPools)
            {
                c.GlowEllipse(x, y, rx * 1.4f, ry * 1.6f, new Color(255, 110, 30) * (0.35f * pulse));
            }

            c.Glow(860f, 240f, 70f, new Color(255, 120, 30) * (0.35f * pulse));
            c.GlowEllipse(W / 2f, H + 20f, W * 0.6f, 40f, new Color(255, 110, 30) * 0.3f);
        },
        PaintSecret = (c, p) =>
        {
            c.Ellipse(p.X, p.Y, 4.5f, 6f, new Color(120, 60, 160));
            c.Circle(p.X - 1.5f, p.Y - 2f, 1f, new Color(120, 230, 120));
            c.Circle(p.X + 2f, p.Y + 1f, 1f, new Color(120, 230, 120));
            c.Circle(p.X - 1f, p.Y + 3f, 1f, new Color(120, 230, 120));
        },
    };

    // ---------- MESA DA COZINHA ----------

    public static SceneryStyle KitchenTable { get; } = new()
    {
        Background = new Color(132, 88, 54),
        GroundA = new Color(158, 108, 68),
        GroundB = new Color(151, 103, 65),
        Track = new Color(214, 196, 160),
        TrackSpeck = new Color(190, 170, 135),
        Edge = EdgeStyle.Blocks,
        EdgeBase = new Color(226, 226, 226),
        EdgeA = new Color(200, 50, 50),
        EdgeB = new Color(246, 246, 246),
        PaintStatic = c =>
        {
            // Veios da madeira da mesa.
            foreach (Rectangle area in MarginAreas().Append(InfieldArea))
            {
                for (float y = area.Y + 4f; y < area.Bottom; y += 9f)
                {
                    for (float x = area.X; x < area.Right; x += 8f)
                    {
                        c.Rect(x, y + (MathF.Sin((x * 0.02f) + (y * 0.5f)) * 2f), 7f, 1f, new Color(110, 70, 40) * 0.35f);
                    }
                }
            }

            foreach (Rectangle area in MarginAreas().Append(InfieldArea))
            {
                for (float y = area.Y + 20f; y < area.Bottom; y += 66f)
                {
                    c.Rect(area.X, y, area.Width, 2f, new Color(95, 60, 34));
                }
            }

            // Prato com ovo frito e bacon, talheres, caneca, torrada, saleiro, farelos e mancha de café.
            c.Circle(450f, 246f, 90f, Color.Black * 0.15f);
            c.Circle(446f, 242f, 88f, new Color(232, 232, 236));
            c.Circle(446f, 242f, 70f, new Color(250, 250, 252));
            c.Circle(430f, 236f, 34f, Color.White);
            c.Circle(452f, 250f, 26f, Color.White);
            c.Circle(440f, 240f, 15f, new Color(255, 190, 40));
            c.Circle(436f, 236f, 5f, new Color(255, 230, 150));
            for (int s = 0; s < 2; s++)
            {
                for (int i = 0; i < 8; i++)
                {
                    float bx = 480f + (i * 9f);
                    float by = 212f + (s * 20f) + (MathF.Sin(i * 1.3f) * 4f);
                    c.RectC(bx, by, 11f, 8f, new Color(180, 60, 50));
                    c.RectC(bx, by, 11f, 2f, new Color(240, 180, 170));
                }
            }

            c.Rect(326f, 170f, 10f, 150f, new Color(190, 192, 200));
            for (int i = 0; i < 4; i++)
            {
                c.Rect(320f + (i * 6f), 150f, 3f, 26f, new Color(190, 192, 200));
            }

            c.Rect(556f, 168f, 10f, 160f, new Color(190, 192, 200));
            c.Rect(556f, 168f, 14f, 90f, new Color(215, 218, 226));

            c.Circle(700f, 196f, 34f, Color.Black * 0.15f);
            c.Circle(696f, 192f, 32f, new Color(240, 240, 244));
            c.Circle(696f, 192f, 26f, new Color(90, 55, 30));
            c.Circle(690f, 188f, 8f, new Color(130, 85, 50));
            c.Rect(726f, 184f, 18f, 12f, new Color(240, 240, 244));

            c.Rect(800f, 262f, 80f, 70f, new Color(170, 110, 50));
            c.Rect(806f, 268f, 68f, 58f, new Color(225, 175, 95));
            c.Rect(826f, 282f, 20f, 16f, new Color(255, 230, 120));

            c.Circle(880f, 170f, 12f, Color.White);
            c.Circle(912f, 172f, 12f, new Color(40, 40, 44));
            for (int i = 0; i < 4; i++)
            {
                c.Rect(876f + (i * 3f), 166f, 1.5f, 1.5f, new Color(120, 120, 130));
            }

            c.GlowEllipse(640f, 316f, 40f, 20f, new Color(90, 55, 30) * 0.5f);
            c.Ellipse(640f, 316f, 28f, 14f, new Color(120, 75, 40) * 0.3f);
            for (int i = 0; i < 50; i++)
            {
                c.Rect(c.R(600f, 940f), c.R(140f, 350f), 2f, 2f, new Color(220, 180, 110));
            }

            c.Rect(254f, 290f, 60f, 50f, new Color(245, 245, 240));
            c.Rect(254f, 290f, 60f, 4f, new Color(200, 60, 60));
        },
        PaintAnimated = c =>
        {
            for (int i = 0; i < 4; i++)
            {
                float phase = ((c.Time * 0.45f) + (i * 0.25f)) % 1f;
                c.Glow(696f + (MathF.Sin((phase * 5f) + i) * 8f), 186f - (phase * 50f), 6f + (phase * 10f), Color.White * (0.35f * (1f - phase)));
            }
        },
        PaintSecret = (c, p) =>
        {
            var ant = new Color(20, 14, 12);
            c.Circle(p.X - 3f, p.Y, 1.8f, ant);
            c.Circle(p.X, p.Y, 1.4f, ant);
            c.Circle(p.X + 3f, p.Y, 2f, ant);
            c.Line(p.X - 1f, p.Y - 3f, p.X + 1f, p.Y + 3f, 0.8f, ant);
            c.Line(p.X + 1f, p.Y - 3f, p.X - 1f, p.Y + 3f, 0.8f, ant);
        },
    };

    // ---------- QUARTO DE CRIANÇA ----------

    public static SceneryStyle KidsRoom { get; } = new()
    {
        Background = new Color(170, 118, 76),
        GroundA = new Color(72, 112, 190),
        GroundB = new Color(66, 104, 180),
        Track = new Color(240, 130, 40),
        TrackSpeck = new Color(250, 160, 70),
        Line = new Color(255, 240, 200),
        Edge = EdgeStyle.Blocks,
        EdgeBase = new Color(40, 40, 50),
        EdgeA = new Color(230, 60, 60),
        EdgeB = new Color(60, 160, 230),
        Darkness = 0.08f,
        DarknessTint = new Color(40, 20, 0),
        PaintStatic = c =>
        {
            // Assoalho de tábuas na margem e tapete estampado no miolo.
            foreach (Rectangle area in MarginAreas())
            {
                for (float y = area.Y; y < area.Bottom; y += 12f)
                {
                    c.Rect(area.X, y, area.Width, 1f, new Color(120, 80, 48));
                }
            }

            c.Rect(InL + 6f, InT + 6f, InR - InL - 12f, 3f, new Color(250, 210, 60));
            c.Rect(InL + 6f, InB - 9f, InR - InL - 12f, 3f, new Color(250, 210, 60));
            for (int i = 0; i < 30; i++)
            {
                float sx = c.R(InL + 12f, InR - 12f);
                float sy = c.R(InT + 14f, InB - 14f);
                c.RectC(sx, sy, 7f, 2f, new Color(250, 220, 90) * 0.7f);
                c.RectC(sx, sy, 2f, 7f, new Color(250, 220, 90) * 0.7f);
            }

            // Blocos ABC, gizes de cera, ursinho, bola, peças de montar e um desenho.
            (float X, float Y, string L, Color C)[] blocks = [(300f, 170f, "A", new(230, 60, 60)), (334f, 168f, "B", new(60, 160, 230)), (318f, 204f, "C", new(80, 190, 90))];
            foreach ((float x, float y, string letter, Color color) in blocks)
            {
                c.Rect(x - 13f, y - 11f, 30f, 30f, Color.Black * 0.25f);
                c.Rect(x - 15f, y - 15f, 30f, 30f, color);
                c.Rect(x - 12f, y - 12f, 24f, 24f, CarPainter.Darken(color, 1.2f));
                c.TextC(letter, x, y, 2.5f, Color.White);
            }

            Color[] crayons = [new(230, 50, 50), new(250, 200, 40), new(60, 170, 80), new(60, 110, 220), new(170, 80, 200)];
            for (int i = 0; i < crayons.Length; i++)
            {
                float cx = 460f + (i * 16f);
                float cy = 170f + (i * 6f);
                c.RectC(cx, cy, 34f, 6f, crayons[i], 0.4f);
                c.RectC(cx + 15f, cy + 7f, 6f, 3f, CarPainter.Darken(crayons[i], 0.7f), 0.4f);
            }

            var fur = new Color(170, 110, 60);
            c.Circle(700f, 270f, 30f, Color.Black * 0.2f);
            c.Circle(696f, 266f, 28f, fur);
            c.Circle(696f, 228f, 20f, fur);
            c.Circle(680f, 212f, 8f, fur);
            c.Circle(712f, 212f, 8f, fur);
            c.Circle(696f, 234f, 7f, new Color(230, 200, 160));
            c.Rect(690f, 224f, 3f, 3f, Color.Black);
            c.Rect(700f, 224f, 3f, 3f, Color.Black);
            c.Circle(670f, 270f, 9f, fur);
            c.Circle(722f, 270f, 9f, fur);

            c.Circle(860f, 300f, 20f, new Color(230, 60, 60));
            c.Rect(840f, 297f, 40f, 6f, Color.White);
            c.Circle(860f, 300f, 5f, new Color(60, 110, 220));

            for (int i = 0; i < 8; i++)
            {
                float bx = c.R(560f, 940f);
                float by = c.R(150f, 340f);
                if (MathF.Abs(bx - 696f) < 50f && by > 190f)
                {
                    continue;
                }

                Color brick = c.Pick(new Color(230, 60, 60), new Color(60, 160, 230), new Color(250, 210, 60), new Color(80, 190, 90));
                c.Rect(bx, by, 22f, 12f, brick);
                c.Circle(bx + 6f, by + 6f, 2.5f, CarPainter.Darken(brick, 1.2f));
                c.Circle(bx + 16f, by + 6f, 2.5f, CarPainter.Darken(brick, 1.2f));
            }

            c.Rect(400f, 260f, 110f, 76f, new Color(250, 250, 245));
            c.Line(410f, 320f, 440f, 280f, 3f, new Color(60, 170, 80));
            c.Line(440f, 280f, 470f, 310f, 3f, new Color(230, 60, 60));
            c.Circle(490f, 278f, 10f, new Color(250, 200, 40));
            c.Rect(420f, 290f, 20f, 18f, new Color(60, 110, 220));
        },
        PaintAnimated = c =>
        {
            float spin = c.Time * 9f;
            c.Circle(780f, 200f, 16f, Color.Black * 0.2f);
            for (int i = 0; i < 4; i++)
            {
                c.RectC(778f, 197f, 30f, 7f, i % 2 == 0 ? new Color(230, 60, 60) : new Color(250, 210, 60), spin + (i * MathF.PI / 4f));
            }

            c.Circle(778f, 197f, 4f, Color.White);
        },
        PaintLights = c => c.Glow(-20f, -20f, 260f, new Color(255, 220, 150) * 0.12f),
        PaintSecret = (c, p) =>
        {
            c.RectC(p.X, p.Y, 6f, 10f, Color.White, 0.3f);
            c.RectC(p.X + 1f, p.Y - 1f, 6f, 2f, new Color(230, 60, 60), 0.3f);
            c.RectC(p.X - 1f, p.Y + 3f, 6f, 2f, new Color(230, 60, 60), 0.3f);
            c.Ellipse(p.X + 3f, p.Y + 5f, 4f, 2.5f, Color.White, 0.3f);
        },
    };
}
