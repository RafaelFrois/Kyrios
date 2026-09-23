using Kyrios.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Kyrios.Game;

/// <summary>Jogo de corrida visto de cima. Toda a física/regras vêm de Kyrios.Core; esta classe só desenha e lê o teclado.</summary>
public sealed partial class GameRoot : Microsoft.Xna.Framework.Game
{
    private const int CellSize = 22;
    private const int TrackMargin = 40;
    private const int CircleTextureSize = 32;
    private const float HudTextSize = 2f;

    private enum State
    {
        Intro,
        ModeSelect,
        Racing,
        Results,
        Settings,
        Achievements,
    }

    private enum SettingsRow
    {
        Music,
        Sfx,
    }

    // Paleta: os menus usam um slate/navy escuro (nada de verde ali), e a grama do circuito ganha duas
    // tonalidades alternadas (tipo grama cortada) em vez de um verde chapado só.
    private static readonly Color MenuBackground = new(21, 24, 34);
    private static readonly Color MenuBackgroundDim = new(21, 24, 34, 205);
    private static readonly Color BackgroundGrass = new(41, 112, 68);
    private static readonly Color GrassCellColorA = new(41, 112, 68);
    private static readonly Color GrassCellColorB = new(47, 124, 76);
    private static readonly Color TrackColor = new(46, 48, 58);
    private static readonly Color TextColor = Color.White;
    private static readonly Color AccentColor = new(255, 200, 40);
    private static readonly Color CurbBase = new(44, 40, 40);
    private static readonly Color CurbRed = new(205, 54, 46);
    private static readonly Color CurbWhite = new(232, 230, 224);
    private static readonly Color BushDark = new(22, 86, 52);
    private static readonly Color BushLight = new(44, 130, 78);
    private static readonly Color ShadowColor = new(0, 0, 0, 90);
    private static readonly Color PanelBorderColor = new(54, 60, 80);
    private static readonly Color PanelFillColor = new(33, 38, 53);
    private static readonly Color OverlayDimColor = new(8, 9, 14, 195);
    private static readonly Color HazardColor = new(230, 122, 40);
    private static readonly Color HazardCapColor = new(35, 34, 38);
    private static readonly Color HudFrameFill = new(16, 18, 27, 225);
    private static readonly Color HudFrameDivider = new(255, 200, 40, 90);
    private static readonly Color StatBadgeFill = new(15, 32, 24);
    private static readonly Color StatBadgeLabelColor = new(150, 158, 175);
    private static readonly Color StudioLogoColor = new(225, 228, 238, 205);

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
    private AudioManager _audio = null!;
    private readonly ParticleSystem _particles = new();
    private readonly Random _particleRandom = new();
    private readonly Random _shakeRandom = new();

    private float _visualTime;
    private float _shakeTimer;
    private float _shakeDuration;
    private float _shakeMagnitude;
    private Vector2 _shakeOffset;

    private RaceSimulation _race = null!;
    private RaceEntrant _player = null!;
    private State _state = State.Intro;
    private RaceMode _selectedMode = RaceMode.Sprint;
    private SaveData _saveData = null!;
    private bool _recordsProcessed;
    private bool _newLapRecord;
    private bool _newRaceRecord;
    private bool _newScoreRecord;
    private bool _playerWasCollidingLastTick;
    private int _lastCountdownTickSecond = int.MaxValue;
    private State _settingsReturnState = State.Intro;
    private SettingsRow _settingsSelection = SettingsRow.Music;
    private SettingsRow? _draggingSettingsRow;

    private CarPainter _carPainter = null!;

    // A skin equipada (a que vai pra corrida) e a que está sendo olhada no seletor são coisas separadas:
    // dá pra navegar até uma skin bloqueada pra ver o requisito sem que ela vire a do carro.
    private int _selectedSkinIndex;
    private int _previewSkinIndex;
    private float _skinArrowFlashLeft;
    private float _skinArrowFlashRight;

    private readonly RaceTracker _raceTracker = new();
    private AchievementIconRenderer _iconRenderer = null!;

    /// <summary>Uma notificação na fila: um desbloqueio (conquista ou skin) ou, quando muita coisa é liberada
    /// de uma vez (ex.: um save antigo aberto pela primeira vez), um resumo só.</summary>
    private sealed record UnlockToast(UnlockNotice Notice, int SummaryAchievements = 0, int SummarySkins = 0)
    {
        public bool IsSummary => Notice is null;
        public bool IsSkin => Notice?.Skin is not null;
    }

    private const float AchievementToastDuration = 3.4f;
    private const float SkinToastDuration = 4.2f;
    private const float QuickToastDuration = 2.4f;
    private const float UnlockToastSlideSeconds = 0.3f;

    /// <summary>A partir de quantos desbloqueios simultâneos a fila vira um resumo único.</summary>
    private const int ToastSummaryThreshold = 5;

    private readonly Queue<UnlockToast> _pendingUnlockToasts = new();
    private UnlockToast _activeUnlockToast;
    private float _unlockToastTimer;
    private float _unlockToastDuration;

    private State _achievementsReturnState = State.Intro;

    /// <summary>Aba da página de conquistas: 0 = todas, 1.. = cada categoria em <see cref="ProgressionStyle.CategoryOrder"/>.</summary>
    private int _achievementTab;
    private int _achievementScrollRow;

    private int _windowWidth;
    private int _windowHeight;
    private bool _isFullscreen;

    public GameRoot()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.Title = "MegRace - Corrida Top-Vision";

        // Tela cheia sem trocar o modo de vídeo (borderless) — mais confiável entre monitores/placas
        // diferentes, e como a gente já escala/centraliza a cena sozinho (ver Draw), não precisa da
        // troca de resolução de verdade.
        _graphics.HardwareModeSwitch = false;

        _saveData = SaveData.Load();

        // Progresso que já cumpre algum requisito (ex.: um save de antes das conquistas) libera conquistas e
        // skins logo ao abrir, com a notificação de sempre. Depois disso, se a skin salva não estiver liberada
        // (ou for o primeiro acesso), o jogador começa com a clássica.
        CheckUnlocks();
        _selectedSkinIndex = CarSkins.IndexOf(_saveData.SelectedSkinId);
        if (!SkinUnlocks.IsUnlocked(SelectedSkin, _saveData))
        {
            _selectedSkinIndex = 0;
        }

        _previewSkinIndex = _selectedSkinIndex;

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
        _playerWasCollidingLastTick = false;
        _lastCountdownTickSecond = int.MaxValue;
        _raceTracker.Reset(_race);
        _particles.Clear();
        _shakeTimer = 0f;
        _shakeOffset = Vector2.Zero;

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

