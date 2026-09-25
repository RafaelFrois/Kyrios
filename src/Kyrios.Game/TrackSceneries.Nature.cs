using Microsoft.Xna.Framework;

namespace Kyrios.Game;

/// <summary>Cenários ao ar livre da segunda leva: montanha, estádio, fundo do mar, céu e cemitério.</summary>
public static partial class TrackSceneries
{
    // Bordas do asfalto (em pixels): o anel vai do retângulo de fora ao de dentro.
    private const float OuterL = 22f;
    private const float OuterT = 22f;
    private const float OuterR = 1166f;
    private const float OuterB = 462f;
    private const float InnerL = 220f;
    private const float InnerT = 110f;
    private const float InnerR = 968f;
    private const float InnerB = 374f;

    // Coluna da linha de chegada (quadriculada) — faixas pintadas no asfalto desviam dela.
    private const float FinishL = 594f;
    private const float FinishR = 616f;

    private static (float L, float T, float R, float B) RingRect(float fraction) => (
        MathHelper.Lerp(OuterL, InnerL, fraction),
        MathHelper.Lerp(OuterT, InnerT, fraction),
        MathHelper.Lerp(OuterR, InnerR, fraction),
        MathHelper.Lerp(OuterB, InnerB, fraction));

    /// <summary>Pinta uma faixa "concêntrica" do anel do asfalto, entre as frações <paramref name="from"/> e
    /// <paramref name="to"/> (0 = borda de fora, 1 = de dentro), sem cobrir a linha de chegada.</summary>
    private static void RingBand(SceneCanvas c, float from, float to, Color color)
    {
        (float l0, float t0, float r0, float b0) = RingRect(from);
        (float l1, float t1, float r1, float b1) = RingRect(to);
        c.Rect(l0, t0, r0 - l0, t1 - t0, color);
        c.Rect(l0, b1, FinishL - l0, b0 - b1, color);
        c.Rect(FinishR, b1, r0 - FinishR, b0 - b1, color);
        c.Rect(l0, t1, l1 - l0, b1 - t1, color);
        c.Rect(r1, t1, r0 - r1, b1 - t1, color);
    }

    /// <summary>Contorno fino do anel na fração dada (faixas de pista de atletismo).</summary>
    private static void RingLine(SceneCanvas c, float fraction, float thickness, Color color)
    {
        (float l, float t, float r, float b) = RingRect(fraction);
        c.Rect(l, t, r - l, thickness, color);
        c.Rect(l, b - thickness, FinishL - l, thickness, color);
        c.Rect(FinishR, b - thickness, r - FinishR, thickness, color);
        c.Rect(l, t, thickness, b - t, color);
        c.Rect(r - thickness, t, thickness, b - t, color);
    }

    private static bool NearInfieldPoint(float x, float y, float cx, float cy, float rx, float ry) =>
        (((x - cx) / rx) * ((x - cx) / rx)) + (((y - cy) / ry) * ((y - cy) / ry)) < 1f;

    // ---------- MONTANHA ----------

