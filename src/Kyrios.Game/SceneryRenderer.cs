using Kyrios.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Kyrios.Game;

/// <summary>Como desenhar a beirada da pista (as células de parede coladas no asfalto).</summary>
public enum EdgeStyle
{
    /// <summary>Zebra de autódromo: listras alternadas na face virada pro asfalto.</summary>
    Stripes,

    /// <summary>Faixa luminosa (neon).</summary>
    Glow,

    /// <summary>Blocos alternados (tijolinhos de brinquedo, faixa de segurança, toalha xadrez).</summary>
    Blocks,

    /// <summary>Pedras irregulares.</summary>
    Rocks,

    /// <summary>Monte de neve.</summary>
    Snowbank,

    /// <summary>Tábuas/troncos de madeira.</summary>
    Planks,

    /// <summary>Guard-rail de estrada: faixa de metal com postes.</summary>
    Rail,

    /// <summary>Tabela de mesa de sinuca: madeira com a borracha de feltro virada pra pista.</summary>
    Cushion,

    /// <summary>Contorno feito a lápis (duas linhas tremidas) — pista desenhada no papel.</summary>
    Sketch,

    /// <summary>Cerca de ferro com pontas.</summary>
    Fence,
}

/// <summary>Partículas de clima: poucas, simples e recicladas (nada de milhares de objetos).</summary>
public enum AmbientKind
{
    None,
    Rain,
    Snow,
    Embers,
    Sand,
    Leaves,

    /// <summary>Bolhas subindo (fundo do mar).</summary>
    Bubbles,

    /// <summary>Confete colorido caindo girando.</summary>
    Confetti,

    /// <summary>Moscas zanzando.</summary>
    Flies,

    /// <summary>Pontinhos de luz flutuando (vaga-lumes, esporos) — brilham por cima da escuridão.</summary>
    Fireflies,
}

/// <summary>
/// A "cara" de uma pista. As cores cobrem o que é comum a todas (chão, asfalto, bordas, clima, escuridão); os
/// três pincéis desenham o que é único de cada cenário: <see cref="PaintStatic"/> (objetos parados — desenhado
/// UMA vez e guardado numa textura), <see cref="PaintAnimated"/> (o que se mexe, todo quadro) e
/// <see cref="PaintLights"/> (brilhos somados por cima de tudo, com mistura aditiva).
/// </summary>
public sealed class SceneryStyle
{
    public Color Background { get; init; }
    public Color GroundA { get; init; }
    public Color GroundB { get; init; }
    public Color Track { get; init; }
    public Color TrackSpeck { get; init; }

    /// <summary>Faixa tracejada no meio do asfalto (transparente = sem faixa).</summary>
    public Color Line { get; init; } = Color.Transparent;

    public EdgeStyle Edge { get; init; }
    public Color EdgeBase { get; init; }
    public Color EdgeA { get; init; }
    public Color EdgeB { get; init; }

    /// <summary>Cor do brilho dos checkpoints nesta pista.</summary>
    public Color Checkpoint { get; init; } = new(255, 215, 0);

    /// <summary>0 = dia claro; perto de 1 = noite fechada (só as luzes aparecem).</summary>
    public float Darkness { get; init; }

    public Color DarknessTint { get; init; } = Color.Black;

    public AmbientKind Ambient { get; init; }
    public int AmbientCount { get; init; } = 80;
    public Color AmbientColor { get; init; } = Color.White;

    /// <summary>Faróis acesos nos carros (pistas escuras).</summary>
    public bool Headlights { get; init; }

    /// <summary>Filtro de cor aplicado aos carros nesta pista (ex.: a paleta de 4 verdes do mundo retrô); null =
    /// cores normais.</summary>
    public Func<Color, Color> CarFilter { get; init; }

    public Action<SceneCanvas> PaintStatic { get; init; }
    public Action<SceneCanvas> PaintAnimated { get; init; }
    public Action<SceneCanvas> PaintLights { get; init; }

    /// <summary>O detalhe escondido, desenhado centrado no ponto recebido (em pixels).</summary>
    public Action<SceneCanvas, Vector2> PaintSecret { get; init; }
}

