using Kyrios.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Kyrios.Game;

/// <summary>Jogo de corrida visto de cima. Toda a física/regras vêm de Kyrios.Core; esta classe só desenha e lê o teclado.</summary>
public sealed class GameRoot : Microsoft.Xna.Framework.Game
{
    private const int CellSize = 22;
    private const int TrackMargin = 40;
    private const int CircleTextureSize = 32;
    private const int HudPaddingTop = 20;
    private const int HudPaddingBottom = 16;
    private const int HudLineGap = 6;
    private const float HudTextSize = 3f;

    private enum State
    {
        Intro,
        Racing,
        Results,
    }

    private static readonly Color BackgroundGrass = new(30, 128, 58);
    private static readonly Color TrackColor = new(58, 58, 66);
    private static readonly Color GrassCellColor = new(38, 142, 66);
    private static readonly Color CheckpointTint = new(255, 215, 0, 90);
    private static readonly Color TextColor = Color.White;
    private static readonly Color AccentColor = new(255, 200, 40);
    private static readonly Color CurbBase = new(52, 46, 44);
    private static readonly Color CurbRed = new(200, 48, 40);
    private static readonly Color CurbWhite = new(232, 230, 224);
    private static readonly Color BushDark = new(26, 104, 48);
    private static readonly Color BushLight = new(52, 158, 78);
    private static readonly Color WheelColor = new(24, 24, 27);
    private static readonly Color ShadowColor = new(0, 0, 0, 90);
    private static readonly Color HudPanelColor = new(18, 82, 40);
    private static readonly Color PanelBorderColor = new(20, 60, 32);
    private static readonly Color PanelFillColor = new(24, 96, 48);

    private static readonly Color[] TitleGradient =
    [
        new Color(255, 236, 150),
        new Color(255, 214, 90),
        new Color(255, 176, 40),
        new Color(240, 120, 30),
        new Color(210, 60, 30),
    ];

    private static readonly Color TitleOutline = new(120, 24, 20);

    private static readonly Color[] AiPalette =
    [
        new Color(64, 200, 220),
        new Color(220, 90, 220),
        new Color(120, 220, 90),
        new Color(230, 90, 70),
        new Color(90, 120, 230),
        new Color(230, 160, 60),
    ];

    private readonly GraphicsDeviceManager _graphics;
    private readonly GameInput _input = new();
    private readonly Dictionary<RaceEntrant, Color> _carColors = new();

    private SpriteBatch _spriteBatch = null!;
    private Texture2D _pixel = null!;
    private Texture2D _circle = null!;

    private RaceSimulation _race = null!;
    private RaceEntrant _player = null!;
    private State _state = State.Intro;

    private int _windowWidth;
    private int _windowHeight;
    private int _hudTop;

    public GameRoot()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.Title = "MegRace - Corrida Top-Vision";