    public static SceneryStyle Mountain { get; } = new()
    {
        Background = new Color(58, 84, 58),
        GroundA = new Color(84, 122, 70),
        GroundB = new Color(78, 114, 66),
        Track = new Color(66, 68, 74),
        TrackSpeck = new Color(80, 82, 88),
        Line = new Color(235, 205, 70),
        Edge = EdgeStyle.Rail,
        EdgeBase = new Color(72, 100, 62),
        EdgeA = new Color(170, 176, 186),
        EdgeB = new Color(232, 236, 242),
        PaintStatic = c =>
        {
            GroundSpeckles(c, InfieldArea, 420, new Color(104, 140, 80), new Color(62, 94, 54));

            // Paredão de rocha na margem de cima e rio correndo na de baixo.
            c.Rect(-40f, -40f, W + 80f, 40f, new Color(104, 96, 90));
            for (float x = -40f; x < W + 40f; x += 26f)
            {
                Rock(c, x + c.R(-6f, 6f), -26f + c.R(-6f, 6f), c.R(10f, 16f), c.Pick(new Color(126, 118, 110), new Color(110, 104, 98), new Color(140, 132, 124)));
            }

            c.Rect(-40f, H, W + 80f, 40f, new Color(46, 112, 160));
            c.Rect(-40f, H + 14f, W + 80f, 12f, new Color(66, 140, 190));
            for (float x = -30f; x < W + 40f; x += c.R(60f, 110f))
            {
                Rock(c, x, H + c.R(10f, 30f), c.R(5f, 9f), new Color(120, 120, 124));
            }

            for (float y = 10f; y < H; y += 40f)
            {
                Pine(c, -20f + c.R(-4f, 4f), y, c.R(14f, 18f), new Color(26, 66, 44), new Color(40, 92, 58), new Color(70, 130, 80));
                Pine(c, W + 20f + c.R(-4f, 4f), y + 20f, c.R(14f, 18f), new Color(26, 66, 44), new Color(40, 92, 58), new Color(70, 130, 80));
            }

            // O maciço no miolo, em curvas de nível: grama -> mato -> rocha -> pedra clara -> neve.
            const float cx = 640f;
            const float cy = 244f;
            Color[] rings = [new(98, 124, 78), new(116, 112, 84), new(128, 112, 94), new(146, 140, 134), new(196, 198, 204), new(246, 248, 252)];
            float[] sizes = [1f, 0.8f, 0.62f, 0.45f, 0.3f, 0.18f];
            for (int k = 0; k < rings.Length; k++)
            {
                float rx = 250f * sizes[k];
                float ry = 102f * sizes[k];
                c.Ellipse(cx + (k * 6f), cy - (k * 3f), rx, ry, rings[k]);
                for (int i = 0; i < 28; i++)
                {
                    float a = i * MathF.Tau / 28f;
                    float bump = 1f + (MathF.Sin((a * 3f) + k) * 0.06f) + (MathF.Sin((a * 7f) + (k * 2f)) * 0.04f);
                    c.Circle(cx + (k * 6f) + (MathF.Cos(a) * rx * bump * 0.9f), cy - (k * 3f) + (MathF.Sin(a) * ry * bump * 0.9f), rx * 0.14f, rings[k]);
                }
            }

            c.GlowEllipse(cx - 70f, cy - 40f, 190f, 70f, Color.White * 0.12f);
            c.GlowEllipse(cx + 90f, cy + 45f, 200f, 70f, Color.Black * 0.18f);
            for (int i = 0; i < 5; i++)
            {
                float a = 0.5f + (i * 1.2f);
                c.Line(cx + 40f, cy - 18f, cx + 40f + (MathF.Cos(a) * 34f), cy - 18f + (MathF.Sin(a) * 16f), 2f, new Color(170, 190, 215));
            }

            // Trilha em zigue-zague subindo, lago com cachoeira e um chalé.
            for (int i = 0; i < 10; i++)
            {
                float t = i / 9f;
                float x1 = 430f + (t * 190f);
                float y1 = 330f - (t * 70f) + ((i % 2) * 14f);
                c.Rect(x1, y1, 16f, 2f, new Color(170, 140, 100));
            }

            c.Ellipse(318f, 272f, 58f, 34f, new Color(60, 90, 70));
            c.Ellipse(318f, 272f, 52f, 29f, new Color(60, 130, 170));
            c.Ellipse(306f, 266f, 30f, 12f, new Color(100, 170, 210));
            c.Rect(356f, 200f, 10f, 64f, new Color(170, 215, 240));
            c.Rect(358f, 200f, 3f, 64f, Color.White * 0.8f);
            Rock(c, 352f, 196f, 10f, new Color(120, 116, 110));
            Rock(c, 372f, 204f, 8f, new Color(120, 116, 110));

            c.Rect(862f, 150f, 50f, 36f, new Color(120, 72, 44));
            c.Rect(858f, 146f, 58f, 20f, new Color(160, 60, 50));
            c.Rect(858f, 164f, 58f, 3f, Color.Black * 0.25f);
            c.Rect(900f, 138f, 8f, 12f, new Color(90, 90, 96));
            c.Rect(878f, 174f, 8f, 12f, new Color(255, 210, 120));

            foreach ((float x, float y) in new[] { (270f, 160f), (300f, 330f), (400f, 160f), (560f, 150f), (880f, 320f), (920f, 250f), (820f, 340f), (268f, 220f), (470f, 180f), (930f, 190f) })
            {
                if (!NearInfieldPoint(x, y, cx, cy, 240f, 95f))
                {
                    Pine(c, x, y, c.R(13f, 17f), new Color(26, 66, 44), new Color(40, 92, 58), new Color(70, 130, 80));
                }
            }
        },
        PaintAnimated = c =>
        {
            for (int i = 0; i < 2; i++)
            {
                float x = ((c.Time * 22f) + (i * 640f)) % (W + 400f) - 200f;
                c.GlowEllipse(x, 150f + (i * 170f), 130f, 52f, Color.Black * 0.1f);
            }

            float a = c.Time * 0.7f;
            float ex = 640f + (MathF.Cos(a) * 110f);
            float ey = 244f + (MathF.Sin(a) * 50f);
            float flap = MathF.Sin(c.Time * 8f) * 3f;
            c.Line(ex - 9f, ey - 3f + flap, ex, ey, 2f, new Color(70, 50, 36));
            c.Line(ex, ey, ex + 9f, ey - 3f + flap, 2f, new Color(70, 50, 36));
            c.Circle(ex, ey, 2f, Color.White);

            float flow = (c.Time * 55f) % 70f;
            for (float x = -40f + flow; x < W + 40f; x += 70f)
            {
                c.Ellipse(x, H + 20f, 14f, 1.5f, Color.White * 0.55f);
            }

            float fall = (c.Time * 90f) % 60f;
            c.Rect(357f, 200f + fall, 8f, 3f, Color.White * 0.7f);
            c.Ellipse(318f + (MathF.Sin(c.Time) * 10f), 272f, 8f + (((c.Time * 0.8f) % 1f) * 30f), 3f + (((c.Time * 0.8f) % 1f) * 12f), Color.White * (0.3f * (1f - ((c.Time * 0.8f) % 1f))));
        },
        PaintSecret = (c, p) =>
        {
            var fur = new Color(240, 238, 230);
            var horn = new Color(120, 90, 60);
            c.Ellipse(p.X, p.Y, 6f, 3.5f, fur);
            c.Circle(p.X + 6f, p.Y - 1f, 2.8f, fur);
            c.Line(p.X + 6f, p.Y - 3f, p.X + 3f, p.Y - 7f, 1.2f, horn);
            c.Line(p.X + 7f, p.Y - 3f, p.X + 5f, p.Y - 7f, 1.2f, horn);
            c.Rect(p.X + 7f, p.Y - 2f, 1f, 1f, Color.Black);
        },
    };

    // ---------- ESTÁDIO ----------

    private static readonly Vector2[] StadiumFloodlights = [new(-20f, -20f), new(W + 20f, -20f), new(-20f, H + 20f), new(W + 20f, H + 20f)];