/// <summary>
/// Pincel dos cenários: primitivas em pixels "de mundo" (0,0 = canto superior-esquerdo da grade da pista; a
/// margem decorativa fica em coordenadas negativas / além da largura). Tudo o que um cenário precisa desenhar
/// passa por aqui, então os pincéis de cada pista não conhecem SpriteBatch nem texturas.
/// </summary>
public sealed class SceneCanvas(SpriteBatch spriteBatch, Texture2D pixel, Texture2D circle, Texture2D glow, Track track)
{
    public const int Cell = 22;
    public const int Margin = 40;

    public Track Track => track;

    /// <summary>Relógio visual (segundos) pra animações.</summary>
    public float Time { get; set; }

    /// <summary>Aleatório com semente fixa por pista — o cenário sai sempre igual.</summary>
    public Random Rng { get; set; } = new(1);

    public float Width => track.Width * Cell;
    public float Height => track.Height * Cell;

    /// <summary>O miolo gramado entre as retas (sem as bordas), em pixels.</summary>
    public Rectangle Infield => new(11 * Cell, 6 * Cell, 32 * Cell, 10 * Cell);

    public float R(float min, float max) => min + ((float)Rng.NextDouble() * (max - min));

    public bool Chance(double probability) => Rng.NextDouble() < probability;

    public Color Pick(params Color[] colors) => colors[Rng.Next(colors.Length)];

    public bool IsTrackCell(int x, int y) => track.CellAt(x, y) != '#';

    public void Rect(float x, float y, float w, float h, Color color)
    {
        if (w > 0f && h > 0f)
        {
            spriteBatch.Draw(pixel, new Vector2(x, y), null, color, 0f, Vector2.Zero, new Vector2(w, h), SpriteEffects.None, 0f);
        }
    }

    /// <summary>Retângulo centrado, opcionalmente girado.</summary>
    public void RectC(float cx, float cy, float w, float h, Color color, float rotation = 0f)
    {
        if (w > 0f && h > 0f)
        {
            spriteBatch.Draw(pixel, new Vector2(cx, cy), null, color, rotation, new Vector2(0.5f, 0.5f), new Vector2(w, h), SpriteEffects.None, 0f);
        }
    }

    public void Circle(float cx, float cy, float r, Color color) => Ellipse(cx, cy, r, r, color);

    public void Ellipse(float cx, float cy, float rx, float ry, Color color, float rotation = 0f)
    {
        if (rx > 0f && ry > 0f)
        {
            var origin = new Vector2(circle.Width / 2f, circle.Height / 2f);
            spriteBatch.Draw(circle, new Vector2(cx, cy), null, color, rotation, origin, new Vector2(rx * 2f / circle.Width, ry * 2f / circle.Height), SpriteEffects.None, 0f);
        }
    }

    /// <summary>Mancha de luz/sombra com bordas suaves (gradiente radial).</summary>
    public void Glow(float cx, float cy, float r, Color color) => GlowEllipse(cx, cy, r, r, color);

    public void GlowEllipse(float cx, float cy, float rx, float ry, Color color, float rotation = 0f)
    {
        if (rx > 0f && ry > 0f)
        {
            var origin = new Vector2(glow.Width / 2f, glow.Height / 2f);
            spriteBatch.Draw(glow, new Vector2(cx, cy), null, color, rotation, origin, new Vector2(rx * 2f / glow.Width, ry * 2f / glow.Height), SpriteEffects.None, 0f);
        }
    }

    public void Line(float x1, float y1, float x2, float y2, float thickness, Color color)
    {
        float dx = x2 - x1;
        float dy = y2 - y1;
        float length = MathF.Sqrt((dx * dx) + (dy * dy));
        RectC((x1 + x2) / 2f, (y1 + y2) / 2f, length, thickness, color, MathF.Atan2(dy, dx));
    }

    public void Text(string text, float x, float y, float size, Color color) =>
        PixelFont.Draw(spriteBatch, pixel, text, new Vector2(x, y), size, color);

    public void TextC(string text, float cx, float cy, float size, Color color) =>
        Text(text, cx - (PixelFont.Measure(text, size) / 2f), cy - (PixelFont.LineHeight(size) / 2f), size, color);

