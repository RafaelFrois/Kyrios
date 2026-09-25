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

    /// <summary>Contra o Relógio: checkpoints cruzados com menos de 1 s no relógio.</summary>
    public int ClutchCheckpoints { get; init; }

    /// <summary>Contra o Relógio: cruzou algum checkpoint com menos de 1 s no relógio.</summary>
    public bool ClutchCheckpoint => ClutchCheckpoints > 0;

    /// <summary>Contra o Relógio: o menor tempo que sobrava no relógio ao cruzar um checkpoint.</summary>
    public float? ClosestCall { get; init; }

    /// <summary>Contra o Relógio: batidas nos obstáculos móveis.</summary>
    public int HazardHits { get; init; }

    /// <summary>Contra o Relógio: mais tempo que chegou a ter no relógio.</summary>
    public float MaxTimeBanked { get; init; }

    /// <summary>Contra o Relógio: segundos perdidos em batidas.</summary>
    public float TimeLostToCrashes { get; init; }

    /// <summary>Passou pelo detalhe escondido da pista.</summary>
    public bool FoundSecret { get; init; }

    /// <summary>Corrida Mortal: nunca saiu do 1º lugar depois da largada.</summary>
    public bool LedWholeRace { get; init; }

    /// <summary>Corrida Mortal: esteve em último com só 3 carros (ou menos) na pista.</summary>
    public bool WasLastInFinalThree { get; init; }

    /// <summary>Corrida Mortal: o último rival eliminado estava colado no jogador.</summary>
    public bool PhotoFinish { get; init; }

    /// <summary>Não soltou o acelerador nenhuma vez depois da largada.</summary>
    public bool FullThrottle { get; init; }

    /// <summary>Não virou pra direita nenhuma vez.</summary>
    public bool NeverSteeredRight { get; init; }

    /// <summary>Checkpoints cruzados andando de ré.</summary>
    public int ReverseCheckpoints { get; init; }

    /// <summary>Mais tempo seguido andando na contramão.</summary>
    public float LongestWrongWaySeconds { get; init; }

    /// <summary>Quando a partida terminou (hora local) — default = desconhecido (testes).</summary>
    public DateTime PlayedAt { get; init; }

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
    private const float PhotoFinishDistance = 2f;
    private const float WrongWaySpeed = 2f;
    private const int ClutchFieldSize = 3;

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
    private int _clutchCheckpoints;
    private float? _closestCall;
    private int _hazardHits;
    private bool _wasHazardColliding;
    private bool _wasEverNotFirst;
    private bool _wasLastInFinalThree;
    private bool _photoFinish;
    private bool _releasedThrottle;
    private bool _steeredRight;
    private int _reverseCheckpoints;
    private float _wrongWay;
    private float _longestWrongWay;
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
        _clutchCheckpoints = 0;
        _closestCall = null;
        _hazardHits = 0;
        _wasHazardColliding = false;
        _wasEverNotFirst = false;
        _wasLastInFinalThree = false;
        _photoFinish = false;
        _releasedThrottle = false;
        _steeredRight = false;
        _reverseCheckpoints = 0;
        _wrongWay = 0f;
        _longestWrongWay = 0f;
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

    /// <summary>Chamar logo depois de cada <see cref="RaceSimulation.Update"/> da partida do jogador, com o mesmo
    /// input que foi usado nele.</summary>
    public void Observe(RaceSimulation race, RaceEntrant player, float dt, CarInput input = default)
    {
        Car car = player.Car;
        SecretFoundThisTick = false;

        int activeCount = race.Entrants.Count(e => !e.Eliminated);
        if (race.Mode == RaceMode.Elimination && activeCount < _previousActiveCount && !player.Eliminated)
        {
            _roundsSurvived += _previousActiveCount - activeCount;
        }

        _previousActiveCount = activeCount;

        if (race.EliminationsThisTick.Contains(player))
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
            if (race.Mode == RaceMode.Elimination && player.Finished)
            {
                _lazyWin = car.Speed <= LazyWinSpeed;
                RaceEntrant runnerUp = race.EliminationsThisTick.LastOrDefault();
                _photoFinish = runnerUp is not null && runnerUp.Car.Position.DistanceTo(car.Position) <= PhotoFinishDistance;
            }
        }

        bool started = race.ElapsedTime > StartGraceSeconds;
        if (started && !done)
        {
            _releasedThrottle |= input.Throttle < 0.95f;
        }

        _steeredRight |= !done && input.Steering > 0.1f;

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

        bool hazardColliding = car.HadHazardCollisionThisTick;
        if (hazardColliding && !_wasHazardColliding)
        {
            _hazardHits++;
        }

        _wasHazardColliding = hazardColliding;

        if (car.CheckpointCrossedThisTick)
        {
            _reverseCheckpoints += car.Speed < ReverseSpeed ? 1 : 0;
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

        _wrongWay = started && IsGoingTheWrongWay(race.Track, car) ? _wrongWay + dt : 0f;
        _longestWrongWay = MathF.Max(_longestWrongWay, _wrongWay);

        if (!_foundSecret && _secretSpot is { } spot && car.Position.DistanceTo(spot) < SecretRadius)
        {
            _foundSecret = true;
            SecretFoundThisTick = true;
        }

        if (race.Mode == RaceMode.TimeAttack)
        {
            if (car.CheckpointCrossedThisTick && _previousTimeRemaining is { } before)
            {
                _clutchCheckpoints += before < ClutchSecondsLeft ? 1 : 0;
                _closestCall = MathF.Min(_closestCall ?? float.MaxValue, before);
            }

            _previousTimeRemaining = race.TimeRemaining;
            _maxTimeBanked = MathF.Max(_maxTimeBanked, race.TimeRemaining ?? 0f);
            _timeLost += race.TimeLostThisTick;
        }
        else if (!done && started)
        {
            List<RaceEntrant> racing = [.. race.GetStandings().Where(e => !e.Eliminated && !e.Finished)];
            bool last = racing.Count > 1 && ReferenceEquals(racing[^1], player);
            _wasEverLast |= last;
            _wasLastInFinalThree |= last && racing.Count <= ClutchFieldSize;
            _wasEverNotFirst |= racing.Count > 0 && !ReferenceEquals(racing[0], player);
        }
    }

    /// <summary>Andando (de frente ou de ré) no sentido contrário ao do circuito, pelo trecho da linha central
    /// mais próximo.</summary>
    private static bool IsGoingTheWrongWay(Track track, Car car)
    {
        if (MathF.Abs(car.Speed) < WrongWaySpeed || track.CenterLine.Count < 2)
        {
            return false;
        }

        IReadOnlyList<Vector2D> line = track.CenterLine;
        float bestDistance = float.MaxValue;
        Vector2D direction = default;
        for (int i = 0; i < line.Count; i++)
        {
            Vector2D a = line[i];
            Vector2D b = line[(i + 1) % line.Count];
            Vector2D ab = b - a;
            float lengthSquared = Vector2D.Dot(ab, ab);
            if (lengthSquared <= 0f)
            {
                continue;
            }

            float t = Math.Clamp(Vector2D.Dot(car.Position - a, ab) / lengthSquared, 0f, 1f);
            float distance = car.Position.DistanceTo(a + (ab * t));
            if (distance < bestDistance)
            {
                bestDistance = distance;
                direction = ab * (1f / MathF.Sqrt(lengthSquared));
            }
        }

        Vector2D velocity = Vector2D.FromAngle(car.Angle) * MathF.Sign(car.Speed);
        return Vector2D.Dot(velocity, direction) < -0.6f;
    }

    public RaceReport BuildReport(RaceSimulation race, RaceEntrant player, string skinId, string trackId, bool silent, DateTime playedAt = default)
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
            ClutchCheckpoints = _clutchCheckpoints,
            ClosestCall = _closestCall,
            HazardHits = _hazardHits,
            LedWholeRace = race.Mode == RaceMode.Elimination && !_wasEverNotFirst,
            WasLastInFinalThree = _wasLastInFinalThree,
            PhotoFinish = _photoFinish,
            FullThrottle = !_releasedThrottle,
            NeverSteeredRight = !_steeredRight,
            ReverseCheckpoints = _reverseCheckpoints,
            LongestWrongWaySeconds = _longestWrongWay,
            PlayedAt = playedAt,
            MaxTimeBanked = _maxTimeBanked,
            TimeLostToCrashes = _timeLost,
            FoundSecret = _foundSecret,
            Pauses = _pauses,
            Silent = silent,
        };
    }

}