    public static SceneryStyle Stadium { get; } = new()
    {
        Background = new Color(28, 30, 40),
        GroundA = new Color(72, 158, 70),
        GroundB = new Color(64, 148, 64),
        Track = new Color(176, 70, 52),
        TrackSpeck = new Color(156, 58, 44),
        Edge = EdgeStyle.Blocks,
        EdgeBase = new Color(30, 30, 36),
        EdgeA = new Color(40, 90, 200),
        EdgeB = new Color(245, 245, 245),
        Checkpoint = new Color(255, 230, 90),
        Darkness = 0.22f,
        DarknessTint = new Color(8, 12, 40),
        PaintStatic = c =>
        {
            // Raias da pista de atletismo.
            foreach (float f in new[] { 0.25f, 0.5f, 0.75f })
            {
                RingLine(c, f, 1.5f, Color.White * 0.55f);
            }

            // Arquibancada lotada em volta de tudo.
            foreach (Rectangle band in MarginAreas())
            {
                c.Rect(band.X, band.Y, band.Width, band.Height, new Color(44, 46, 58));
                int people = band.Width * band.Height / 14;
                for (int i = 0; i < people; i++)
                {
                    c.Rect(band.X + c.R(0f, band.Width - 2f), band.Y + c.R(2f, band.Height - 4f), 2f, 2f,
                        c.Pick(Color.White, new(230, 60, 60), new(60, 120, 230), new(250, 210, 60), new(40, 40, 40), new(90, 200, 110)));
                }

                bool horizontal = band.Width > band.Height;
                for (float d = 60f; d < (horizontal ? band.Width : band.Height); d += 180f)
                {
                    c.Rect(horizontal ? band.X + d : band.X, horizontal ? band.Y : band.Y + d, horizontal ? 8f : band.Width, horizontal ? band.Height : 8f, new Color(70, 72, 84));
                }
            }

            foreach (Vector2 tower in StadiumFloodlights)
            {
                c.Rect(tower.X - 18f, tower.Y - 18f, 36f, 36f, new Color(90, 94, 104));
                for (int i = 0; i < 9; i++)
                {
                    c.Rect(tower.X - 14f + ((i % 3) * 10f), tower.Y - 14f + ((i / 3) * 10f), 8f, 8f, new Color(250, 250, 225));
                }
            }

            // O gramado em faixas e as linhas do campo.
            for (float x = InL; x < InR; x += 44f)
            {
                c.Rect(x, InT, 22f, InB - InT, Color.White * 0.05f);
            }

            var line = new Color(245, 245, 245);
            const float fl = 264f;
            const float ft = 144f;
            const float fr = 924f;
            const float fb = 340f;
            c.Rect(fl, ft, fr - fl, 2f, line);
            c.Rect(fl, fb - 2f, fr - fl, 2f, line);
            c.Rect(fl, ft, 2f, fb - ft, line);
            c.Rect(fr - 2f, ft, 2f, fb - ft, line);
            c.Rect(593f, ft, 2f, fb - ft, line);
            for (int i = 0; i < 48; i++)
            {
                float a = i * MathF.Tau / 48f;
                c.Rect(594f + (MathF.Cos(a) * 42f) - 1f, 242f + (MathF.Sin(a) * 42f) - 1f, 2f, 2f, line);
            }

            c.Circle(594f, 242f, 3f, line);
            foreach (bool left in new[] { true, false })
            {
                float gx = left ? fl : fr;
                float dir = left ? 1f : -1f;
                c.Rect(left ? gx : gx - 90f, 186f, 90f, 2f, line);
                c.Rect(left ? gx : gx - 90f, 296f, 90f, 2f, line);
                c.Rect(gx + (dir * 90f) - (left ? 0f : 2f), 186f, 2f, 112f, line);
                c.Rect(left ? gx : gx - 34f, 216f, 34f, 2f, line);
                c.Rect(left ? gx : gx - 34f, 266f, 34f, 2f, line);
                c.Rect(gx + (dir * 34f) - (left ? 0f : 2f), 216f, 2f, 52f, line);
                c.Circle(gx + (dir * 62f), 242f, 2.5f, line);

                // Gol com rede.
                float netX = left ? gx - 12f : gx;
                c.Rect(netX, 222f, 12f, 40f, Color.White * 0.25f);
                for (int i = 0; i < 5; i++)
                {
                    c.Rect(netX, 224f + (i * 8f), 12f, 1f, Color.White * 0.6f);
                }

                c.Rect(left ? gx - 2f : gx, 220f, 2f, 44f, Color.White);
            }

            // Os dois times em formação.
            (float X, float Y)[] formation = [(0.08f, 0f), (0.22f, -0.3f), (0.22f, -0.1f), (0.22f, 0.1f), (0.22f, 0.3f), (0.36f, -0.25f), (0.36f, 0f), (0.36f, 0.25f), (0.44f, -0.12f), (0.44f, 0.12f), (0.02f, 0f)];
            foreach (bool home in new[] { true, false })
            {
                Color shirt = home ? new Color(220, 50, 50) : new Color(50, 100, 220);
                foreach ((float fx, float fy) in formation.Skip(1).Prepend(formation[0]))
                {
                    float px = home ? 264f + (fx * 660f) : 924f - (fx * 660f);
                    float py = 242f + (fy * 190f);
                    c.Circle(px + 1f, py + 2f, 4.5f, Color.Black * 0.25f);
                    c.Circle(px, py, 4.5f, shirt);
                    c.Circle(px, py, 2f, new Color(80, 50, 30));
                }
            }
        },
        PaintAnimated = c =>
        {
            float bx = 594f + (MathF.Sin(c.Time * 0.9f) * 250f);
            float by = 242f + (MathF.Sin(c.Time * 1.7f) * 70f);
            c.Circle(bx + 2f, by + 3f, 3.5f, Color.Black * 0.3f);
            c.Circle(bx, by, 3.5f, Color.White);
            c.Rect(bx - 1f, by - 1f, 2f, 2f, new Color(30, 30, 30));

            float wave = (c.Time * 220f) % (W + 80f);
            c.Rect(wave - 40f, -38f, 30f, 36f, Color.White * 0.14f);
            c.Rect(W - wave, H + 2f, 30f, 36f, Color.White * 0.14f);

            // Flashes de câmera na torcida.
            int slot = (int)(c.Time * 4f);
            for (int i = 0; i < 4; i++)
            {
                int hash = ((slot * 73) + (i * 191)) & 1023;
                float fx = -30f + (hash * (W + 60f) / 1023f);
                float fy = i % 2 == 0 ? -20f + ((hash % 30) - 15f) * 0.6f : H + 20f + ((hash % 30) - 15f) * 0.6f;
                c.Circle(fx, fy, 2f, Color.White);
            }
        },
        PaintLights = c =>
        {
            foreach (Vector2 tower in StadiumFloodlights)
            {
                c.Glow(tower.X, tower.Y, 90f, new Color(255, 255, 230) * 0.35f);
                c.GlowEllipse(tower.X + ((W / 2f) - tower.X) * 0.25f, tower.Y + ((H / 2f) - tower.Y) * 0.25f, 260f, 120f, new Color(255, 255, 230) * 0.07f);
            }

            c.GlowEllipse(594f, 242f, 360f, 120f, new Color(255, 255, 240) * 0.08f);
        },
        PaintSecret = (c, p) =>
        {
            c.Ellipse(p.X, p.Y, 7f, 3f, new Color(25, 25, 30), 0.2f);
            c.Rect(p.X - 2f, p.Y - 2f, 5f, 1f, Color.White);
            c.Rect(p.X - 3f, p.Y, 5f, 1f, Color.White);
            c.Circle(p.X + 6f, p.Y + 1f, 1f, new Color(220, 220, 220));
        },
    };