    /// <summary>Chama <paramref name="action"/> em pontos espaçados ao longo da margem decorativa em volta da
    /// pista (x, y do centro da faixa da margem) — útil pra enfileirar árvores, prédios, pneus...</summary>
    public void AlongMargin(float spacing, Action<float, float> action)
    {
        float half = Margin / 2f;
        for (float x = -half; x <= Width + half; x += spacing)
        {
            action(x, -half);
            action(x, Height + half);
        }

        for (float y = half; y < Height; y += spacing)
        {
            action(-half, y);
            action(Width + half, y);
        }
    }
}

/// <summary>
/// Desenha as pistas. A parte estática de cada cenário (chão, asfalto, bordas, objetos) é pintada uma única vez
/// numa textura (cache das últimas pistas usadas) e só "colada" a cada quadro — é o que permite cenários
/// detalhados sem pesar em hardware simples. Por cima: animações leves, partículas de clima recicladas,
/// escuridão e luzes aditivas.
/// </summary>
public sealed class SceneryRenderer
{
    private const int CacheSize = 4;

    private static readonly Color[] ConfettiColors =
    [
        new(255, 90, 90), new(255, 210, 60), new(90, 200, 255), new(120, 230, 120), new(230, 120, 240), Color.White,
    ];
    private const int GlowTextureSize = 64;

    private readonly GraphicsDevice _device;
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly Texture2D _circle;
    private readonly Texture2D _glow;
    private readonly Dictionary<string, RenderTarget2D> _cache = new();
    private readonly LinkedList<string> _recent = new();
    private readonly Random _ambientRandom = new();
    private float _ambientTime;
    private Vector2[] _ambient = [];
    private float[] _ambientSeed = [];
    private string _ambientThemeId;

    public SceneryRenderer(GraphicsDevice device, SpriteBatch spriteBatch, Texture2D pixel, Texture2D circle)
    {
        _device = device;
        _spriteBatch = spriteBatch;
        _pixel = pixel;
        _circle = circle;
        _glow = CreateGlowTexture(device);
    }

    public Texture2D GlowTexture => _glow;

    private static Texture2D CreateGlowTexture(GraphicsDevice device)
    {
        var texture = new Texture2D(device, GlowTextureSize, GlowTextureSize);
        var data = new Color[GlowTextureSize * GlowTextureSize];
        float center = (GlowTextureSize - 1) / 2f;
        for (int y = 0; y < GlowTextureSize; y++)
        {
            for (int x = 0; x < GlowTextureSize; x++)
            {
                float distance = MathF.Sqrt(((x - center) * (x - center)) + ((y - center) * (y - center))) / center;
                float alpha = MathF.Pow(Math.Clamp(1f - distance, 0f, 1f), 1.6f);
                data[(y * GlowTextureSize) + x] = Color.White * alpha;
            }
        }

        texture.SetData(data);
        return texture;
    }

    private SceneCanvas CreateCanvas(Track track, float time, int seed) =>
        new(_spriteBatch, _pixel, _circle, _glow, track) { Time = time, Rng = new Random(seed) };

    private static int SeedFor(TrackTheme theme) => theme.Id.Aggregate(17, (hash, c) => (hash * 31) + c);

    /// <summary>Garante a textura estática da pista pronta. Chamar FORA de um SpriteBatch.Begin/End.</summary>
    public void Prepare(TrackTheme theme, Track track)
    {
        if (_cache.TryGetValue(theme.Id, out RenderTarget2D cached) && !cached.IsDisposed)
        {
            _recent.Remove(theme.Id);
            _recent.AddFirst(theme.Id);
            return;
        }

        int width = (track.Width * SceneCanvas.Cell) + (2 * SceneCanvas.Margin);
        int height = (track.Height * SceneCanvas.Cell) + (2 * SceneCanvas.Margin);
        RenderTarget2D target = cached is { IsDisposed: false } ? cached
            : new RenderTarget2D(_device, width, height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

        _device.SetRenderTarget(target);
        _device.Clear(theme.Scenery.Background);
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: Matrix.CreateTranslation(SceneCanvas.Margin, SceneCanvas.Margin, 0f));
        PaintStaticLayer(theme, CreateCanvas(track, 0f, SeedFor(theme)));
        _spriteBatch.End();
        _device.SetRenderTarget(null);

        _cache[theme.Id] = target;
        _recent.Remove(theme.Id);
        _recent.AddFirst(theme.Id);
        while (_recent.Count > CacheSize)
        {
            string oldest = _recent.Last!.Value;
            _recent.RemoveLast();
            _cache[oldest].Dispose();
            _cache.Remove(oldest);
        }
    }