        // Em tela cheia o back buffer já ocupa a tela toda (a cena escala/centraliza sozinha no Draw);
        // só reaplica o tamanho de janela de verdade quando estiver mesmo em modo janela.
        if (!_isFullscreen)
        {
            _graphics.ApplyChanges();
        }
    }

    /// <summary>Mantém a "corrida" que serve de fundo animado dos menus sempre andando: atualiza a
    /// simulação sem nenhum input do jogador (as IAs dirigem por conta própria) e reinicia
    /// silenciosamente quando ela termina, sem passar pelas telas de resultado — é só decoração visual,
    /// nunca dispara som de colisão/motor nem afeta recordes salvos.</summary>
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
    /// (<see cref="Progression.RecordRace"/>) → conquistas e skins conferidas e liberadas → salva → notifica.</summary>
    private void ProcessRaceEndRecords()
    {
        if (_recordsProcessed)
        {
            return;
        }

        _recordsProcessed = true;

        RaceReport report = _raceTracker.BuildReport(_race, _player, SelectedSkin.Id);
        RecordFlags records = Progression.RecordRace(_saveData, report);
        _newLapRecord = records.NewLapRecord;
        _newRaceRecord = records.NewRaceRecord;
        _newScoreRecord = records.NewScoreRecord;

        _saveData.Save();
        CheckUnlocks();
    }

    /// <summary>Libera as conquistas e skins que o progresso atual já cumpre, salva e enfileira os avisos.
    /// Não sabe nada de modos de jogo — só compara o progresso salvo com os catálogos.</summary>
    private void CheckUnlocks()
    {
        List<UnlockNotice> notices = Progression.CheckUnlocks(_saveData);
        if (notices.Count == 0)
        {
            return;
        }

        _saveData.Save();

        if (notices.Count >= ToastSummaryThreshold)
        {
            _pendingUnlockToasts.Enqueue(new UnlockToast(
                null,
                SummaryAchievements: notices.Count(n => n.Achievement is not null),
                SummarySkins: notices.Count(n => n.Skin is not null)));
            return;
        }

        foreach (UnlockNotice notice in notices)
        {
            _pendingUnlockToasts.Enqueue(new UnlockToast(notice));
        }
    }

    /// <summary>Mostra os avisos de desbloqueio um de cada vez, em qualquer tela (normalmente por cima do
    /// pop-up de resultado, ou da tela inicial quando o desbloqueio vem de um save antigo). Com fila
    /// comprida, cada aviso fica menos tempo.</summary>
    private void UpdateUnlockToast(float frameSeconds)
    {
        if (_activeUnlockToast is null && _pendingUnlockToasts.Count > 0)
        {
            _activeUnlockToast = _pendingUnlockToasts.Dequeue();
            _unlockToastDuration = _pendingUnlockToasts.Count >= 2 ? QuickToastDuration
                : _activeUnlockToast.IsSkin || _activeUnlockToast.IsSummary ? SkinToastDuration
                : AchievementToastDuration;
            _unlockToastTimer = _unlockToastDuration;
            _audio?.PlayUnlock(isSkin: !_activeUnlockToast.IsSummary && _activeUnlockToast.IsSkin);
        }

        if (_activeUnlockToast is null)
        {
            return;
        }

        _unlockToastTimer -= frameSeconds;
        if (_unlockToastTimer <= 0f)
        {
            _activeUnlockToast = null;
        }
    }

    /// <summary>Decide qual dos três jingles de resultado combina com o desfecho da corrida pro jogador.</summary>
    private RaceOutcome DetermineOutcome()
    {
        switch (_race.Mode)
        {
            case RaceMode.Elimination:
                return _player.Finished ? RaceOutcome.Victory : RaceOutcome.Defeat;

            case RaceMode.TimeAttack:
                return _newScoreRecord ? RaceOutcome.Victory : RaceOutcome.Neutral;

            default:
                int place = _player.FinishPlace ?? _race.Entrants.Count;
                if (place == 1)
                {
                    return RaceOutcome.Victory;
                }

                return place == _race.Entrants.Count ? RaceOutcome.Defeat : RaceOutcome.Neutral;
        }
    }

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

    /// <summary>Poeira ao sair da pista, faíscas na colisão e um rastro atrás do carro ao usar turbo — nada
    /// de novas texturas, só o mesmo círculo procedural de sempre, em pequeno, com vida curta.</summary>
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

            bool offTrack = _race.Track.IsOffTrack((int)MathF.Floor(car.Position.X), (int)MathF.Floor(car.Position.Y));
            if (offTrack && MathF.Abs(car.Speed) > 2f && _particleRandom.NextDouble() < 0.5)
            {
                Vector2 dustVelocity = (-forward * car.Speed * 0.3f) + RandomSpread(30f);
                _particles.Spawn(worldPos, dustVelocity, life: 0.5f, size: CellSize * 0.35f, new Color(150, 130, 90, 160));
            }

            if (car.IsBoosting)
            {
                Vector2 boostVelocity = (-forward * 40f) + RandomSpread(20f);
                Vector2 spawnPos = worldPos - (forward * CellSize * 0.7f);
                _particles.Spawn(spawnPos, boostVelocity, life: 0.35f, size: CellSize * 0.22f, new Color(255, 160, 60, 200));
            }

            bool colliding = car.HadHeadOnCollisionThisTick || car.HadCarCollisionThisTick || car.HadHazardCollisionThisTick;
            if (colliding)
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

    private void SizeWindowForCurrentRace()
    {
        int trackPixelWidth = _race.Track.Width * CellSize;
        int trackPixelHeight = _race.Track.Height * CellSize;

        // Sem rodapé de HUD pra acomodar, a janela agora só precisa caber a pista + a margem decorativa
        // ao redor — não depende mais do modo escolhido nem de quantos carros estão correndo. Isso é a
        // resolução "lógica" usada em todo o layout; em tela cheia o back buffer de verdade é maior
        // (tamanho do monitor) e a cena é escalada/centralizada até caber (ver Draw).
        _windowWidth = Math.Max(trackPixelWidth + (2 * TrackMargin), 640);
        _windowHeight = trackPixelHeight + (2 * TrackMargin);

        if (!_isFullscreen)
        {
            _graphics.PreferredBackBufferWidth = _windowWidth;
            _graphics.PreferredBackBufferHeight = _windowHeight;
        }
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
        _skinArrowFlashLeft = MathF.Max(0f, _skinArrowFlashLeft - frameSeconds);
        _skinArrowFlashRight = MathF.Max(0f, _skinArrowFlashRight - frameSeconds);
        UpdateUnlockToast(frameSeconds);

        bool altHeld = _input.IsDown(Keys.LeftAlt) || _input.IsDown(Keys.RightAlt);
        if (_input.WasJustPressed(Keys.F11) || (altHeld && _input.WasJustPressed(Keys.Enter)))
        {
            // Trata a tecla de tela cheia à parte e sai cedo, senão o mesmo Enter também dispararia
            // "confirmar" na seleção de modo ou "jogar de novo" nos resultados neste mesmo quadro.
            ToggleFullscreen();
            base.Update(gameTime);
            return;
        }

        if (_state is State.Intro or State.ModeSelect or State.Settings or State.Achievements)
        {
            // Chamada idempotente: só entra em ação se o tema de menu ainda não estiver tocando, então é
            // seguro chamar em todo quadro sem reiniciar a música toda vez. Continua tocando com o pop-up
            // de configurações aberto por cima, pra dar pra ouvir o volume mudando na hora.
            _audio.PlayMenuTheme();
            UpdateMenuBackgroundRace(frameSeconds);
        }

        switch (_state)
        {
            case State.Intro:
                if (_input.WasJustPressed(Keys.Escape))
                {
                    Exit();
                    return;
                }

                if (_input.WasJustPressed(Keys.Q))
                {
                    OpenSettings(State.Intro);
                    break;
                }

                if (_input.WasJustPressed(Keys.C) || WasAchievementsButtonClicked())
                {
                    OpenAchievements(State.Intro);
                    break;
                }

                if (TryChangeSkinFromInput())
                {
                    break;
                }

                // F11 (tela cheia), Q (opções), C (conquistas) e as teclas de trocar skin nunca contam como
                // "aperte qualquer tecla" aqui — senão usá-las na tela inicial também avançaria pro menu.
                if (_input.AnyKeyJustPressedExcept(Keys.F11, Keys.Q, Keys.C, Keys.Left, Keys.Right, Keys.A, Keys.D))
                {
                    // Se o jogador estava só espiando uma skin bloqueada, na volta o seletor mostra a equipada.
                    _previewSkinIndex = _selectedSkinIndex;
                    _state = State.ModeSelect;
                }

                break;

            case State.ModeSelect:
                if (_input.WasJustPressed(Keys.Escape))
                {
                    _state = State.Intro;
                    break;
                }

                if (_input.WasJustPressed(Keys.Q))
                {
                    OpenSettings(State.ModeSelect);
                    break;
                }

                if (_input.WasJustPressed(Keys.C))
                {
                    OpenAchievements(State.ModeSelect);
                    break;
                }

                if (_input.WasJustPressed(Keys.Right) || _input.WasJustPressed(Keys.D)
                    || _input.WasJustPressed(Keys.Down) || _input.WasJustPressed(Keys.S))
                {
                    _selectedMode = NextMode(_selectedMode);
                    _audio.PlayMenuMove();
                }
                else if (_input.WasJustPressed(Keys.Left) || _input.WasJustPressed(Keys.A)
                    || _input.WasJustPressed(Keys.Up) || _input.WasJustPressed(Keys.W))
                {
                    _selectedMode = PreviousMode(_selectedMode);
                    _audio.PlayMenuMove();
                }

                if (_input.WasJustPressed(Keys.Space) || _input.WasJustPressed(Keys.Enter))
                {
                    StartNewRaceAndResize(_selectedMode);
                    _audio.PlayMenuConfirm();
                    _audio.PlayMusic(_selectedMode);
                    _audio.StartEngine();
                    _state = State.Racing;
                }

                break;

            case State.Racing:
                if (_input.WasJustPressed(Keys.Escape))
                {
                    _audio.StopMusic();
                    _audio.StopEngine();
                    _state = State.Intro;
                    break;
                }

                float dt = Math.Min(frameSeconds, 0.1f);
                _race.Update(dt, _input.BuildCarInput());
                _raceTracker.Observe(_race, _player, dt);

                _audio.UpdateEngine(_player.Car.Speed, _player.Car.Settings.MaxForwardSpeed, _player.Car.IsBoosting);
                UpdateParticleSpawns();
                _particles.Update(dt);

                bool collidingNow = _player.Car.HadHeadOnCollisionThisTick
                    || _player.Car.HadCarCollisionThisTick
                    || _player.Car.HadHazardCollisionThisTick;
                if (collidingNow && !_playerWasCollidingLastTick)
                {
                    _audio.PlayCollision();
                    TriggerScreenShake(_player.Car.HadHeadOnCollisionThisTick ? 6f : 3.5f);
                }

                _playerWasCollidingLastTick = collidingNow;

                if (_player.Car.CheckpointCrossedThisTick)
                {
                    _audio.PlayCheckpoint();
                }

                if (_race.Mode == RaceMode.TimeAttack && _race.TimeRemaining is { } timeRemaining && timeRemaining <= 3f)
                {
                    int currentSecond = Math.Max(0, (int)MathF.Ceiling(timeRemaining));
                    if (currentSecond >= 1 && currentSecond != _lastCountdownTickSecond)
                    {
                        _audio.PlayCountdownTick();
                        _lastCountdownTickSecond = currentSecond;
                    }
                }

                if (_race.IsRaceOver)
                {
                    _audio.StopMusic();
                    _audio.StopEngine();
                    ProcessRaceEndRecords();
                    _audio.PlayResultJingle(DetermineOutcome());
                    _state = State.Results;
                }

                break;

            case State.Results:
                if (_input.WasJustPressed(Keys.Escape))
                {
                    _state = State.Intro;
                    break;
                }

                if (_input.WasJustPressed(Keys.R) || _input.WasJustPressed(Keys.Space) || _input.WasJustPressed(Keys.Enter))
                {
                    StartNewRaceAndResize(_race.Mode);
                    _audio.PlayMusic(_race.Mode);
                    _audio.StartEngine();
                    _state = State.Racing;
                }
                else if (_input.WasJustPressed(Keys.M))
                {
                    _state = State.ModeSelect;
                }

                break;

            case State.Settings:
                if (_input.WasJustPressed(Keys.Escape))
                {
                    _audio.PlayMenuConfirm();
                    _state = _settingsReturnState;
                    _draggingSettingsRow = null;
                    break;
                }

                UpdateSettingsMouseDrag();

                if (_input.WasJustPressed(Keys.Up) || _input.WasJustPressed(Keys.W)
                    || _input.WasJustPressed(Keys.Down) || _input.WasJustPressed(Keys.S))
                {
                    _settingsSelection = _settingsSelection == SettingsRow.Music ? SettingsRow.Sfx : SettingsRow.Music;
                    _audio.PlayMenuMove();
                }
                else if (_input.WasJustPressed(Keys.Right) || _input.WasJustPressed(Keys.D))
                {
                    AdjustSelectedVolume(0.1f);
                }
                else if (_input.WasJustPressed(Keys.Left) || _input.WasJustPressed(Keys.A))
                {
                    AdjustSelectedVolume(-0.1f);
                }

                if (_input.WasJustPressed(Keys.Enter) || _input.WasJustPressed(Keys.Space))
                {
                    ToggleSelectedMute();
                }

                break;

            case State.Achievements:
                UpdateAchievementsPage();
                break;
        }

        base.Update(gameTime);
    }

    private CarSkin SelectedSkin => CarSkins.All[_selectedSkinIndex];

    /// <summary>Seletor de skin da tela inicial: setas/A-D do teclado ou clique nas setinhas desenhadas.
    /// Retorna true se trocou (pra essa mesma tecla/clique não ser tratada como mais nada no quadro).</summary>
    private bool TryChangeSkinFromInput()
    {
        if (_input.WasJustPressed(Keys.Left) || _input.WasJustPressed(Keys.A))
        {
            ChangeSkin(-1);
            return true;
        }

        if (_input.WasJustPressed(Keys.Right) || _input.WasJustPressed(Keys.D))
        {
            ChangeSkin(1);
            return true;
        }

        if (!_input.WasMouseLeftJustPressed)
        {
            return false;
        }

        IntroLayout layout = ComputeIntroLayout();
        Vector2 mouse = ScreenToLogicalPosition(_input.MousePosition);
        var mousePoint = new Point((int)mouse.X, (int)mouse.Y);

        if (layout.LeftArrow.Contains(mousePoint))
        {
            ChangeSkin(-1);
            return true;
        }

        if (layout.RightArrow.Contains(mousePoint))
        {
            ChangeSkin(1);
            return true;
        }

        return false;
    }

    /// <summary>Navega pelo catálogo inteiro (as bloqueadas aparecem, pra mostrar o requisito), mas só
    /// equipa — e salva — quando a skin mostrada está liberada.</summary>
    private void ChangeSkin(int direction)
    {
        int count = CarSkins.All.Count;
        _previewSkinIndex = (_previewSkinIndex + direction + count) % count;

        if (SkinUnlocks.IsUnlocked(CarSkins.All[_previewSkinIndex], _saveData))
        {
            _selectedSkinIndex = _previewSkinIndex;
            _saveData.SelectedSkinId = SelectedSkin.Id;
            _saveData.Save();
        }

        _audio.PlayMenuMove();

        if (direction < 0)
        {
            _skinArrowFlashLeft = 0.15f;
        }
        else
        {
            _skinArrowFlashRight = 0.15f;
        }
    }

    /// <summary>Abre o pop-up de configurações lembrando de qual tela (inicial ou menu de modos) ele foi
    /// aberto, pra voltar exatamente pra lá quando fechar.</summary>
    private void OpenSettings(State returnState)
    {
        _settingsReturnState = returnState;
        _state = State.Settings;
        _audio.PlayMenuConfirm();
    }

    private void AdjustSelectedVolume(float delta)
    {
        if (_settingsSelection == SettingsRow.Music)
        {
            _audio.SetMusicVolume(_audio.MusicVolume + delta);
            _saveData.MusicVolume = _audio.MusicVolume;
        }
        else
        {
            _audio.SetSfxVolume(_audio.SfxVolume + delta);
            _saveData.SfxVolume = _audio.SfxVolume;
        }

        _saveData.Save();
        _audio.PlayMenuMove();
    }

    private void ToggleSelectedMute()
    {
        if (_settingsSelection == SettingsRow.Music)
        {
            _audio.SetMusicMuted(!_audio.MusicMuted);
            _saveData.MusicMuted = _audio.MusicMuted;
        }
        else
        {
            _audio.SetSfxMuted(!_audio.SfxMuted);
            _saveData.SfxMuted = _audio.SfxMuted;
        }

        _saveData.Save();
        _audio.PlayMenuConfirm();
    }

    /// <summary>Clicar ou arrastar com o mouse numa das barras de volume do pop-up de configurações faz a
    /// mesma coisa que as setas/WASD, só que direto pra posição apontada em vez de incremento — e destrava
    /// o mudo automaticamente, do mesmo jeito que um controle de volume de sistema.</summary>
    private void UpdateSettingsMouseDrag()
    {
        (Rectangle _, Rectangle musicBar, Rectangle sfxBar) = ComputeSettingsLayout();
        Vector2 mouseLogical = ScreenToLogicalPosition(_input.MousePosition);
        var mousePoint = new Point((int)mouseLogical.X, (int)mouseLogical.Y);

        if (_input.IsMouseLeftDown && _draggingSettingsRow is null)
        {
            if (InflateRect(musicBar, 6f, 8f).Contains(mousePoint))
            {
                _draggingSettingsRow = SettingsRow.Music;
                _settingsSelection = SettingsRow.Music;
            }
            else if (InflateRect(sfxBar, 6f, 8f).Contains(mousePoint))
            {
                _draggingSettingsRow = SettingsRow.Sfx;
                _settingsSelection = SettingsRow.Sfx;
            }
        }

        if (_draggingSettingsRow is not { } draggingRow)
        {
            return;
        }

        if (!_input.IsMouseLeftDown)
        {
            _draggingSettingsRow = null;
            _saveData.Save();
            return;
        }

        Rectangle bar = draggingRow == SettingsRow.Music ? musicBar : sfxBar;
        float fraction = bar.Width > 0 ? Math.Clamp((mouseLogical.X - bar.X) / bar.Width, 0f, 1f) : 0f;
        SetSelectedVolumeAbsolute(draggingRow, fraction);
    }

    private void SetSelectedVolumeAbsolute(SettingsRow row, float fraction)
    {
        if (row == SettingsRow.Music)
        {
            _audio.SetMusicMuted(false);
            _audio.SetMusicVolume(fraction);
            _saveData.MusicMuted = false;
            _saveData.MusicVolume = fraction;
        }
        else
        {
            _audio.SetSfxMuted(false);
            _audio.SetSfxVolume(fraction);
            _saveData.SfxMuted = false;
            _saveData.SfxVolume = fraction;
        }
    }

    /// <summary>Converte um ponto em pixels de tela (ex.: posição do mouse) pro espaço de coordenadas
    /// "lógico" em que todo o resto do jogo é desenhado — a inversa exata da transformação usada no
    /// SpriteBatch.Begin (ver BuildScreenTransform), incluindo escala e centralização da tela cheia.</summary>
    private Vector2 ScreenToLogicalPosition(Point screenPoint)
    {
        Matrix inverse = Matrix.Invert(BuildScreenTransform());
        return Vector2.Transform(new Vector2(screenPoint.X, screenPoint.Y), inverse);
    }

    private static Rectangle InflateRect(Rectangle rect, float x, float y) =>
        new((int)(rect.X - x), (int)(rect.Y - y), (int)(rect.Width + (2 * x)), (int)(rect.Height + (2 * y)));

    protected override void Draw(GameTime gameTime)
    {
        bool isMenuState = _state is State.Intro or State.ModeSelect or State.Settings or State.Achievements;
        GraphicsDevice.Clear(isMenuState ? MenuBackground : BackgroundGrass);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: BuildScreenTransform());

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
                DrawHazards();
                DrawParticles();
                DrawCars();
                DrawLiveHud();
                break;

            case State.Results:
                DrawTrack();
                DrawHazards();
                DrawParticles();
                DrawCars();
                DrawResultsPopup();
                break;

            case State.Settings:
                if (_settingsReturnState == State.ModeSelect)
                {
                    DrawModeSelect();
                }
                else
                {
                    DrawIntro();
                }

                DrawSettingsPopup();
                break;

            case State.Achievements:
                DrawAchievementsPage();
                break;
        }

        DrawUnlockToast();

        _spriteBatch.End();
        base.Draw(gameTime);
    }

    /// <summary>
    /// Todo o jogo é desenhado numa resolução "lógica" fixa (<see cref="_windowWidth"/> x
    /// <see cref="_windowHeight"/>, do tamanho da pista atual). Esse método escala e centraliza essa cena
    /// pra caber no back buffer de verdade — que em modo janela é do mesmo tamanho (escala 1, sem
    /// diferença visual), mas em tela cheia é do tamanho do monitor. Preserva a proporção (letterbox/
    /// pillarbox) em vez de esticar, então layout, pop-up e HUD ficam idênticos nos dois casos.
    /// </summary>
    private Matrix BuildScreenTransform()
    {
        int actualWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
        int actualHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;

        float scale = MathF.Min((float)actualWidth / _windowWidth, (float)actualHeight / _windowHeight);
        float offsetX = (actualWidth - (_windowWidth * scale)) / 2f;
        float offsetY = (actualHeight - (_windowHeight * scale)) / 2f;

        // O tremor de câmera é definido em pixels "lógicos" (antes da escala), então precisa escalar
        // junto pra parecer do mesmo tamanho relativo em janela ou em tela cheia.
        return Matrix.CreateTranslation(TrackMargin, TrackMargin, 0f)
            * Matrix.CreateScale(scale, scale, 1f)
            * Matrix.CreateTranslation(offsetX + (_shakeOffset.X * scale), offsetY + (_shakeOffset.Y * scale), 0f);
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

    /// <summary>Retângulo com os quatro cantos arredondados (corpo em cruz + um círculo em cada canto) —
    /// só funciona bem com cores opacas, já que os círculos dos cantos se sobrepõem levemente ao corpo.</summary>
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

    private void DrawParticles()
    {
        foreach (Particle particle in _particles.Particles)
        {
            float lifeFraction = Math.Clamp(particle.Life / particle.MaxLife, 0f, 1f);
            var color = new Color(particle.Color.R, particle.Color.G, particle.Color.B, (byte)(particle.Color.A * lifeFraction));
            DrawCircle(particle.Position, particle.Size * lifeFraction, color);
        }
    }

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
                        _spriteBatch.Draw(_pixel, rect, GrassColorAt(x, y));
                        deepMinX = Math.Min(deepMinX, x);
                        deepMaxX = Math.Max(deepMaxX, x);
                        deepMinY = Math.Min(deepMinY, y);
                        deepMaxY = Math.Max(deepMaxY, y);
                    }

                    continue;
                }

                Color baseColor = cell == ',' ? GrassColorAt(x, y) : TrackColor;
                _spriteBatch.Draw(_pixel, rect, baseColor);

                if (cell == 'S')
                {
                    DrawFinishChecker(rect);
                }
                else if (cell is >= '1' and <= '9')
                {
                    // Pulsa suavemente em vez de ficar num tom fixo — fica mais claro que é uma zona
                    // "viva" do circuito, não só uma pintura estática no chão.
                    int pulseAlpha = (int)(70 + (MathF.Sin(_visualTime * 4f) * 35f));
                    _spriteBatch.Draw(_pixel, rect, new Color(255, 215, 0, pulseAlpha));
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

    /// <summary>Textura simples de "grama cortada": duas tonalidades em xadrez em vez de um verde chapado só.</summary>
    private static Color GrassColorAt(int x, int y) => (x + y) % 2 == 0 ? GrassCellColorA : GrassCellColorB;

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

        // Bandeira balançando: um leve ângulo oscilante em vez de um retângulo parado.
        float flutterAngle = MathF.Sin(_visualTime * 6f) * 0.18f;
        float flagWidth = CellSize * 0.9f;
        var flagCenter = new Vector2(cx + 2f + (flagWidth / 2f), cy - (CellSize * 1.35f));
        DrawFilledRectRotated(flagCenter, flagWidth, CellSize * 0.55f, flutterAngle, AccentColor);
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

    // ---------- Obstáculos (Contrarrelógio) ----------

    private void DrawHazards()
    {
        if (_race.Mode != RaceMode.TimeAttack)
        {
            return;
        }

        foreach (Hazard hazard in _race.Hazards)
        {
            var center = new Vector2(hazard.Position.X * CellSize, hazard.Position.Y * CellSize);
            float radius = hazard.Radius * CellSize;

            DrawCircle(center + new Vector2(2f, 3f), radius, ShadowColor);
            DrawCircle(center, radius, HazardColor);
            DrawCircle(center, radius * 0.5f, HazardCapColor);
        }
    }

    // ---------- Carros ----------

    private void DrawCars()
    {
        foreach (RaceEntrant entrant in _race.Entrants)
        {
            Car car = entrant.Car;
            bool eliminated = entrant.Eliminated;
            var center = new Vector2(car.Position.X * CellSize, car.Position.Y * CellSize);

            if (!eliminated && car.IsBoosting)
            {
                DrawBoostFlame(center, car.Angle, CellSize * 1.35f / 2f);
            }

            // Só o jogador usa a skin escolhida no menu; os rivais continuam com o carro clássico, cada um
            // na sua cor. A skin é só visual — posição, ângulo e colisão vêm da mesma física de sempre.
            CarSkin skin = entrant.Kind == DriverKind.Human ? SelectedSkin : CarSkins.Classic;
            _carPainter.Begin(center, car.Angle, CellSize, _carColors[entrant], eliminated, _visualTime);
            skin.Paint(_carPainter);
        }
    }

    private void DrawBoostFlame(Vector2 center, float angle, float halfLength)
    {
        Vector2 flamePos = center + Rotate(new Vector2(-halfLength * 1.05f, 0f), angle);
        DrawCircle(flamePos, CellSize * 0.34f, new Color(255, 140, 30, 220));
        DrawCircle(flamePos, CellSize * 0.2f, new Color(255, 230, 90, 230));
    }

    // ---------- HUD flutuante durante a corrida ----------

    /// <summary>Mini-HUD discreto sobreposto no canto superior esquerdo da pista — sem rodapé, sem lista
    /// de classificação. O resultado completo só aparece no pop-up quando a corrida termina.</summary>
    private void DrawLiveHud()
    {
        const float padding = 10f;

        bool warn = _race.Mode switch
        {
            RaceMode.TimeAttack => (_race.TimeRemaining ?? 99f) <= 5f,
            RaceMode.Elimination => !_player.Eliminated && IsLastPlaceActive(_player),
            _ => false,
        };
        Color primaryColor = warn ? new Color(255, 110, 90) : TextColor;

        string primary = _race.Mode switch
        {
            RaceMode.TimeAttack => $"TEMPO {(_race.TimeRemaining ?? 0f):0.0}s   PONTOS {_player.Score:0}",
            RaceMode.Elimination => $"VOLTA {_player.Car.LapsCompleted + 1}   RESTAM {_race.Entrants.Count(e => !e.Eliminated)}",
            _ => $"VOLTA {Math.Min(_player.Car.LapsCompleted + 1, _race.TargetLaps)}/{_race.TargetLaps}   {TimeFormat.Precise(_player.Car.CurrentLapTime)}",
        };

        float textWidth = PixelFont.Measure(primary, HudTextSize);
        float barWidth = MathF.Max(textWidth + (padding * 2f), 250f);
        float barHeight = PixelFont.LineHeight(HudTextSize) + (padding * 2f);

        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, (int)barWidth, (int)barHeight), new Color(14, 16, 23, 175));
        _spriteBatch.Draw(_pixel, new Rectangle(0, (int)barHeight - 3, (int)barWidth, 3), AccentColor);
        PixelFont.DrawShadowed(_spriteBatch, _pixel, primary, new Vector2(padding, padding), HudTextSize, primaryColor);

        DrawBoostBar(new Vector2(padding, barHeight + 10f));
    }

    private bool IsLastPlaceActive(RaceEntrant entrant)
    {
        List<RaceEntrant> active = [.. _race.GetStandings().Where(e => !e.Eliminated && !e.Finished)];
        return active.Count > 0 && ReferenceEquals(active[^1], entrant);
    }

    private void DrawBoostBar(Vector2 position)
    {
        const float width = 150f;
        const float height = 14f;
        float x = position.X;
        float y = position.Y;

        var backRect = new Rectangle((int)x - 2, (int)y - 2, (int)width + 4, (int)height + 4);
        _spriteBatch.Draw(_pixel, backRect, new Color(10, 40, 20));

        float fraction = Math.Clamp(_player.Car.BoostFuel / _player.Car.Settings.BoostMaxFuel, 0f, 1f);
        var fillRect = new Rectangle((int)x, (int)y, (int)(width * fraction), (int)height);
        Color fillColor = _player.Car.IsBoosting ? BoostActiveColor : BoostFillColor;
        _spriteBatch.Draw(_pixel, fillRect, fillColor);

        const string label = "TURBO (SHIFT)";
        PixelFont.DrawShadowed(_spriteBatch, _pixel, label, new Vector2(x, y + height + 6f), 1.5f, TextColor);
    }

    // ---------- Pop-up de resultado ----------

    private void DrawResultsPopup()
    {
        float trackAreaWidth = _race.Track.Width * CellSize;
        float trackAreaHeight = _race.Track.Height * CellSize;

        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, (int)trackAreaWidth, (int)trackAreaHeight), OverlayDimColor);

        (string headline, List<string> lines) = BuildResultsContent();

        const float headerSize = 3.5f;
        const float lineSize = 2f;
        const float promptSize = 1.75f;
        const float lineGap = 12f;
        const float panelPaddingV = 24f;
        const float panelPaddingH = 34f;
        const string prompt = "ESPACO: JOGAR DE NOVO    M: MENU PRINCIPAL    ESC: TELA INICIAL";

        // Painel largo o bastante pro texto mais comprido (título, linha de estatística ou o rodapé de
        // instruções), nunca cortando nada, mas sem passar de um teto proporcional à pista nem de um
        // mínimo — assim funciona bem tanto pra um resultado com poucas linhas quanto pra um mais extenso.
        float widestContent = PixelFont.Measure(headline, headerSize);
        foreach (string line in lines)
        {
            widestContent = MathF.Max(widestContent, PixelFont.Measure(line, lineSize));
        }

        widestContent = MathF.Max(widestContent, PixelFont.Measure(prompt, promptSize));
        float panelWidth = Math.Clamp(widestContent + (panelPaddingH * 2f), 340f, trackAreaWidth - 80f);

        float dividerGap = 18f;
        float contentHeight = PixelFont.LineHeight(headerSize) + 22f
            + (lines.Count * (PixelFont.LineHeight(lineSize) + lineGap))
            + dividerGap
            + PixelFont.LineHeight(promptSize);

        float panelHeight = contentHeight + (panelPaddingV * 2f);

        var panelRect = new Rectangle(
            (int)((trackAreaWidth - panelWidth) / 2f),
            (int)((trackAreaHeight - panelHeight) / 2f),
            (int)panelWidth,
            (int)panelHeight);

        DrawPanel(panelRect);
        _spriteBatch.Draw(_pixel, new Rectangle(panelRect.X + 4, panelRect.Y + 4, panelRect.Width - 8, 4), AccentColor);

        float y = panelRect.Y + panelPaddingV;
        float headerWidth = PixelFont.Measure(headline, headerSize);
        PixelFont.DrawShadowed(_spriteBatch, _pixel, headline, new Vector2(panelRect.X + ((panelRect.Width - headerWidth) / 2f), y), headerSize, AccentColor);
        y += PixelFont.LineHeight(headerSize) + 22f;

        foreach (string line in lines)
        {
            Color lineColor = line.Contains("RECORDE", StringComparison.Ordinal) ? RecordColor : TextColor;
            float lineWidth = PixelFont.Measure(line, lineSize);
            PixelFont.DrawShadowed(_spriteBatch, _pixel, line, new Vector2(panelRect.X + ((panelRect.Width - lineWidth) / 2f), y), lineSize, lineColor);
            y += PixelFont.LineHeight(lineSize) + lineGap;
        }

        var dividerRect = new Rectangle(panelRect.X + (int)panelPaddingH, (int)y + 2, panelRect.Width - (int)(panelPaddingH * 2f), 2);
        _spriteBatch.Draw(_pixel, dividerRect, PanelBorderColor);
        y += dividerGap;

        float promptWidth = PixelFont.Measure(prompt, promptSize);
        PixelFont.Draw(_spriteBatch, _pixel, prompt, new Vector2(panelRect.X + ((panelRect.Width - promptWidth) / 2f), y), promptSize, AccentColor);
    }

    /// <summary>Monta o título e as linhas de estatísticas do pop-up de resultado, de acordo com o modo.</summary>
    private (string Headline, List<string> Lines) BuildResultsContent()
    {
        switch (_race.Mode)
        {
            case RaceMode.TimeAttack:
            {
                var lines = new List<string>
                {
                    $"PONTUACAO: {_player.Score:0} PTS",
                    $"VOLTAS COMPLETAS: {_player.Car.LapsCompleted}",
                };

                if (_saveData.BestScoreTimeAttack is { } best)
                {
                    lines.Add(_newScoreRecord ? $"NOVO RECORDE! (ANTES: {best:0} PTS)" : $"RECORDE PESSOAL: {best:0} PTS");
                }

                return ("TEMPO ESGOTADO!", lines);
            }

            case RaceMode.Elimination:
            {
                bool champion = _player.Finished;
                string headline = champion ? "VOCE E O CAMPEAO!" : "VOCE FOI ELIMINADO";
                var lines = new List<string>
                {
                    $"POSICAO FINAL: {_player.FinishPlace ?? _race.Entrants.Count}º DE {_race.Entrants.Count}",
                    $"RECORDE: {_saveData.EliminationWins} VITORIAS EM {_saveData.EliminationRaces} CORRIDAS",
                };

                return (headline, lines);
            }

            default:
            {
                var lines = new List<string>
                {
                    $"POSICAO FINAL: {_player.FinishPlace ?? _race.Entrants.Count}º DE {_race.Entrants.Count}",
                    $"TEMPO TOTAL: {TimeFormat.Precise(_player.FinishTime ?? _player.Car.TotalRaceTime)}",
                };

                string best = _player.Car.BestLapTime is { } b ? TimeFormat.Precise(b) : "--:--.---";
                lines.Add($"MELHOR VOLTA: {best}");

                if (_newRaceRecord)
                {
                    lines.Add("NOVO RECORDE DE CORRIDA!");
                }
                else if (_saveData.BestRaceTimeSprint is { } bestRace)
                {
                    lines.Add($"RECORDE DE CORRIDA: {TimeFormat.Precise(bestRace)}");
                }

                if (_newLapRecord)
                {
                    lines.Add("NOVO RECORDE DE VOLTA!");
                }

                return ("CORRIDA FINALIZADA", lines);
            }
        }
    }

    // ---------- Pop-up de configurações ----------

    private const float SettingsPanelWidth = 480f;
    private const float SettingsPanelPaddingV = 26f;
    private const float SettingsHeaderSize = 3f;
    private const float SettingsRowSpacing = 42f;
    private const float SettingsHintSize = 1.5f;
    private const float SettingsHintGap = 10f;
    private const float SettingsBarWidth = 120f;
    private const float SettingsBarHeight = 14f;
    private const float SettingsPaddingH = 30f;
    private const float SettingsBarValueGap = 10f;
    private const float SettingsValueSlotWidth = 44f;

    /// <summary>Calcula o retângulo do painel e das barras de volume do pop-up de configurações — usado
    /// tanto pra desenhar quanto pra testar clique/arraste do mouse, então os dois lados sempre concordam
    /// exatamente sobre onde cada barra está.</summary>
    private (Rectangle Panel, Rectangle MusicBar, Rectangle SfxBar) ComputeSettingsLayout()
    {
        float areaWidth = _windowWidth - (2f * TrackMargin);
        float areaHeight = _windowHeight - (2f * TrackMargin);

        float contentHeight = PixelFont.LineHeight(SettingsHeaderSize) + 30f
            + SettingsRowSpacing
            + 34f
            + (PixelFont.LineHeight(SettingsHintSize) * 2f) + SettingsHintGap;
        float panelHeight = contentHeight + (SettingsPanelPaddingV * 2f);

        var panelRect = new Rectangle(
            (int)((areaWidth - SettingsPanelWidth) / 2f),
            (int)((areaHeight - panelHeight) / 2f),
            (int)SettingsPanelWidth,
            (int)panelHeight);

        float rowY = panelRect.Y + SettingsPanelPaddingV + PixelFont.LineHeight(SettingsHeaderSize) + 30f;
        Rectangle musicBar = ComputeVolumeBarRect(panelRect, rowY);
        Rectangle sfxBar = ComputeVolumeBarRect(panelRect, rowY + SettingsRowSpacing);

        return (panelRect, musicBar, sfxBar);
    }

    private static Rectangle ComputeVolumeBarRect(Rectangle panel, float y)
    {
        float blockRight = panel.Right - SettingsPaddingH;
        float barRight = blockRight - SettingsValueSlotWidth - SettingsBarValueGap;
        float barX = barRight - SettingsBarWidth;

        return new Rectangle((int)barX, (int)y, (int)SettingsBarWidth, (int)SettingsBarHeight);
    }

    private void DrawSettingsPopup()
    {
        (Rectangle panelRect, Rectangle musicBar, Rectangle sfxBar) = ComputeSettingsLayout();

        float areaWidth = _windowWidth - (2f * TrackMargin);
        float areaHeight = _windowHeight - (2f * TrackMargin);
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, (int)areaWidth, (int)areaHeight), OverlayDimColor);

        const string header = "CONFIGURACOES";
        const string hint1 = "SETAS/WASD OU MOUSE: AJUSTAR";
        const string hint2 = "ENTER: MUDO    ESC: FECHAR";

        DrawPanel(panelRect);
        _spriteBatch.Draw(_pixel, new Rectangle(panelRect.X + 4, panelRect.Y + 4, panelRect.Width - 8, 4), AccentColor);

        float headerWidth = PixelFont.Measure(header, SettingsHeaderSize);
        float y = panelRect.Y + SettingsPanelPaddingV;
        PixelFont.DrawShadowed(_spriteBatch, _pixel, header, new Vector2(panelRect.X + ((panelRect.Width - headerWidth) / 2f), y), SettingsHeaderSize, AccentColor);

        DrawSettingsRow(panelRect, musicBar, "TRILHA SONORA", _audio.MusicVolume, _audio.MusicMuted, _settingsSelection == SettingsRow.Music);
        DrawSettingsRow(panelRect, sfxBar, "EFEITOS SONOROS", _audio.SfxVolume, _audio.SfxMuted, _settingsSelection == SettingsRow.Sfx);

        float hintY = sfxBar.Y + 34f;
        float hint1Width = PixelFont.Measure(hint1, SettingsHintSize);
        PixelFont.Draw(_spriteBatch, _pixel, hint1, new Vector2(panelRect.X + ((panelRect.Width - hint1Width) / 2f), hintY), SettingsHintSize, TextColor);

        float hint2Y = hintY + PixelFont.LineHeight(SettingsHintSize) + SettingsHintGap;
        float hint2Width = PixelFont.Measure(hint2, SettingsHintSize);
        PixelFont.Draw(_spriteBatch, _pixel, hint2, new Vector2(panelRect.X + ((panelRect.Width - hint2Width) / 2f), hint2Y), SettingsHintSize, TextColor);
    }

    /// <summary>Uma linha do pop-up de configurações: rótulo (destacado se selecionada) + barra de volume
    /// (com um "manípulo" na posição atual, pra deixar claro que dá pra arrastar) + porcentagem — ou
    /// "MUDO" no lugar da porcentagem, com a barra apagada, quando estiver sem som.</summary>
    private void DrawSettingsRow(Rectangle panel, Rectangle barRect, string label, float volume, bool muted, bool selected)
    {
        const float labelSize = 2f;
        const float valueSize = 1.75f;

        Color labelColor = selected ? AccentColor : TextColor;
        // O glifo ">" não existe na fonte pixelizada (só letras/números/pontuação básica) — usa "*" como
        // marcador de linha selecionada, que é um caractere que a fonte realmente desenha.
        string fullLabel = (selected ? "* " : "  ") + label;
        PixelFont.Draw(_spriteBatch, _pixel, fullLabel, new Vector2(panel.X + SettingsPaddingH, barRect.Y), labelSize, labelColor);

        var backRect = new Rectangle(barRect.X - 2, barRect.Y - 2, barRect.Width + 4, barRect.Height + 4);
        _spriteBatch.Draw(_pixel, backRect, new Color(10, 12, 18));

        float fraction = muted ? 0f : Math.Clamp(volume, 0f, 1f);
        var fillRect = new Rectangle(barRect.X, barRect.Y, (int)(barRect.Width * fraction), barRect.Height);
        Color fillColor = muted ? new Color(90, 94, 104) : BoostFillColor;
        _spriteBatch.Draw(_pixel, fillRect, fillColor);

        if (!muted)
        {
            float handleX = barRect.X + (barRect.Width * fraction);
            DrawCircle(new Vector2(handleX, barRect.Y + (barRect.Height / 2f)), barRect.Height * 0.55f, Color.White);
        }

        string valueText = muted ? "MUDO" : $"{(int)MathF.Round(volume * 100f)}%";
        float valueWidth = PixelFont.Measure(valueText, valueSize);
        float valueX = (panel.Right - SettingsPaddingH) - valueWidth;
        Color valueColor = muted ? new Color(150, 155, 165) : TextColor;
        PixelFont.Draw(_spriteBatch, _pixel, valueText, new Vector2(valueX, barRect.Y), valueSize, valueColor);
    }

    /// <summary>Fundo animado dos menus (tela inicial e seleção de modo): a própria pista atual com os
    /// carros de IA circulando por baixo, escurecida por um véu translúcido pra não competir com o texto
    /// por cima. É a mesma corrida que já existe em <see cref="_race"/> — atualizada continuamente em vez
    /// de parada, e reiniciada sozinha quando "termina" (ver <see cref="UpdateMenuBackgroundRace"/>).</summary>
    private void DrawMenuBackground()
    {
        DrawTrack();
        DrawHazards();
        DrawCars();

        float areaWidth = _windowWidth - (2f * TrackMargin);
        float areaHeight = _windowHeight - (2f * TrackMargin);
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, (int)areaWidth, (int)areaHeight), MenuBackgroundDim);
    }

    // ---------- Tela inicial ----------

    private const float IntroTitleTop = 36f;
    private const float IntroTitleSize = 5.5f;
    private const float IntroPanelHeight = 175f;
    private const float SkinPreviewUnit = 58f;

    /// <summary>Retângulos da tela inicial que o desenho e o clique do mouse precisam concordar: o console
    /// de controles/dicas, o painel do seletor de skin ao lado dele e as duas setinhas desse seletor.</summary>
    private readonly record struct IntroLayout(Rectangle InfoFrame, Rectangle SkinPanel, Rectangle LeftArrow, Rectangle RightArrow, Vector2 PreviewCenter);

    private IntroLayout ComputeIntroLayout()
    {
        float trackAreaWidth = _windowWidth - (2f * TrackMargin);
        float panelsTop = IntroTitleTop + PixelFont.LineHeight(IntroTitleSize) + 32f;
        const float gap = 16f;

        float usableWidth = trackAreaWidth - 40f;
        float skinWidth = MathF.Min(340f, usableWidth * 0.34f);

        var infoFrame = new Rectangle(20, (int)panelsTop, (int)(usableWidth - skinWidth - gap), (int)IntroPanelHeight);
        var skinPanel = new Rectangle(infoFrame.Right + (int)gap, (int)panelsTop, (int)skinWidth, (int)IntroPanelHeight);

        var previewCenter = new Vector2(skinPanel.Center.X, skinPanel.Y + 86f);
        const int arrowWidth = 34;
        const int arrowHeight = 46;
        int arrowY = (int)previewCenter.Y - (arrowHeight / 2);
        var leftArrow = new Rectangle(skinPanel.X + 14, arrowY, arrowWidth, arrowHeight);
        var rightArrow = new Rectangle(skinPanel.Right - 14 - arrowWidth, arrowY, arrowWidth, arrowHeight);

        return new IntroLayout(infoFrame, skinPanel, leftArrow, rightArrow, previewCenter);
    }

    private void DrawIntro()
    {
        float trackAreaWidth = _windowWidth - (2f * TrackMargin);
        float trackAreaHeight = _windowHeight - (2f * TrackMargin);

        DrawMenuBackground();

        string title = "MEGRACE";
        float titleWidth = PixelFont.Measure(title, IntroTitleSize);
        var titlePos = new Vector2((trackAreaWidth - titleWidth) / 2f, IntroTitleTop);

        foreach (Vector2 offset in OutlineOffsets)
        {
            PixelFont.Draw(_spriteBatch, _pixel, title, titlePos + (offset * 1.5f), IntroTitleSize, TitleOutline);
        }

        PixelFont.DrawGradient(_spriteBatch, _pixel, title, titlePos, IntroTitleSize, TitleGradient);

        // Um único "console de HUD" com cantos marcados em vez de dois cards iguais aos da tela de
        // seleção de modo — pra tela inicial não parecer mais uma tela de escolha. Ao lado dele fica o
        // seletor de skin do carro do jogador.
        IntroLayout layout = ComputeIntroLayout();
        Rectangle infoFrame = layout.InfoFrame;
        DrawHudFrame(infoFrame);

        int columnWidth = infoFrame.Width / 2;
        var leftColumn = new Rectangle(infoFrame.X, infoFrame.Y, columnWidth, infoFrame.Height);
        var rightColumn = new Rectangle(infoFrame.X + columnWidth, infoFrame.Y, infoFrame.Width - columnWidth, infoFrame.Height);

        _spriteBatch.Draw(_pixel, new Rectangle(infoFrame.X + columnWidth - 1, infoFrame.Y + 16, 2, infoFrame.Height - 32), HudFrameDivider);

        DrawIntroInfoColumn(leftColumn, "CONTROLES", ["SETAS/WASD DIRIGIR", "SHIFT: TURBO", "ESPACO: FREIO DE MAO", "F11: TELA CHEIA", "Q: OPCOES", "ESC: SAIR"], highlightedLine: "Q: OPCOES");
        DrawIntroInfoColumn(rightColumn, "DICAS", ["ENCHA O TURBO NOS", "CHECKPOINTS E RETAS", "CUIDADO AO BATER NOS", "RIVAIS E NAS PAREDES"]);

        DrawSkinSelector(layout);

        // Pisca lentamente (aparece/desaparece) em vez de ficar num tom fixo — chama mais atenção sem
        // ser irritante.
        const string prompt = "APERTE QUALQUER TECLA";
        const float promptSize = 2.5f;
        float blink = (MathF.Sin(_visualTime * 1.6f) + 1f) / 2f;
        // Alfa pré-multiplicado (padrão do SpriteBatch): pra sumir de verdade, a cor inteira é escalada.
        Color promptColor = AccentColor * blink;
        Color promptShadowColor = Color.Black * (0.5f * blink);
        float promptWidth = PixelFont.Measure(prompt, promptSize);
        var promptPos = new Vector2((trackAreaWidth - promptWidth) / 2f, infoFrame.Bottom + 20f);
        PixelFont.DrawShadowed(_spriteBatch, _pixel, prompt, promptPos, promptSize, promptColor, promptShadowColor);

        DrawStudioLogo(new Vector2(16f, trackAreaHeight - 16f));
        DrawAchievementsButton();
    }

    /// <summary>Painel "[ ← ] skin [ → ]": a skin mostrada desenhada grande no centro (a mesma pintura usada
    /// na corrida, só que ampliada). Liberada: nome + "EQUIPADO". Bloqueada: vulto escuro com cadeado,
    /// nome apagado, o requisito entre aspas e quanto falta.</summary>
    private void DrawSkinSelector(IntroLayout layout)
    {
        Rectangle panel = layout.SkinPanel;
        DrawHudFrame(panel);

        CarSkin skin = CarSkins.All[_previewSkinIndex];
        bool unlocked = SkinUnlocks.IsUnlocked(skin, _saveData);

        const string header = "SEU CARRO";
        const float headerSize = 2.25f;
        float headerWidth = PixelFont.Measure(header, headerSize);
        float headerX = panel.X + ((panel.Width - headerWidth) / 2f);
        PixelFont.Draw(_spriteBatch, _pixel, header, new Vector2(headerX, panel.Y + 16f), headerSize, AccentColor);
        _spriteBatch.Draw(_pixel, new Rectangle((int)headerX, (int)(panel.Y + 16f + PixelFont.LineHeight(headerSize) + 4f), (int)headerWidth, 2), AccentColor);

        if (skin.Requirement is not AlwaysUnlockedRequirement)
        {
            DrawDifficultyTag(new Vector2(panel.X + 16f, panel.Y + 20f), skin.Difficulty, 1.4f, dimmed: !unlocked);
        }

        const float counterSize = 1.5f;
        string counter = $"{_previewSkinIndex + 1}/{CarSkins.All.Count}";
        float counterWidth = PixelFont.Measure(counter, counterSize);
        PixelFont.Draw(_spriteBatch, _pixel, counter, new Vector2(panel.Right - 16f - counterWidth, panel.Y + 20f), counterSize, StatBadgeLabelColor);

        // Um "holofote" discreto por baixo, pra skin parecer em exposição numa vitrine. O SpriteBatch usa
        // alfa pré-multiplicado, então a transparência tem que vir de "cor * fração" (não do canal A).
        Color glow = unlocked ? AccentColor : StatBadgeLabelColor;
        DrawCircle(layout.PreviewCenter + new Vector2(0f, 6f), 48f, glow * 0.07f);
        DrawCircle(layout.PreviewCenter, 38f, glow * 0.06f);

        // Skin secreta ainda bloqueada: nem o vulto aparece — só um "?" no lugar.
        bool hiddenSecret = skin.IsSecret && !unlocked;
        if (hiddenSecret)
        {
            const float questionSize = 6f;
            float questionWidth = PixelFont.Measure("?", questionSize);
            PixelFont.Draw(_spriteBatch, _pixel, "?", layout.PreviewCenter - new Vector2(questionWidth / 2f, PixelFont.LineHeight(questionSize) / 2f), questionSize, new Color(80, 86, 104));
        }
        else
        {
            float sway = MathF.Sin(_visualTime * 1.5f) * 0.12f;
            _carPainter.Begin(layout.PreviewCenter, sway, SkinPreviewUnit, AccentColor, eliminated: false, _visualTime, silhouette: !unlocked);
            skin.Paint(_carPainter);
        }

        if (!unlocked)
        {
            DrawLock(layout.PreviewCenter + new Vector2(hiddenSecret ? 34f : 0f, hiddenSecret ? 18f : 2f), hiddenSecret ? 1.1f : 1.7f, AccentColor);
        }

        Vector2 mouse = ScreenToLogicalPosition(_input.MousePosition);
        var mousePoint = new Point((int)mouse.X, (int)mouse.Y);
        DrawSkinArrow(layout.LeftArrow, pointRight: false, layout.LeftArrow.Contains(mousePoint), _skinArrowFlashLeft > 0f);
        DrawSkinArrow(layout.RightArrow, pointRight: true, layout.RightArrow.Contains(mousePoint), _skinArrowFlashRight > 0f);

        const float nameSize = 2.25f;
        string displayName = hiddenSecret ? "SKIN SECRETA" : skin.Name;
        float nameWidth = PixelFont.Measure(displayName, nameSize);

        if (unlocked)
        {
            PixelFont.DrawShadowed(_spriteBatch, _pixel, skin.Name, new Vector2(panel.X + ((panel.Width - nameWidth) / 2f), panel.Y + 134f), nameSize, TextColor);
            DrawCenteredText(panel, "EQUIPADO", panel.Y + 157f, 1.5f, RecordColor);
            return;
        }

        // Cadeado pequeno + nome apagado, centralizados juntos.
        const float lockGap = 8f;
        const float smallLockScale = 0.6f;
        float lockWidth = 18f * smallLockScale;
        float nameX = panel.X + ((panel.Width - (lockWidth + lockGap + nameWidth)) / 2f) + lockWidth + lockGap;
        float nameY = panel.Y + 125f;
        DrawLock(new Vector2(nameX - lockGap - (lockWidth / 2f), nameY + 7f), smallLockScale, StatBadgeLabelColor);
        PixelFont.Draw(_spriteBatch, _pixel, displayName, new Vector2(nameX, nameY), nameSize, StatBadgeLabelColor);

        if (hiddenSecret)
        {
            DrawCenteredText(panel, "\"???\"", panel.Y + 146f, 1.4f, AccentColor);
            return;
        }

        DrawCenteredText(panel, $"\"{skin.Requirement.Description}\"", panel.Y + 146f, 1.4f, AccentColor);
        if (skin.Requirement.ProgressText(_saveData) is { } progress)
        {
            DrawCenteredText(panel, progress, panel.Y + 160f, 1.3f, StatBadgeLabelColor);
        }
    }

    private void DrawCenteredText(Rectangle area, string text, float y, float size, Color color)
    {
        float width = PixelFont.Measure(text, size);
        PixelFont.Draw(_spriteBatch, _pixel, text, new Vector2(area.X + ((area.Width - width) / 2f), y), size, color);
    }

    /// <summary>Cadeado pixelizado (alça em "∩" + corpo com buraco da chave), centrado em
    /// <paramref name="center"/> — no tamanho 1 ocupa uns 18x23 px.</summary>
    private void DrawLock(Vector2 center, float scale, Color color)
    {
        int Px(float value) => Math.Max(1, (int)MathF.Round(value * scale));
        int x = (int)center.X;
        int y = (int)center.Y;

        _spriteBatch.Draw(_pixel, new Rectangle(x - Px(7), y - Px(12), Px(3), Px(10)), color);
        _spriteBatch.Draw(_pixel, new Rectangle(x + Px(4), y - Px(12), Px(3), Px(10)), color);
        _spriteBatch.Draw(_pixel, new Rectangle(x - Px(7), y - Px(13), Px(14), Px(3)), color);

        _spriteBatch.Draw(_pixel, new Rectangle(x - Px(9), y - Px(3), Px(18), Px(13)), color);
        _spriteBatch.Draw(_pixel, new Rectangle(x - Px(1), y + Px(1), Px(2), Px(5)), new Color(40, 30, 10));
    }

    /// <summary>Botão de seta do seletor de skin: acende com o mouse em cima e dá um "flash" rápido quando
    /// usado (clique ou teclado), pra ficar claro que a troca aconteceu.</summary>
    private void DrawSkinArrow(Rectangle rect, bool pointRight, bool hovered, bool flashing)
    {
        Color border = flashing ? Color.White : hovered ? AccentColor : PanelBorderColor;
        DrawRoundedRect(rect, border, 6f);
        DrawRoundedRect(new Rectangle(rect.X + 3, rect.Y + 3, rect.Width - 6, rect.Height - 6), PanelFillColor, 5f);

        // A fonte pixelizada não tem "<"/">" — o triângulo é desenhado em colunas de 2 px que vão
        // encurtando até a ponta, no mesmo estilo pixelado do resto.
        Color arrowColor = flashing || hovered ? AccentColor : TextColor;
        const int triangleWidth = 12;
        const float halfHeight = 11f;
        Point center = rect.Center;
        for (int i = 0; i < triangleWidth; i += 2)
        {
            float columnHalf = halfHeight * (1f - (i / (float)triangleWidth));
            int x = pointRight ? center.X - (triangleWidth / 2) + i : center.X + (triangleWidth / 2) - i - 2;
            _spriteBatch.Draw(_pixel, new Rectangle(x, (int)(center.Y - columnHalf), 2, (int)(columnHalf * 2f)), arrowColor);
        }
    }

    // ---------- Tela de seleção de modo ----------

    private void DrawModeSelect()
    {
        float trackAreaWidth = _windowWidth - (2f * TrackMargin);
        float trackAreaHeight = _windowHeight - (2f * TrackMargin);

        DrawMenuBackground();

        const float headerSize = 3.75f;
        const string header = "ESCOLHA O MODO";
        float headerWidth = PixelFont.Measure(header, headerSize);
        var headerPos = new Vector2((trackAreaWidth - headerWidth) / 2f, 44f);
        PixelFont.DrawShadowed(_spriteBatch, _pixel, header, headerPos, headerSize, AccentColor);

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

        string[] sprintLines = ["COMPLETE 3 VOLTAS", "NA FRENTE DE TODOS."];
        string[] eliminationLines = ["10 CARROS. A CADA", "VOLTA, O ULTIMO", "LUGAR E ELIMINADO.", "SOBREVIVA!"];
        string[] timeAttackLines = ["O RELOGIO SO DESCE.", "DESVIE DOS OBSTACULOS", "E PONTUE PRA GANHAR", "MAIS TEMPO!"];

        DrawPanelText(sprintPanel, "CORRIDA CLASSICA", sprintLines);
        DrawPanelText(eliminationPanel, "CORRIDA MORTAL", eliminationLines);
        DrawPanelText(timeAttackPanel, "CONTRA O RELOGIO", timeAttackLines);

        // Recorde/vitórias ganham um "chip" próprio dentro do card, separado da descrição — em vez de
        // mais uma linha de texto igual às outras, com pouco destaque.
        if (_saveData.BestRaceTimeSprint is { } bestRace)
        {
            DrawStatBadge(sprintPanel, "MELHOR TEMPO", TimeFormat.Precise(bestRace));
        }

        if (_saveData.EliminationRaces > 0)
        {
            DrawStatBadge(eliminationPanel, "VITORIAS", $"{_saveData.EliminationWins}/{_saveData.EliminationRaces}");
        }

        if (_saveData.BestScoreTimeAttack is { } bestScore)
        {
            DrawStatBadge(timeAttackPanel, "RECORDE", $"{bestScore:0} PTS");
        }

        // "Q: OPCOES" sai destacado em relação ao resto do rodapé, pra chamar atenção pro atalho novo.
        const string promptBase = "SETAS: TROCAR    ESPACO: CONFIRMAR    ";
        const string promptHighlight = "Q: OPCOES    C: CONQUISTAS";
        const float promptSize = 2f;
        float promptBaseWidth = PixelFont.Measure(promptBase, promptSize);
        float promptHighlightWidth = PixelFont.Measure(promptHighlight, promptSize);
        var promptPos = new Vector2((trackAreaWidth - promptBaseWidth - promptHighlightWidth) / 2f, panelsTop + panelHeight + 24f);
        PixelFont.Draw(_spriteBatch, _pixel, promptBase, promptPos, promptSize, TextColor);
        PixelFont.Draw(_spriteBatch, _pixel, promptHighlight, promptPos + new Vector2(promptBaseWidth, 0f), promptSize, AccentColor);
    }

    private void DrawSelectablePanel(Rectangle rect, bool selected)
    {
        Color borderColor = PanelBorderColor;
        if (selected)
        {
            // Um brilho suave e pulsante na borda do painel escolhido, pra chamar mais atenção do que
            // só uma cor sólida.
            float pulse = (MathF.Sin(_visualTime * 5f) + 1f) / 2f;
            borderColor = Color.Lerp(AccentColor, Color.White, pulse * 0.35f);
        }

        DrawRoundedRect(rect, borderColor, 10f);
        var inner = new Rectangle(rect.X + 4, rect.Y + 4, rect.Width - 8, rect.Height - 8);
        DrawRoundedRect(inner, PanelFillColor, 8f);
    }

    private static readonly Vector2[] OutlineOffsets =
    [
        new(-1, -1), new(0, -1), new(1, -1),
        new(-1, 0), new(1, 0),
        new(-1, 1), new(0, 1), new(1, 1),
    ];

    private void DrawPanel(Rectangle rect)
    {
        DrawRoundedRect(rect, PanelBorderColor, 10f);
        var inner = new Rectangle(rect.X + 4, rect.Y + 4, rect.Width - 8, rect.Height - 8);
        DrawRoundedRect(inner, PanelFillColor, 8f);
    }

    /// <summary>Estatística em destaque (recorde, vitórias) num chip próprio dentro do card do modo —
    /// tarja colorida + rótulo discreto + valor grande, em vez de mais uma linha igual à descrição.</summary>
    private void DrawStatBadge(Rectangle panel, string label, string value)
    {
        const float labelSize = 1.4f;
        const float valueSize = 2f;
        const float paddingH = 10f;
        const float paddingV = 6f;
        const float stripeWidth = 4f;

        float badgeHeight = PixelFont.LineHeight(valueSize) + (paddingV * 2f);
        var badgeRect = new Rectangle(
            panel.X + 10,
            panel.Bottom - (int)badgeHeight - 12,
            panel.Width - 20,
            (int)badgeHeight);

        DrawRoundedRect(badgeRect, StatBadgeFill, 6f);
        _spriteBatch.Draw(_pixel, new Rectangle(badgeRect.X, badgeRect.Y, (int)stripeWidth, badgeRect.Height), RecordColor);

        float labelY = badgeRect.Y + ((badgeRect.Height - PixelFont.LineHeight(labelSize)) / 2f);
        PixelFont.Draw(_spriteBatch, _pixel, label, new Vector2(badgeRect.X + paddingH + stripeWidth, labelY), labelSize, StatBadgeLabelColor);

        float valueWidth = PixelFont.Measure(value, valueSize);
        PixelFont.DrawShadowed(_spriteBatch, _pixel, value, new Vector2(badgeRect.Right - paddingH - valueWidth, badgeRect.Y + paddingV), valueSize, RecordColor);
    }

    /// <summary>Painel "console de HUD" da tela inicial: preenchimento translúcido com cantos marcados só
    /// por tirinhas em L (em vez da borda arredondada uniforme dos cards de seleção de modo), pra não
    /// parecer mais uma tela de escolha.</summary>
    private void DrawHudFrame(Rectangle rect)
    {
        const int bracketLength = 20;
        const int bracketThickness = 3;

        _spriteBatch.Draw(_pixel, rect, HudFrameFill);

        _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, bracketLength, bracketThickness), AccentColor);
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, bracketThickness, bracketLength), AccentColor);

        _spriteBatch.Draw(_pixel, new Rectangle(rect.Right - bracketLength, rect.Y, bracketLength, bracketThickness), AccentColor);
        _spriteBatch.Draw(_pixel, new Rectangle(rect.Right - bracketThickness, rect.Y, bracketThickness, bracketLength), AccentColor);

        _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - bracketThickness, bracketLength, bracketThickness), AccentColor);
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - bracketLength, bracketThickness, bracketLength), AccentColor);

        _spriteBatch.Draw(_pixel, new Rectangle(rect.Right - bracketLength, rect.Bottom - bracketThickness, bracketLength, bracketThickness), AccentColor);
        _spriteBatch.Draw(_pixel, new Rectangle(rect.Right - bracketThickness, rect.Bottom - bracketLength, bracketThickness, bracketLength), AccentColor);
    }

    /// <summary>Uma coluna de texto dentro do console de HUD da tela inicial: título pequeno alinhado à
    /// esquerda com um traço sublinhado, em vez do cabeçalho grande e centralizado dos cards de modo.</summary>
    /// <summary>Desenha uma coluna de dicas da tela inicial. <paramref name="highlightedLine"/> (se
    /// alguma linha bater com ela) sai destacada em <see cref="AccentColor"/> em vez do branco padrão —
    /// usado pra chamar atenção pro atalho de configurações no meio das outras dicas.</summary>
    private void DrawIntroInfoColumn(Rectangle column, string header, string[] lines, string highlightedLine = null)
    {
        const float headerSize = 2.25f;
        const float lineSize = 1.75f;
        const float paddingX = 18f;

        float x = column.X + paddingX;
        float y = column.Y + 16f;

        PixelFont.Draw(_spriteBatch, _pixel, header, new Vector2(x, y), headerSize, AccentColor);
        float headerWidth = PixelFont.Measure(header, headerSize);
        y += PixelFont.LineHeight(headerSize) + 4f;
        _spriteBatch.Draw(_pixel, new Rectangle((int)x, (int)y, (int)headerWidth, 2), AccentColor);
        y += 10f;

        foreach (string line in lines)
        {
            Color lineColor = line == highlightedLine ? AccentColor : Color.White;
            PixelFont.Draw(_spriteBatch, _pixel, line, new Vector2(x, y), lineSize, lineColor);
            y += PixelFont.LineHeight(lineSize) + 8f;
        }
    }

    /// <summary>Logo pixel-art da Domus Arcis (espada + nome do estúdio) no canto inferior-esquerdo da tela
    /// inicial — só um discreto selo de estúdio, nunca competindo com o título do jogo.</summary>
    private void DrawStudioLogo(Vector2 bottomLeft)
    {
        const float iconSize = 34f;
        const float textSize = 1.1f;
        const float textGap = 4f;
        const float lineGap = 2f;

        float textBlockHeight = (PixelFont.LineHeight(textSize) * 2f) + lineGap;
        float totalHeight = iconSize + textGap + textBlockHeight;
        Vector2 topLeft = bottomLeft - new Vector2(0f, totalHeight);

        // Lâmina na diagonal com o cabo no alto e a ponta pro canto inferior — a mesma composição da
        // logo de referência, só simplificada pro tamanho pixel-art bem pequeno.
        float angle = MathF.PI * 0.7f;
        Vector2 iconCenter = topLeft + new Vector2(iconSize * 0.55f, iconSize * 0.5f);
        DrawFilledRectRotated(iconCenter, iconSize * 0.85f, 3.5f, angle, StudioLogoColor);

        Vector2 guardCenter = iconCenter + Rotate(new Vector2(iconSize * 0.3f, 0f), angle);
        DrawFilledRectRotated(guardCenter, 10f, 3f, angle + (MathF.PI / 2f), StudioLogoColor);

        Vector2 pommelCenter = iconCenter + Rotate(new Vector2(iconSize * 0.42f, 0f), angle);
        DrawCircle(pommelCenter, 3f, StudioLogoColor);

        const string studioLine1 = "DOMUS";
        const string studioLine2 = "ARCIS";
        float centerX = topLeft.X + (iconSize / 2f);
        float textY = topLeft.Y + iconSize + textGap;

        float line1Width = PixelFont.Measure(studioLine1, textSize);
        PixelFont.Draw(_spriteBatch, _pixel, studioLine1, new Vector2(centerX - (line1Width / 2f), textY), textSize, StudioLogoColor);

        float line2Width = PixelFont.Measure(studioLine2, textSize);
        PixelFont.Draw(_spriteBatch, _pixel, studioLine2, new Vector2(centerX - (line2Width / 2f), textY + PixelFont.LineHeight(textSize) + lineGap), textSize, StudioLogoColor);
    }

    private void DrawPanelText(Rectangle panel, string header, string[] lines)
    {
        const float headerSize = 2.5f;
        const float lineSize = 1.75f;

        float y = panel.Y + 12f;
        float headerWidth = PixelFont.Measure(header, headerSize);
        PixelFont.DrawShadowed(_spriteBatch, _pixel, header, new Vector2(panel.X + ((panel.Width - headerWidth) / 2f), y), headerSize, AccentColor);
        y += PixelFont.LineHeight(headerSize) + 12f;

        foreach (string line in lines)
        {
            PixelFont.Draw(_spriteBatch, _pixel, line, new Vector2(panel.X + 10f, y), lineSize, Color.White);
            y += PixelFont.LineHeight(lineSize) + 8f;
        }
    }
}