    // ---------- FUNDO DO MAR ----------

    private static readonly Vector2[] Seaweed = BuildSeaweed();

    private static Vector2[] BuildSeaweed()
    {
        var list = new List<Vector2>();
        for (float x = 260f; x < 940f; x += 58f)
        {
            list.Add(new Vector2(x, x % 3 < 1.5f ? 150f : 340f));
        }

        for (float x = 20f; x < W; x += 90f)
        {
            list.Add(new Vector2(x, -18f));
            list.Add(new Vector2(x + 45f, H + 18f));
        }

        return [.. list];
    }

    private static void Coral(SceneCanvas c, float x, float y, float size, Color color)
    {
        for (int i = 0; i < 7; i++)
        {
            float a = c.R(0f, MathF.Tau);
            float d = c.R(0f, size);
            c.Circle(x + (MathF.Cos(a) * d), y + (MathF.Sin(a) * d * 0.7f), c.R(size * 0.25f, size * 0.45f), color);
        }

        c.Circle(x, y, size * 0.3f, CarPainter.Darken(color, 1.15f));
    }

    public static SceneryStyle Underwater { get; } = new()
    {
        Background = new Color(14, 52, 84),
        GroundA = new Color(196, 182, 134),
        GroundB = new Color(188, 174, 126),
        Track = new Color(104, 124, 136),
        TrackSpeck = new Color(134, 154, 162),
        Edge = EdgeStyle.Rocks,
        EdgeBase = new Color(60, 88, 104),
        EdgeA = new Color(232, 108, 118),
        EdgeB = new Color(164, 96, 186),
        Checkpoint = new Color(120, 240, 255),
        Darkness = 0.24f,
        DarknessTint = new Color(0, 40, 90),
        Ambient = AmbientKind.Bubbles,
        AmbientCount = 45,
        AmbientColor = new Color(200, 240, 255),
        PaintStatic = c =>
        {
            foreach (Rectangle area in MarginAreas().Append(InfieldArea))
            {
                for (float y = area.Y + 5f; y < area.Bottom; y += 10f)
                {
                    for (float x = area.X; x < area.Right; x += 7f)
                    {
                        c.Rect(x, y + (MathF.Sin((x * 0.04f) + (y * 0.3f)) * 2.5f), 6f, 1f, new Color(160, 146, 100) * 0.35f);
                    }
                }
            }

            Color[] corals = [new(240, 110, 120), new(250, 160, 70), new(170, 100, 200), new(250, 200, 90), new(90, 200, 190)];
            foreach (Rectangle band in MarginAreas())
            {
                int count = Math.Max(4, (band.Width + band.Height) / 60);
                for (int i = 0; i < count; i++)
                {
                    float x = c.R(band.Left + 8f, band.Right - 8f);
                    float y = c.R(band.Top + 8f, band.Bottom - 8f);
                    if (c.Chance(0.5))
                    {
                        Coral(c, x, y, c.R(8f, 13f), c.Pick(corals));
                    }
                    else
                    {
                        Rock(c, x, y, c.R(6f, 11f), new Color(90, 110, 120));
                    }
                }
            }

            // Naufrágio: casco tombado, convés, mastro quebrado e escotilhas.
            c.Ellipse(760f, 250f, 128f, 46f, Color.Black * 0.25f, -0.15f);
            c.Ellipse(752f, 244f, 124f, 42f, new Color(92, 62, 40), -0.15f);
            c.Ellipse(752f, 244f, 112f, 32f, new Color(122, 86, 56), -0.15f);
            for (int i = -4; i <= 4; i++)
            {
                c.RectC(752f + (i * 22f), 244f - (i * 3.3f), 2f, 56f, new Color(92, 62, 40), -0.15f);
            }

            c.Circle(700f, 238f, 14f, new Color(40, 30, 24));
            c.RectC(800f, 212f, 110f, 6f, new Color(110, 76, 46), -0.9f);
            foreach (float x in new[] { 690f, 730f, 770f, 810f })
            {
                c.Circle(x, 276f - ((x - 752f) * 0.15f), 5f, new Color(60, 44, 30));
                c.Circle(x, 276f - ((x - 752f) * 0.15f), 3f, new Color(90, 150, 170));
            }

            // Baú do tesouro, âncora, estrelas-do-mar e conchas.
            c.Rect(868f, 300f, 34f, 22f, new Color(120, 76, 40));
            c.Rect(868f, 300f, 34f, 5f, new Color(90, 56, 30));
            c.Rect(883f, 306f, 4f, 6f, new Color(240, 200, 60));
            for (int i = 0; i < 16; i++)
            {
                c.Circle(c.R(862f, 910f), c.R(318f, 334f), 3f, new Color(250, 205, 60));
            }

            var anchor = new Color(70, 76, 86);
            c.Rect(452f, 180f, 5f, 44f, anchor);
            c.Rect(440f, 184f, 29f, 4f, anchor);
            c.Line(438f, 222f, 454f, 230f, 4f, anchor);
            c.Line(470f, 222f, 454f, 230f, 4f, anchor);
            for (int i = 0; i < 8; i++)
            {
                c.Circle(456f - (i * 9f), 176f - (i * 3f), 3f, anchor);
            }

            foreach ((float x, float y) in new[] { (320f, 190f), (560f, 320f), (640f, 160f), (920f, 170f), (300f, 310f) })
            {
                for (int k = 0; k < 5; k++)
                {
                    float a = k * MathF.Tau / 5f;
                    c.RectC(x + (MathF.Cos(a) * 5f), y + (MathF.Sin(a) * 5f), 10f, 4f, new Color(245, 130, 80), a);
                }

                c.Circle(x, y, 3f, new Color(250, 160, 100));
            }

            for (int i = 0; i < 12; i++)
            {
                float x = c.R(InL + 10f, InR - 10f);
                float y = c.R(InT + 10f, InB - 10f);
                if (!NearInfieldPoint(x, y, 760f, 250f, 160f, 70f))
                {
                    Coral(c, x, y, c.R(9f, 15f), c.Pick(corals));
                }
            }

            foreach (Vector2 weed in Seaweed)
            {
                c.Circle(weed.X, weed.Y, 5f, new Color(30, 80, 50));
            }
        },
        PaintAnimated = c =>
        {
            foreach (Vector2 weed in Seaweed)
            {
                for (int s = 1; s < 6; s++)
                {
                    float sway = MathF.Sin((c.Time * 1.8f) + (weed.X * 0.05f) + (s * 0.5f)) * s * 2.2f;
                    c.Circle(weed.X + sway, weed.Y + (weed.Y < 0f ? s * 3f : weed.Y > H ? -s * 3f : -s * 5f), 4.5f - (s * 0.4f), s % 2 == 0 ? new Color(50, 140, 80) : new Color(40, 115, 66));
                }
            }

            for (int school = 0; school < 2; school++)
            {
                float baseX = ((c.Time * (40f + (school * 18f))) + (school * 420f)) % (InR - InL + 120f) + InL - 60f;
                float baseY = 180f + (school * 110f) + (MathF.Sin(c.Time + school) * 12f);
                Color fish = school == 0 ? new Color(250, 150, 50) : new Color(120, 200, 250);
                for (int i = 0; i < 6; i++)
                {
                    float fx = baseX - ((i % 3) * 12f) - (school == 0 ? 0f : 20f);
                    float fy = baseY + ((i / 3) * 10f) - 5f;
                    c.Ellipse(fx, fy, 4.5f, 2.5f, fish);
                    c.RectC(fx - 5f, fy, 3f, 4f, fish);
                }
            }

            float jy = 250f + (MathF.Sin(c.Time * 0.6f) * 60f);
            float pulse = 1f + (MathF.Sin(c.Time * 4f) * 0.15f);
            c.Circle(600f, jy, 9f * pulse, new Color(250, 150, 220) * 0.6f);
            for (int i = 0; i < 4; i++)
            {
                c.Line(594f + (i * 4f), jy + 6f, 594f + (i * 4f) + (MathF.Sin((c.Time * 3f) + i) * 3f), jy + 20f, 1f, new Color(250, 180, 230) * 0.6f);
            }
        },
        PaintLights = c =>
        {
            for (int i = 0; i < 5; i++)
            {
                float sway = MathF.Sin((c.Time * 0.4f) + i) * 30f;
                c.GlowEllipse(160f + (i * 230f) + sway, 200f, 50f, 300f, new Color(180, 230, 255) * 0.06f, 0.35f);
            }

            c.Glow(884f, 318f, 30f, new Color(255, 220, 90) * (0.25f + (MathF.Sin(c.Time * 3f) * 0.1f)));
            float jy = 250f + (MathF.Sin(c.Time * 0.6f) * 60f);
            c.Glow(600f, jy, 26f, new Color(255, 140, 230) * 0.35f);
        },
        PaintSecret = (c, p) =>
        {
            c.Ellipse(p.X, p.Y + 2f, 7f, 4f, new Color(150, 130, 170));
            c.Ellipse(p.X, p.Y - 2f, 7f, 3.5f, new Color(190, 170, 205));
            c.Circle(p.X, p.Y + 1f, 2.5f, new Color(250, 250, 255));
            c.Rect(p.X - 1f, p.Y, 1f, 1f, Color.White);
        },
    };