    private static void PaintStaticLayer(TrackTheme theme, SceneCanvas c)
    {
        SceneryStyle style = theme.Scenery;
        Track track = c.Track;
        const int cell = SceneCanvas.Cell;

        for (int x = 0; x < track.Width; x++)
        {
            for (int y = 0; y < track.Height; y++)
            {
                char kind = track.CellAt(x, y);
                float px = x * cell;
                float py = y * cell;

                if (kind == '#')
                {
                    bool up = c.IsTrackCell(x, y - 1);
                    bool down = c.IsTrackCell(x, y + 1);
                    bool left = c.IsTrackCell(x - 1, y);
                    bool right = c.IsTrackCell(x + 1, y);
                    if (up || down || left || right)
                    {
                        PaintEdgeCell(c, style, x, y, up, down, left, right);
                    }
                    else
                    {
                        c.Rect(px, py, cell, cell, (x + y) % 2 == 0 ? style.GroundA : style.GroundB);
                    }

                    continue;
                }

                c.Rect(px, py, cell, cell, style.Track);
                for (int i = 0; i < 3; i++)
                {
                    c.Rect(px + c.R(1f, cell - 3f), py + c.R(1f, cell - 3f), 2f, 2f, style.TrackSpeck);
                }

                if (kind == 'S')
                {
                    const int half = cell / 2;
                    c.Rect(px, py, half, half, Color.White);
                    c.Rect(px + half, py + half, half, half, Color.White);
                    c.Rect(px + half, py, half, half, new Color(20, 20, 24));
                    c.Rect(px, py + half, half, half, new Color(20, 20, 24));
                }
            }
        }

        if (style.Line.A > 0)
        {
            PaintLaneDashes(c, style.Line);
        }

        style.PaintStatic?.Invoke(c);
        style.PaintSecret?.Invoke(c, new Vector2(theme.SecretSpot.X * cell, theme.SecretSpot.Y * cell));
    }

    /// <summary>Faixa tracejada no meio de cada reta, seguindo a linha central do circuito.</summary>
    private static void PaintLaneDashes(SceneCanvas c, Color color)
    {
        IReadOnlyList<Vector2D> line = c.Track.CenterLine;
        const float cell = SceneCanvas.Cell;
        for (int i = 0; i < line.Count; i++)
        {
            Vector2D a = line[i];
            Vector2D b = line[(i + 1) % line.Count];
            float length = a.DistanceTo(b) * cell;
            var from = new Vector2(a.X * cell, a.Y * cell);
            var dir = Vector2.Normalize(new Vector2((b.X - a.X) * cell, (b.Y - a.Y) * cell));
            for (float d = 30f; d < length - 30f; d += 26f)
            {
                Vector2 p = from + (dir * d);
                c.RectC(p.X, p.Y, MathF.Abs(dir.X) > 0.5f ? 12f : 3f, MathF.Abs(dir.X) > 0.5f ? 3f : 12f, color);
            }
        }
    }

