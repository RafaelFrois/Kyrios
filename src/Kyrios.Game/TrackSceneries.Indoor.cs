using Microsoft.Xna.Framework;

namespace Kyrios.Game;

/// <summary>Cenários "dentro de casa" em escala gigante: escritório, sala, setup gamer, caderno, sinuca e bolo.</summary>
public static partial class TrackSceneries
{
    /// <summary>Cor pelo matiz (0-1), com saturação e brilho fixos — pro RGB que fica trocando de cor.</summary>
    private static Color Hue(float hue, float brightness = 1f)
    {
        hue -= MathF.Floor(hue);
        float r = Math.Clamp(MathF.Abs((hue * 6f) - 3f) - 1f, 0f, 1f);
        float g = Math.Clamp(2f - MathF.Abs((hue * 6f) - 2f), 0f, 1f);
        float b = Math.Clamp(2f - MathF.Abs((hue * 6f) - 4f), 0f, 1f);
        return new Color(r * brightness, g * brightness, b * brightness);
    }

    private static void PottedPlant(SceneCanvas c, float x, float y, float r)
    {
        c.Circle(x + 2f, y + 3f, r, Color.Black * 0.25f);
        c.Circle(x, y, r * 0.75f, new Color(180, 100, 60));
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f;
            c.Ellipse(x + (MathF.Cos(a) * r * 0.45f), y + (MathF.Sin(a) * r * 0.45f), r * 0.55f, r * 0.22f, i % 2 == 0 ? new Color(60, 150, 70) : new Color(40, 120, 56), a);
        }
    }

    // ---------- ESCRITÓRIO ----------

    private static readonly Vector2[] OfficeMonitors = BuildOfficeMonitors();

    private static Vector2[] BuildOfficeMonitors()
    {
        var list = new List<Vector2>();
        for (int col = 0; col < 5; col++)
        {
            list.Add(new Vector2(302f + (col * 132f), 152f));
            list.Add(new Vector2(302f + (col * 132f), 334f));
        }

        return [.. list];
    }

    public static SceneryStyle Office { get; } = new()
    {
        Background = new Color(70, 78, 96),
        GroundA = new Color(88, 96, 116),
        GroundB = new Color(84, 92, 112),
        Track = new Color(196, 198, 204),
        TrackSpeck = new Color(178, 180, 188),
        Edge = EdgeStyle.Blocks,
        EdgeBase = new Color(60, 64, 76),
        EdgeA = new Color(124, 132, 150),
        EdgeB = new Color(104, 112, 130),
        Checkpoint = new Color(255, 200, 40),
        PaintStatic = c =>
        {
            foreach (Rectangle area in MarginAreas().Append(InfieldArea))
            {
                GroundSpeckles(c, area, area.Width * area.Height / 120, new Color(100, 108, 128), new Color(74, 82, 100));
            }

            // Janelões na margem de cima, arquivos e plantas embaixo.
            for (float x = -30f; x < W + 30f; x += 70f)
            {
                c.Rect(x, -36f, 62f, 30f, new Color(150, 200, 235));
                c.Rect(x, -36f, 62f, 3f, new Color(200, 204, 214));
                c.Rect(x + 30f, -36f, 2f, 30f, new Color(200, 204, 214));
                c.Rect(x + 6f, -30f, 16f, 3f, Color.White * 0.6f);
            }

            for (float x = -30f; x < W + 30f; x += 90f)
            {
                c.Rect(x + 3f, H + 8f, 34f, 26f, Color.Black * 0.25f);
                c.Rect(x, H + 5f, 34f, 26f, new Color(150, 156, 168));
                c.Rect(x + 12f, H + 10f, 10f, 2f, new Color(90, 96, 108));
                c.Rect(x + 12f, H + 22f, 10f, 2f, new Color(90, 96, 108));
                PottedPlant(c, x + 62f, H + 20f, 12f);
            }

            for (float y = 40f; y < H; y += 110f)
            {
                PottedPlant(c, -20f, y, 13f);
                c.Circle(W + 20f, y + 50f, 10f, new Color(60, 64, 76));
                c.Circle(W + 20f, y + 50f, 7f, new Color(40, 44, 54));
            }

            // Baias: duas fileiras de mesas com monitor, teclado, cadeira e papelada.
            for (int col = 0; col < 5; col++)
            {
                foreach (bool top in new[] { true, false })
                {
                    float x = 252f + (col * 132f);
                    float y = top ? 140f : 262f;
                    c.Rect(x, y, 122f, 4f, new Color(120, 128, 146));
                    c.Rect(x, y, 4f, 80f, new Color(120, 128, 146));
                    c.Rect(x + 118f, y, 4f, 80f, new Color(120, 128, 146));
                    float deskY = top ? y + 6f : y + 50f;
                    c.Rect(x + 8f, deskY, 106f, 26f, new Color(170, 130, 90));
                    c.Rect(x + 8f, deskY, 106f, 3f, new Color(190, 150, 110));
                    Vector2 monitor = OfficeMonitors[(col * 2) + (top ? 0 : 1)];
                    c.Rect(monitor.X - 22f, deskY + 4f, 44f, 6f, new Color(30, 32, 38));
                    c.Rect(monitor.X - 18f, deskY + 13f, 36f, 8f, new Color(200, 202, 208));
                    c.Rect(x + 90f, deskY + 6f, 16f, 12f, new Color(250, 250, 245));
                    c.Rect(x + 92f, deskY + 8f, 16f, 12f, new Color(240, 240, 235));
                    c.Circle(x + 20f, deskY + 12f, 5f, new Color(250, 250, 250));
                    c.Circle(x + 20f, deskY + 12f, 3f, new Color(90, 55, 30));
                    float chairY = top ? deskY + 40f : deskY - 14f;
                    c.Circle(monitor.X + 2f, chairY + 2f, 10f, Color.Black * 0.25f);
                    c.Circle(monitor.X, chairY, 10f, new Color(40, 42, 50));
                    c.Circle(monitor.X, chairY, 6f, new Color(60, 64, 76));
                }
            }

            // Bebedouro e impressora no corredor do meio.
            c.Circle(612f, 242f, 10f, new Color(230, 232, 238));
            c.Circle(612f, 242f, 7f, new Color(120, 190, 240));
            c.Rect(840f, 232f, 34f, 22f, new Color(210, 212, 218));
            c.Rect(844f, 228f, 26f, 6f, new Color(250, 250, 250));
            c.Rect(846f, 240f, 22f, 3f, new Color(40, 42, 50));
            PottedPlant(c, 380f, 242f, 14f);
        },
        PaintAnimated = c =>
        {
            for (int i = 0; i < OfficeMonitors.Length; i++)
            {
                Vector2 m = OfficeMonitors[i];
                float deskY = i % 2 == 0 ? 146f : 312f;
                Color screen = ((int)((c.Time * 0.7f) + i) % 3) switch
                {
                    0 => new Color(80, 150, 230),
                    1 => new Color(90, 200, 140),
                    _ => new Color(230, 230, 240),
                };
                c.Rect(m.X - 20f, deskY + 5f, 40f, 3f, screen);
            }

            float paper = (c.Time * 0.4f) % 1f;
            if (paper < 0.5f)
            {
                c.Rect(846f, 224f - (paper * 30f), 22f, 14f, Color.White);
            }

            float bubble = (c.Time * 1.3f) % 1f;
            c.Circle(612f + (MathF.Sin(c.Time * 5f) * 2f), 244f - (bubble * 6f), 1.5f, Color.White * (1f - bubble));
        },
        PaintSecret = (c, p) =>
        {
            c.RectC(p.X, p.Y + 1f, 14f, 4f, new Color(40, 40, 46));
            c.RectC(p.X, p.Y - 1f, 14f, 4f, new Color(220, 40, 40));
            c.RectC(p.X + 5f, p.Y - 1f, 3f, 3f, new Color(160, 20, 20));
        },
    };

    // ---------- SALA DE ESTAR ----------

    public static SceneryStyle LivingRoom { get; } = new()
    {
        Background = new Color(140, 94, 58),
        GroundA = new Color(158, 108, 68),
        GroundB = new Color(152, 103, 64),
        Track = new Color(208, 196, 172),
        TrackSpeck = new Color(190, 176, 150),
        Edge = EdgeStyle.Planks,
        EdgeBase = new Color(120, 80, 48),
        EdgeA = new Color(96, 62, 36),
        EdgeB = new Color(170, 120, 76),
        Darkness = 0.18f,
        DarknessTint = new Color(40, 20, 0),
        PaintStatic = c =>
        {
            foreach (Rectangle area in MarginAreas().Append(InfieldArea))
            {
                for (float y = area.Y; y < area.Bottom; y += 14f)
                {
                    c.Rect(area.X, y, area.Width, 1f, new Color(110, 72, 42));
                    for (float x = area.X + ((y / 14f) % 2f * 40f); x < area.Right; x += 80f)
                    {
                        c.Rect(x, y, 1f, 14f, new Color(110, 72, 42));
                    }
                }
            }

            // Tapete estampado.
            c.Rect(270f, 150f, 650f, 186f, new Color(150, 40, 46));
            c.Rect(278f, 158f, 634f, 170f, new Color(230, 190, 90));
            c.Rect(284f, 164f, 622f, 158f, new Color(150, 40, 46));
            for (float x = 310f; x < 900f; x += 46f)
            {
                c.RectC(x, 243f, 20f, 20f, new Color(230, 190, 90), MathF.PI / 4f);
                c.RectC(x, 243f, 10f, 10f, new Color(60, 90, 130), MathF.PI / 4f);
            }

            // Sofá em L, mesinha de centro, rack com TV e poltrona.
            var sofa = new Color(70, 100, 140);
            var sofaDark = new Color(50, 76, 110);
            c.Rect(300f, 136f, 300f, 40f, sofaDark);
            c.Rect(300f, 136f, 36f, 100f, sofaDark);
            for (int i = 0; i < 4; i++)
            {
                c.Rect(342f + (i * 64f), 146f, 60f, 28f, sofa);
            }

            c.Rect(304f, 180f, 30f, 50f, sofa);
            c.Circle(420f, 158f, 8f, new Color(240, 200, 80));
            c.RectC(528f, 158f, 16f, 16f, new Color(230, 110, 120), 0.3f);

            c.Rect(470f, 222f, 160f, 60f, Color.Black * 0.2f);
            c.Rect(466f, 218f, 160f, 60f, new Color(120, 76, 44));
            c.Rect(472f, 224f, 148f, 48f, new Color(150, 98, 58));
            c.Circle(510f, 248f, 12f, new Color(240, 240, 240));
            for (int i = 0; i < 9; i++)
            {
                c.Circle(505f + ((i % 3) * 5f), 243f + ((i / 3) * 5f), 2.5f, new Color(255, 240, 200));
            }

            c.Rect(560f, 232f, 36f, 26f, new Color(90, 150, 200));
            c.Rect(564f, 236f, 28f, 4f, Color.White * 0.7f);

            c.Rect(460f, 318f, 190f, 26f, new Color(90, 58, 34));
            c.Rect(480f, 322f, 150f, 8f, new Color(20, 20, 26));
            c.Rect(700f, 324f, 26f, 14f, new Color(30, 30, 36));
            c.Circle(716f, 331f, 2f, new Color(80, 200, 255));

            c.Rect(800f, 150f, 70f, 60f, sofaDark);
            c.Rect(808f, 158f, 54f, 44f, sofa);

            // Gato dormindo na caminha e plantas nos cantos.
            c.Circle(880f, 300f, 22f, new Color(110, 70, 90));
            c.Circle(880f, 300f, 17f, new Color(170, 120, 140));
            c.Circle(880f, 300f, 12f, new Color(235, 150, 70));
            c.Circle(889f, 294f, 6f, new Color(235, 150, 70));
            c.Rect(886f, 288f, 3f, 4f, new Color(235, 150, 70));
            PottedPlant(c, 264f, 330f, 16f);
            PottedPlant(c, 930f, 150f, 14f);
            c.Circle(700f, 166f, 14f, new Color(80, 80, 88));
            c.Circle(700f, 166f, 10f, new Color(250, 230, 170));

            // Estantes de livros em cima e a lareira embaixo.
            Color[] books = [new(200, 60, 60), new(60, 110, 200), new(230, 190, 60), new(80, 160, 90), new(160, 90, 180), new(240, 240, 230)];
            for (float x = -40f; x < W + 40f; x += 150f)
            {
                c.Rect(x + 4f, -38f, 130f, 30f, new Color(100, 64, 36));
                for (float bx = x + 8f; bx < x + 128f; bx += c.R(5f, 9f))
                {
                    c.Rect(bx, -34f, 4f, 22f, c.Pick(books));
                }
            }

            c.Rect(520f, H + 4f, 150f, 34f, new Color(150, 70, 50));
            for (int row = 0; row < 3; row++)
            {
                for (float bx = 520f + (row % 2 * 10f); bx < 666f; bx += 20f)
                {
                    c.Rect(bx, H + 6f + (row * 11f), 18f, 9f, new Color(170, 86, 62));
                }
            }

            c.Rect(560f, H + 8f, 70f, 22f, new Color(30, 20, 18));
            for (float y = 60f; y < H; y += 130f)
            {
                PottedPlant(c, -20f, y, 14f);
                PottedPlant(c, W + 20f, y + 60f, 14f);
            }
        },
        PaintAnimated = c =>
        {
            float t = c.Time * 0.5f;
            c.Rect(482f, 324f, 146f, 4f, Hue(t, 0.9f));
            float wag = MathF.Sin(c.Time * 3f) * 5f;
            c.Line(868f, 306f, 858f, 314f + wag, 3f, new Color(235, 150, 70));

            float spin = c.Time * 2.2f;
            for (int i = 0; i < 4; i++)
            {
                float a = spin + (i * MathF.PI / 2f);
                c.RectC(594f + (MathF.Cos(a) * 60f), 243f + (MathF.Sin(a) * 60f), 110f, 18f, Color.Black * 0.06f, a);
            }

            for (int i = 0; i < 5; i++)
            {
                float flick = MathF.Sin((c.Time * 12f) + (i * 2f)) * 3f;
                c.Ellipse(572f + (i * 11f), H + 20f + flick, 5f, 8f, i % 2 == 0 ? new Color(255, 150, 40) : new Color(255, 220, 90));
            }
        },
        PaintLights = c =>
        {
            float flick = 0.85f + (MathF.Sin(c.Time * 13f) * 0.1f);
            c.GlowEllipse(595f, H + 14f, 120f, 70f, new Color(255, 140, 40) * (0.35f * flick));
            c.Glow(700f, 166f, 70f, new Color(255, 220, 150) * 0.25f);
            c.GlowEllipse(555f, 324f, 90f, 30f, Hue(c.Time * 0.5f, 0.6f) * 0.25f);
        },
        PaintSecret = (c, p) =>
        {
            c.RectC(p.X, p.Y, 14f, 6f, new Color(30, 30, 36), 0.2f);
            c.Circle(p.X + 4f, p.Y + 1f, 1.5f, new Color(230, 50, 50));
            c.Rect(p.X - 3f, p.Y - 1f, 1.5f, 1.5f, new Color(180, 180, 190));
            c.Rect(p.X, p.Y, 1.5f, 1.5f, new Color(180, 180, 190));
        },
    };

    // ---------- SETUP GAMER ----------

    private const float KeyboardX = 300f;
    private const float KeyboardY = 190f;
    private const int KeyColumns = 15;
    private const int KeyRows = 5;
    private const float KeyPitch = 24f;

    public static SceneryStyle GamerDesk { get; } = new()
    {
        Background = new Color(22, 22, 28),
        GroundA = new Color(40, 42, 50),
        GroundB = new Color(38, 40, 48),
        Track = new Color(30, 32, 38),
        TrackSpeck = new Color(40, 42, 50),
        Edge = EdgeStyle.Glow,
        EdgeBase = new Color(26, 26, 32),
        EdgeA = new Color(0, 230, 255),
        EdgeB = new Color(255, 60, 200),
        Checkpoint = new Color(0, 230, 255),
        Darkness = 0.3f,
        DarknessTint = new Color(10, 0, 30),
        Headlights = true,
        PaintStatic = c =>
        {
            foreach (Rectangle area in MarginAreas().Append(InfieldArea))
            {
                GroundSpeckles(c, area, area.Width * area.Height / 250, new Color(50, 52, 60), new Color(30, 32, 38));
            }

            // Teclado mecânico: 5 fileiras de teclas, WASD destacado.
            c.Rect(KeyboardX - 10f, KeyboardY - 10f, (KeyColumns * KeyPitch) + 16f, (KeyRows * KeyPitch) + 16f, new Color(24, 24, 30));
            for (int row = 0; row < KeyRows; row++)
            {
                for (int col = 0; col < KeyColumns; col++)
                {
                    float kx = KeyboardX + (col * KeyPitch);
                    float ky = KeyboardY + (row * KeyPitch);
                    bool wasd = (row == 1 && col == 2) || (row == 2 && col is >= 1 and <= 3);
                    c.Rect(kx, ky, 20f, 20f, wasd ? new Color(60, 30, 70) : new Color(46, 48, 56));
                    c.Rect(kx + 2f, ky + 2f, 16f, 14f, wasd ? new Color(110, 50, 120) : new Color(62, 64, 74));
                }
            }

            c.Rect(KeyboardX + (4 * KeyPitch), KeyboardY + (4 * KeyPitch), 6 * KeyPitch, 20f, new Color(46, 48, 56));
            c.Rect(KeyboardX + (4 * KeyPitch) + 2f, KeyboardY + (4 * KeyPitch) + 2f, (6 * KeyPitch) - 4f, 14f, new Color(62, 64, 74));

            // Mouse, headset, latinhas, post-its, cabo e controle.
            c.Rect(760f, 176f, 150f, 130f, new Color(28, 30, 36));
            c.Ellipse(837f, 247f, 17f, 27f, Color.Black * 0.35f);
            c.Ellipse(835f, 244f, 17f, 27f, new Color(40, 42, 50));
            c.Ellipse(835f, 242f, 13f, 22f, new Color(56, 58, 68));
            c.Rect(834f, 218f, 2f, 22f, new Color(30, 30, 36));
            c.Rect(833f, 226f, 4f, 10f, new Color(80, 80, 90));
            c.Circle(284f, 330f, 16f, new Color(30, 30, 36));
            c.Circle(284f, 330f, 10f, new Color(60, 30, 70));
            c.Circle(344f, 330f, 16f, new Color(30, 30, 36));
            c.Circle(344f, 330f, 10f, new Color(60, 30, 70));
            c.Rect(284f, 312f, 60f, 5f, new Color(30, 30, 36));
            foreach ((float x, float y, Color color) in new[] { (940f, 150f, new Color(60, 200, 90)), (900f, 330f, new Color(230, 60, 80)) })
            {
                c.Circle(x + 2f, y + 3f, 11f, Color.Black * 0.3f);
                c.Circle(x, y, 11f, color);
                c.Circle(x, y, 7f, new Color(190, 196, 206));
                c.Circle(x + 2f, y - 2f, 2.5f, new Color(90, 96, 106));
            }

            c.RectC(713f, 163f, 26f, 26f, new Color(250, 225, 80), 0.1f);
            c.Rect(704f, 158f, 16f, 2f, new Color(120, 110, 60));
            c.Rect(704f, 164f, 12f, 2f, new Color(120, 110, 60));
            c.RectC(281f, 161f, 22f, 22f, new Color(120, 230, 150), -0.15f);
            for (int i = 0; i < 12; i++)
            {
                float t = i / 11f;
                c.Rect(835f + (MathF.Sin(t * 5f) * 30f), 218f - (t * 90f), 3f, 9f, new Color(20, 20, 24));
            }

            c.Ellipse(640f, 330f, 30f, 12f, new Color(40, 42, 50));
            c.Circle(622f, 330f, 12f, new Color(40, 42, 50));
            c.Circle(658f, 330f, 12f, new Color(40, 42, 50));
            c.Circle(622f, 330f, 4f, new Color(90, 94, 104));
            c.Circle(662f, 326f, 2f, new Color(230, 70, 70));
            c.Circle(656f, 333f, 2f, new Color(70, 200, 90));

            // Base do monitor e caixas de som em cima; caneca e celular embaixo.
            c.Rect(360f, -40f, 470f, 22f, new Color(18, 18, 22));
            c.Rect(560f, -18f, 70f, 14f, new Color(34, 34, 40));
            foreach (float x in new[] { 200f, 960f })
            {
                c.Rect(x, -38f, 40f, 32f, new Color(30, 30, 36));
                c.Circle(x + 20f, -22f, 10f, new Color(50, 52, 60));
                c.Circle(x + 20f, -22f, 5f, new Color(20, 20, 24));
            }

            c.Circle(300f, H + 20f, 13f, new Color(230, 230, 236));
            c.Circle(300f, H + 20f, 9f, new Color(70, 40, 20));
            c.Rect(760f, H + 6f, 26f, 44f, new Color(20, 20, 24));
            c.Rect(763f, H + 10f, 20f, 34f, new Color(40, 60, 100));
        },
        PaintAnimated = c =>
        {
            float t = c.Time * 0.35f;
            for (int row = 0; row < KeyRows; row++)
            {
                for (int col = 0; col < KeyColumns; col++)
                {
                    float kx = KeyboardX + (col * KeyPitch);
                    float ky = KeyboardY + (row * KeyPitch);
                    c.Rect(kx + 1f, ky + 17f, 18f, 2f, Hue(t + (col * 0.04f) + (row * 0.02f)));
                }
            }

            float pulse = (MathF.Sin(c.Time * 3f) + 1f) / 2f;
            c.Rect(833f, 250f, 4f, 10f, Hue(t) * (0.5f + (0.5f * pulse)));
            if ((c.Time % 4f) < 0.4f)
            {
                c.Rect(763f, H + 10f, 20f, 34f, new Color(120, 180, 255));
            }
        },
        PaintLights = c =>
        {
            float t = c.Time * 0.35f;
            c.GlowEllipse(594f, -20f, 320f, 70f, new Color(90, 150, 255) * 0.3f);
            c.GlowEllipse(KeyboardX + (KeyColumns * KeyPitch / 2f), KeyboardY + (KeyRows * KeyPitch / 2f), 230f, 90f, Hue(t, 0.9f) * 0.18f);
            c.Glow(835f, 244f, 40f, Hue(t + 0.3f, 0.9f) * 0.2f);
        },
        PaintSecret = (c, p) =>
        {
            c.Rect(p.X - 5f, p.Y - 5f, 10f, 10f, new Color(46, 48, 56));
            c.Rect(p.X - 4f, p.Y - 4f, 8f, 7f, new Color(110, 50, 120));
            c.Rect(p.X - 1f, p.Y - 2f, 2f, 2f, new Color(230, 230, 240));
        },
    };

    // ---------- CADERNO ----------

    private static void DoodleCircle(SceneCanvas c, float x, float y, float r, Color ink, int segments = 24)
    {
        for (int i = 0; i < segments; i++)
        {
            float a0 = i * MathF.Tau / segments;
            float a1 = (i + 1) * MathF.Tau / segments;
            c.Line(x + (MathF.Cos(a0) * r), y + (MathF.Sin(a0) * r), x + (MathF.Cos(a1) * r), y + (MathF.Sin(a1) * r), 1.5f, ink);
        }
    }

    public static SceneryStyle Notebook { get; } = new()
    {
        Background = new Color(238, 236, 226),
        GroundA = new Color(247, 245, 237),
        GroundB = new Color(247, 245, 237),
        Track = new Color(242, 240, 232),
        TrackSpeck = new Color(212, 212, 218),
        Line = new Color(150, 150, 165),
        Edge = EdgeStyle.Sketch,
        EdgeBase = new Color(247, 245, 237),
        EdgeA = new Color(70, 70, 82),
        EdgeB = new Color(130, 130, 145),
        Checkpoint = new Color(230, 70, 70),
        PaintStatic = c =>
        {
            foreach (Rectangle band in MarginAreas())
            {
                c.Rect(band.X, band.Y, band.Width, band.Height, new Color(247, 245, 237));
            }

            c.Rect(InL, InT, InR - InL, InB - InT, new Color(247, 245, 237));

            // Pauta azul e a margem vermelha.
            for (float y = -20f; y < H + 40f; y += 22f)
            {
                c.Rect(-40f, y, W + 80f, 1f, new Color(120, 170, 230) * 0.35f);
            }

            c.Rect(62f, -40f, 2f, H + 80f, new Color(230, 90, 90) * 0.45f);

            // Espiral do caderno na margem de cima.
            for (float x = -30f; x < W + 40f; x += 28f)
            {
                c.Circle(x, -22f, 6f, new Color(200, 196, 186));
                c.Circle(x, -22f, 3.5f, new Color(90, 88, 84));
                c.Rect(x - 1.5f, -40f, 3f, 16f, new Color(160, 164, 172));
            }

            var ink = new Color(60, 60, 75);
            var blue = new Color(50, 80, 170);

            // Rabiscos de aula: sol, casinha, bonequinhos, estrelas, corações, jogo da velha, foguete.
            DoodleCircle(c, 300f, 180f, 18f, ink);
            for (int i = 0; i < 8; i++)
            {
                float a = i * MathF.Tau / 8f;
                c.Line(300f + (MathF.Cos(a) * 24f), 180f + (MathF.Sin(a) * 24f), 300f + (MathF.Cos(a) * 34f), 180f + (MathF.Sin(a) * 34f), 1.5f, ink);
            }

            c.Line(400f, 320f, 460f, 320f, 1.5f, ink);
            c.Line(400f, 320f, 400f, 280f, 1.5f, ink);
            c.Line(460f, 320f, 460f, 280f, 1.5f, ink);
            c.Line(395f, 282f, 430f, 255f, 1.5f, ink);
            c.Line(465f, 282f, 430f, 255f, 1.5f, ink);
            c.Line(420f, 320f, 420f, 300f, 1.5f, ink);
            c.Line(440f, 320f, 440f, 300f, 1.5f, ink);

            foreach (float sx in new[] { 520f, 560f })
            {
                DoodleCircle(c, sx, 290f, 6f, ink, 12);
                c.Line(sx, 296f, sx, 316f, 1.5f, ink);
                c.Line(sx - 8f, 304f, sx + 8f, 304f, 1.5f, ink);
                c.Line(sx, 316f, sx - 6f, 328f, 1.5f, ink);
                c.Line(sx, 316f, sx + 6f, 328f, 1.5f, ink);
            }

            foreach ((float x, float y) in new[] { (380f, 160f), (640f, 170f), (900f, 300f) })
            {
                for (int i = 0; i < 5; i++)
                {
                    float a0 = (i * MathF.Tau / 5f) - (MathF.PI / 2f);
                    float a1 = ((i + 2) * MathF.Tau / 5f) - (MathF.PI / 2f);
                    c.Line(x + (MathF.Cos(a0) * 12f), y + (MathF.Sin(a0) * 12f), x + (MathF.Cos(a1) * 12f), y + (MathF.Sin(a1) * 12f), 1.5f, blue);
                }
            }

            foreach ((float x, float y) in new[] { (700f, 300f), (740f, 316f) })
            {
                DoodleCircle(c, x - 5f, y, 6f, new Color(220, 70, 90), 12);
                DoodleCircle(c, x + 5f, y, 6f, new Color(220, 70, 90), 12);
                c.Line(x - 11f, y + 2f, x, y + 14f, 1.5f, new Color(220, 70, 90));
                c.Line(x + 11f, y + 2f, x, y + 14f, 1.5f, new Color(220, 70, 90));
            }

            for (int i = 1; i < 3; i++)
            {
                c.Line(800f + (i * 22f), 150f, 800f + (i * 22f), 216f, 1.5f, ink);
                c.Line(800f, 150f + (i * 22f), 866f, 150f + (i * 22f), 1.5f, ink);
            }

            c.Line(806f, 156f, 820f, 170f, 1.5f, blue);
            c.Line(820f, 156f, 806f, 170f, 1.5f, blue);
            DoodleCircle(c, 855f, 185f, 7f, new Color(220, 70, 90), 12);
            c.Line(828f, 200f, 842f, 214f, 1.5f, blue);
            c.Line(842f, 200f, 828f, 214f, 1.5f, blue);

            c.Line(620f, 250f, 680f, 230f, 1.5f, ink);
            c.Line(620f, 250f, 680f, 270f, 1.5f, ink);
            c.Line(680f, 230f, 700f, 250f, 1.5f, ink);
            c.Line(680f, 270f, 700f, 250f, 1.5f, ink);
            c.Line(620f, 250f, 606f, 238f, 1.5f, ink);
            c.Line(620f, 250f, 606f, 262f, 1.5f, ink);
            DoodleCircle(c, 668f, 250f, 5f, blue, 10);

            // Lápis, borracha, mancha de café e farelos de borracha.
            c.RectC(540f, 200f, 150f, 12f, new Color(250, 200, 50), -0.2f);
            c.RectC(540f, 200f, 150f, 3f, new Color(230, 170, 40), -0.2f);
            c.RectC(612f, 185f, 18f, 12f, new Color(240, 210, 170), -0.2f);
            c.RectC(622f, 183f, 6f, 5f, new Color(50, 50, 55), -0.2f);
            c.RectC(466f, 214f, 16f, 13f, new Color(170, 175, 185), -0.2f);
            c.RectC(456f, 216f, 12f, 12f, new Color(240, 150, 170), -0.2f);
            c.RectC(300f, 250f, 40f, 22f, new Color(245, 160, 180), 0.3f);
            c.RectC(290f, 246f, 20f, 23f, new Color(60, 110, 200), 0.3f);
            for (int i = 0; i < 10; i++)
            {
                c.Rect(c.R(320f, 360f), c.R(262f, 290f), 3f, 2f, new Color(230, 170, 180));
            }

            for (int i = 0; i < 40; i++)
            {
                float a = i * MathF.Tau / 40f;
                c.Circle(880f + (MathF.Cos(a) * 34f), 230f + (MathF.Sin(a) * 34f), 3.5f, new Color(170, 120, 70) * 0.25f);
            }
        },
        PaintAnimated = c =>
        {
            // Uma espiral sendo rabiscada, de novo e de novo.
            float progress = (c.Time * 0.25f) % 1f;
            int segments = (int)(progress * 60f);
            float px = 760f;
            float py = 250f;
            for (int i = 0; i < segments; i++)
            {
                float a = i * 0.35f;
                float r = 2f + (i * 0.45f);
                float nx = 760f + (MathF.Cos(a) * r);
                float ny = 250f + (MathF.Sin(a) * r * 0.8f);
                c.Line(px, py, nx, ny, 1.5f, new Color(60, 60, 75));
                px = nx;
                py = ny;
            }

            c.RectC(px + 24f, py - 20f, 56f, 6f, new Color(250, 200, 50), -0.7f);
            c.RectC(px + 3f, py - 2f, 8f, 5f, new Color(240, 210, 170), -0.7f);
        },
        PaintSecret = (c, p) =>
        {
            var ink = new Color(60, 60, 75);
            DoodleCircle(c, p.X, p.Y, 5f, ink, 12);
            DoodleCircle(c, p.X + 5f, p.Y - 3f, 2.5f, ink, 8);
            c.Line(p.X + 7f, p.Y - 3f, p.X + 10f, p.Y - 2f, 1.5f, new Color(240, 150, 30));
            c.Line(p.X + 4f, p.Y - 6f, p.X + 6f, p.Y - 8f, 1.5f, new Color(230, 60, 60));
        },
    };

    // ---------- MESA DE SINUCA ----------

    private static readonly Color[] PoolBallColors =
    [
        new(250, 200, 40), new(40, 80, 200), new(220, 50, 40), new(110, 50, 160), new(250, 130, 40), new(40, 140, 70), new(130, 40, 40),
    ];

    private static void PoolBall(SceneCanvas c, float x, float y, Color color, bool striped)
    {
        c.Circle(x + 1.5f, y + 2f, 7f, Color.Black * 0.35f);
        c.Circle(x, y, 7f, striped ? Color.White : color);
        if (striped)
        {
            c.Rect(x - 7f, y - 3f, 14f, 6f, color);
        }

        c.Circle(x, y, 2.5f, Color.White);
        c.Circle(x - 2.5f, y - 2.5f, 1.5f, Color.White * 0.7f);
    }

    public static SceneryStyle PoolTable { get; } = new()
    {
        Background = new Color(36, 24, 16),
        GroundA = new Color(28, 112, 58),
        GroundB = new Color(26, 106, 55),
        Track = new Color(86, 58, 38),
        TrackSpeck = new Color(72, 48, 30),
        Edge = EdgeStyle.Cushion,
        EdgeBase = new Color(92, 52, 28),
        EdgeA = new Color(24, 104, 54),
        EdgeB = new Color(40, 140, 72),
        Checkpoint = new Color(255, 220, 120),
        Darkness = 0.34f,
        DarknessTint = new Color(20, 10, 0),
        Headlights = true,
        PaintStatic = c =>
        {
            // Assoalho do bar na margem, banquetas e a jukebox.
            foreach (Rectangle band in MarginAreas())
            {
                c.Rect(band.X, band.Y, band.Width, band.Height, new Color(56, 38, 24));
                bool horizontal = band.Width > band.Height;
                for (float d = 0f; d < (horizontal ? band.Height : band.Width); d += 10f)
                {
                    c.Rect(horizontal ? band.X : band.X + d, horizontal ? band.Y + d : band.Y, horizontal ? band.Width : 1f, horizontal ? 1f : band.Height, new Color(44, 30, 18));
                }
            }

            c.AlongMargin(92f, (x, y) =>
            {
                c.Circle(x + 2f, y + 3f, 10f, Color.Black * 0.3f);
                c.Circle(x, y, 10f, new Color(150, 40, 40));
                c.Circle(x, y, 6f, new Color(180, 60, 60));
            });

            // A mesa: feltro com textura, caçapas, marcadores e as bolas.
            GroundSpeckles(c, InfieldArea, 700, new Color(34, 124, 64), new Color(22, 96, 50));
            foreach ((float x, float y) in new[] { (InL, InT), (594f, InT - 4f), (InR, InT), (InL, InB), (594f, InB + 4f), (InR, InB) })
            {
                c.Circle(x, y, 16f, new Color(60, 36, 20));
                c.Circle(x, y, 12f, new Color(8, 8, 8));
            }

            for (float x = InL + 88f; x < InR - 40f; x += 88f)
            {
                c.Circle(x, InT - 11f, 2f, new Color(235, 225, 190));
                c.Circle(x, InB + 11f, 2f, new Color(235, 225, 190));
            }

            c.Circle(420f, 242f, 3f, Color.White * 0.35f);
            c.Rect(420f, InT, 1f, InB - InT, Color.White * 0.12f);

            // Triângulo com as bolas arrumadas à direita, bola branca e taco à esquerda.
            int ball = 0;
            for (int col = 0; col < 5; col++)
            {
                for (int row = 0; row <= col; row++)
                {
                    float bx = 760f + (col * 13f);
                    float by = 242f - (col * 7.5f) + (row * 15f);
                    if (ball == 4)
                    {
                        c.Circle(bx + 1.5f, by + 2f, 7f, Color.Black * 0.35f);
                        c.Circle(bx, by, 7f, new Color(15, 15, 18));
                        c.Circle(bx, by, 3f, Color.White);
                    }
                    else
                    {
                        PoolBall(c, bx, by, PoolBallColors[ball % PoolBallColors.Length], ball >= 7);
                    }

                    ball++;
                }
            }

            c.RectC(420f, 290f, 260f, 5f, new Color(200, 160, 100), -0.12f);
            c.RectC(300f, 305f, 60f, 6f, new Color(60, 40, 30), -0.12f);
            c.RectC(551f, 274f, 6f, 5f, new Color(90, 150, 210), -0.12f);
            c.Rect(880f, 320f, 12f, 12f, new Color(80, 140, 220));

            c.Rect(80f, H + 4f, 60f, 32f, new Color(120, 40, 60));
            c.Rect(86f, H + 8f, 48f, 12f, new Color(250, 200, 80));
        },
        PaintAnimated = c =>
        {
            // Bola branca e a bola 3 rolando e quicando nas tabelas.
            (float X, float Y) Bounce(float t, float speedX, float speedY, float phase)
            {
                float w = InR - InL - 30f;
                float h = InB - InT - 30f;
                float x = MathF.Abs(((t * speedX) + phase) % (2f * w) - w);
                float y = MathF.Abs(((t * speedY) + phase) % (2f * h) - h);
                return (InL + 15f + x, InT + 15f + y);
            }

            (float wx, float wy) = Bounce(c.Time, 70f, 44f, 100f);
            c.Circle(wx + 1.5f, wy + 2f, 7f, Color.Black * 0.35f);
            c.Circle(wx, wy, 7f, new Color(250, 250, 245));
            c.Circle(wx - 2.5f, wy - 2.5f, 1.5f, Color.White);
            (float rx, float ry) = Bounce(c.Time, 52f, 61f, 300f);
            PoolBall(c, rx, ry, PoolBallColors[2], striped: false);

            float blink = (MathF.Sin(c.Time * 4f) + 1f) / 2f;
            c.Rect(86f, H + 24f, 48f, 6f, Hue(c.Time * 0.4f, 0.6f + (0.4f * blink)));
        },
        PaintLights = c =>
        {
            float sway = MathF.Sin(c.Time * 0.8f) * 6f;
            c.GlowEllipse(430f + sway, 242f, 200f, 130f, new Color(255, 220, 150) * 0.28f);
            c.GlowEllipse(760f + sway, 242f, 200f, 130f, new Color(255, 220, 150) * 0.28f);
            c.Glow(110f, H + 20f, 50f, Hue(c.Time * 0.4f, 0.8f) * 0.25f);
        },
        PaintSecret = (c, p) =>
        {
            c.Circle(p.X + 1.5f, p.Y + 2f, 6f, Color.Black * 0.35f);
            c.Circle(p.X, p.Y, 6f, new Color(15, 15, 18));
            c.Circle(p.X, p.Y, 2.5f, Color.White);
            c.Circle(p.X - 2f, p.Y - 2f, 1.2f, Color.White * 0.5f);
        },
    };

    // ---------- BOLO DE ANIVERSÁRIO ----------

    private static readonly Vector2[] CakeCandles = BuildCakeCandles();

    private static Vector2[] BuildCakeCandles()
    {
        var list = new List<Vector2>();
        for (int i = 0; i < 9; i++)
        {
            float a = i * MathF.Tau / 9f;
            list.Add(new Vector2(594f + (MathF.Cos(a) * 250f), 242f + (MathF.Sin(a) * 78f)));
        }

        return [.. list];
    }

    public static SceneryStyle Cake { get; } = new()
    {
        Background = new Color(240, 200, 214),
        GroundA = new Color(255, 230, 238),
        GroundB = new Color(252, 224, 234),
        Track = new Color(120, 70, 50),
        TrackSpeck = new Color(146, 92, 66),
        Edge = EdgeStyle.Snowbank,
        EdgeBase = new Color(252, 220, 232),
        EdgeA = new Color(255, 245, 250),
        EdgeB = new Color(240, 170, 200),
        Checkpoint = new Color(255, 255, 255),
        Darkness = 0.2f,
        DarknessTint = new Color(40, 10, 30),
        Ambient = AmbientKind.Confetti,
        AmbientCount = 50,
        PaintStatic = c =>
        {
            // Granulado colorido espalhado pela cobertura de chocolate (só no asfalto).
            Color[] sprinkles = [new(255, 90, 90), new(255, 220, 70), new(90, 200, 255), new(130, 230, 130), new(240, 140, 240), Color.White];
            for (int x = 0; x < c.Track.Width; x++)
            {
                for (int y = 0; y < c.Track.Height; y++)
                {
                    char cell = c.Track.CellAt(x, y);
                    if (cell == '#' || cell == 'S')
                    {
                        continue;
                    }

                    for (int i = 0; i < 2; i++)
                    {
                        c.RectC((x * 22f) + c.R(3f, 19f), (y * 22f) + c.R(3f, 19f), 4f, 1.5f, c.Pick(sprinkles), c.R(0f, MathF.PI));
                    }
                }
            }

            // Toalha com pratinhos, chapéus de festa e presentes em volta.
            foreach (Rectangle band in MarginAreas())
            {
                c.Rect(band.X, band.Y, band.Width, band.Height, new Color(250, 250, 252));
                for (float d = 0f; d < band.Width; d += 20f)
                {
                    c.Rect(band.X + d, band.Y, 10f, band.Height, new Color(250, 200, 215) * 0.35f);
                }
            }

            c.AlongMargin(110f, (x, y) =>
            {
                switch ((int)MathF.Abs(x + y) % 3)
                {
                    case 0:
                        c.Circle(x, y, 15f, new Color(220, 220, 228));
                        c.Circle(x, y, 11f, Color.White);
                        break;
                    case 1:
                        c.Circle(x, y, 12f, new Color(90, 180, 240));
                        for (int k = 0; k < 4; k++)
                        {
                            c.RectC(x, y, 24f, 3f, new Color(255, 230, 90), k * MathF.PI / 4f);
                        }

                        c.Circle(x, y, 3f, Color.White);
                        break;
                    default:
                        c.Rect(x - 12f, y - 12f, 24f, 24f, new Color(160, 110, 220));
                        c.Rect(x - 2f, y - 12f, 4f, 24f, new Color(255, 220, 90));
                        c.Rect(x - 12f, y - 2f, 24f, 4f, new Color(255, 220, 90));
                        break;
                }
            });

            // Topo do bolo: redemoinhos de chantili, morangos e o "caminho" das velinhas.
            c.Ellipse(594f, 242f, 300f, 100f, new Color(255, 240, 246));
            c.Ellipse(594f, 242f, 280f, 88f, new Color(255, 225, 236));
            for (int i = 0; i < 30; i++)
            {
                float a = i * MathF.Tau / 30f;
                c.Circle(594f + (MathF.Cos(a) * 290f), 242f + (MathF.Sin(a) * 94f), 10f, new Color(255, 250, 252));
                c.Circle(594f + (MathF.Cos(a) * 290f) - 2f, 242f + (MathF.Sin(a) * 94f) - 2f, 4f, Color.White);
            }

            foreach ((float x, float y) in new[] { (470f, 210f), (520f, 280f), (690f, 200f), (720f, 290f), (594f, 242f), (400f, 250f), (790f, 240f) })
            {
                c.Circle(x + 2f, y + 3f, 11f, Color.Black * 0.15f);
                c.Circle(x - 4f, y, 8f, new Color(220, 40, 50));
                c.Circle(x + 4f, y, 8f, new Color(220, 40, 50));
                c.RectC(x, y + 5f, 11f, 11f, new Color(220, 40, 50), MathF.PI / 4f);
                for (int s = 0; s < 5; s++)
                {
                    c.Rect(x - 5f + (s * 2.5f), y - 2f + ((s % 2) * 4f), 1f, 1.5f, new Color(250, 220, 120));
                }

                c.Rect(x - 4f, y - 11f, 8f, 3f, new Color(70, 160, 70));
            }

            for (int i = 0; i < 40; i++)
            {
                c.RectC(c.R(380f, 810f), c.R(170f, 314f), 6f, 2f, new Color(110, 60, 40), c.R(0f, 3f));
            }

            foreach (Vector2 candle in CakeCandles)
            {
                c.Circle(candle.X + 2f, candle.Y + 3f, 6f, Color.Black * 0.15f);
                c.Circle(candle.X, candle.Y, 6f, new Color(120, 200, 250));
                c.Circle(candle.X, candle.Y, 4f, Color.White);
                c.Rect(candle.X - 0.5f, candle.Y - 3f, 1f, 3f, new Color(40, 40, 40));
            }
        },
        PaintAnimated = c =>
        {
            for (int i = 0; i < CakeCandles.Length; i++)
            {
                Vector2 candle = CakeCandles[i];
                float flick = MathF.Sin((c.Time * 14f) + (i * 1.9f)) * 0.8f;
                c.Ellipse(candle.X + flick, candle.Y - 6f, 2.5f, 4f, new Color(255, 170, 40));
                c.Ellipse(candle.X + flick, candle.Y - 5f, 1.2f, 2.5f, new Color(255, 240, 150));
            }
        },
        PaintLights = c =>
        {
            for (int i = 0; i < CakeCandles.Length; i++)
            {
                Vector2 candle = CakeCandles[i];
                float flick = 0.85f + (MathF.Sin((c.Time * 11f) + (i * 2.3f)) * 0.15f);
                c.Glow(candle.X, candle.Y - 6f, 26f, new Color(255, 180, 80) * (0.35f * flick));
            }
        },
        PaintSecret = (c, p) =>
        {
            c.Circle(p.X + 1f, p.Y + 2f, 4.5f, Color.Black * 0.2f);
            c.Circle(p.X, p.Y, 4.5f, new Color(200, 20, 40));
            c.Circle(p.X - 1.5f, p.Y - 1.5f, 1.3f, Color.White * 0.6f);
            c.Line(p.X, p.Y - 4f, p.X + 4f, p.Y - 9f, 1f, new Color(80, 130, 50));
        },
    };
}
