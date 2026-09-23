using Kyrios.Core;

namespace Kyrios.Game;

/// <summary>Tudo o que aconteceu com o jogador numa partida terminada — é a única entrada de
/// <see cref="Progression.RecordRace"/>. Dado puro (fácil de montar em teste, sem MonoGame nem simulação).</summary>
public sealed class RaceReport
{
    public RaceMode Mode { get; init; }

    /// <summary>Skin usada pelo jogador nesta partida.</summary>
    public string SkinId { get; init; }

    /// <summary>1º lugar na Clássica ou campeão da Mortal (o Contra o Relógio não tem vencedor).</summary>
    public bool Won { get; init; }

    public int Place { get; init; }
    public int EntrantCount { get; init; }
    public float? FinishTime { get; init; }
    public float? BestLapTime { get; init; }
    public float Score { get; init; }
    public float RaceSeconds { get; init; }
    public int Collisions { get; init; }
    public float LongestStandstillSeconds { get; init; }
    public float BoostSeconds { get; init; }

    /// <summary>Ficou em último entre os carros ainda correndo em algum momento (fora da largada).</summary>
    public bool WasEverLast { get; init; }

    /// <summary>Corrida Mortal: quantas eliminações de rivais o jogador sobreviveu.</summary>
    public int RoundsSurvived { get; init; }

    /// <summary>Contra o Relógio: cruzou algum checkpoint com menos de 1 s no relógio.</summary>
    public bool ClutchCheckpoint { get; init; }

    /// <summary>Corrida Clássica: cruzou a linha de chegada final andando de ré.</summary>
    public bool FinishedInReverse { get; init; }
}

/// <summary>
/// Observa o carro do jogador a cada quadro de uma partida e acumula o que as estatísticas precisam mas que a
/// simulação não guarda (batidas, tempo parado, turbo, se já esteve em último...). No fim, gera o
/// <see cref="RaceReport"/>. Não desenha nada nem mexe no save.
/// </summary>
public sealed class RaceTracker
{
    /// <summary>Na largada o grid está parado e embolado; posição e "parado" só contam depois disso.</summary>
    private const float StartGraceSeconds = 3f;
    private const float StandstillSpeed = 0.3f;
    private const float ClutchSecondsLeft = 1f;

    private int _collisions;
    private bool _wasColliding;
    private float _standstill;
    private float _longestStandstill;
    private float _boostSeconds;
    private bool _wasEverLast;
    private int _roundsSurvived;
    private int _previousActiveCount;
    private float? _previousTimeRemaining;
    private bool _clutchCheckpoint;
    private bool _finishedInReverse;
    private bool _playerWasDone;

    public void Reset(RaceSimulation race)
    {
        _collisions = 0;
        _wasColliding = false;
        _standstill = 0f;
        _longestStandstill = 0f;
        _boostSeconds = 0f;
        _wasEverLast = false;
        _roundsSurvived = 0;
        _previousActiveCount = race.Entrants.Count;
        _previousTimeRemaining = race.TimeRemaining ?? RaceSimulation.TimeAttackStartSeconds;
        _clutchCheckpoint = false;
        _finishedInReverse = false;
        _playerWasDone = false;
    }

    /// <summary>Chamar logo depois de cada <see cref="RaceSimulation.Update"/> da partida do jogador.</summary>
    public void Observe(RaceSimulation race, RaceEntrant player, float dt)
    {
        Car car = player.Car;

        int activeCount = race.Entrants.Count(e => !e.Eliminated);
        if (race.Mode == RaceMode.Elimination && activeCount < _previousActiveCount && !player.Eliminated)
        {
            _roundsSurvived += _previousActiveCount - activeCount;
        }

        _previousActiveCount = activeCount;

        if (_playerWasDone)
        {
            return;
        }

        bool done = player.Finished || player.Eliminated;
        if (done)
        {
            _playerWasDone = true;
            if (race.Mode == RaceMode.Sprint && player.Finished && car.Speed < 0f)
            {
                _finishedInReverse = true;
            }
        }

        bool colliding = car.HadHeadOnCollisionThisTick || car.HadCarCollisionThisTick || car.HadHazardCollisionThisTick;
        if (colliding && !_wasColliding)
        {
            _collisions++;
        }

        _wasColliding = colliding;

        if (car.IsBoosting)
        {
            _boostSeconds += dt;
        }

        if (MathF.Abs(car.Speed) < StandstillSpeed)
        {
            _standstill += dt;
            _longestStandstill = MathF.Max(_longestStandstill, _standstill);
        }
        else
        {
            _standstill = 0f;
        }

        if (race.Mode == RaceMode.TimeAttack)
        {
            if (car.CheckpointCrossedThisTick && _previousTimeRemaining is { } before && before < ClutchSecondsLeft)
            {
                _clutchCheckpoint = true;
            }

            _previousTimeRemaining = race.TimeRemaining;
        }
        else if (!done && race.ElapsedTime > StartGraceSeconds && IsLastAmongRacing(race, player))
        {
            _wasEverLast = true;
        }
    }

    public RaceReport BuildReport(RaceSimulation race, RaceEntrant player, string skinId)
    {
        bool won = race.Mode switch
        {
            RaceMode.Sprint => player.FinishPlace == 1,
            RaceMode.Elimination => player.Finished,
            _ => false,
        };

        return new RaceReport
        {
            Mode = race.Mode,
            SkinId = skinId,
            Won = won,
            Place = player.FinishPlace ?? race.Entrants.Count,
            EntrantCount = race.Entrants.Count,
            FinishTime = player.Finished ? player.FinishTime : null,
            BestLapTime = player.Car.BestLapTime,
            Score = player.Score,
            RaceSeconds = race.ElapsedTime,
            Collisions = _collisions,
            LongestStandstillSeconds = _longestStandstill,
            BoostSeconds = _boostSeconds,
            WasEverLast = _wasEverLast,
            RoundsSurvived = _roundsSurvived,
            ClutchCheckpoint = _clutchCheckpoint,
            FinishedInReverse = _finishedInReverse,
        };
    }

    private static bool IsLastAmongRacing(RaceSimulation race, RaceEntrant player)
    {
        List<RaceEntrant> racing = [.. race.GetStandings().Where(e => !e.Eliminated && !e.Finished)];
        return racing.Count > 1 && ReferenceEquals(racing[^1], player);
    }
}