    private static void PaintEdgeCell(SceneCanvas c, SceneryStyle style, int x, int y, bool up, bool down, bool left, bool right)
    {
        const int cell = SceneCanvas.Cell;
        float px = x * cell;
        float py = y * cell;
        bool alternate = (x + y) % 2 == 0;

        switch (style.Edge)
        {
            case EdgeStyle.Stripes:
            {
                c.Rect(px, py, cell, cell, style.EdgeBase);
                Color stripe = alternate ? style.EdgeA : style.EdgeB;
                const int t = 6;
                ForEachSide(px, py, up, down, left, right, t, (rx, ry, rw, rh) => c.Rect(rx, ry, rw, rh, stripe));
                break;
            }

            case EdgeStyle.Glow:
            {
                c.Rect(px, py, cell, cell, style.EdgeBase);
                Color line = alternate ? style.EdgeA : style.EdgeB;
                ForEachSide(px, py, up, down, left, right, 8, (rx, ry, rw, rh) => c.Rect(rx, ry, rw, rh, line * 0.25f));
                ForEachSide(px, py, up, down, left, right, 3, (rx, ry, rw, rh) => c.Rect(rx, ry, rw, rh, line));
                break;
            }

            case EdgeStyle.Blocks:
            {
                c.Rect(px, py, cell, cell, style.EdgeBase);
                Color block = alternate ? style.EdgeA : style.EdgeB;
                c.Rect(px + 1, py + 1, cell - 2, cell - 2, block);
                c.Rect(px + 1, py + 1, cell - 2, 3, Color.White * 0.25f);
                c.Circle(px + (cell * 0.32f), py + (cell * 0.5f), 3f, CarPainter.Darken(block, 0.8f));
                c.Circle(px + (cell * 0.68f), py + (cell * 0.5f), 3f, CarPainter.Darken(block, 0.8f));
                break;
            }

            case EdgeStyle.Rocks:
            {
                c.Rect(px, py, cell, cell, style.EdgeBase);
                for (int i = 0; i < 3; i++)
                {
                    float r = c.R(5f, 8f);
                    Color rock = c.Chance(0.5) ? style.EdgeA : style.EdgeB;
                    float rx = px + c.R(r, cell - r);
                    float ry = py + c.R(r, cell - r);
                    c.Ellipse(rx, ry, r, r * 0.8f, rock);
                    c.Ellipse(rx - 1.5f, ry - 1.5f, r * 0.45f, r * 0.35f, Color.White * 0.15f);
                }

                break;
            }

            case EdgeStyle.Snowbank:
            {
                c.Rect(px, py, cell, cell, style.EdgeBase);
                c.Circle(px + (cell * 0.3f), py + (cell * 0.45f), 8f, style.EdgeA);
                c.Circle(px + (cell * 0.72f), py + (cell * 0.55f), 9f, style.EdgeA);
                ForEachSide(px, py, up, down, left, right, 4, (rx, ry, rw, rh) => c.Rect(rx, ry, rw, rh, style.EdgeB));
                break;
            }

            case EdgeStyle.Planks:
            {
                c.Rect(px, py, cell, cell, style.EdgeBase);
                bool horizontal = up || down;
                for (int i = 0; i < 3; i++)
                {
                    float offset = 3f + (i * 7f);
                    if (horizontal)
                    {
                        c.Rect(px, py + offset, cell, 1f, style.EdgeA);
                    }
                    else
                    {
                        c.Rect(px + offset, py, 1f, cell, style.EdgeA);
                    }
                }

                c.Rect(px + (horizontal ? (alternate ? 0 : cell - 1) : 0), py + (horizontal ? 0 : (alternate ? 0 : cell - 1)), horizontal ? 1 : cell, horizontal ? cell : 1, style.EdgeA);
                ForEachSide(px, py, up, down, left, right, 3, (rx, ry, rw, rh) => c.Rect(rx, ry, rw, rh, style.EdgeB));
                break;
            }

            case EdgeStyle.Rail:
            {
                c.Rect(px, py, cell, cell, style.EdgeBase);
                ForEachSide(px, py, up, down, left, right, 7, (rx, ry, rw, rh) => c.Rect(rx, ry, rw, rh, style.EdgeA));
                ForEachSide(px, py, up, down, left, right, 3, (rx, ry, rw, rh) => c.Rect(rx, ry, rw, rh, style.EdgeB));
                if (alternate)
                {
                    c.Rect(px + (cell / 2f) - 2f, py + (cell / 2f) - 2f, 4f, 4f, CarPainter.Darken(style.EdgeA, 0.6f));
                }

                break;
            }

            case EdgeStyle.Cushion:
            {
                c.Rect(px, py, cell, cell, style.EdgeBase);
                c.Rect(px, py + (alternate ? 4f : 14f), cell, 1f, CarPainter.Darken(style.EdgeBase, 0.8f));
                ForEachSide(px, py, up, down, left, right, 6, (rx, ry, rw, rh) => c.Rect(rx, ry, rw, rh, style.EdgeA));
                ForEachSide(px, py, up, down, left, right, 2, (rx, ry, rw, rh) => c.Rect(rx, ry, rw, rh, style.EdgeB));
                if ((x + y) % 4 == 0)
                {
                    c.Circle(px + (cell / 2f), py + (cell / 2f), 2f, new Color(235, 225, 190));
                }

                break;
            }

            case EdgeStyle.Sketch:
            {
                // Duas linhas de lápis meio tortas: o contorno colado no asfalto e um reforço logo atrás.
                c.Rect(px, py, cell, cell, style.EdgeBase);
                if (up)
                {
                    c.Rect(px, py + c.R(0f, 1f), cell, 2f, style.EdgeA);
                    c.Rect(px, py + 6f + c.R(0f, 1.5f), cell, 1f, style.EdgeB);
                }

                if (down)
                {
                    c.Rect(px, py + cell - 2f - c.R(0f, 1f), cell, 2f, style.EdgeA);
                    c.Rect(px, py + cell - 7f - c.R(0f, 1.5f), cell, 1f, style.EdgeB);
                }

                if (left)
                {
                    c.Rect(px + c.R(0f, 1f), py, 2f, cell, style.EdgeA);
                    c.Rect(px + 6f + c.R(0f, 1.5f), py, 1f, cell, style.EdgeB);
                }

                if (right)
                {
                    c.Rect(px + cell - 2f - c.R(0f, 1f), py, 2f, cell, style.EdgeA);
                    c.Rect(px + cell - 7f - c.R(0f, 1.5f), py, 1f, cell, style.EdgeB);
                }

                break;
            }

            case EdgeStyle.Fence:
            {
                c.Rect(px, py, cell, cell, style.EdgeBase);
                ForEachSide(px, py, up, down, left, right, 3, (rx, ry, rw, rh) => c.Rect(rx, ry, rw, rh, style.EdgeA));
                bool horizontal = up || down;
                for (int i = 0; i < 3; i++)
                {
                    float offset = 3f + (i * 7f);
                    float bx = horizontal ? px + offset : px + (cell / 2f) - 1f;
                    float by = horizontal ? py + (cell / 2f) - 1f : py + offset;
                    c.Rect(bx, by, 3f, 3f, style.EdgeB);
                    c.Rect(horizontal ? bx + 1f : px + 2f, horizontal ? py + 2f : by + 1f, horizontal ? 1f : cell - 4f, horizontal ? cell - 4f : 1f, style.EdgeA);
                }

                break;
            }
        }
    }