    // ---------- ACIMA DAS NUVENS ----------

    private static void CloudPuff(SceneCanvas c, float x, float y, float size)
    {
        (float Dx, float Dy, float R)[] puffs = [(0f, 0f, 1f), (-0.9f, 0.2f, 0.7f), (0.9f, 0.25f, 0.75f), (-0.4f, -0.45f, 0.65f), (0.45f, -0.4f, 0.6f)];
        foreach ((float dx, float dy, float r) in puffs)
        {
            c.Circle(x + (dx * size) + 3f, y + (dy * size) + 4f, r * size, new Color(170, 196, 232));
        }

        foreach ((float dx, float dy, float r) in puffs)
        {
            c.Circle(x + (dx * size), y + (dy * size), r * size, new Color(250, 252, 255));
        }

        c.Circle(x - (size * 0.3f), y - (size * 0.3f), size * 0.35f, Color.White);
    }

    public static SceneryStyle Clouds { get; } = new()
    {
        Background = new Color(122, 184, 242),
        GroundA = new Color(132, 192, 246),
        GroundB = new Color(126, 186, 242),
        Track = new Color(246, 242, 232),
        TrackSpeck = new Color(232, 228, 218),
        Edge = EdgeStyle.Snowbank,
        EdgeBase = new Color(214, 228, 248),
        EdgeA = new Color(255, 255, 255),
        EdgeB = new Color(186, 208, 240),
        Checkpoint = new Color(255, 255, 255),
        PaintStatic = c =>
        {
            // Estrada arco-íris: seis faixas acompanhando o anel.
            Color[] rainbow = [new(240, 80, 80), new(250, 150, 60), new(250, 220, 70), new(90, 200, 100), new(80, 150, 240), new(160, 100, 220)];
            for (int i = 0; i < rainbow.Length; i++)
            {
                RingBand(c, i / 6f, (i + 1) / 6f, rainbow[i] * 0.55f);
            }

            foreach (Rectangle band in MarginAreas())
            {
                c.Rect(band.X, band.Y, band.Width, band.Height, new Color(120, 180, 240));
            }

            c.AlongMargin(46f, (x, y) => CloudPuff(c, x + c.R(-6f, 6f), y + c.R(-4f, 4f), c.R(11f, 15f)));

            // Ilhas de nuvem no céu do miolo, uma com castelinho.
            CloudPuff(c, 330f, 190f, 34f);
            CloudPuff(c, 380f, 214f, 26f);
            CloudPuff(c, 860f, 290f, 30f);
            CloudPuff(c, 560f, 320f, 20f);
            CloudPuff(c, 700f, 160f, 18f);
            foreach ((float x, float y, float w, float h) in new[] { (318f, 162f, 16f, 22f), (340f, 156f, 14f, 30f), (360f, 166f, 14f, 18f) })
            {
                c.Rect(x + 3f, y + 4f, w, h, Color.Black * 0.12f);
                c.Rect(x, y, w, h, new Color(236, 232, 240));
                c.Rect(x - 2f, y - 6f, w + 4f, 8f, new Color(240, 130, 170));
            }

            // Arco-íris pequeno e pássaros.
            for (int i = 0; i < rainbow.Length; i++)
            {
                for (int s = 0; s < 18; s++)
                {
                    float a = MathF.PI + (s / 17f * MathF.PI);
                    c.Circle(620f + (MathF.Cos(a) * (60f - (i * 5f))), 300f + (MathF.Sin(a) * (40f - (i * 4f))), 3f, rainbow[i] * 0.8f);
                }
            }

            c.Circle(-10f, -10f, 34f, new Color(255, 230, 120));
            c.Circle(-10f, -10f, 26f, new Color(255, 245, 170));
        },
        PaintAnimated = c =>
        {
            Color[] balloons = [new(230, 70, 70), new(250, 200, 50), new(80, 170, 240)];
            for (int i = 0; i < 3; i++)
            {
                float x = InL + 40f + ((((c.Time * (10f + (i * 4f))) + (i * 230f)) % (InR - InL - 80f)));
                float y = 180f + (i * 55f) + (MathF.Sin((c.Time * 0.8f) + i) * 10f);
                c.Circle(x + 10f, y + 14f, 12f, Color.Black * 0.12f);
                c.Circle(x, y, 12f, balloons[i]);
                for (int k = 0; k < 4; k++)
                {
                    c.RectC(x, y, 24f, 3f, Color.White * 0.5f, k * MathF.PI / 4f);
                }

                c.Rect(x - 3f, y - 3f, 6f, 6f, new Color(140, 100, 60));
            }

            for (int i = 0; i < 3; i++)
            {
                float bx = ((c.Time * 60f) + (i * 30f)) % (W + 200f) - 100f;
                float by = 140f + (i * 12f) + (MathF.Sin((c.Time * 2f) + i) * 4f);
                float flap = MathF.Sin((c.Time * 9f) + i) * 3f;
                c.Line(bx - 6f, by - 3f + flap, bx, by, 1.5f, new Color(40, 50, 70));
                c.Line(bx, by, bx + 6f, by - 3f + flap, 1.5f, new Color(40, 50, 70));
            }

            float plane = (c.Time * 90f) % (W + 600f) - 300f;
            c.Rect(plane - 160f, 60f, 150f, 3f, Color.White * 0.5f);
            c.RectC(plane, 61f, 20f, 5f, Color.White);
            c.RectC(plane + 2f, 61f, 5f, 18f, Color.White);
        },
        PaintLights = c => c.Glow(-10f, -10f, 180f, new Color(255, 240, 180) * 0.3f),
        PaintSecret = (c, p) =>
        {
            var gold = new Color(255, 205, 60);
            c.Line(p.X - 7f, p.Y + 3f, p.X + 7f, p.Y - 3f, 1f, new Color(180, 130, 30));
            for (int i = 0; i < 5; i++)
            {
                float t = (i / 4f) - 0.5f;
                c.Ellipse(p.X + (t * 11f), p.Y - (t * 4f), 3f, 2f, gold, -0.4f);
            }

            Sparkle(c, p + new Vector2(6f, -6f), Color.White);
        },
    };

