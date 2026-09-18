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
        ModeSelect,
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
    private static readonly Color RecordColor = new(140, 255, 150);
    private static readonly Color EliminatedTextColor = new(150, 150, 150);
    private static readonly Color BoostFillColor = new(70, 200, 255);
    private static readonly Color BoostActiveColor = new(255, 200, 60);

    private static readonly Color[] AiPalette =
    [
        new Color(64, 200, 220),
        new Color(220, 90, 220),
        new Color(120, 220, 90),
        new Color(230, 90, 70),
        new Color(90, 120, 230),
        new Color(230, 160, 60),
        new Color(190, 225, 60),
        new Color(255, 110, 180),
        new Color(60, 200, 170),
        new Color(165, 110, 70),
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
    private RaceMode _selectedMode = RaceMode.Sprint;
    private SaveData _saveData = null!;
    private bool _recordsProcessed;
    private bool _newLapRecord;
    private bool _newRaceRecord;
    private bool _newScoreRecord;

    private int _windowWidth;
    private int _windowHeight;
    private int _hudTop;

    public GameRoot()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.Title = "MegRace - Corrida Top-Vision";

        _saveData = SaveData.Load();

        StartNewRace(_selectedMode);
        SizeWindowForCurrentRace();
    }

    private static readonly RaceMode[] AllModes = [RaceMode.Sprint, RaceMode.Elimination, RaceMode.TimeAttack];

    private static RaceMode NextMode(RaceMode current)
    {
        int index = Array.IndexOf(AllModes, current);
        return AllModes[(index + 1) % AllModes.Length];
    }

    private static RaceMode PreviousMode(RaceMode current)
    {
        int index = Array.IndexOf(AllModes, current);
        return AllModes[(index - 1 + AllModes.Length) % AllModes.Length];
    }

    private static int AiOpponentsFor(RaceMode mode) => mode switch
    {
        RaceMode.Elimination => 9,
        RaceMode.TimeAttack => 4,
        _ => 3,
    };

    private void StartNewRace(RaceMode mode)
    {
        _race = RaceFactory.CreateDefaultRace(aiOpponents: AiOpponentsFor(mode), targetLaps: 3, mode: mode);
        _player = _race.Entrants.First(e => e.Kind == DriverKind.Human);
        _recordsProcessed = false;
        _newLapRecord = false;
        _newRaceRecord = false;
        _newScoreRecord = false;

        _carColors.Clear();
        int aiIndex = 0;
        foreach (RaceEntrant entrant in _race.Entrants)
        {
            _carColors[entrant] = entrant.Kind == DriverKind.Human
                ? AccentColor
                : AiPalette[aiIndex++ % AiPalette.Length];
        }
    }

    /// <summary>Chamado sempre que uma nova corrida começa depois que o jogo já está rodando (não no construtor):
    /// redimensiona a janela pro novo modo/quantidade de carros e efetiva a mudança de verdade.</summary>
    private void StartNewRaceAndResize(RaceMode mode)
    {
        StartNewRace(mode);
        SizeWindowForCurrentRace();
        _graphics.ApplyChanges();
    }

    /// <summary>Compara o resultado da corrida com os recordes salvos, atualiza e persiste se necessário.</summary>
    private void ProcessRaceEndRecords()
    {
        if (_recordsProcessed)
        {
            return;
        }

        _recordsProcessed = true;

        switch (_race.Mode)
        {
            case RaceMode.Sprint:
                if (_player.Car.BestLapTime is { } lap
                    && (_saveData.BestLapTimeSprint is null || lap < _saveData.BestLapTimeSprint))
                {
                    _saveData.BestLapTimeSprint = lap;
                    _newLapRecord = true;
                }

                if (_player.Finished && _player.FinishTime is { } raceTime
                    && (_saveData.BestRaceTimeSprint is null || raceTime < _saveData.BestRaceTimeSprint))
                {
                    _saveData.BestRaceTimeSprint = raceTime;
                    _newRaceRecord = true;
                }

                break;

            case RaceMode.Elimination:
                _saveData.EliminationRaces++;
                if (_player.Finished)
                {
                    _saveData.EliminationWins++;
                }

                break;

            case RaceMode.TimeAttack:
                if (_saveData.BestScoreTimeAttack is null || _player.Score > _saveData.BestScoreTimeAttack)
                {
                    _saveData.BestScoreTimeAttack = _player.Score;
                    _newScoreRecord = true;
                }

                break;
        }

        _saveData.Save();
    }

    private void SizeWindowForCurrentRace()
    {
        int trackPixelWidth = _race.Track.Width * CellSize;
        int trackPixelHeight = _race.Track.Height * CellSize;

        int hudLines = _race.Mode == RaceMode.TimeAttack ? 5 : 4 + _race.Entrants.Count;
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
                    _state = State.ModeSelect;
                }

                break;

            case State.ModeSelect:
                if (_input.WasJustPressed(Keys.Escape))
                {
                    Exit();
                    return;
                }

                if (_input.WasJustPressed(Keys.Right) || _input.WasJustPressed(Keys.D)
                    || _input.WasJustPressed(Keys.Down) || _input.WasJustPressed(Keys.S))
                {
                    _selectedMode = NextMode(_selectedMode);
                }
                else if (_input.WasJustPressed(Keys.Left) || _input.WasJustPressed(Keys.A)
                    || _input.WasJustPressed(Keys.Up) || _input.WasJustPressed(Keys.W))
                {
                    _selectedMode = PreviousMode(_selectedMode);
                }

                if (_input.WasJustPressed(Keys.Space) || _input.WasJustPressed(Keys.Enter))
                {
                    StartNewRaceAndResize(_selectedMode);
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
                    ProcessRaceEndRecords();
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
                    StartNewRaceAndResize(_race.Mode);
                    _state = State.Racing;
                }
                else if (_input.WasJustPressed(Keys.M))
                {
                    _state = State.ModeSelect;
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

            case State.ModeSelect:
                DrawModeSelect();
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
            bool eliminated = entrant.Eliminated;
            Color color = eliminated ? Darken(_carColors[entrant], 0.4f) : _carColors[entrant];

            var center = new Vector2(car.Position.X * CellSize, car.Position.Y * CellSize);
            float length = CellSize * 1.35f;
            float width = CellSize * 0.85f;
            float halfLength = length / 2f;
            float halfWidth = width / 2f;

            if (!eliminated && car.IsBoosting)
            {
                DrawBoostFlame(center, car.Angle, halfLength);
            }

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

            if (!eliminated)
            {
                Vector2 headlightL = center + Rotate(new Vector2(halfLength * 0.9f, halfWidth * 0.55f), car.Angle);
                Vector2 headlightR = center + Rotate(new Vector2(halfLength * 0.9f, -halfWidth * 0.55f), car.Angle);
                DrawCircle(headlightL, CellSize * 0.11f, new Color(255, 250, 210));
                DrawCircle(headlightR, CellSize * 0.11f, new Color(255, 250, 210));

                Vector2 tailLight = center + Rotate(new Vector2(-halfLength * 0.92f, 0f), car.Angle);
                DrawCircle(tailLight, CellSize * 0.1f, new Color(200, 20, 20));
            }
        }
    }

    private void DrawBoostFlame(Vector2 center, float angle, float halfLength)
    {
        Vector2 flamePos = center + Rotate(new Vector2(-halfLength * 1.05f, 0f), angle);
        DrawCircle(flamePos, CellSize * 0.34f, new Color(255, 140, 30, 220));
        DrawCircle(flamePos, CellSize * 0.2f, new Color(255, 230, 90, 230));
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

        bool isElimination = _race.Mode == RaceMode.Elimination;
        bool isTimeAttack = _race.Mode == RaceMode.TimeAttack;

        if (raceOver)
        {
            string headline = _race.Mode switch
            {
                RaceMode.Elimination => _player.Finished ? "VOCE E O CAMPEAO!" : "VOCE FOI ELIMINADO",
                RaceMode.TimeAttack => "TEMPO ESGOTADO!",
                _ => "CORRIDA FINALIZADA",
            };
            Line(headline, AccentColor);

            if (isTimeAttack)
            {
                if (_newScoreRecord)
                {
                    Line("NOVO RECORDE DE PONTUACAO!", RecordColor);
                }
            }
            else if (_newRaceRecord)
            {
                Line("NOVO RECORDE DE CORRIDA!", RecordColor);
            }
            else if (_newLapRecord)
            {
                Line("NOVO RECORDE DE VOLTA!", RecordColor);
            }
        }
        else if (isTimeAttack)
        {
            float time = _race.TimeRemaining ?? 0f;
            Color timeColor = time <= 5f ? new Color(255, 100, 90) : TextColor;
            Line($"TEMPO {time:0.0}s   PONTOS {_player.Score:0}", timeColor);
        }
        else if (isElimination)
        {
            int active = _race.Entrants.Count(e => !e.Eliminated);
            bool playerIsLast = !_player.Eliminated && IsLastPlaceActive(_player);
            string warning = playerIsLast ? "  CUIDADO, VOCE ESTA POR ULTIMO!" : string.Empty;
            Line($"VOLTA {_player.Car.LapsCompleted + 1}   RESTAM {active}{warning}", playerIsLast ? new Color(255, 100, 90) : TextColor);
        }
        else
        {
            int lap = Math.Min(_player.Car.LapsCompleted + 1, _race.TargetLaps);
            Line($"VOLTA {lap}/{_race.TargetLaps}   TEMPO {FormatTime(_player.Car.CurrentLapTime)}", TextColor);
        }

        if (!raceOver)
        {
            DrawBoostBar();
        }

        if (isTimeAttack)
        {
            string scoreRecordSuffix = _saveData.BestScoreTimeAttack is { } sr ? $"   RECORDE {sr:0}" : string.Empty;
            Line($"PONTUACAO {_player.Score:0}{scoreRecordSuffix}   VEL {_player.Car.Speed:0}", TextColor);

            Line(
                raceOver
                    ? "R: CORRER DE NOVO   M: TROCAR MODO   ESC: SAIR"
                    : "SETAS/WASD DIRIGIR  ESPACO FREIO  SHIFT TURBO  ESC SAIR",
                TextColor);
            return;
        }

        string best = _player.Car.BestLapTime is { } b ? FormatTime(b) : "--:--.---";
        string recordSuffix = _saveData.BestLapTimeSprint is { } r ? $"   RECORDE {FormatTime(r)}" : string.Empty;
        Line($"MELHOR VOLTA {best}{recordSuffix}   VEL {_player.Car.Speed:0}", TextColor);

        Line("CLASSIFICACAO", AccentColor);

        int position = 1;
        foreach (RaceEntrant entrant in _race.GetStandings())
        {
            bool isHuman = entrant.Kind == DriverKind.Human;
            string displayName = isHuman ? "VOCE" : entrant.Car.Name.ToUpperInvariant();

            string status;
            if (entrant.Finished)
            {
                status = isElimination ? "CAMPEAO" : $"CHEGOU {FormatTime(entrant.FinishTime ?? 0f)}";
            }
            else if (entrant.Eliminated)
            {
                status = "ELIMINADO";
            }
            else
            {
                status = isElimination
                    ? $"VOLTA {entrant.Car.LapsCompleted}"
                    : $"VOLTA {entrant.Car.LapsCompleted}/{_race.TargetLaps}";
            }

            Color rowColor = entrant.Eliminated ? EliminatedTextColor : (isHuman ? AccentColor : TextColor);
            Color iconColor = entrant.Eliminated ? Darken(_carColors[entrant], 0.4f) : _carColors[entrant];

            string left = $"{position,2}";
            PixelFont.Draw(_spriteBatch, _pixel, left, new Vector2(x, y), HudTextSize, rowColor);

            float iconX = x + PixelFont.Measure(left, HudTextSize) + 10f;
            DrawCapsule(new Vector2(iconX + 8f, y + (PixelFont.LineHeight(HudTextSize) / 2f)), 16f, 8f, 0f, iconColor);

            string right = $"{displayName,-6} {status}";
            PixelFont.Draw(_spriteBatch, _pixel, right, new Vector2(iconX + 22f, y), HudTextSize, rowColor);

            y += lineHeight;
            position++;
        }

        Line(
            raceOver
                ? "R: CORRER DE NOVO   M: TROCAR MODO   ESC: SAIR"
                : "SETAS/WASD DIRIGIR  ESPACO FREIO  SHIFT TURBO  ESC SAIR",
            TextColor);
    }

    private bool IsLastPlaceActive(RaceEntrant entrant)
    {
        List<RaceEntrant> active = [.. _race.GetStandings().Where(e => !e.Eliminated && !e.Finished)];
        return active.Count > 0 && ReferenceEquals(active[^1], entrant);
    }

    private void DrawBoostBar()
    {
        const float width = 150f;
        const float height = 14f;
        float rightEdge = _windowWidth - TrackMargin;
        float x = rightEdge - width - 16f;
        float y = _hudTop + HudPaddingTop;

        var backRect = new Rectangle((int)x - 2, (int)y - 2, (int)width + 4, (int)height + 4);
        _spriteBatch.Draw(_pixel, backRect, new Color(10, 40, 20));

        float fraction = Math.Clamp(_player.Car.BoostFuel / _player.Car.Settings.BoostMaxFuel, 0f, 1f);
        var fillRect = new Rectangle((int)x, (int)y, (int)(width * fraction), (int)height);
        Color fillColor = _player.Car.IsBoosting ? BoostActiveColor : BoostFillColor;
        _spriteBatch.Draw(_pixel, fillRect, fillColor);

        const string label = "TURBO (SHIFT)";
        PixelFont.Draw(_spriteBatch, _pixel, label, new Vector2(x, y + height + 6f), 2f, TextColor);
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

        DrawPanelText(leftPanel, "CONTROLES", ["SETAS/WASD DIRIGIR", "SHIFT: TURBO", "ESPACO: FREIO DE MAO", "ESC: SAIR"]);
        DrawPanelText(rightPanel, "DICAS", ["ENCHA O TURBO NOS", "CHECKPOINTS E RETAS", "CUIDADO AO BATER NOS", "RIVAIS E NAS PAREDES"]);

        const string prompt = "APERTE QUALQUER TECLA";
        float promptWidth = PixelFont.Measure(prompt, 3.5f);
        var promptPos = new Vector2((trackAreaWidth - promptWidth) / 2f, panelsTop + panelHeight + 20f);
        PixelFont.Draw(_spriteBatch, _pixel, prompt, promptPos, 3.5f, AccentColor);
    }

    // ---------- Tela de seleção de modo ----------

    private void DrawModeSelect()
    {
        float trackAreaWidth = _windowWidth - (2f * TrackMargin);
        float trackAreaHeight = _windowHeight - TrackMargin;

        DrawTireStack(new Vector2(-24f, -24f), CellSize * 0.5f);
        DrawTireStack(new Vector2(trackAreaWidth + 24f, -24f), CellSize * 0.5f);
        DrawTireStack(new Vector2(-24f, trackAreaHeight - 24f), CellSize * 0.5f);
        DrawTireStack(new Vector2(trackAreaWidth + 24f, trackAreaHeight - 24f), CellSize * 0.5f);

        const float headerSize = 5.5f;
        const string header = "ESCOLHA O MODO";
        float headerWidth = PixelFont.Measure(header, headerSize);
        var headerPos = new Vector2((trackAreaWidth - headerWidth) / 2f, 44f);
        PixelFont.Draw(_spriteBatch, _pixel, header, headerPos, headerSize, AccentColor);

        float panelsTop = headerPos.Y + PixelFont.LineHeight(headerSize) + 36f;
        float panelHeight = 210f;
        float panelGap = 16f;
        float panelWidth = (trackAreaWidth - 40f - (2f * panelGap)) / 3f;

        var sprintPanel = new Rectangle(20, (int)panelsTop, (int)panelWidth, (int)panelHeight);
        var eliminationPanel = new Rectangle((int)(20f + panelWidth + panelGap), (int)panelsTop, (int)panelWidth, (int)panelHeight);
        var timeAttackPanel = new Rectangle((int)(20f + (2f * (panelWidth + panelGap))), (int)panelsTop, (int)panelWidth, (int)panelHeight);

        DrawSelectablePanel(sprintPanel, _selectedMode == RaceMode.Sprint);
        DrawSelectablePanel(eliminationPanel, _selectedMode == RaceMode.Elimination);
        DrawSelectablePanel(timeAttackPanel, _selectedMode == RaceMode.TimeAttack);

        List<string> sprintLines = ["CORRIDA CLASSICA:", "COMPLETE 3 VOLTAS", "NA FRENTE DE TODOS."];
        if (_saveData.BestLapTimeSprint is { } bestLap)
        {
            sprintLines.Add("");
            sprintLines.Add($"RECORDE: {FormatTime(bestLap)}");
        }

        List<string> eliminationLines = ["10 CARROS. A CADA", "VOLTA, O ULTIMO", "LUGAR E ELIMINADO.", "SOBREVIVA!"];
        if (_saveData.EliminationRaces > 0)
        {
            eliminationLines.Add("");
            eliminationLines.Add($"VITORIAS: {_saveData.EliminationWins}/{_saveData.EliminationRaces}");
        }

        List<string> timeAttackLines = ["O RELOGIO SO DESCE.", "PONTUE EM VOLTAS E", "CHECKPOINTS PRA", "GANHAR MAIS TEMPO!"];
        if (_saveData.BestScoreTimeAttack is { } bestScore)
        {
            timeAttackLines.Add("");
            timeAttackLines.Add($"RECORDE: {bestScore:0} PTS");
        }

        DrawPanelText(sprintPanel, "CORRIDA", [.. sprintLines]);
        DrawPanelText(eliminationPanel, "ELIMINACAO", [.. eliminationLines]);
        DrawPanelText(timeAttackPanel, "CONTRARRELOGIO", [.. timeAttackLines]);

        const string prompt = "SETAS: TROCAR    ESPACO: CONFIRMAR";
        float promptWidth = PixelFont.Measure(prompt, 3f);
        var promptPos = new Vector2((trackAreaWidth - promptWidth) / 2f, panelsTop + panelHeight + 24f);
        PixelFont.Draw(_spriteBatch, _pixel, prompt, promptPos, 3f, TextColor);
    }

    private void DrawSelectablePanel(Rectangle rect, bool selected)
    {
        _spriteBatch.Draw(_pixel, rect, selected ? AccentColor : PanelBorderColor);
        var inner = new Rectangle(rect.X + 4, rect.Y + 4, rect.Width - 8, rect.Height - 8);
        _spriteBatch.Draw(_pixel, inner, PanelFillColor);
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
