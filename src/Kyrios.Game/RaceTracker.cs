using Kyrios.Core;

namespace Kyrios.Game;

/// <summary>Tudo o que aconteceu com o jogador numa partida terminada — é a única entrada de
/// <see cref="Progression.RecordRace"/>. Dado puro (fácil de montar em teste, sem MonoGame nem simulação).</summary>
public sealed class RaceReport
{
    public RaceMode Mode { get; init; }

    /// <summary>Skin usada pelo jogador nesta partida.</summary>
    public string SkinId { get; init; }

    /// <summary>Pista (cenário) em que a partida foi jogada.</summary>
    public string TrackId { get; init; }

    /// <summary>Campeão da Corrida Mortal (o Contra o Relógio não tem vencedor).</summary>
    public bool Won { get; init; }

    public int Place { get; init; }
    public int EntrantCount { get; init; }
    public float RaceSeconds { get; init; }
    public float? BestLapTime { get; init; }
    public int LapsCompleted { get; init; }
    public float Score { get; init; }
    public int Collisions { get; init; }

    /// <summary>Bateu num rival logo nos primeiros segundos da largada.</summary>
    public bool StartLineCrash { get; init; }

    public float LongestStandstillSeconds { get; init; }
    public float LongestReverseSeconds { get; init; }
    public float BoostSeconds { get; init; }

    /// <summary>Ficou em último entre os carros ainda correndo em algum momento (fora da largada).</summary>
    public bool WasEverLast { get; init; }

    /// <summary>Corrida Mortal: quantas eliminações de rivais o jogador sobreviveu.</summary>
    public int RoundsSurvived { get; init; }

    /// <summary>Corrida Mortal: foi eliminado com o turbo ligado.</summary>
    public bool EliminatedWhileBoosting { get; init; }

    /// <summary>Corrida Mortal: venceu parado ou de ré no instante da última eliminação.</summary>
    public bool LazyWin { get; init; }

    public int Checkpoints { get; init; }

    /// <summary>Maior sequência de checkpoints seguidos sem bater.</summary>
    public int BestCleanCheckpointStreak { get; init; }

    /// <summary>Contra o Relógio: cruzou algum checkpoint com menos de 1 s no relógio.</summary>
    public bool ClutchCheckpoint { get; init; }

    /// <summary>Contra o Relógio: mais tempo que chegou a ter no relógio.</summary>
    public float MaxTimeBanked { get; init; }

    /// <summary>Contra o Relógio: segundos perdidos em batidas.</summary>
    public float TimeLostToCrashes { get; init; }

    /// <summary>Passou pelo detalhe escondido da pista.</summary>
    public bool FoundSecret { get; init; }

    public int Pauses { get; init; }

    /// <summary>Jogou com música e efeitos no mudo.</summary>
    public bool Silent { get; init; }
}

/// <summary>
/// Observa o carro do jogador a cada quadro de uma partida e acumula o que as estatísticas precisam mas que a
/// simulação não guarda (batidas, tempo parado, turbo, se já esteve em último, segredo da pista...). No fim,
/// gera o <see cref="RaceReport"/>. Não desenha nada nem mexe no save.
/// </summary>
public sealed class RaceTracker
{
    /// <summary>Na largada o grid está parado e embolado; posição só conta depois disso.</summary>
    private const float StartGraceSeconds = 3f;
    private const float StartLineCrashSeconds = 3.5f;
    private const float StandstillSpeed = 0.3f;
    private const float ReverseSpeed = -0.3f;
    private const float LazyWinSpeed = 0.5f;
    private const float ClutchSecondsLeft = 1f;
    private const float SecretRadius = 1.1f;

    private Vector2D? _secretSpot;
    private int _collisions;
    private bool _wasColliding;
    private bool _startLineCrash;
    private float _standstill;
    private float _longestStandstill;
    private float _reverse;
    private float _longestReverse;
    private float _boostSeconds;
    private bool _wasEverLast;
    private int _roundsSurvived;
    private int _previousActiveCount;
    private float? _previousTimeRemaining;
    private bool _clutchCheckpoint;
    private int _checkpoints;
    private int _cleanStreak;
    private int _bestCleanStreak;
    private float _maxTimeBanked;
    private float _timeLost;
    private bool _eliminatedWhileBoosting;
    private bool _lazyWin;
    private bool _foundSecret;
    private int _pauses;
    private bool _playerWasDone;

    /// <summary>Verdadeiro só no quadro em que o jogador passou pelo detalhe escondido da pista.</summary>
    public bool SecretFoundThisTick { get; private set; }

    public bool FoundSecret => _foundSecret;

    public int Collisions => _collisions;

    /// <param name="secretSpot">Onde fica o detalhe escondido da pista (em células), se houver.</param>
    public void Reset(RaceSimulation race, Vector2D? secretSpot = null)
    {
        _secretSpot = secretSpot;
        _collisions = 0;
        _wasColliding = false;
        _startLineCrash = false;
        _standstill = 0f;
        _longestStandstill = 0f;
        _reverse = 0f;
        _longestReverse = 0f;
        _boostSeconds = 0f;
        _wasEverLast = false;
        _roundsSurvived = 0;
        _previousActiveCount = race.Entrants.Count;
        _previousTimeRemaining = race.TimeRemaining ?? RaceSimulation.TimeAttackStartSeconds;
        _clutchCheckpoint = false;
        _checkpoints = 0;
        _cleanStreak = 0;
        _bestCleanStreak = 0;
        _maxTimeBanked = race.TimeRemaining ?? 0f;
        _timeLost = 0f;
        _eliminatedWhileBoosting = false;
        _lazyWin = false;
        _foundSecret = false;
        _pauses = 0;
        _playerWasDone = false;
        SecretFoundThisTick = false;
    }