        StartNewRace();
        SizeWindowForCurrentRace();
    }

    private void StartNewRace()
    {
        _race = RaceFactory.CreateDefaultRace(aiOpponents: 3, targetLaps: 3);
        _player = _race.Entrants.First(e => e.Kind == DriverKind.Human);

        _carColors.Clear();
        int aiIndex = 0;
        foreach (RaceEntrant entrant in _race.Entrants)
        {
            _carColors[entrant] = entrant.Kind == DriverKind.Human
                ? AccentColor
                : AiPalette[aiIndex++ % AiPalette.Length];
        }
    }

    private void SizeWindowForCurrentRace()
    {
        int trackPixelWidth = _race.Track.Width * CellSize;
        int trackPixelHeight = _race.Track.Height * CellSize;

        int hudLines = 4 + _race.Entrants.Count;
        int hudHeight = HudPaddingTop + HudPaddingBottom
            + (int)(hudLines * (PixelFont.LineHeight(HudTextSize) + HudLineGap));

        _hudTop = trackPixelHeight;
        _windowWidth = Math.Max(trackPixelWidth + (2 * TrackMargin), 640);
        _windowHeight = TrackMargin + trackPixelHeight + hudHeight;

        _graphics.PreferredBackBufferWidth = _windowWidth;
        _graphics.PreferredBackBufferHeight = _windowHeight;
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);

        _circle = new Texture2D(GraphicsDevice, CircleTextureSize, CircleTextureSize);
        var circleData = new Color[CircleTextureSize * CircleTextureSize];
        var center = new Vector2(CircleTextureSize / 2f, CircleTextureSize / 2f);
        float radius = (CircleTextureSize / 2f) - 1f;
        for (int y = 0; y < CircleTextureSize; y++)
        {
            for (int x = 0; x < CircleTextureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                circleData[(y * CircleTextureSize) + x] = distance <= radius ? Color.White : Color.Transparent;
            }
        }

        _circle.SetData(circleData);
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Update();

        switch (_state)
        {
            case State.Intro:
                if (_input.AnyKeyJustPressed())
                {
                    _state = State.Racing;
                }

                break;

            case State.Racing:
                if (_input.WasJustPressed(Keys.Escape))
                {
                    Exit();
                    return;
                }

                float dt = Math.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 0.1f);
                _race.Update(dt, _input.BuildCarInput());
                if (_race.IsRaceOver)
                {
                    _state = State.Results;
                }

                break;

            case State.Results:
                if (_input.WasJustPressed(Keys.Escape))
                {
                    Exit();
                    return;
                }

                if (_input.WasJustPressed(Keys.R))
                {
                    StartNewRace();
                    _state = State.Racing;
                }

                break;
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(BackgroundGrass);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: Matrix.CreateTranslation(TrackMargin, TrackMargin, 0f));

        switch (_state)
        {
            case State.Intro:
                DrawIntro();
                break;

            case State.Racing:
                DrawTrack();
                DrawCars();
                DrawHud(raceOver: false);
                break;

            case State.Results:
                DrawTrack();
                DrawCars();
                DrawHud(raceOver: true);
                break;
        }

        _spriteBatch.End();
        base.Draw(gameTime);
    }

    // ---------- Primitivas de desenho ----------

    private void DrawCircle(Vector2 center, float radius, Color color)
    {
        float scale = (radius * 2f) / CircleTextureSize;
        _spriteBatch.Draw(_circle, center, null, color, 0f, new Vector2(CircleTextureSize / 2f), scale, SpriteEffects.None, 0f);
    }

    private void DrawFilledRectRotated(Vector2 center, float length, float width, float angle, Color color)
    {
        if (length <= 0f || width <= 0f)
        {
            return;
        }

        _spriteBatch.Draw(_pixel, center, null, color, angle, new Vector2(0.5f, 0.5f), new Vector2(length, width), SpriteEffects.None, 0f);
    }

    /// <summary>Desenha uma "cápsula" (retângulo com as pontas arredondadas) — a silhueta base dos carros.</summary>
    private void DrawCapsule(Vector2 center, float length, float width, float angle, Color color)
    {
        float radius = width / 2f;
        float straight = MathF.Max(length - width, 0f);

        DrawFilledRectRotated(center, straight, width, angle, color);

        Vector2 frontCenter = center + Rotate(new Vector2(straight / 2f, 0f), angle);
        Vector2 backCenter = center + Rotate(new Vector2(-straight / 2f, 0f), angle);
        DrawCircle(frontCenter, radius, color);
        DrawCircle(backCenter, radius, color);
    }

    private static Vector2 Rotate(Vector2 local, float angle)
    {
        float cos = MathF.Cos(angle);
        float sin = MathF.Sin(angle);
        return new Vector2((local.X * cos) - (local.Y * sin), (local.X * sin) + (local.Y * cos));
    }

    private static Color Darken(Color color, float factor) =>
        new((byte)(color.R * factor), (byte)(color.G * factor), (byte)(color.B * factor), color.A);

    // ---------- Pista ----------

    private void DrawTrack()
    {
        Track track = _race.Track;

        int deepMinX = int.MaxValue, deepMinY = int.MaxValue, deepMaxX = int.MinValue, deepMaxY = int.MinValue;

        for (int x = 0; x < track.Width; x++)
        {
            for (int y = 0; y < track.Height; y++)
            {
                char cell = track.CellAt(x, y);
                var rect = new Rectangle(x * CellSize, y * CellSize, CellSize, CellSize);

                if (cell == '#')
                {
                    bool up = IsTrackSurface(track, x, y - 1);
                    bool down = IsTrackSurface(track, x, y + 1);
                    bool left = IsTrackSurface(track, x - 1, y);
                    bool right = IsTrackSurface(track, x + 1, y);

                    if (up || down || left || right)
                    {
                        DrawCurbCell(rect, x, y, up, down, left, right);
                    }
                    else
                    {
                        _spriteBatch.Draw(_pixel, rect, GrassCellColor);
                        deepMinX = Math.Min(deepMinX, x);
                        deepMaxX = Math.Max(deepMaxX, x);
                        deepMinY = Math.Min(deepMinY, y);
                        deepMaxY = Math.Max(deepMaxY, y);
                    }

                    continue;
                }

                Color baseColor = cell == ',' ? GrassCellColor : TrackColor;
                _spriteBatch.Draw(_pixel, rect, baseColor);

                if (cell == 'S')
                {
                    DrawFinishChecker(rect);
                }
                else if (cell is >= '1' and <= '9')
                {
                    _spriteBatch.Draw(_pixel, rect, CheckpointTint);
                }
            }
        }

        if (deepMaxX >= deepMinX)
        {
            DrawInfieldDecorations(deepMinX, deepMinY, deepMaxX, deepMaxY);
        }

        DrawOuterTireStacks(track);
    }

    private static bool IsTrackSurface(Track track, int x, int y) => track.CellAt(x, y) != '#';

    private void DrawCurbCell(Rectangle rect, int x, int y, bool up, bool down, bool left, bool right)
    {
        _spriteBatch.Draw(_pixel, rect, CurbBase);

        int thickness = Math.Max(CellSize / 4, 4);
        Color stripeColor = (x + y) % 2 == 0 ? CurbRed : CurbWhite;

        if (up)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), stripeColor);
        }

        if (down)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), stripeColor);
        }

        if (left)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), stripeColor);
        }

        if (right)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), stripeColor);
        }
    }

    private void DrawFinishChecker(Rectangle cellRect)
    {
        int half = CellSize / 2;
        var topLeft = new Rectangle(cellRect.X, cellRect.Y, half, half);
        var bottomRight = new Rectangle(cellRect.X + half, cellRect.Y + half, CellSize - half, CellSize - half);
        var topRight = new Rectangle(cellRect.X + half, cellRect.Y, CellSize - half, half);
        var bottomLeft = new Rectangle(cellRect.X, cellRect.Y + half, half, CellSize - half);

        _spriteBatch.Draw(_pixel, topLeft, Color.White);
        _spriteBatch.Draw(_pixel, bottomRight, Color.White);
        _spriteBatch.Draw(_pixel, topRight, Color.Black);
        _spriteBatch.Draw(_pixel, bottomLeft, Color.Black);
    }

    private void DrawInfieldDecorations(int minX, int minY, int maxX, int maxY)
    {
        float cx = (minX + maxX + 1) / 2f * CellSize;
        float cy = (minY + maxY + 1) / 2f * CellSize;
        float w = (maxX - minX + 1) * CellSize;
        float h = (maxY - minY + 1) * CellSize;

        var bush1 = new Vector2(cx - (w * 0.28f), cy - (h * 0.12f));
        DrawCircle(bush1, CellSize * 0.95f, BushDark);
        DrawCircle(bush1, CellSize * 0.7f, BushLight);

        var bush2 = new Vector2(cx + (w * 0.30f), cy + (h * 0.18f));
        DrawCircle(bush2, CellSize * 0.85f, BushDark);
        DrawCircle(bush2, CellSize * 0.6f, BushLight);

        var poleRect = new Rectangle((int)(cx - 2f), (int)(cy - (CellSize * 1.6f)), 4, (int)(CellSize * 1.7f));
        _spriteBatch.Draw(_pixel, poleRect, new Color(220, 220, 220));

        var flagRect = new Rectangle((int)(cx + 2f), (int)(cy - (CellSize * 1.6f)), (int)(CellSize * 0.9f), (int)(CellSize * 0.55f));
        _spriteBatch.Draw(_pixel, flagRect, AccentColor);
    }

    private void DrawOuterTireStacks(Track track)
    {
        float w = track.Width * CellSize;
        float h = track.Height * CellSize;
        const float spacing = CellSize * 1.8f;
        const float offset = CellSize * 0.5f;
        const float radius = CellSize * 0.32f;

        for (float px = spacing / 2f; px < w; px += spacing)
        {
            DrawTireStack(new Vector2(px, -offset), radius);
            DrawTireStack(new Vector2(px, h + offset), radius);
        }

        for (float py = spacing / 2f; py < h; py += spacing)
        {
            DrawTireStack(new Vector2(-offset, py), radius);
            DrawTireStack(new Vector2(w + offset, py), radius);
        }
    }

    private void DrawTireStack(Vector2 center, float radius)
    {
        DrawCircle(center, radius, new Color(28, 28, 30));
        DrawCircle(center, radius * 0.55f, new Color(78, 78, 82));
    }

    // ---------- Carros ----------

    private void DrawCars()
    {
        foreach (RaceEntrant entrant in _race.Entrants)
        {
            Car car = entrant.Car;
            Color color = _carColors[entrant];

            var center = new Vector2(car.Position.X * CellSize, car.Position.Y * CellSize);
            float length = CellSize * 1.35f;
            float width = CellSize * 0.85f;
            float halfLength = length / 2f;
            float halfWidth = width / 2f;

            DrawCapsule(center + new Vector2(2f, 3f), length, width, car.Angle, ShadowColor);

            Color wheelColor = WheelColor;
            float wheelLength = length * 0.4f;
            float wheelWidth = width * 0.3f;
            foreach (float sideX in new[] { -1f, 1f })
            {
                foreach (float sideY in new[] { -1f, 1f })
                {
                    Vector2 local = new(sideX * halfLength * 0.5f, sideY * (halfWidth + (wheelWidth * 0.55f)));
                    Vector2 wheelPos = center + Rotate(local, car.Angle);
                    DrawFilledRectRotated(wheelPos, wheelLength, wheelWidth, car.Angle, wheelColor);
                }
            }

            DrawCapsule(center, length, width, car.Angle, color);

            Color roofColor = Darken(color, 0.5f);
            Vector2 roofCenter = center + Rotate(new Vector2(halfLength * 0.05f, 0f), car.Angle);
            DrawCapsule(roofCenter, length * 0.5f, width * 0.6f, car.Angle, roofColor);

            Vector2 headlightL = center + Rotate(new Vector2(halfLength * 0.9f, halfWidth * 0.55f), car.Angle);
            Vector2 headlightR = center + Rotate(new Vector2(halfLength * 0.9f, -halfWidth * 0.55f), car.Angle);
            DrawCircle(headlightL, CellSize * 0.11f, new Color(255, 250, 210));
            DrawCircle(headlightR, CellSize * 0.11f, new Color(255, 250, 210));

            Vector2 tailLight = center + Rotate(new Vector2(-halfLength * 0.92f, 0f), car.Angle);
            DrawCircle(tailLight, CellSize * 0.1f, new Color(200, 20, 20));
        }
    }

    // ---------- HUD ----------

    private void DrawHud(bool raceOver)
    {
        var panelRect = new Rectangle(-TrackMargin, _hudTop, _windowWidth, _windowHeight - _hudTop - TrackMargin);
        _spriteBatch.Draw(_pixel, panelRect, HudPanelColor);
        _spriteBatch.Draw(_pixel, new Rectangle(-TrackMargin, _hudTop, _windowWidth, 4), AccentColor);

        float y = _hudTop + HudPaddingTop;
        const float x = 14f;
        float lineHeight = PixelFont.LineHeight(HudTextSize) + HudLineGap;

        void Line(string text, Color color)
        {
            PixelFont.Draw(_spriteBatch, _pixel, text, new Vector2(x, y), HudTextSize, color);
            y += lineHeight;
        }

        if (raceOver)
        {
            Line("CORRIDA FINALIZADA", AccentColor);
        }
        else
        {
            int lap = Math.Min(_player.Car.LapsCompleted + 1, _race.TargetLaps);
            Line($"VOLTA {lap}/{_race.TargetLaps}   TEMPO {FormatTime(_player.Car.CurrentLapTime)}   VEL {_player.Car.Speed:0}", TextColor);
        }

        string best = _player.Car.BestLapTime is { } b ? FormatTime(b) : "--:--.---";
        Line($"MELHOR VOLTA {best}   TEMPO TOTAL {FormatTime(_player.Car.TotalRaceTime)}", TextColor);

        Line("CLASSIFICACAO", AccentColor);

        int position = 1;
        foreach (RaceEntrant entrant in _race.GetStandings())
        {
            bool isHuman = entrant.Kind == DriverKind.Human;
            string displayName = isHuman ? "VOCE" : entrant.Car.Name.ToUpperInvariant();
            string status = entrant.Finished
                ? $"CHEGOU {FormatTime(entrant.FinishTime ?? 0f)}"
                : $"VOLTA {entrant.Car.LapsCompleted}/{_race.TargetLaps}";
            Color rowColor = isHuman ? AccentColor : TextColor;

            string left = $"{position,2}";
            PixelFont.Draw(_spriteBatch, _pixel, left, new Vector2(x, y), HudTextSize, rowColor);

            float iconX = x + PixelFont.Measure(left, HudTextSize) + 10f;
            DrawCapsule(new Vector2(iconX + 8f, y + (PixelFont.LineHeight(HudTextSize) / 2f)), 16f, 8f, 0f, _carColors[entrant]);

            string right = $"{displayName,-6} {status}";
            PixelFont.Draw(_spriteBatch, _pixel, right, new Vector2(iconX + 22f, y), HudTextSize, rowColor);

            y += lineHeight;
            position++;
        }

        Line(raceOver ? "R: CORRER DE NOVO   ESC: SAIR" : "SETAS/WASD DIRIGIR   ESPACO FREIO   ESC SAIR", TextColor);
    }

    // ---------- Tela inicial ----------

    private void DrawIntro()
    {
        float trackAreaWidth = (_windowWidth - (2f * TrackMargin));
        float trackAreaHeight = (_windowHeight - TrackMargin);

        DrawTireStack(new Vector2(-24f, -24f), CellSize * 0.5f);
        DrawTireStack(new Vector2(trackAreaWidth + 24f, -24f), CellSize * 0.5f);
        DrawTireStack(new Vector2(-24f, trackAreaHeight - 24f), CellSize * 0.5f);
        DrawTireStack(new Vector2(trackAreaWidth + 24f, trackAreaHeight - 24f), CellSize * 0.5f);

        DrawCircle(new Vector2(20f, 40f), CellSize * 1.1f, BushDark);
        DrawCircle(new Vector2(20f, 40f), CellSize * 0.8f, BushLight);
        DrawCircle(new Vector2(trackAreaWidth - 20f, 40f), CellSize * 1.1f, BushDark);
        DrawCircle(new Vector2(trackAreaWidth - 20f, 40f), CellSize * 0.8f, BushLight);

        const float titleSize = 8f;
        string title = "MEGRACE";
        float titleWidth = PixelFont.Measure(title, titleSize);
        var titlePos = new Vector2((trackAreaWidth - titleWidth) / 2f, 36f);

        foreach (Vector2 offset in OutlineOffsets)
        {
            PixelFont.Draw(_spriteBatch, _pixel, title, titlePos + (offset * 2f), titleSize, TitleOutline);
        }

        PixelFont.DrawGradient(_spriteBatch, _pixel, title, titlePos, titleSize, TitleGradient);

        const float subtitleSize = 4f;
        string subtitle = "CORRIDA TOP-VISION";
        float subtitleWidth = PixelFont.Measure(subtitle, subtitleSize);
        var subtitlePos = new Vector2((trackAreaWidth - subtitleWidth) / 2f, titlePos.Y + PixelFont.LineHeight(titleSize) + 16f);
        PixelFont.Draw(_spriteBatch, _pixel, subtitle, subtitlePos, subtitleSize, Color.White);

        float panelsTop = subtitlePos.Y + PixelFont.LineHeight(subtitleSize) + 28f;
        float panelHeight = 130f;
        float panelWidth = (trackAreaWidth - 60f) / 2f;

        var leftPanel = new Rectangle(20, (int)panelsTop, (int)panelWidth, (int)panelHeight);
        var rightPanel = new Rectangle((int)(20f + panelWidth + 20f), (int)panelsTop, (int)panelWidth, (int)panelHeight);

        DrawPanel(leftPanel);
        DrawPanel(rightPanel);

        DrawPanelText(leftPanel, "CONTROLES", ["SETAS/WASD DIRIGIR", "ESPACO FREIO DE MAO", "ESC SAIR"]);
        DrawPanelText(rightPanel, "REGRAS", ["COMPLETE 3 VOLTAS", "PASSANDO PELOS", "CHECKPOINTS NA ORDEM", "ANTES DA CHEGADA"]);

        const string prompt = "APERTE QUALQUER TECLA";
        float promptWidth = PixelFont.Measure(prompt, 3.5f);
        var promptPos = new Vector2((trackAreaWidth - promptWidth) / 2f, panelsTop + panelHeight + 20f);
        PixelFont.Draw(_spriteBatch, _pixel, prompt, promptPos, 3.5f, AccentColor);
    }

    private static readonly Vector2[] OutlineOffsets =
    [
        new(-1, -1), new(0, -1), new(1, -1),
        new(-1, 0), new(1, 0),
        new(-1, 1), new(0, 1), new(1, 1),
    ];

    private void DrawPanel(Rectangle rect)
    {
        _spriteBatch.Draw(_pixel, rect, PanelBorderColor);
        var inner = new Rectangle(rect.X + 4, rect.Y + 4, rect.Width - 8, rect.Height - 8);
        _spriteBatch.Draw(_pixel, inner, PanelFillColor);
    }

    private void DrawPanelText(Rectangle panel, string header, string[] lines)
    {
        float y = panel.Y + 12f;
        float headerSize = 3.5f;
        float headerWidth = PixelFont.Measure(header, headerSize);
        PixelFont.Draw(_spriteBatch, _pixel, header, new Vector2(panel.X + ((panel.Width - headerWidth) / 2f), y), headerSize, AccentColor);
        y += PixelFont.LineHeight(headerSize) + 12f;

        foreach (string line in lines)
        {
            PixelFont.Draw(_spriteBatch, _pixel, line, new Vector2(panel.X + 10f, y), 2.5f, Color.White);
            y += PixelFont.LineHeight(2.5f) + 8f;
        }
    }

    private static string FormatTime(float seconds)
    {
        if (seconds < 0f)
        {
            seconds = 0f;
        }

        var span = TimeSpan.FromSeconds(seconds);
        return $"{(int)span.TotalMinutes:00}:{span.Seconds:00}.{span.Milliseconds:000}";
    }
}