    // ---------- NOITE ASSOMBRADA ----------

    private static readonly (float X, float Y)[] Pumpkins =
    [
        (282f, 320f), (430f, 150f), (560f, 336f), (910f, 154f), (880f, 330f), (-20f, 120f), (W + 20f, 300f), (300f, -20f), (820f, H + 20f),
    ];

    private static readonly (float X, float Y)[] HauntedWindows = [(700f, 214f), (730f, 214f), (760f, 214f), (715f, 262f), (760f, 262f)];

    private static void Tombstone(SceneCanvas c, float x, float y, bool cross)
    {
        c.Rect(x - 5f, y - 2f, 14f, 8f, Color.Black * 0.3f);
        var stone = new Color(120, 124, 132);
        if (cross)
        {
            c.Rect(x - 2f, y - 12f, 4f, 16f, stone);
            c.Rect(x - 7f, y - 8f, 14f, 4f, stone);
        }
        else
        {
            c.Rect(x - 6f, y - 10f, 12f, 14f, stone);
            c.Circle(x, y - 10f, 6f, stone);
            c.Rect(x - 3f, y - 7f, 6f, 1f, new Color(80, 84, 92));
            c.Rect(x - 3f, y - 4f, 6f, 1f, new Color(80, 84, 92));
        }

        c.Ellipse(x, y + 8f, 8f, 3f, new Color(64, 54, 44));
    }