    public void RegisterPause() => _pauses++;

    /// <summary>Chamar logo depois de cada <see cref="RaceSimulation.Update"/> da partida do jogador.</summary>
    public void Observe(RaceSimulation race, RaceEntrant player, float dt)
    {
        Car car = player.Car;
        SecretFoundThisTick = false;

        int activeCount = race.Entrants.Count(e => !e.Eliminated);
        if (race.Mode == RaceMode.Elimination && activeCount < _previousActiveCount && !player.Eliminated)
        {
            _roundsSurvived += _previousActiveCount - activeCount;
        }

        _previousActiveCount = activeCount;

        if (ReferenceEquals(race.EliminatedThisTick, player))
        {
            _eliminatedWhileBoosting = car.IsBoosting;
        }

        if (_playerWasDone)
        {
            return;
        }

        bool done = player.Finished || player.Eliminated;
        if (done)
        {
            _playerWasDone = true;
            if (race.Mode == RaceMode.Elimination && player.Finished && car.Speed <= LazyWinSpeed)
            {
                _lazyWin = true;
            }
        }

        bool colliding = car.HadHeadOnCollisionThisTick || car.HadCarCollisionThisTick || car.HadHazardCollisionThisTick;
        if (colliding && !_wasColliding)
        {
            _collisions++;
            _cleanStreak = 0;
            if (car.HadCarCollisionThisTick && race.ElapsedTime <= StartLineCrashSeconds)
            {
                _startLineCrash = true;
            }
        }

        _wasColliding = colliding;

        if (car.CheckpointCrossedThisTick)
        {
            _checkpoints++;
            _cleanStreak++;
            _bestCleanStreak = Math.Max(_bestCleanStreak, _cleanStreak);
        }

        if (car.IsBoosting)
        {
            _boostSeconds += dt;
        }

        _standstill = MathF.Abs(car.Speed) < StandstillSpeed ? _standstill + dt : 0f;
        _longestStandstill = MathF.Max(_longestStandstill, _standstill);

        _reverse = car.Speed < ReverseSpeed ? _reverse + dt : 0f;
        _longestReverse = MathF.Max(_longestReverse, _reverse);

        if (!_foundSecret && _secretSpot is { } spot && car.Position.DistanceTo(spot) < SecretRadius)
        {
            _foundSecret = true;
            SecretFoundThisTick = true;
        }

        if (race.Mode == RaceMode.TimeAttack)
        {
            if (car.CheckpointCrossedThisTick && _previousTimeRemaining is { } before && before < ClutchSecondsLeft)
            {
                _clutchCheckpoint = true;
            }

            _previousTimeRemaining = race.TimeRemaining;
            _maxTimeBanked = MathF.Max(_maxTimeBanked, race.TimeRemaining ?? 0f);
            _timeLost += race.TimeLostThisTick;
        }
        else if (!done && race.ElapsedTime > StartGraceSeconds && IsLastAmongRacing(race, player))
        {
            _wasEverLast = true;
        }
    }

    public RaceReport BuildReport(RaceSimulation race, RaceEntrant player, string skinId, string trackId, bool silent)
    {
        return new RaceReport
        {
            Mode = race.Mode,
            SkinId = skinId,
            TrackId = trackId,
            Won = race.Mode == RaceMode.Elimination && player.Finished,
            Place = player.FinishPlace ?? race.Entrants.Count,
            EntrantCount = race.Entrants.Count,
            RaceSeconds = race.ElapsedTime,
            BestLapTime = player.Car.BestLapTime,
            LapsCompleted = player.Car.LapsCompleted,
            Score = player.Score,
            Collisions = _collisions,
            StartLineCrash = _startLineCrash,
            LongestStandstillSeconds = _longestStandstill,
            LongestReverseSeconds = _longestReverse,
            BoostSeconds = _boostSeconds,
            WasEverLast = _wasEverLast,
            RoundsSurvived = _roundsSurvived,
            EliminatedWhileBoosting = _eliminatedWhileBoosting,
            LazyWin = _lazyWin,
            Checkpoints = _checkpoints,
            BestCleanCheckpointStreak = _bestCleanStreak,
            ClutchCheckpoint = _clutchCheckpoint,
            MaxTimeBanked = _maxTimeBanked,
            TimeLostToCrashes = _timeLost,
            FoundSecret = _foundSecret,
            Pauses = _pauses,
            Silent = silent,
        };
    }

    private static bool IsLastAmongRacing(RaceSimulation race, RaceEntrant player)
    {
        List<RaceEntrant> racing = [.. race.GetStandings().Where(e => !e.Eliminated && !e.Finished)];
        return racing.Count > 1 && ReferenceEquals(racing[^1], player);
    }
}