    private static void ForEachSide(float px, float py, bool up, bool down, bool left, bool right, int thickness, Action<float, float, float, float> draw)
    {
        const int cell = SceneCanvas.Cell;
        if (up)
        {
            draw(px, py, cell, thickness);
        }

        if (down)
        {
            draw(px, py + cell - thickness, cell, thickness);
        }

        if (left)
        {
            draw(px, py, thickness, cell);
        }

        if (right)
        {
            draw(px + cell - thickness, py, thickness, cell);
        }
    }

    /// <summary>Cenário da pista: a textura estática + o que se mexe. Chamar dentro do SpriteBatch principal.</summary>
    public void DrawBase(TrackTheme theme, Track track, float time)
    {
        if (_cache.TryGetValue(theme.Id, out RenderTarget2D target))
        {
            _spriteBatch.Draw(target, new Vector2(-SceneCanvas.Margin, -SceneCanvas.Margin), Color.White);
        }

        theme.Scenery.PaintAnimated?.Invoke(CreateCanvas(track, time, SeedFor(theme)));
    }

    /// <summary>Miniatura da pista (a textura estática reduzida) — usada no seletor de pistas.</summary>
    public void DrawPreview(TrackTheme theme, Rectangle destination, Color tint)
    {
        if (_cache.TryGetValue(theme.Id, out RenderTarget2D target))
        {
            _spriteBatch.Draw(target, destination, null, tint, 0f, Vector2.Zero, SpriteEffects.None, 0f);
        }
    }