    private static void Pumpkin(SceneCanvas c, float x, float y)
    {
        c.Ellipse(x + 2f, y + 3f, 10f, 8f, Color.Black * 0.3f);
        c.Ellipse(x, y, 10f, 8f, new Color(230, 120, 30));
        c.Ellipse(x - 4f, y, 3f, 7f, new Color(200, 95, 20));
        c.Ellipse(x + 4f, y, 3f, 7f, new Color(200, 95, 20));
        c.Rect(x - 1f, y - 10f, 2f, 4f, new Color(70, 110, 40));
        c.Rect(x - 5f, y - 3f, 3f, 3f, new Color(255, 220, 90));
        c.Rect(x + 2f, y - 3f, 3f, 3f, new Color(255, 220, 90));
        c.Rect(x - 4f, y + 2f, 8f, 2f, new Color(255, 220, 90));
    }

    private static void DeadTree(SceneCanvas c, float x, float y, float size)
    {
        var bark = new Color(50, 40, 36);
        c.Circle(x, y, size * 0.25f, bark);
        for (int i = 0; i < 6; i++)
        {
            float a = (i * MathF.Tau / 6f) + c.R(-0.3f, 0.3f);
            float len = size * c.R(0.6f, 1f);
            float ex = x + (MathF.Cos(a) * len);
            float ey = y + (MathF.Sin(a) * len);
            c.Line(x, y, ex, ey, 2.5f, bark);
            c.Line(ex, ey, ex + (MathF.Cos(a + 0.6f) * size * 0.3f), ey + (MathF.Sin(a + 0.6f) * size * 0.3f), 1.5f, bark);
        }
    }

