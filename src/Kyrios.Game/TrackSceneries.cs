using Microsoft.Xna.Framework;

namespace Kyrios.Game;

/// <summary>
/// Os cenários das pistas. Cada um é um <see cref="SceneryStyle"/>: cores + pincéis que desenham só FORA do
/// asfalto (miolo, margem e bordas) — o traçado é sempre o mesmo. Tudo procedural (retângulos, círculos e
/// manchas suaves), com os objetos reaproveitados por helpers (árvore, prédio, pedra...) e a parte parada
/// desenhada uma vez só (ver <see cref="SceneryRenderer"/>).
/// </summary>
public static partial class TrackSceneries
{
    // Referências de posição (pixels): miolo útil = x 242..946, y 132..352; margem = 40 px em volta da grade.
    private const float InL = 242f;
    private const float InT = 132f;
    private const float InR = 946f;
    private const float InB = 352f;
    private const float W = 1188f;
    private const float H = 484f;

    // ---------- Helpers reaproveitados pelos cenários ----------

    private static void DropShadow(SceneCanvas c, float x, float y, float rx, float ry, float strength = 0.35f) =>
        c.GlowEllipse(x + (rx * 0.25f), y + (ry * 0.35f), rx * 1.25f, ry * 1.2f, Color.Black * strength);

    private static void Tree(SceneCanvas c, float x, float y, float r, Color dark, Color mid, Color light)
    {
        DropShadow(c, x, y, r, r);
        c.Circle(x, y, r, dark);
        c.Circle(x - (r * 0.12f), y - (r * 0.14f), r * 0.8f, mid);
        c.Circle(x - (r * 0.3f), y - (r * 0.32f), r * 0.38f, light);
    }

    private static void Pine(SceneCanvas c, float x, float y, float r, Color dark, Color mid, Color top)
    {
        DropShadow(c, x, y, r, r);
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f;
            c.Circle(x + (MathF.Cos(a) * r * 0.62f), y + (MathF.Sin(a) * r * 0.62f), r * 0.4f, dark);
        }