    /// <summary>Avança as partículas de clima da pista (recicladas: quem sai de um lado volta pelo outro).</summary>
    public void UpdateAmbient(TrackTheme theme, float width, float height, float dt)
    {
        SceneryStyle style = theme.Scenery;
        if (_ambientThemeId != theme.Id)
        {
            _ambientThemeId = theme.Id;
            int count = style.Ambient == AmbientKind.None ? 0 : style.AmbientCount;
            _ambient = new Vector2[count];
            _ambientSeed = new float[count];
            for (int i = 0; i < count; i++)
            {
                _ambient[i] = new Vector2(Random(-40f, width + 40f), Random(-40f, height + 40f));
                _ambientSeed[i] = Random(0f, 1f);
            }
        }

        _ambientTime += dt;
        for (int i = 0; i < _ambient.Length; i++)
        {
            float seed = _ambientSeed[i];
            Vector2 velocity = style.Ambient switch
            {
                AmbientKind.Rain => new Vector2(-60f, 520f + (seed * 120f)),
                AmbientKind.Snow => new Vector2(MathF.Sin((_ambient[i].Y * 0.03f) + (seed * 6f)) * 25f, 40f + (seed * 30f)),
                AmbientKind.Embers => new Vector2(MathF.Sin((_ambient[i].Y * 0.05f) + (seed * 6f)) * 20f, -30f - (seed * 40f)),
                AmbientKind.Sand => new Vector2(260f + (seed * 160f), 18f * MathF.Sin(seed * 10f)),
                AmbientKind.Leaves => new Vector2(30f + (MathF.Sin((_ambient[i].Y * 0.04f) + (seed * 6f)) * 30f), 35f + (seed * 20f)),
                AmbientKind.Bubbles => new Vector2(MathF.Sin((_ambient[i].Y * 0.05f) + (seed * 6f)) * 14f, -35f - (seed * 45f)),
                AmbientKind.Confetti => new Vector2(MathF.Sin((_ambientTime * 2f) + (seed * 12f)) * 30f, 45f + (seed * 35f)),
                AmbientKind.Flies => new Vector2(MathF.Sin((_ambientTime * 9f) + (seed * 40f)) * 110f, MathF.Cos((_ambientTime * 7f) + (seed * 23f)) * 110f),
                AmbientKind.Fireflies => new Vector2(MathF.Sin((_ambientTime * 0.8f) + (seed * 30f)) * 18f, MathF.Cos((_ambientTime * 0.6f) + (seed * 17f)) * 14f),
                _ => Vector2.Zero,
            };

            Vector2 p = _ambient[i] + (velocity * dt);
            if (p.Y > height + 40f)
            {
                p.Y = -40f;
            }
            else if (p.Y < -40f)
            {
                p.Y = height + 40f;
            }

            if (p.X > width + 40f)
            {
                p.X = -40f;
            }
            else if (p.X < -40f)
            {
                p.X = width + 40f;
            }

            _ambient[i] = p;
        }
    }

    private float Random(float min, float max) => min + ((float)_ambientRandom.NextDouble() * (max - min));

