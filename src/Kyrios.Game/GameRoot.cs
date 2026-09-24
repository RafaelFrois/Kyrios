using Kyrios.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Kyrios.Game;

/// <summary>
/// Jogo de corrida visto de cima. Toda a física/regras vêm de Kyrios.Core; esta classe cuida das telas,
/// do desenho e da entrada. Dividida em partes: este arquivo (estados, ciclo da partida e desenho do mundo),
/// <c>GameRoot.Ui</c> (componentes visuais compartilhados), <c>GameRoot.Menus</c> (menu, modo, pistas, skins,
/// configurações), <c>GameRoot.Race</c> (HUD, pausa, feedback e resultado) e <c>GameRoot.Achievements</c>.
/// </summary>
public sealed partial class GameRoot : Microsoft.Xna.Framework.Game
{
    private const int CellSize = SceneCanvas.Cell;
    private const int TrackMargin = SceneCanvas.Margin;
    private const int CircleTextureSize = 32;

    private enum State
    {
        MainMenu,
        ModeSelect,
        TrackSelect,
        SkinSelect,
        Racing,
        Paused,
        Results,
        Achievements,
        Settings,
    }

    private enum SettingsRow
    {
        Music,
        Sfx,
    }

    // Paleta da interface (menus, painéis, HUD) — a mesma em todas as telas.
    private static readonly Color MenuBackground = new(21, 24, 34);
    private static readonly Color MenuBackgroundDim = new(21, 24, 34, 205);
    private static readonly Color TextColor = Color.White;
    private static readonly Color AccentColor = new(255, 200, 40);
    private static readonly Color DangerColor = new(255, 95, 80);
    private static readonly Color ShadowColor = new(0, 0, 0, 90);
    private static readonly Color PanelBorderColor = new(54, 60, 80);
    private static readonly Color PanelFillColor = new(33, 38, 53);
    private static readonly Color OverlayDimColor = new(8, 9, 14, 195);
    private static readonly Color HazardColor = new(230, 122, 40);
    private static readonly Color HazardCapColor = new(35, 34, 38);
    private static readonly Color HudFrameFill = new(16, 18, 27, 225);
    private static readonly Color StatBadgeFill = new(15, 32, 24);
    private static readonly Color StatBadgeLabelColor = new(150, 158, 175);
    private static readonly Color StudioLogoColor = new(225, 228, 238, 205);
    private static readonly Color RecordColor = new(140, 255, 150);
    private static readonly Color BoostFillColor = new(70, 200, 255);
    private static readonly Color BoostActiveColor = new(255, 200, 60);

    private static readonly Color[] TitleGradient =
    [
        new Color(255, 236, 150),
        new Color(255, 214, 90),
        new Color(255, 176, 40),
        new Color(240, 120, 30),
        new Color(210, 60, 30),
    ];

    private static readonly Color TitleOutline = new(120, 24, 20);

    /// <summary>Soma de luz pra cores pré-multiplicadas (o padrão do SpriteBatch): o <see cref="BlendState.Additive"/>
    /// multiplicaria pelo alfa de novo e as luzes quase sumiriam.</summary>
    private static readonly BlendState PremultipliedAdditive = new()
    {
        ColorSourceBlend = Blend.One,
        AlphaSourceBlend = Blend.One,
        ColorDestinationBlend = Blend.One,
        AlphaDestinationBlend = Blend.One,
    };

    // Cores e nomes de exibição dos rivais (o jogador é sempre o amarelo de destaque).
    private static readonly (Color Color, string Name)[] AiLooks =
    [
        (new Color(64, 200, 220), "AZUL"),
        (new Color(220, 90, 220), "ROXO"),
        (new Color(120, 220, 90), "VERDE"),
        (new Color(230, 90, 70), "VERMELHO"),
    ];

    private readonly GraphicsDeviceManager _graphics;
    private readonly GameInput _input = new();
    private readonly Dictionary<RaceEntrant, Color> _carColors = new();
    private readonly Dictionary<RaceEntrant, string> _carNames = new();