    public static SceneryStyle Haunted { get; } = new()
    {
        Background = new Color(14, 16, 22),
        GroundA = new Color(42, 52, 40),
        GroundB = new Color(38, 48, 36),
        Track = new Color(88, 78, 70),
        TrackSpeck = new Color(70, 62, 56),
        Edge = EdgeStyle.Fence,
        EdgeBase = new Color(34, 40, 32),
        EdgeA = new Color(18, 18, 22),
        EdgeB = new Color(80, 80, 92),
        Checkpoint = new Color(190, 120, 255),
        Darkness = 0.42f,
        DarknessTint = new Color(12, 0, 30),
        Headlights = true,
        Ambient = AmbientKind.Fireflies,
        AmbientCount = 28,
        AmbientColor = new Color(170, 255, 140),
        PaintStatic = c =>
        {
            GroundSpeckles(c, InfieldArea, 400, new Color(60, 72, 52), new Color(30, 38, 28));
            foreach (Rectangle band in MarginAreas())
            {
                GroundSpeckles(c, band, band.Width * band.Height / 60, new Color(60, 72, 52), new Color(30, 38, 28));
            }

            c.AlongMargin(58f, (x, y) =>
            {
                if (c.Chance(0.55))
                {
                    Tombstone(c, x + c.R(-8f, 8f), y + c.R(-4f, 4f), c.Chance(0.3));
                }
                else
                {
                    DeadTree(c, x, y, c.R(12f, 18f));
                }
            });

            // Casa assombrada no miolo: paredes escuras, telhados pontudos e janelas acesas.
            c.Rect(676f, 184f, 124f, 104f, Color.Black * 0.35f);
            c.Rect(670f, 178f, 120f, 100f, new Color(56, 44, 66));
            c.RectC(700f, 186f, 44f, 44f, new Color(40, 30, 50), MathF.PI / 4f);
            c.RectC(760f, 190f, 36f, 36f, new Color(40, 30, 50), MathF.PI / 4f);
            c.Rect(736f, 240f, 20f, 38f, new Color(30, 22, 36));
            foreach ((float wx, float wy) in HauntedWindows)
            {
                c.Rect(wx - 5f, wy - 6f, 10f, 12f, new Color(250, 210, 90));
                c.Rect(wx - 0.5f, wy - 6f, 1f, 12f, new Color(40, 30, 50));
            }

            // Lago escuro com o reflexo da lua.
            c.Ellipse(430f, 262f, 70f, 32f, new Color(20, 26, 36));
            c.Ellipse(430f, 262f, 64f, 27f, new Color(26, 36, 56));
            c.Circle(446f, 256f, 10f, new Color(230, 230, 200));
            c.Circle(450f, 253f, 9f, new Color(26, 36, 56));

            foreach ((float x, float y) in new[] { (300f, 170f), (340f, 176f), (320f, 206f), (520f, 170f), (560f, 200f), (600f, 176f), (860f, 200f), (900f, 230f), (560f, 280f), (600f, 300f), (300f, 250f), (860f, 270f) })
            {
                Tombstone(c, x, y, ((int)x + (int)y) % 3 == 0);
            }

            DeadTree(c, 640f, 240f, 30f);
            DeadTree(c, 920f, 320f, 22f);
            DeadTree(c, 260f, 340f, 20f);

            foreach ((float x, float y) in Pumpkins)
            {
                Pumpkin(c, x, y);
            }

            // Teias de aranha nos cantos do miolo.
            foreach ((float x, float y, float dx, float dy) in new[] { (InL + 2f, InT + 2f, 1f, 1f), (InR - 2f, InB - 2f, -1f, -1f) })
            {
                for (int i = 0; i < 5; i++)
                {
                    float a = i * (MathF.PI / 2f) / 4f;
                    c.Line(x, y, x + (MathF.Cos(a) * 36f * dx), y + (MathF.Sin(a) * 36f * dy), 1f, Color.White * 0.35f);
                }

                for (int r = 1; r < 4; r++)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        float a0 = i * (MathF.PI / 2f) / 4f;
                        float a1 = (i + 1) * (MathF.PI / 2f) / 4f;
                        c.Line(x + (MathF.Cos(a0) * r * 10f * dx), y + (MathF.Sin(a0) * r * 10f * dy), x + (MathF.Cos(a1) * r * 10f * dx), y + (MathF.Sin(a1) * r * 10f * dy), 1f, Color.White * 0.3f);
                    }
                }
            }
        },
        PaintAnimated = c =>
        {
            for (int i = 0; i < 3; i++)
            {
                float a = (c.Time * (0.6f + (i * 0.15f))) + (i * 2f);
                float bx = 594f + (MathF.Sin(a) * (300f - (i * 40f)));
                float by = 242f + (MathF.Sin(a * 2f) * 80f);
                float flap = MathF.Sin(c.Time * 14f + i) * 4f;
                c.Line(bx - 7f, by - flap, bx, by, 2f, new Color(20, 16, 24));
                c.Line(bx, by, bx + 7f, by - flap, 2f, new Color(20, 16, 24));
                c.Circle(bx, by, 2f, new Color(20, 16, 24));
            }

            float gx = ((c.Time * 30f) % (InR - InL + 100f)) + InL - 50f;
            float gy = 300f + (MathF.Sin(c.Time * 1.5f) * 16f);
            c.Circle(gx, gy, 9f, Color.White * 0.45f);
            c.Rect(gx - 9f, gy, 18f, 8f, Color.White * 0.45f);
            c.Rect(gx + 2f, gy - 2f, 2f, 3f, Color.Black * 0.6f);
            c.Rect(gx + 6f, gy - 2f, 2f, 3f, Color.Black * 0.6f);

            for (int i = 0; i < 4; i++)
            {
                float fx = ((c.Time * (12f + (i * 5f))) + (i * 330f)) % (W + 400f) - 200f;
                c.GlowEllipse(fx, 60f + (i * 110f), 180f, 40f, Color.White * 0.05f);
            }
        },
        PaintLights = c =>
        {
            float flicker = 0.85f + (MathF.Sin(c.Time * 11f) * 0.08f) + (MathF.Sin(c.Time * 17f) * 0.07f);
            foreach ((float x, float y) in Pumpkins)
            {
                c.Glow(x, y, 26f, new Color(255, 140, 40) * (0.35f * flicker));
            }

            foreach ((float wx, float wy) in HauntedWindows)
            {
                c.Glow(wx, wy, 18f, new Color(255, 210, 90) * 0.3f);
            }

            c.Glow(446f, 256f, 40f, new Color(200, 210, 255) * 0.2f);
            float gx = ((c.Time * 30f) % (InR - InL + 100f)) + InL - 50f;
            c.Glow(gx, 300f + (MathF.Sin(c.Time * 1.5f) * 16f), 30f, new Color(200, 220, 255) * 0.2f);
            c.Glow((50.5f * 22f), 20.6f * 22f, 10f, new Color(150, 200, 255) * 0.5f);
        },
        PaintSecret = (c, p) =>
        {
            c.Circle(p.X, p.Y, 4f, new Color(200, 225, 255) * 0.8f);
            c.Circle(p.X - 3f, p.Y + 2f, 2.5f, new Color(200, 225, 255) * 0.6f);
            c.Circle(p.X - 5f, p.Y + 4f, 1.5f, new Color(200, 225, 255) * 0.4f);
            c.Rect(p.X - 1f, p.Y - 1f, 1f, 1f, new Color(40, 50, 90));
            c.Rect(p.X + 1f, p.Y - 1f, 1f, 1f, new Color(40, 50, 90));
        },
    };
}