        c.Circle(x, y, r * 0.72f, mid);
        c.Circle(x - (r * 0.1f), y - (r * 0.1f), r * 0.32f, top);
    }

    private static void Palm(SceneCanvas c, float x, float y, float r)
    {
        var leaf = new Color(58, 150, 62);
        var leafDark = new Color(38, 110, 44);
        c.GlowEllipse(x + (r * 0.5f), y + (r * 0.6f), r * 1.2f, r * 0.9f, Color.Black * 0.3f);
        for (int i = 0; i < 7; i++)
        {
            float a = (i * MathF.Tau / 7f) + 0.3f;
            float cx = x + (MathF.Cos(a) * r * 0.55f);
            float cy = y + (MathF.Sin(a) * r * 0.55f);
            c.Ellipse(cx, cy, r * 0.6f, r * 0.2f, i % 2 == 0 ? leaf : leafDark, a);
            c.RectC(cx, cy, r * 0.9f, 1.5f, CarPainter.Darken(leaf, 0.6f), a);
        }

        c.Circle(x, y, r * 0.2f, new Color(120, 85, 45));
        c.Circle(x + 3f, y - 2f, r * 0.11f, new Color(95, 65, 35));
        c.Circle(x - 2f, y + 3f, r * 0.11f, new Color(95, 65, 35));
    }

    private static void Rock(SceneCanvas c, float x, float y, float r, Color color)
    {
        DropShadow(c, x, y, r, r * 0.8f, 0.3f);
        c.Ellipse(x, y, r, r * 0.78f, color);
        c.Ellipse(x - (r * 0.25f), y - (r * 0.25f), r * 0.45f, r * 0.3f, Color.White * 0.18f);
    }

    /// <summary>Prédio visto de cima com a fachada sul aparecendo (dá profundidade): telhado + face com janelas.</summary>
    private static void Building(SceneCanvas c, float x, float y, float w, float h, Color roof, Color face, Color window, double litChance)
    {
        float faceH = MathF.Min(h * 0.35f, 16f);
        c.Rect(x + 4f, y + 6f, w, h, Color.Black * 0.3f);
        c.Rect(x, y, w, h - faceH, roof);
        c.Rect(x, y, w, 2f, CarPainter.Darken(roof, 1.25f));
        c.Rect(x, y + h - faceH, w, faceH, face);
        for (float wx = x + 3f; wx < x + w - 4f; wx += 6f)
        {
            for (float wy = y + h - faceH + 3f; wy < y + h - 3f; wy += 5f)
            {
                c.Rect(wx, wy, 3f, 2f, c.Chance(litChance) ? window : CarPainter.Darken(face, 0.6f));
            }
        }

        if (w > 30f && h > 30f)
        {
            c.Rect(x + c.R(4f, w - 14f), y + c.R(4f, h - faceH - 12f), 9f, 7f, CarPainter.Darken(roof, 0.75f));
            c.Circle(x + c.R(8f, w - 8f), y + c.R(8f, h - faceH - 8f), 4f, CarPainter.Darken(roof, 1.2f));
        }
    }

    private static void Umbrella(SceneCanvas c, float x, float y, float r, Color a, Color b)
    {
        c.GlowEllipse(x + 6f, y + 8f, r * 1.2f, r * 1.1f, Color.Black * 0.3f);
        c.Circle(x, y, r, a);
        for (int i = 0; i < 8; i += 2)
        {
            float angle = i * MathF.Tau / 8f;
            c.Ellipse(x + (MathF.Cos(angle) * r * 0.5f), y + (MathF.Sin(angle) * r * 0.5f), r * 0.5f, r * 0.22f, b, angle);
        }

        c.Circle(x, y, 2f, Color.White);
    }

    private static void Cactus(SceneCanvas c, float x, float y, float h)
    {
        var green = new Color(70, 150, 70);
        var dark = new Color(45, 110, 50);
        c.GlowEllipse(x + 8f, y + 2f, h * 0.45f, 4f, Color.Black * 0.3f);
        c.Rect(x - 4f, y - h, 8f, h, green);
        c.Rect(x + 1f, y - h, 2f, h, dark);
        c.Circle(x, y - h, 4f, green);
        c.Rect(x - 12f, y - (h * 0.6f), 8f, 5f, green);
        c.Rect(x - 12f, y - (h * 0.85f), 5f, h * 0.3f, green);
        c.Circle(x - 9.5f, y - (h * 0.85f), 2.5f, green);
        c.Rect(x + 4f, y - (h * 0.45f), 8f, 5f, green);
        c.Rect(x + 7f, y - (h * 0.7f), 5f, h * 0.3f, green);
        c.Circle(x + 9.5f, y - (h * 0.7f), 2.5f, green);
    }

    private static void GroundSpeckles(SceneCanvas c, Rectangle area, int count, Color a, Color b)
    {
        for (int i = 0; i < count; i++)
        {
            c.Rect(area.X + c.R(0f, area.Width), area.Y + c.R(0f, area.Height), 2f, c.Chance(0.5) ? 2f : 3f, c.Chance(0.5) ? a : b);
        }
    }

    private static Rectangle InfieldArea => new((int)InL, (int)InT, (int)(InR - InL), (int)(InB - InT));

    private static Rectangle WholeArea => new(-SceneCanvas.Margin, -SceneCanvas.Margin, (int)W + (2 * SceneCanvas.Margin), (int)H + (2 * SceneCanvas.Margin));

    /// <summary>Faixas da margem (fora da grade da pista), pra espalhar textura só onde não tem asfalto.</summary>
    private static IEnumerable<Rectangle> MarginAreas()
    {
        const int m = SceneCanvas.Margin;
        yield return new Rectangle(-m, -m, (int)W + (2 * m), m);
        yield return new Rectangle(-m, (int)H, (int)W + (2 * m), m);
        yield return new Rectangle(-m, 0, m, (int)H);
        yield return new Rectangle((int)W, 0, m, (int)H);
    }

    private static void Sparkle(SceneCanvas c, Vector2 p, Color color)
    {
        c.RectC(p.X, p.Y, 7f, 1.5f, color);
        c.RectC(p.X, p.Y, 1.5f, 7f, color);
    }

    // ---------- AUTÓDROMO ----------

    public static SceneryStyle Circuit { get; } = new()
    {
        Background = new Color(34, 92, 56),
        GroundA = new Color(41, 112, 68),
        GroundB = new Color(47, 124, 76),
        Track = new Color(46, 48, 58),
        TrackSpeck = new Color(56, 58, 68),
        Edge = EdgeStyle.Stripes,
        EdgeBase = new Color(44, 40, 40),
        EdgeA = new Color(205, 54, 46),
        EdgeB = new Color(232, 230, 224),
        PaintStatic = c =>
        {
            // Margem: alambrado de proteção atrás de uma fileira de pneus empilhados, com arbustos por fora.
            foreach (Rectangle band in MarginAreas())
            {
                GroundSpeckles(c, band, band.Width * band.Height / 90, new Color(54, 136, 84), new Color(30, 90, 54));
            }

            for (float x = -40f; x < W + 40f; x += 34f)
            {
                Tree(c, x + c.R(-4f, 4f), -34f + c.R(-2f, 2f), c.R(8f, 11f), new Color(22, 86, 52), new Color(36, 112, 62), new Color(70, 150, 90));
            }

            foreach ((float y, bool top) in new[] { (-8f, true), (H + 8f, false) })
            {
                c.Rect(-40f, y - 1f, W + 80f, 2f, new Color(170, 176, 186));
                for (float x = -40f; x < W + 40f; x += 8f)
                {
                    c.Rect(x, y - (top ? 6f : -2f), 1f, 4f, new Color(150, 156, 166) * 0.8f);
                }

                for (float x = -40f; x < W + 40f; x += 48f)
                {
                    c.Rect(x, y - 3f, 3f, 6f, new Color(120, 126, 136));
                }
            }

            c.AlongMargin(40f, (x, y) =>
            {
                c.Circle(x + 1f, y + 2f, 7f, Color.Black * 0.3f);
                c.Circle(x, y, 7f, new Color(28, 28, 30));
                c.Circle(x, y, 3.8f, new Color(78, 78, 82));
                c.Circle(x - 2f, y - 2f, 1.5f, Color.White * 0.15f);
            });

            // Grama cortada em faixas, arquibancada lotada, box e árvores.
            for (float x = InL; x < InR; x += 44f)
            {
                c.Rect(x, InT, 22f, InB - InT, Color.White * 0.04f);
            }

            c.Rect(262f, 136f, 664f, 50f, new Color(96, 100, 112));
            for (int row = 0; row < 3; row++)
            {
                c.Rect(262f, 142f + (row * 14f), 664f, 2f, new Color(70, 74, 84));
            }

            for (int i = 0; i < 520; i++)
            {
                c.Rect(c.R(266f, 920f), c.R(144f, 182f), 2f, 2f, c.Pick(Color.White, new(230, 60, 60), new(60, 120, 230), new(250, 210, 60), new(40, 40, 40), new(240, 140, 180)));
            }

            for (float x = 330f; x < 920f; x += 132f)
            {
                c.Rect(x, 136f, 6f, 50f, new Color(80, 84, 96));
            }

            c.Rect(262f, 186f, 664f, 4f, Color.Black * 0.25f);

            // Box: prédio sem letreiro, com faixa azul, garagens e o pátio de concreto na frente.
            c.Rect(494f, 288f, 202f, 56f, new Color(150, 154, 162));
            c.Rect(500f, 292f, 190f, 48f, new Color(228, 230, 236));
            c.Rect(500f, 292f, 190f, 10f, new Color(40, 90, 200));
            c.Rect(500f, 302f, 190f, 2f, new Color(250, 200, 40));
            for (int i = 0; i < 6; i++)
            {
                c.Rect(508f + (i * 30f), 312f, 22f, 24f, new Color(70, 74, 84));
                for (int k = 0; k < 4; k++)
                {
                    c.Rect(508f + (i * 30f), 314f + (k * 6f), 22f, 1f, new Color(96, 100, 112));
                }
            }

            c.Rect(500f, 278f, 190f, 10f, new Color(170, 172, 178));
            foreach (float x in new[] { 520f, 580f, 640f })
            {
                c.Rect(x, 280f, 16f, 5f, new Color(250, 250, 250) * 0.6f);
            }

            // Paddock no meio do gramado: tendas das equipes, caminhões e o caminho de cascalho.
            c.Rect(300f, 238f, 420f, 12f, new Color(150, 146, 136));
            c.Rect(300f, 238f, 420f, 2f, new Color(176, 172, 162));
            (float X, Color Color)[] tents = [(330f, new(220, 60, 60)), (400f, new(60, 120, 220)), (470f, new(250, 200, 50)), (620f, new(40, 150, 90)), (690f, new(230, 120, 200))];
            foreach ((float tx, Color color) in tents)
            {
                c.Rect(tx - 20f, 206f, 44f, 34f, Color.Black * 0.25f);
                c.Rect(tx - 22f, 202f, 44f, 34f, color);
                c.Rect(tx - 22f, 218f, 44f, 2f, CarPainter.Darken(color, 0.7f));
                c.Rect(tx - 1f, 202f, 2f, 34f, CarPainter.Darken(color, 0.8f));
                c.Rect(tx - 22f, 202f, 44f, 3f, Color.White * 0.3f);
            }

            foreach ((float tx, Color color) in new[] { (360f, new Color(220, 60, 60)), (560f, new Color(60, 120, 220)), (700f, new Color(40, 150, 90)) })
            {
                c.Rect(tx - 38f, 256f, 80f, 24f, Color.Black * 0.25f);
                c.Rect(tx - 40f, 252f, 62f, 24f, new Color(236, 238, 242));
                c.Rect(tx - 40f, 262f, 62f, 4f, color);
                c.Rect(tx + 24f, 254f, 18f, 20f, color);
                c.Rect(tx + 34f, 257f, 6f, 14f, new Color(60, 80, 110));
            }

            foreach ((float x, float y, float r) in new[] { (272f, 300f, 20f), (305f, 330f, 14f), (915f, 300f, 20f), (880f, 330f, 14f), (268f, 222f, 12f), (922f, 225f, 12f), (820f, 300f, 11f), (760f, 330f, 9f) })
            {
                Tree(c, x, y, r, new Color(22, 86, 52), new Color(36, 112, 62), new Color(70, 150, 90));
            }

            c.Rect(778f, 214f, 4f, 40f, new Color(220, 220, 220));
        },
        PaintAnimated = c =>
        {
            float flutter = MathF.Sin(c.Time * 6f) * 0.18f;
            c.RectC(799f, 222f, 34f, 18f, new Color(255, 200, 40), flutter);
            c.RectC(799f, 222f, 34f, 6f, new Color(210, 60, 30), flutter);

            // "Ola" na arquibancada: uma faixa de torcedores levantando.
            float wave = 266f + ((c.Time * 160f) % 660f);
            c.Rect(wave, 144f, 26f, 38f, Color.White * 0.12f);
        },
        PaintSecret = (c, p) =>
        {
            c.Circle(p.X, p.Y, 6f, new Color(255, 205, 60));
            c.Circle(p.X, p.Y, 3f, new Color(160, 110, 20));
            Sparkle(c, p + new Vector2(5f, -5f), Color.White * 0.8f);
        },
    };

    // ---------- PRAIA ----------

    public static SceneryStyle Beach { get; } = new()
    {
        Background = new Color(232, 208, 155),
        GroundA = new Color(236, 213, 162),
        GroundB = new Color(229, 204, 151),
        Track = new Color(126, 122, 122),
        TrackSpeck = new Color(205, 188, 150),
        Line = new Color(240, 240, 235),
        Edge = EdgeStyle.Planks,
        EdgeBase = new Color(166, 116, 70),
        EdgeA = new Color(124, 82, 46),
        EdgeB = new Color(196, 146, 94),
        PaintStatic = c =>
        {
            // Mar na margem de cima, areia com conchinhas no resto.
            c.Rect(-40f, -40f, W + 80f, 26f, new Color(40, 120, 185));
            c.Rect(-40f, -14f, W + 80f, 14f, new Color(80, 175, 215));
            GroundSpeckles(c, InfieldArea, 260, new Color(245, 230, 190), new Color(205, 180, 130));
            foreach (Rectangle band in MarginAreas().Skip(1))
            {
                GroundSpeckles(c, band, 120, new Color(245, 230, 190), new Color(205, 180, 130));
            }

            for (float x = -40f; x < W + 40f; x += 130f)
            {
                Palm(c, x + 30f, H + 20f, 18f);
                Umbrella(c, x + 95f, H + 20f, 13f, new Color(230, 70, 70), Color.White);
            }

            for (float y = 40f; y < H; y += 110f)
            {
                Palm(c, -20f, y, 16f);
                Palm(c, W + 20f, y + 50f, 16f);
            }

            // Lagoa no miolo, com areia molhada e espuma.
            const float cx = 612f;
            const float cy = 248f;
            c.Ellipse(cx, cy, 276f, 78f, new Color(214, 190, 140));
            c.Ellipse(cx, cy, 262f, 66f, new Color(90, 190, 215));
            c.Ellipse(cx + 10f, cy + 4f, 220f, 44f, new Color(52, 145, 200));
            for (int i = 0; i < 72; i++)
            {
                float a = i * MathF.Tau / 72f;
                c.Circle(cx + (MathF.Cos(a) * 262f), cy + (MathF.Sin(a) * 66f), 2.2f, Color.White * 0.8f);
            }

            c.Ellipse(700f, 250f, 30f, 12f, new Color(225, 205, 150));
            Palm(c, 700f, 246f, 14f);
            c.Rect(520f, 238f, 26f, 8f, new Color(240, 240, 245));
            c.Rect(532f, 230f, 2f, 10f, new Color(120, 90, 60));

            // Quiosque, guarda-sóis, toalhas, bola.
            c.Rect(252f, 140f, 60f, 52f, new Color(196, 152, 78));
            for (float y = 144f; y < 190f; y += 5f)
            {
                c.Rect(252f, y, 60f, 1f, new Color(160, 118, 56));
            }

            c.Rect(252f, 192f, 60f, 10f, new Color(120, 80, 45));
            c.TextC("SUCO", 282f, 197f, 1.1f, Color.White);
            Color[] umbrellaColors = [new(230, 70, 70), new(60, 140, 230), new(250, 200, 40), new(90, 190, 90)];
            for (int i = 0; i < 6; i++)
            {
                float ux = 360f + (i * 96f);
                Umbrella(c, ux, 150f, 11f, umbrellaColors[i % 4], Color.White);
                c.RectC(ux + 16f, 336f, 22f, 10f, umbrellaColors[(i + 1) % 4]);
            }

            Palm(c, 270f, 325f, 20f);
            Palm(c, 920f, 160f, 20f);
            Palm(c, 915f, 322f, 18f);
            c.Circle(340f, 330f, 6f, Color.White);
            c.Circle(340f, 330f, 3f, new Color(230, 60, 60));
        },
        PaintAnimated = c =>
        {
            for (int i = 0; i < 7; i++)
            {
                float x = 420f + ((c.Time * (18f + (i * 4f)) + (i * 70f)) % 380f);
                c.Rect(x, 225f + (i * 7f), 14f, 1.5f, Color.White * 0.35f);
            }

            float foam = MathF.Sin(c.Time * 2f) * 3f;
            for (float x = -40f; x < W + 40f; x += 12f)
            {
                c.Circle(x + foam, -2f + (MathF.Sin((x * 0.05f) + c.Time * 3f) * 2f), 2.5f, Color.White * 0.8f);
            }

            for (int i = 0; i < 2; i++)
            {
                float gx = ((c.Time * 50f) + (i * 500f)) % (W + 200f) - 100f;
                float gy = 60f + (i * 170f) + (MathF.Sin(c.Time * 2f + i) * 10f);
                float flap = MathF.Sin(c.Time * 10f + i) * 0.4f;
                c.Line(gx - 6f, gy - 3f + (flap * 4f), gx, gy, 1.5f, Color.White);
                c.Line(gx, gy, gx + 6f, gy - 3f + (flap * 4f), 1.5f, Color.White);
            }
        },
        PaintSecret = (c, p) =>
        {
            var red = new Color(220, 70, 50);
            c.Ellipse(p.X, p.Y, 5f, 4f, red);
            c.Circle(p.X - 6f, p.Y - 3f, 2.2f, red);
            c.Circle(p.X + 6f, p.Y - 3f, 2.2f, red);
            c.Rect(p.X - 2f, p.Y - 3f, 1f, 1f, Color.Black);
            c.Rect(p.X + 1f, p.Y - 3f, 1f, 1f, Color.Black);
        },
    };

    // ---------- FLORESTA ----------

    public static SceneryStyle Forest { get; } = new()
    {
        Background = new Color(26, 62, 32),
        GroundA = new Color(44, 96, 48),
        GroundB = new Color(50, 104, 54),
        Track = new Color(122, 92, 62),
        TrackSpeck = new Color(98, 72, 46),
        Edge = EdgeStyle.Planks,
        EdgeBase = new Color(104, 70, 40),
        EdgeA = new Color(78, 50, 28),
        EdgeB = new Color(140, 100, 60),
        Ambient = AmbientKind.Leaves,
        AmbientCount = 40,
        AmbientColor = new Color(220, 150, 50),
        Darkness = 0.12f,
        DarknessTint = new Color(0, 20, 0),
        PaintStatic = c =>
        {
            GroundSpeckles(c, InfieldArea, 500, new Color(70, 130, 60), new Color(34, 80, 38));
            c.AlongMargin(30f, (x, y) =>
            {
                float r = c.R(16f, 24f);
                Tree(c, x + c.R(-6f, 6f), y + c.R(-6f, 6f), r, new Color(20, 64, 30), new Color(34, 92, 44), new Color(70, 140, 70));
            });

            // Lago com vitórias-régias.
            c.Ellipse(720f, 258f, 76f, 40f, new Color(60, 90, 70));
            c.Ellipse(720f, 258f, 70f, 35f, new Color(60, 112, 124));
            c.Ellipse(712f, 252f, 46f, 20f, new Color(84, 146, 158));
            foreach ((float x, float y) in new[] { (690f, 262f), (735f, 245f), (752f, 270f), (700f, 240f) })
            {
                c.Circle(x, y, 5f, new Color(60, 150, 60));
                c.Circle(x + 2f, y - 1f, 1.5f, new Color(240, 200, 220));
            }

            // Cabana, tronco caído, pedras, cogumelos e flores.
            c.Rect(262f, 146f, 64f, 46f, new Color(110, 60, 40));
            c.Rect(262f, 168f, 64f, 2f, new Color(80, 40, 28));
            c.Rect(304f, 140f, 8f, 12f, new Color(90, 90, 95));
            c.Rect(470f, 318f, 90f, 14f, new Color(110, 72, 40));
            c.Circle(560f, 325f, 7f, new Color(150, 110, 70));
            c.Circle(560f, 325f, 3.5f, new Color(110, 72, 40));
            foreach ((float x, float y, float r) in new[] { (420f, 170f, 9f), (850f, 330f, 11f), (620f, 150f, 7f), (360f, 300f, 8f) })
            {
                Rock(c, x, y, r, new Color(120, 124, 120));
            }

            for (int i = 0; i < 14; i++)
            {
                float mx = c.R(InL + 10f, InR - 10f);
                float my = c.R(InT + 10f, InB - 10f);
                if (MathF.Abs(mx - 720f) < 90f && MathF.Abs(my - 258f) < 50f)
                {
                    continue;
                }

                c.Rect(mx - 0.5f, my, 2f, 4f, new Color(235, 225, 200));
                c.Circle(mx, my, 3.5f, new Color(210, 50, 40));
                c.Rect(mx - 1.5f, my - 1.5f, 1f, 1f, Color.White);
            }

            for (int i = 0; i < 40; i++)
            {
                c.Rect(c.R(InL, InR), c.R(InT, InB), 2f, 2f, c.Pick(new Color(250, 220, 60), new Color(240, 240, 250), new Color(200, 120, 220)));
            }

            // Árvores do miolo, em grade com variação (desviando do lago e da cabana).
            for (float x = InL + 30f; x < InR - 20f; x += 58f)
            {
                for (float y = InT + 26f; y < InB - 10f; y += 62f)
                {
                    float tx = x + c.R(-14f, 14f);
                    float ty = y + c.R(-12f, 12f);
                    bool nearPond = MathF.Abs(tx - 720f) < 110f && MathF.Abs(ty - 258f) < 65f;
                    bool nearCabin = tx < 350f && ty < 210f;
                    if (!nearPond && !nearCabin && c.Chance(0.75))
                    {
                        Tree(c, tx, ty, c.R(15f, 23f), new Color(20, 70, 32), new Color(36, 100, 46), new Color(80, 150, 70));
                    }
                }
            }
        },
        PaintAnimated = c =>
        {
            float ripple = (c.Time * 0.6f) % 1f;
            c.Ellipse(712f, 258f, 6f + (ripple * 30f), 3f + (ripple * 14f), Color.White * (0.25f * (1f - ripple)));
        },
        PaintLights = c =>
        {
            for (int i = 0; i < 6; i++)
            {
                float sway = MathF.Sin((c.Time * 0.5f) + i) * 8f;
                c.GlowEllipse(300f + (i * 120f) + sway, 200f + ((i % 3) * 50f), 70f, 26f, new Color(255, 240, 170) * 0.06f, 0.6f);
            }
        },
        PaintSecret = (c, p) =>
        {
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.Tau / 6f;
                c.Circle(p.X + (MathF.Cos(a) * 6f), p.Y + (MathF.Sin(a) * 6f), 2f, new Color(220, 50, 40));
            }

            c.Circle(p.X, p.Y, 1.5f, new Color(250, 240, 150));
        },
    };

    // ---------- CIDADE À NOITE ----------

    private static readonly Vector2[] NightLamps = BuildNightLamps();

    private static Vector2[] BuildNightLamps()
    {
        var lamps = new List<Vector2>();
        for (float x = 262f; x < InR; x += 76f)
        {
            lamps.Add(new Vector2(x, InT + 6f));
            lamps.Add(new Vector2(x + 30f, InB - 6f));
        }

        for (float x = 60f; x < W; x += 150f)
        {
            lamps.Add(new Vector2(x, -6f));
            lamps.Add(new Vector2(x + 70f, H + 6f));
        }

        for (float y = 80f; y < H; y += 150f)
        {
            lamps.Add(new Vector2(-6f, y));
            lamps.Add(new Vector2(W + 6f, y + 60f));
        }

        return [.. lamps];
    }

    public static SceneryStyle NightCity { get; } = new()
    {
        Background = new Color(22, 24, 32),
        GroundA = new Color(62, 64, 72),
        GroundB = new Color(57, 59, 67),
        Track = new Color(36, 38, 46),
        TrackSpeck = new Color(47, 49, 59),
        Line = new Color(210, 180, 70),
        Edge = EdgeStyle.Stripes,
        EdgeBase = new Color(88, 90, 98),
        EdgeA = new Color(160, 160, 168),
        EdgeB = new Color(128, 128, 136),
        Darkness = 0.5f,
        DarknessTint = new Color(8, 10, 30),
        Headlights = true,
        PaintStatic = c =>
        {
            var window = new Color(255, 214, 120);
            for (float x = -40f; x < W + 40f; x += c.R(56f, 80f))
            {
                float w = c.R(40f, 62f);
                Building(c, x, -40f, w, 36f, new Color(50, 52, 64), new Color(36, 38, 48), window, 0.35);
                Building(c, x + 10f, H + 2f, w, 36f, new Color(46, 50, 60), new Color(34, 36, 46), window, 0.35);
            }

            for (float y = 0f; y < H; y += 70f)
            {
                Building(c, -40f, y, 36f, 60f, new Color(52, 50, 62), new Color(36, 36, 48), window, 0.3);
                Building(c, W + 3f, y + 20f, 36f, 60f, new Color(48, 52, 62), new Color(34, 38, 46), window, 0.3);
            }

            // Quarteirão no miolo: prédios, praça com chafariz e carros estacionados.
            (float X, float Y, float W, float H)[] blocks =
            [
                (252f, 146f, 96f, 84f), (360f, 146f, 70f, 60f), (442f, 146f, 88f, 92f), (700f, 146f, 76f, 70f),
                (788f, 146f, 64f, 96f), (864f, 146f, 72f, 70f), (252f, 250f, 110f, 76f), (374f, 270f, 72f, 56f),
                (740f, 262f, 90f, 64f), (842f, 256f, 94f, 70f),
            ];
            foreach ((float bx, float by, float bw, float bh) in blocks)
            {
                Color roof = c.Pick(new Color(60, 62, 76), new Color(70, 64, 60), new Color(52, 60, 70));
                Building(c, bx, by, bw, bh, roof, CarPainter.Darken(roof, 0.65f), window, 0.4);
            }

            c.Rect(548f, 160f, 140f, 150f, new Color(34, 70, 44));
            Tree(c, 568f, 182f, 12f, new Color(20, 50, 30), new Color(30, 70, 40), new Color(50, 100, 60));
            Tree(c, 668f, 290f, 12f, new Color(20, 50, 30), new Color(30, 70, 40), new Color(50, 100, 60));
            c.Circle(618f, 236f, 20f, new Color(120, 120, 128));
            c.Circle(618f, 236f, 15f, new Color(60, 110, 150));
            c.Circle(618f, 236f, 4f, new Color(150, 200, 230));

            Color[] carColors = [new(180, 40, 40), new(40, 90, 170), new(220, 220, 220), new(230, 190, 40), new(40, 140, 80)];
            for (float x = 460f; x < 720f; x += 34f)
            {
                c.Rect(x, 336f, 26f, 12f, c.Pick(carColors));
                c.Rect(x + 16f, 338f, 6f, 8f, new Color(30, 40, 60));
            }

            foreach (Vector2 lamp in NightLamps)
            {
                c.Circle(lamp.X, lamp.Y, 3f, new Color(40, 40, 46));
                c.Circle(lamp.X, lamp.Y, 1.5f, new Color(255, 230, 160));
            }
        },
        PaintAnimated = c =>
        {
            bool blink = (c.Time % 1.6f) < 0.4f;
            if (blink)
            {
                c.Circle(488f, 150f, 2f, new Color(255, 60, 60));
                c.Circle(820f, 150f, 2f, new Color(255, 60, 60));
            }
        },
        PaintLights = c =>
        {
            foreach (Vector2 lamp in NightLamps)
            {
                c.Glow(lamp.X, lamp.Y, 44f, new Color(255, 200, 120) * 0.32f);
            }

            c.Glow(618f, 236f, 36f, new Color(120, 190, 255) * 0.25f);
            bool blink = (c.Time % 1.6f) < 0.4f;
            if (blink)
            {
                c.Glow(488f, 150f, 10f, new Color(255, 60, 60) * 0.8f);
                c.Glow(820f, 150f, 10f, new Color(255, 60, 60) * 0.8f);
            }

            c.Glow(39f, 426f, 5f, new Color(255, 230, 80) * 0.6f);
        },
        PaintSecret = (c, p) =>
        {
            var fur = new Color(20, 20, 24);
            c.Ellipse(p.X, p.Y, 6f, 3.5f, fur);
            c.Circle(p.X + 6f, p.Y - 2f, 3f, fur);
            c.Rect(p.X - 10f, p.Y - 1f, 5f, 1.5f, fur);
            c.Rect(p.X + 5f, p.Y - 3f, 1f, 1f, new Color(250, 230, 80));
            c.Rect(p.X + 7f, p.Y - 3f, 1f, 1f, new Color(250, 230, 80));
        },
    };

    // ---------- DESERTO ----------

    public static SceneryStyle Desert { get; } = new()
    {
        Background = new Color(206, 156, 96),
        GroundA = new Color(214, 170, 112),
        GroundB = new Color(209, 164, 106),
        Track = new Color(156, 136, 114),
        TrackSpeck = new Color(184, 162, 128),
        Edge = EdgeStyle.Rocks,
        EdgeBase = new Color(178, 128, 80),
        EdgeA = new Color(146, 100, 62),
        EdgeB = new Color(198, 150, 98),
        Ambient = AmbientKind.Sand,
        AmbientCount = 70,
        AmbientColor = new Color(245, 215, 165),
        Darkness = 0.06f,
        DarknessTint = new Color(80, 30, 0),
        PaintStatic = c =>
        {
            // Ondulações de duna no chão todo (fora do asfalto).
            foreach (Rectangle area in MarginAreas().Append(InfieldArea))
            {
                for (float y = area.Y + 6f; y < area.Bottom; y += 11f)
                {
                    for (float x = area.X; x < area.Right; x += 6f)
                    {
                        c.Rect(x, y + (MathF.Sin((x * 0.03f) + (y * 0.2f)) * 3f), 5f, 1f, new Color(170, 120, 70) * 0.35f);
                    }
                }
            }

            foreach ((float x, float y, float rx, float ry) in new[] { (430f, 250f, 110f, 55f), (820f, 200f, 90f, 45f), (640f, 300f, 80f, 30f) })
            {
                c.GlowEllipse(x - (rx * 0.2f), y - (ry * 0.2f), rx, ry, new Color(255, 230, 180) * 0.35f);
                c.GlowEllipse(x + (rx * 0.3f), y + (ry * 0.3f), rx * 0.8f, ry * 0.7f, new Color(140, 90, 50) * 0.3f);
            }

            // Posto abandonado: laje, cobertura quebrada, bombas e placa.
            c.Rect(254f, 144f, 150f, 96f, new Color(170, 165, 155));
            c.Rect(262f, 150f, 90f, 40f, new Color(150, 60, 50));
            c.Rect(322f, 150f, 30f, 18f, new Color(170, 165, 155));
            c.Rect(262f, 190f, 90f, 4f, Color.Black * 0.3f);
            foreach (float px in new[] { 280f, 310f, 340f })
            {
                c.Rect(px, 206f, 10f, 16f, new Color(190, 60, 50));
                c.Rect(px + 2f, 209f, 6f, 5f, new Color(230, 220, 180));
            }

            c.Rect(380f, 150f, 4f, 50f, new Color(90, 90, 95));
            c.Rect(366f, 146f, 32f, 16f, new Color(210, 190, 60));
            c.TextC("GAS", 382f, 154f, 1.3f, new Color(120, 60, 40));
            c.Rect(430f, 312f, 44f, 22f, new Color(150, 80, 50));
            c.Rect(452f, 316f, 14f, 14f, new Color(60, 50, 50));
            foreach ((float x, float y) in new[] { (500f, 330f), (512f, 324f), (905f, 150f) })
            {
                c.Circle(x, y, 6f, new Color(90, 80, 70));
                c.Circle(x, y, 3f, new Color(60, 55, 50));
            }

            // Oásis com palmeiras, um arco de pedra e uma caveira de boi.
            c.Ellipse(820f, 228f, 56f, 26f, new Color(190, 170, 110));
            c.Ellipse(820f, 228f, 48f, 20f, new Color(70, 150, 170));
            c.Ellipse(808f, 223f, 22f, 7f, new Color(130, 200, 215));
            for (int i = 0; i < 10; i++)
            {
                float a = i * MathF.Tau / 10f;
                c.Rect(820f + (MathF.Cos(a) * 50f), 228f + (MathF.Sin(a) * 23f), 2f, 6f, new Color(90, 140, 60));
            }

            Palm(c, 770f, 210f, 16f);
            Palm(c, 872f, 246f, 18f);
            c.Ellipse(700f, 176f, 34f, 12f, new Color(150, 100, 60));
            c.Ellipse(700f, 176f, 22f, 6f, new Color(214, 170, 112));
            c.GlowEllipse(706f, 186f, 34f, 10f, Color.Black * 0.25f);
            c.Ellipse(640f, 300f, 7f, 5f, new Color(240, 235, 220));
            c.Line(634f, 297f, 626f, 292f, 2f, new Color(240, 235, 220));
            c.Line(646f, 297f, 654f, 292f, 2f, new Color(240, 235, 220));
            c.Rect(638f, 300f, 1.5f, 1.5f, Color.Black);
            c.Rect(641f, 300f, 1.5f, 1.5f, Color.Black);

            foreach ((float x, float y, float h) in new[] { (560f, 190f, 32f), (700f, 342f, 26f), (905f, 300f, 34f), (620f, 160f, 22f), (920f, 340f, 24f) })
            {
                Cactus(c, x, y, h);
            }

            foreach (Rectangle band in MarginAreas())
            {
                for (int i = 0; i < 6; i++)
                {
                    Rock(c, c.R(band.Left + 10f, band.Right - 10f), c.R(band.Top + 10f, band.Bottom - 10f), c.R(6f, 12f), new Color(170, 110, 70));
                }
            }
        },
        PaintAnimated = c =>
        {
            float t = c.Time;
            float x = InL + 20f + ((t * 55f) % (InR - InL - 40f));
            float y = 250f + (MathF.Sin(t * 3f) * 8f);
            c.Circle(x, y + 4f, 7f, Color.Black * 0.2f);
            c.Circle(x, y, 7f, new Color(150, 110, 60));
            for (int i = 0; i < 4; i++)
            {
                c.RectC(x, y, 12f, 1f, new Color(110, 80, 40), (t * 6f) + (i * 0.8f));
            }

            float vx = 620f + (MathF.Cos(t * 0.5f) * 120f);
            float vy = 240f + (MathF.Sin(t * 0.5f) * 60f);
            c.Line(vx - 8f, vy - 3f, vx, vy, 2f, Color.Black * 0.25f);
            c.Line(vx, vy, vx + 8f, vy - 3f, 2f, Color.Black * 0.25f);
        },
        PaintSecret = (c, p) =>
        {
            var bone = new Color(240, 235, 220);
            c.Ellipse(p.X, p.Y, 4f, 5f, bone);
            c.Line(p.X - 3f, p.Y - 3f, p.X - 8f, p.Y - 7f, 2f, bone);
            c.Line(p.X + 3f, p.Y - 3f, p.X + 8f, p.Y - 7f, 2f, bone);
            c.Rect(p.X - 2f, p.Y - 1f, 1.5f, 1.5f, Color.Black);
            c.Rect(p.X + 1f, p.Y - 1f, 1.5f, 1.5f, Color.Black);
        },
    };

    // ---------- NEVE ----------

    public static SceneryStyle Snow { get; } = new()
    {
        Background = new Color(214, 224, 236),
        GroundA = new Color(236, 241, 248),
        GroundB = new Color(228, 235, 244),
        Track = new Color(112, 126, 146),
        TrackSpeck = new Color(182, 200, 220),
        Edge = EdgeStyle.Snowbank,
        EdgeBase = new Color(206, 218, 234),
        EdgeA = new Color(250, 252, 255),
        EdgeB = new Color(170, 190, 218),
        Ambient = AmbientKind.Snow,
        AmbientCount = 110,
        AmbientColor = Color.White,
        Darkness = 0.06f,
        DarknessTint = new Color(20, 40, 90),
        PaintStatic = c =>
        {
            GroundSpeckles(c, InfieldArea, 300, Color.White, new Color(190, 205, 228));
            var pineDark = new Color(30, 70, 60);
            var pineMid = new Color(44, 96, 80);
            var snow = new Color(245, 248, 255);
            c.AlongMargin(34f, (x, y) => Pine(c, x + c.R(-5f, 5f), y + c.R(-5f, 5f), c.R(14f, 19f), pineDark, pineMid, snow));

            // Lago congelado com rachaduras e brilho.
            c.Ellipse(700f, 246f, 128f, 60f, new Color(170, 205, 230));
            c.Ellipse(700f, 246f, 120f, 54f, new Color(196, 228, 244));
            for (int i = 0; i < 7; i++)
            {
                float a = c.R(0f, MathF.Tau);
                float len = c.R(30f, 80f);
                c.Line(700f + (MathF.Cos(a) * 20f), 246f + (MathF.Sin(a) * 10f), 700f + (MathF.Cos(a) * len), 246f + (MathF.Sin(a) * len * 0.45f), 1f, Color.White);
            }

            c.Ellipse(660f, 226f, 40f, 8f, Color.White * 0.5f);

            // Cabana com telhado nevado, boneco de neve, trenó e pinheiros.
            c.Rect(262f, 150f, 70f, 50f, new Color(120, 70, 45));
            c.Rect(258f, 146f, 78f, 26f, snow);
            c.Rect(312f, 140f, 9f, 12f, new Color(90, 90, 96));
            c.Rect(290f, 184f, 12f, 16f, new Color(80, 45, 30));
            c.Rect(270f, 178f, 10f, 8f, new Color(255, 210, 120));
            c.Circle(330f, 312f, 12f, snow);
            c.Circle(330f, 294f, 8f, snow);
            c.Rect(336f, 293f, 7f, 2f, new Color(240, 130, 40));
            c.Rect(326f, 291f, 1.5f, 1.5f, Color.Black);
            c.Rect(332f, 291f, 1.5f, 1.5f, Color.Black);
            c.Rect(322f, 300f, 16f, 3f, new Color(200, 40, 40));
            c.Rect(420f, 320f, 24f, 8f, new Color(180, 60, 50));
            for (float x = 360f; x < 540f; x += 12f)
            {
                c.Rect(x, 330f + (MathF.Sin(x) * 3f), 3f, 2f, new Color(180, 195, 220));
            }

            foreach ((float x, float y, float r) in new[] { (430f, 170f, 18f), (480f, 200f, 15f), (880f, 170f, 18f), (910f, 220f, 14f), (870f, 320f, 17f), (560f, 330f, 13f), (520f, 160f, 14f) })
            {
                Pine(c, x, y, r, pineDark, pineMid, snow);
            }
        },
        PaintAnimated = c =>
        {
            for (int i = 0; i < 4; i++)
            {
                float phase = ((c.Time * 0.5f) + (i * 0.25f)) % 1f;
                c.Glow(316f + (phase * 14f), 138f - (phase * 40f), 5f + (phase * 8f), new Color(210, 210, 220) * (0.7f * (1f - phase)));
            }

            float glint = (MathF.Sin(c.Time * 1.3f) + 1f) / 2f;
            c.Ellipse(740f, 262f, 18f * glint, 3f, Color.White * 0.6f);
        },
        PaintSecret = (c, p) =>
        {
            c.Ellipse(p.X, p.Y, 4f, 6f, new Color(25, 25, 30));
            c.Ellipse(p.X, p.Y + 1f, 2.5f, 4f, Color.White);
            c.Rect(p.X - 1f, p.Y - 7f, 2f, 2f, new Color(245, 150, 40));
        },
    };
}