    private SpriteBatch _spriteBatch = null!;
    private Texture2D _pixel = null!;
    private Texture2D _circle = null!;
    private AudioManager _audio = null!;
    private CarPainter _carPainter = null!;
    private AchievementIconRenderer _iconRenderer = null!;
    private SceneryRenderer _scenery = null!;
    private readonly ParticleSystem _particles = new();
    private readonly Random _particleRandom = new();
    private readonly Random _shakeRandom = new();
    private readonly RaceTracker _raceTracker = new();

    private float _visualTime;
    private float _shakeTimer;
    private float _shakeDuration;
    private float _shakeMagnitude;
    private Vector2 _shakeOffset;

    private RaceSimulation _race = null!;
    private RaceEntrant _player = null!;
    private State _state = State.MainMenu;
    private RaceMode _selectedMode;
    private SaveData _saveData = null!;
    private bool _recordsProcessed;
    private RecordFlags _records;
    private List<UnlockNotice> _raceUnlocks = [];
    private bool _playerWasCollidingLastTick;
    private int _lastCountdownTickSecond = int.MaxValue;

    // Escolhas: a equipada (vai pra corrida) e a que está sendo olhada no seletor são coisas separadas — dá
    // pra navegar até algo bloqueado pra ver o requisito sem que vire a escolha.
    private int _selectedSkinIndex;
    private int _previewSkinIndex;
    private int _selectedTrackIndex;
    private int _previewTrackIndex;

    private int _windowWidth;
    private int _windowHeight;
    private bool _isFullscreen;

    public GameRoot()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.Title = "MegRace";

        // Tela cheia sem trocar o modo de vídeo (borderless) — a cena já é escalada/centralizada no Draw.
        _graphics.HardwareModeSwitch = false;

        _saveData = SaveData.Load();
        Progression.Normalize(_saveData);

        // Progresso que já cumpre algum requisito (ex.: um save de uma versão anterior) libera tudo logo ao
        // abrir, com as notificações de sempre. Escolhas salvas que não estejam liberadas voltam pro padrão.
        CheckUnlocks(showToasts: true);
        _selectedSkinIndex = CarSkins.IndexOf(_saveData.SelectedSkinId);
        if (!SkinUnlocks.IsUnlocked(SelectedSkin, _saveData))
        {
            _selectedSkinIndex = 0;
        }

        _selectedTrackIndex = TrackThemes.IndexOf(_saveData.SelectedTrackId);
        if (!TrackThemes.IsUnlocked(SelectedTrack, _saveData))
        {
            _selectedTrackIndex = 0;
        }

        _previewSkinIndex = _selectedSkinIndex;
        _previewTrackIndex = _selectedTrackIndex;
        _selectedMode = _saveData.LastMode;