    /// <summary>Partículas de clima. Chamada duas vezes por quadro: antes da escuridão (<paramref name="glowing"/> =
    /// false: chuva, neve, folhas...) e depois dela (true: brasas e vaga-lumes, que brilham no escuro).</summary>
    public void DrawAmbient(TrackTheme theme, float time, bool glowing = false)
    {
        SceneryStyle style = theme.Scenery;
        if (_ambientThemeId != theme.Id || glowing != (style.Ambient is AmbientKind.Embers or AmbientKind.Fireflies))
        {
            return;
        }

        Color color = style.AmbientColor;
        for (int i = 0; i < _ambient.Length; i++)
        {
            Vector2 p = _ambient[i];
            float seed = _ambientSeed[i];
            switch (style.Ambient)
            {
                case AmbientKind.Rain:
                    _spriteBatch.Draw(_pixel, p, null, color * 0.45f, 1.68f, Vector2.Zero, new Vector2(12f, 1f), SpriteEffects.None, 0f);
                    break;
                case AmbientKind.Snow:
                    _spriteBatch.Draw(_pixel, p, null, color * (0.6f + (0.4f * seed)), 0f, Vector2.Zero, new Vector2(seed > 0.6f ? 3f : 2f), SpriteEffects.None, 0f);
                    break;
                case AmbientKind.Embers:
                    float flicker = (MathF.Sin((time * 8f) + (seed * 20f)) + 1f) / 2f;
                    _spriteBatch.Draw(_pixel, p, null, color * (0.4f + (0.6f * flicker)), 0f, Vector2.Zero, new Vector2(2f), SpriteEffects.None, 0f);
                    break;
                case AmbientKind.Sand:
                    _spriteBatch.Draw(_pixel, p, null, color * 0.35f, 0.05f, Vector2.Zero, new Vector2(10f + (seed * 14f), 1f), SpriteEffects.None, 0f);
                    break;
                case AmbientKind.Leaves:
                    _spriteBatch.Draw(_pixel, p, null, seed > 0.5f ? color : CarPainter.Darken(color, 0.7f), (time * 2f) + (seed * 6f), new Vector2(0.5f), new Vector2(4f, 2f), SpriteEffects.None, 0f);
                    break;
                case AmbientKind.Bubbles:
                    float bubble = 1.5f + (seed * 3f);
                    _spriteBatch.Draw(_circle, p, null, color * 0.35f, 0f, new Vector2(_circle.Width / 2f), bubble * 2f / _circle.Width, SpriteEffects.None, 0f);
                    _spriteBatch.Draw(_pixel, p - new Vector2(bubble * 0.4f), null, Color.White * 0.7f, 0f, Vector2.Zero, new Vector2(1f), SpriteEffects.None, 0f);
                    break;
                case AmbientKind.Confetti:
                    Color confetti = ConfettiColors[(int)(seed * 97f) % ConfettiColors.Length];
                    _spriteBatch.Draw(_pixel, p, null, confetti, (time * 4f) + (seed * 10f), new Vector2(0.5f), new Vector2(4f, 2f + (MathF.Abs(MathF.Sin((time * 6f) + (seed * 9f))) * 2f)), SpriteEffects.None, 0f);
                    break;
                case AmbientKind.Flies:
                    _spriteBatch.Draw(_pixel, p, null, color, 0f, Vector2.Zero, new Vector2(2f), SpriteEffects.None, 0f);
                    _spriteBatch.Draw(_pixel, p + new Vector2(-1f, -1f), null, Color.White * (0.4f * MathF.Abs(MathF.Sin((time * 40f) + (seed * 9f)))), 0f, Vector2.Zero, new Vector2(4f, 1f), SpriteEffects.None, 0f);
                    break;
                case AmbientKind.Fireflies:
                    float pulse = (MathF.Sin((time * 3f) + (seed * 20f)) + 1f) / 2f;
                    _spriteBatch.Draw(_glow, p, null, color * (0.25f * pulse), 0f, new Vector2(_glow.Width / 2f), 14f / _glow.Width, SpriteEffects.None, 0f);
                    _spriteBatch.Draw(_pixel, p - Vector2.One, null, color * (0.4f + (0.6f * pulse)), 0f, Vector2.Zero, new Vector2(2f), SpriteEffects.None, 0f);
                    break;
            }
        }
    }

    /// <summary>Véu de escuridão da pista (depois dos carros, antes das luzes).</summary>
    public void DrawDarkness(TrackTheme theme, float width, float height)
    {
        SceneryStyle style = theme.Scenery;
        if (style.Darkness > 0f)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(-SceneCanvas.Margin, -SceneCanvas.Margin, (int)width + (2 * SceneCanvas.Margin), (int)height + (2 * SceneCanvas.Margin)), style.DarknessTint * style.Darkness);
        }
    }

    /// <summary>Luzes da pista e faróis. Chamar dentro de um SpriteBatch com mistura aditiva (pré-multiplicada).</summary>
    public void DrawLights(TrackTheme theme, Track track, float time, IEnumerable<(Vector2 Position, float Angle)> cars)
    {
        SceneryStyle style = theme.Scenery;
        SceneCanvas canvas = CreateCanvas(track, time, SeedFor(theme));
        style.PaintLights?.Invoke(canvas);

        if (!style.Headlights)
        {
            return;
        }

        foreach ((Vector2 position, float angle) in cars)
        {
            var forward = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            canvas.GlowEllipse(position.X + (forward.X * 30f), position.Y + (forward.Y * 30f), 34f, 16f, new Color(255, 240, 200) * 0.35f, angle);
            canvas.Glow(position.X + (forward.X * 10f), position.Y + (forward.Y * 10f), 12f, new Color(255, 250, 220) * 0.4f);
        }
    }

    public bool HasLights(TrackTheme theme) => theme.Scenery.PaintLights is not null || theme.Scenery.Headlights;
}