        StartNewRace(_selectedMode);
        SizeWindow();
    }

    private CarSkin SelectedSkin => CarSkins.All[_selectedSkinIndex];

    private TrackTheme SelectedTrack => TrackThemes.All[_selectedTrackIndex];

    /// <summary>O cenário mostrado agora: no seletor de pistas, o fundo inteiro vira a pista que está sendo
    /// olhada (uma prévia "ao vivo"); no resto, a pista escolhida.</summary>
    private TrackTheme WorldTheme => _state == State.TrackSelect ? TrackThemes.All[_previewTrackIndex] : SelectedTrack;

    private static bool IsMenuState(State state) =>
        state is State.MainMenu or State.ModeSelect or State.TrackSelect or State.SkinSelect or State.Achievements or State.Settings;

    private float AreaWidth => _windowWidth - (2f * TrackMargin);

    private float AreaHeight => _windowHeight - (2f * TrackMargin);

    /// <summary>Corrida Mortal com 4 carros; o Contra o Relógio tem rivais só como tráfego.</summary>
    private static int AiOpponentsFor(RaceMode mode) => mode == RaceMode.Elimination ? 3 : 4;

    private void StartNewRace(RaceMode mode)
    {
        _race = RaceFactory.CreateDefaultRace(mode, aiOpponents: AiOpponentsFor(mode));
        _player = _race.Entrants.First(e => e.Kind == DriverKind.Human);
        _recordsProcessed = false;
        _records = default;
        _raceUnlocks = [];
        _playerWasCollidingLastTick = false;
        _lastCountdownTickSecond = int.MaxValue;
        _raceTracker.Reset(_race, SelectedTrack.SecretSpot);
        _particles.Clear();
        _shakeTimer = 0f;
        _shakeOffset = Vector2.Zero;
        ResetRaceFeedback();

        _carColors.Clear();
        _carNames.Clear();
        int aiIndex = 0;
        foreach (RaceEntrant entrant in _race.Entrants)
        {
            if (entrant.Kind == DriverKind.Human)
            {
                _carColors[entrant] = AccentColor;
                _carNames[entrant] = "VOCE";
            }
            else
            {
                (Color color, string name) = AiLooks[aiIndex++ % AiLooks.Length];
                _carColors[entrant] = color;
                _carNames[entrant] = name;
            }
        }
    }

    /// <summary>Começa uma partida de verdade (sai dos menus pra pista), com a pista e a skin escolhidas.</summary>
    private void BeginRace(RaceMode mode)
    {
        _selectedMode = mode;
        _saveData.LastMode = mode;
        _saveData.Save();
        StartNewRace(mode);
        _audio.PlayMenuConfirm();
        _audio.PlayMusic(mode);
        _audio.StartEngine();
        _state = State.Racing;
    }

    /// <summary>Mantém a corrida que serve de fundo animado dos menus sempre andando (as IAs dirigem sozinhas)
    /// e reinicia quando ela termina — é só decoração, nunca toca som nem mexe no progresso.</summary>
    private void UpdateMenuBackgroundRace(float frameSeconds)
    {
        float dt = Math.Min(frameSeconds, 0.1f);
        _race.Update(dt, CarInput.None);

        if (_race.IsRaceOver)
        {
            StartNewRace(_race.Mode);
        }
    }

    private void ToggleFullscreen()
    {
        _isFullscreen = !_isFullscreen;

        if (_isFullscreen)
        {
            DisplayMode display = GraphicsDevice.Adapter.CurrentDisplayMode;
            _graphics.PreferredBackBufferWidth = display.Width;
            _graphics.PreferredBackBufferHeight = display.Height;
        }
        else
        {
            _graphics.PreferredBackBufferWidth = _windowWidth;
            _graphics.PreferredBackBufferHeight = _windowHeight;
        }

        _graphics.IsFullScreen = _isFullscreen;
        _graphics.ApplyChanges();
    }

    /// <summary>Fim de partida — o único ponto em que o progresso muda. Fluxo: estatísticas atualizadas
    /// (<see cref="Progression.RecordRace"/>) → conquistas, skins e pistas conferidas e liberadas → salva →
    /// a tela de resultado mostra o que foi desbloqueado.</summary>
    private void ProcessRaceEndRecords()
    {
        if (_recordsProcessed)
        {
            return;
        }

        _recordsProcessed = true;

        bool silent = _audio.MusicMuted && _audio.SfxMuted;
        RaceReport report = _raceTracker.BuildReport(_race, _player, SelectedSkin.Id, SelectedTrack.Id, silent);
        _records = Progression.RecordRace(_saveData, report);
        _saveData.Save();
        _raceUnlocks = CheckUnlocks(showToasts: false);
        StartResultsReveal();
    }

    /// <summary>Libera o que o progresso atual já cumpre, salva e devolve a lista. Fora da tela de resultado
    /// (ex.: ao abrir o jogo), cada desbloqueio vira uma notificação — ou um resumo, se forem muitos.</summary>
    private List<UnlockNotice> CheckUnlocks(bool showToasts)
    {
        List<UnlockNotice> notices = Progression.CheckUnlocks(_saveData);
        if (notices.Count == 0)
        {
            return notices;
        }

        _saveData.Save();
        if (showToasts)
        {
            EnqueueUnlockToasts(notices);
        }

        return notices;
    }

    /// <summary>Decide qual dos três jingles de resultado combina com o desfecho da partida.</summary>
    private RaceOutcome DetermineOutcome() => _race.Mode switch
    {
        RaceMode.Elimination => _player.Finished ? RaceOutcome.Victory : RaceOutcome.Defeat,
        _ => _records.NewScoreRecord ? RaceOutcome.Victory : RaceOutcome.Neutral,
    };

    private void TriggerScreenShake(float magnitude)
    {
        _shakeDuration = 0.18f;
        _shakeTimer = _shakeDuration;
        _shakeMagnitude = magnitude;
    }

    private void DecayScreenShake(float dt)
    {
        if (_shakeTimer <= 0f)
        {
            _shakeOffset = Vector2.Zero;
            return;
        }

        _shakeTimer = MathF.Max(0f, _shakeTimer - dt);
        float fraction = _shakeDuration > 0f ? _shakeTimer / _shakeDuration : 0f;
        float magnitude = _shakeMagnitude * fraction;
        _shakeOffset = new Vector2(
            (((float)_shakeRandom.NextDouble() * 2f) - 1f) * magnitude,
            (((float)_shakeRandom.NextDouble() * 2f) - 1f) * magnitude);
    }

    /// <summary>Poeira de pneu, faíscas na colisão e rastro de turbo — o mesmo círculo procedural de sempre,
    /// pequeno e com vida curta.</summary>
    private void UpdateParticleSpawns()
    {
        foreach (RaceEntrant entrant in _race.Entrants)
        {
            if (entrant.Eliminated)
            {
                continue;
            }

            Car car = entrant.Car;
            var worldPos = new Vector2(car.Position.X * CellSize, car.Position.Y * CellSize);
            Vector2 forward = Rotate(new Vector2(1f, 0f), car.Angle);

            if (car.IsBoosting)
            {
                Vector2 boostVelocity = (-forward * 40f) + RandomSpread(20f);
                Vector2 spawnPos = worldPos - (forward * CellSize * 0.7f);
                _particles.Spawn(spawnPos, boostVelocity, life: 0.35f, size: CellSize * 0.22f, new Color(255, 160, 60, 200));
            }

            if (car.HadHeadOnCollisionThisTick || car.HadCarCollisionThisTick || car.HadHazardCollisionThisTick)
            {
                for (int i = 0; i < 3; i++)
                {
                    _particles.Spawn(worldPos, RandomSpread(90f), life: 0.25f, size: CellSize * 0.15f, new Color(255, 225, 150, 220));
                }
            }
        }
    }

    private Vector2 RandomSpread(float maxSpeed)
    {
        float angle = (float)(_particleRandom.NextDouble() * Math.PI * 2.0);
        float speed = (float)_particleRandom.NextDouble() * maxSpeed;
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;
    }

    /// <summary>Resolução "lógica" do jogo: a pista + a margem decorativa. Todas as pistas têm o mesmo
    /// circuito, então o tamanho é fixo; em tela cheia a cena é escalada até caber (ver Draw).</summary>
    private void SizeWindow()
    {
        _windowWidth = (_race.Track.Width * CellSize) + (2 * TrackMargin);
        _windowHeight = (_race.Track.Height * CellSize) + (2 * TrackMargin);
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
        _carPainter = new CarPainter(_spriteBatch, _pixel, _circle);
        _iconRenderer = new AchievementIconRenderer(_spriteBatch, _pixel, _carPainter);
        _scenery = new SceneryRenderer(GraphicsDevice, _spriteBatch, _pixel, _circle);

        _audio = new AudioManager();
        _audio.LoadContent();
        _audio.SetMusicVolume(_saveData.MusicVolume);
        _audio.SetSfxVolume(_saveData.SfxVolume);
        _audio.SetMusicMuted(_saveData.MusicMuted);
        _audio.SetSfxMuted(_saveData.SfxMuted);
    }

    protected override void UnloadContent()
    {
        _audio.Dispose();
        base.UnloadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Update();

        float frameSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _visualTime += frameSeconds;
        DecayScreenShake(frameSeconds);
        UpdateUiAnimations(frameSeconds);
        UpdateUnlockToast(frameSeconds);

        bool altHeld = _input.IsDown(Keys.LeftAlt) || _input.IsDown(Keys.RightAlt);
        if (_input.WasJustPressed(Keys.F11) || (altHeld && _input.WasJustPressed(Keys.Enter)))
        {
            // Trata a tela cheia à parte e sai cedo, senão o mesmo Enter também confirmaria algo neste quadro.
            ToggleFullscreen();
            base.Update(gameTime);
            return;
        }

        // As configurações abertas da pausa ficam por cima da corrida congelada (não viram fundo de menu).
        bool pausedSettings = _state == State.Settings && _settingsReturnState == State.Paused;
        if (IsMenuState(_state) && !pausedSettings)
        {
            // Idempotente: só dispara se o tema de menu ainda não estiver tocando.
            _audio.PlayMenuTheme();
            UpdateMenuBackgroundRace(frameSeconds);
        }

        if (_state != State.Paused && !pausedSettings)
        {
            _scenery.UpdateAmbient(WorldTheme, AreaWidth, AreaHeight, Math.Min(frameSeconds, 0.1f));
        }

        switch (_state)
        {
            case State.MainMenu:
                UpdateMainMenu();
                break;
            case State.ModeSelect:
                UpdateModeSelect();
                break;
            case State.TrackSelect:
            case State.SkinSelect:
                UpdateCarousel();
                break;
            case State.Racing:
                UpdateRacing(frameSeconds);
                break;
            case State.Paused:
                UpdatePaused();
                break;
            case State.Results:
                UpdateResults(frameSeconds);
                break;
            case State.Achievements:
                UpdateAchievementsPage();
                break;
            case State.Settings:
                UpdateSettings();
                break;
        }

        base.Update(gameTime);
    }

    /// <summary>Converte um ponto em pixels de tela (ex.: o mouse) pro espaço "lógico" em que o jogo é
    /// desenhado — a inversa exata de <see cref="BuildScreenTransform"/>.</summary>
    private Vector2 ScreenToLogicalPosition(Point screenPoint)
    {
        Matrix inverse = Matrix.Invert(BuildScreenTransform());
        return Vector2.Transform(new Vector2(screenPoint.X, screenPoint.Y), inverse);
    }

    private Point LogicalMousePoint()
    {
        Vector2 mouse = ScreenToLogicalPosition(_input.MousePosition);
        return new Point((int)mouse.X, (int)mouse.Y);
    }

    private static Rectangle InflateRect(Rectangle rect, float x, float y) =>
        new((int)(rect.X - x), (int)(rect.Y - y), (int)(rect.Width + (2 * x)), (int)(rect.Height + (2 * y)));

    protected override void Draw(GameTime gameTime)
    {
        TrackTheme theme = WorldTheme;

        // Texturas estáticas dos cenários ficam prontas antes de começar a desenhar (troca de render target
        // não pode acontecer no meio de um SpriteBatch).
        _scenery.Prepare(theme, _race.Track);

        GraphicsDevice.Clear(CarPainter.Darken(theme.Scenery.Background, 0.4f));
        Matrix transform = BuildScreenTransform();

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: transform);
        DrawWorld(theme);
        _spriteBatch.End();

        if (_scenery.HasLights(theme))
        {
            _spriteBatch.Begin(blendState: PremultipliedAdditive, samplerState: SamplerState.LinearClamp, transformMatrix: transform);
            _scenery.DrawLights(theme, _race.Track, _visualTime, CarLightSources());
            _spriteBatch.End();
        }

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: transform);
        switch (_state)
        {
            case State.MainMenu:
                DrawMainMenu();
                break;
            case State.ModeSelect:
                DrawModeSelect();
                break;
            case State.TrackSelect:
            case State.SkinSelect:
                DrawCarousel();
                break;
            case State.Racing:
                DrawRaceFeedback();
                DrawLiveHud();
                break;
            case State.Paused:
                DrawLiveHud();
                DrawPauseMenu();
                break;
            case State.Results:
                DrawResults();
                break;
            case State.Achievements:
                DrawAchievementsPage();
                break;
            case State.Settings:
                DrawSettingsBackdrop();
                DrawSettingsPopup();
                break;
        }

        DrawUnlockToast();
        _spriteBatch.End();
        base.Draw(gameTime);
    }

    private IEnumerable<(Vector2 Position, float Angle)> CarLightSources() =>
        _race.Entrants.Where(e => !e.Eliminated).Select(e => (new Vector2(e.Car.Position.X * CellSize, e.Car.Position.Y * CellSize), e.Car.Angle));

    /// <summary>
    /// Todo o jogo é desenhado numa resolução "lógica" fixa (<see cref="_windowWidth"/> x <see cref="_windowHeight"/>).
    /// Este método escala e centraliza essa cena no back buffer de verdade (igual em janela; em tela cheia,
    /// com letterbox), já com o tremor de câmera.
    /// </summary>
    private Matrix BuildScreenTransform()
    {
        int actualWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
        int actualHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;

        float scale = MathF.Min((float)actualWidth / _windowWidth, (float)actualHeight / _windowHeight);
        float offsetX = (actualWidth - (_windowWidth * scale)) / 2f;
        float offsetY = (actualHeight - (_windowHeight * scale)) / 2f;

        return Matrix.CreateTranslation(TrackMargin, TrackMargin, 0f)
            * Matrix.CreateScale(scale, scale, 1f)
            * Matrix.CreateTranslation(offsetX + (_shakeOffset.X * scale), offsetY + (_shakeOffset.Y * scale), 0f);
    }

    // ---------- Mundo (pista + carros) ----------

    /// <summary>O cenário e tudo o que está na pista, na ordem: chão/objetos → checkpoints → obstáculos →
    /// partículas → carros → clima → escuridão. As luzes vêm depois, num passe aditivo à parte.</summary>
    private void DrawWorld(TrackTheme theme)
    {
        _scenery.DrawBase(theme, _race.Track, _visualTime);
        DrawCheckpoints(theme);
        DrawHazards();
        DrawParticles();
        DrawCars();
        _scenery.DrawAmbient(theme, _visualTime);
        _scenery.DrawDarkness(theme, AreaWidth, AreaHeight);
    }

    /// <summary>Faixas de checkpoint pulsando na cor da pista. Durante a partida, o PRÓXIMO checkpoint do
    /// jogador pulsa mais forte e ganha setas — dá pra saber pra onde ir sem olhar o HUD.</summary>
    private void DrawCheckpoints(TrackTheme theme)
    {
        Track track = _race.Track;
        bool playing = _state is State.Racing or State.Paused && !_player.Eliminated && !_player.Finished;
        int next = _player.Car.NextCheckpointIndex;
        bool nextIsFinish = next > track.CheckpointCount;
        Color color = theme.Scenery.Checkpoint;
        float pulse = (MathF.Sin(_visualTime * 4f) + 1f) / 2f;

        for (int x = 0; x < track.Width; x++)
        {
            for (int y = 0; y < track.Height; y++)
            {
                int checkpoint = track.CheckpointAt(x, y);
                bool isFinish = track.IsFinishLine(x, y);
                if (checkpoint == 0 && !isFinish)
                {
                    continue;
                }

                bool highlighted = playing && ((checkpoint != 0 && checkpoint == next) || (isFinish && nextIsFinish));
                var rect = new Rectangle(x * CellSize, y * CellSize, CellSize, CellSize);
                if (highlighted)
                {
                    _spriteBatch.Draw(_pixel, rect, color * (0.35f + (0.3f * pulse)));
                }
                else if (!isFinish)
                {
                    _spriteBatch.Draw(_pixel, rect, color * (0.12f + (0.1f * pulse)));
                }
            }
        }

        if (playing)
        {
            Vector2D target = nextIsFinish ? track.FinishLineCenter : track.CheckpointCenters[next];
            var center = new Vector2(target.X * CellSize, target.Y * CellSize);
            float bob = MathF.Sin(_visualTime * 6f) * 3f;
            DrawChevron(center + new Vector2(0f, -30f + bob), color);
            PixelFont.DrawShadowed(_spriteBatch, _pixel, "PROXIMO", center + new Vector2(-PixelFont.Measure("PROXIMO", 1.2f) / 2f, -50f + bob), 1.2f, color);
        }
    }

    /// <summary>Seta pixelizada apontando pra baixo (marca o próximo checkpoint).</summary>
    private void DrawChevron(Vector2 tip, Color color)
    {
        for (int i = 0; i < 6; i++)
        {
            float half = 8f - (i * 1.4f);
            _spriteBatch.Draw(_pixel, new Rectangle((int)(tip.X - half), (int)(tip.Y - 10f + (i * 2f)), (int)(half * 2f), 2), color);
        }
    }

    private void DrawHazards()
    {
        foreach (Hazard hazard in _race.Hazards)
        {
            var center = new Vector2(hazard.Position.X * CellSize, hazard.Position.Y * CellSize);
            float radius = hazard.Radius * CellSize;

            DrawCircle(center + new Vector2(2f, 3f), radius, ShadowColor);
            DrawCircle(center, radius, HazardColor);
            DrawCircle(center, radius * 0.5f, HazardCapColor);
        }
    }

    private void DrawCars()
    {
        foreach (RaceEntrant entrant in _race.Entrants)
        {
            Car car = entrant.Car;
            bool eliminated = entrant.Eliminated;
            var center = new Vector2(car.Position.X * CellSize, car.Position.Y * CellSize);

            if (!eliminated && car.IsBoosting)
            {
                Vector2 flamePos = center + Rotate(new Vector2(-CellSize * 0.7f, 0f), car.Angle);
                DrawCircle(flamePos, CellSize * 0.34f, new Color(255, 140, 30, 220));
                DrawCircle(flamePos, CellSize * 0.2f, new Color(255, 230, 90, 230));
            }

            // Só o jogador usa a skin escolhida; os rivais usam o carro de corrida padrão, cada um na sua cor.
            // A skin é só visual — posição, ângulo e colisão vêm da mesma física de sempre.
            CarSkin skin = entrant.Kind == DriverKind.Human ? SelectedSkin : CarSkins.Default;
            _carPainter.Begin(center, car.Angle, CellSize, _carColors[entrant], eliminated, _visualTime);
            skin.Paint(_carPainter);
        }
    }

    private void DrawParticles()
    {
        foreach (Particle particle in _particles.Particles)
        {
            float lifeFraction = Math.Clamp(particle.Life / particle.MaxLife, 0f, 1f);
            DrawCircle(particle.Position, particle.Size * lifeFraction, particle.Color * lifeFraction);
        }
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

    private static Vector2 Rotate(Vector2 local, float angle)
    {
        float cos = MathF.Cos(angle);
        float sin = MathF.Sin(angle);
        return new Vector2((local.X * cos) - (local.Y * sin), (local.X * sin) + (local.Y * cos));
    }

    /// <summary>Retângulo com os quatro cantos arredondados (corpo em cruz + um círculo em cada canto).</summary>
    private void DrawRoundedRect(Rectangle rect, Color color, float radius)
    {
        radius = MathF.Max(0f, MathF.Min(radius, MathF.Min(rect.Width, rect.Height) / 2f));
        if (radius < 1f)
        {
            _spriteBatch.Draw(_pixel, rect, color);
            return;
        }

        int r = (int)radius;
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X + r, rect.Y, rect.Width - (2 * r), rect.Height), color);
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y + r, r, rect.Height - (2 * r)), color);
        _spriteBatch.Draw(_pixel, new Rectangle(rect.Right - r, rect.Y + r, r, rect.Height - (2 * r)), color);

        DrawCircle(new Vector2(rect.X + r, rect.Y + r), radius, color);
        DrawCircle(new Vector2(rect.Right - r, rect.Y + r), radius, color);
        DrawCircle(new Vector2(rect.X + r, rect.Bottom - r), radius, color);
        DrawCircle(new Vector2(rect.Right - r, rect.Bottom - r), radius, color);
    }
}
