using Kyrios.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Kyrios.Game;

/// <summary>A partida: atualização, HUD de cada modo, feedback imediato (textos flutuantes, faixas de aviso,
/// flashes), menu de pausa e a tela de resultado.</summary>
public sealed partial class GameRoot
{
    private sealed class FloatingText
    {
        public string Text;
        public Vector2 Position;
        public Color Color;
        public float Size;
        public float Life;
        public float MaxLife;
    }

    private sealed class RingFlash
    {
        public Vector2 Position;
        public Color Color;
        public float Life;
    }

    private readonly List<FloatingText> _floatingTexts = [];
    private readonly List<RingFlash> _ringFlashes = [];

    private string _bannerText;
    private string _bannerSubtext;
    private Color _bannerColor;
    private float _bannerTimer;
    private float _bannerDuration;

    private int _lastPlayerLaps;
    private bool _recordBannerShown;

    // Contagem de largada (3, 2, 1, VAI!): a corrida só começa a andar quando ela termina.
    private const float CountdownSeconds = 3f;
    private const float GoSeconds = 0.8f;
    private float _countdown;
    private float _goTimer;
    private int _lastCountdownBeep;

    private sealed class Confetti
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Color Color;
        public float Spin;
        public float Life;
    }

    private readonly List<Confetti> _confetti = [];
    private readonly Random _confettiRandom = new();

    private enum PauseItem
    {
        Resume,
        Restart,
        Settings,
        Quit,
    }

    private static readonly PauseItem[] PauseItems = Enum.GetValues<PauseItem>();
    private int _pauseFocus;

    private enum ResultItem
    {
        Replay,
        Menu,
    }

    private static readonly ResultItem[] ResultItems = Enum.GetValues<ResultItem>();
    private int _resultFocus;
    private float _resultsTime;

    private const float BannerSeconds = 1.8f;
    private const float ResultsInputDelay = 0.7f;

    private void ResetRaceFeedback()
    {
        _floatingTexts.Clear();
        _ringFlashes.Clear();
        _confetti.Clear();
        _bannerTimer = 0f;
        _lastPlayerLaps = 0;
        _recordBannerShown = false;
        _countdown = CountdownSeconds;
        _goTimer = 0f;
        _lastCountdownBeep = (int)CountdownSeconds + 1;
    }

    private bool CountingDown => _countdown > 0f;

    /// <summary>3, 2, 1 com um bip em cada número e o "VAI!" no fim. O motor já ronca (e responde ao acelerador)
    /// enquanto isso, mas nenhum carro sai do lugar.</summary>
    private void UpdateCountdown(float dt, CarInput input)
    {
        _countdown -= dt;
        if (_countdown <= 0f)
        {
            _countdown = 0f;
            _goTimer = GoSeconds;
            _audio.PlayStartBeep(go: true);
            return;
        }

        int shown = (int)MathF.Ceiling(_countdown);
        if (shown < _lastCountdownBeep)
        {
            _lastCountdownBeep = shown;
            _audio.PlayStartBeep(go: false);
        }

        float rev = Math.Max(0f, input.Throttle);
        _audio.UpdateEngine(rev * _player.Car.Settings.MaxForwardSpeed * 0.6f, _player.Car.Settings.MaxForwardSpeed, false);
        UpdateRaceFeedback(dt);
    }

    private void DrawCountdown()
    {
        var center = new Vector2(AreaWidth / 2f, (AreaHeight / 2f) - 10f);
        if (CountingDown)
        {
            int shown = (int)MathF.Ceiling(_countdown);
            float local = Math.Clamp(shown - _countdown, 0f, 1f);
            float pop = 1f + ((1f - local) * (1f - local) * 0.6f);
            DimScreen(Color.Black * 0.22f);
            DrawCircle(center, 62f * MathF.Min(pop, 1.2f), HudFrameFill);
            DrawRingDots(center, 62f * MathF.Min(pop, 1.2f), AccentColor * 0.9f, (1f - local) * MathF.Tau);
            string text = shown.ToString();
            float size = 8f * pop;
            PixelFont.DrawShadowed(_spriteBatch, _pixel, text, center - new Vector2(PixelFont.Measure(text, size) / 2f, PixelFont.LineHeight(size) / 2f), size, Color.Lerp(AccentColor, Color.White, local * 0.5f));
            DrawCenteredText(new Rectangle(0, 0, (int)AreaWidth, 0), L.T("PREPARE-SE", "GET READY"), center.Y + 76f, MobileUi ? 2.8f : 2f, TextColor, shadow: true);
        }
        else if (_goTimer > 0f)
        {
            float t = 1f - (_goTimer / GoSeconds);
            float size = 8f * (1f + (t * 0.35f));
            float alpha = 1f - (t * t);
            PixelFont.DrawShadowed(_spriteBatch, _pixel, L.T("VAI!", "GO!"), center - new Vector2(PixelFont.Measure(L.T("VAI!", "GO!"), size) / 2f, PixelFont.LineHeight(size) / 2f), size, RecordColor * alpha, Color.Black * (0.6f * alpha));
        }
    }

    /// <summary>Contorno pontilhado de um círculo (a volta que o relógio da contagem vai "gastando").</summary>
    private void DrawRingDots(Vector2 center, float radius, Color color, float sweep)
    {
        const int dots = 36;
        for (int i = 0; i < dots; i++)
        {
            float a = (i * MathF.Tau / dots) - (MathF.PI / 2f);
            if (a + (MathF.PI / 2f) <= sweep)
            {
                DrawCircle(center + (new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius), 2.5f, color);
            }
        }
    }

    /// <summary>Chuva de confete na tela de resultado de uma vitória ou recorde.</summary>
    private void SpawnConfetti()
    {
        Color[] colors = [AccentColor, RecordColor, BoostFillColor, DangerColor, new Color(230, 120, 240), Color.White];
        int pieces = (int)(90 * EffectsDensity);
        for (int i = 0; i < pieces; i++)
        {
            _confetti.Add(new Confetti
            {
                Position = new Vector2((float)_confettiRandom.NextDouble() * AreaWidth, -20f - ((float)_confettiRandom.NextDouble() * 200f)),
                Velocity = new Vector2(((float)_confettiRandom.NextDouble() - 0.5f) * 60f, 90f + ((float)_confettiRandom.NextDouble() * 90f)),
                Color = colors[i % colors.Length],
                Spin = (float)_confettiRandom.NextDouble() * 6f,
                Life = 3.5f,
            });
        }
    }

    private void UpdateConfetti(float dt)
    {
        for (int i = _confetti.Count - 1; i >= 0; i--)
        {
            Confetti piece = _confetti[i];
            piece.Life -= dt;
            piece.Position += piece.Velocity * dt;
            piece.Velocity.X += MathF.Sin((piece.Life * 3f) + piece.Spin) * 20f * dt;
            if (piece.Life <= 0f || piece.Position.Y > AreaHeight + 20f)
            {
                _confetti.RemoveAt(i);
            }
        }
    }

    private void DrawConfetti()
    {
        foreach (Confetti piece in _confetti)
        {
            float alpha = Math.Clamp(piece.Life / 0.6f, 0f, 1f);
            float flip = MathF.Abs(MathF.Sin((piece.Life * 8f) + piece.Spin));
            _spriteBatch.Draw(_pixel, piece.Position, null, piece.Color * alpha, piece.Spin + piece.Life, new Vector2(0.5f), new Vector2(6f, 3f * flip + 1f), SpriteEffects.None, 0f);
        }
    }

    private void ShowBanner(string text, Color color, string subtext = null, float seconds = BannerSeconds)
    {
        _bannerText = text;
        _bannerSubtext = subtext;
        _bannerColor = color;
        _bannerDuration = seconds;
        _bannerTimer = seconds;
    }

    private void AddFloatingText(string text, Vector2 position, Color color, float size = 2f)
    {
        _floatingTexts.Add(new FloatingText { Text = text, Position = position, Color = color, Size = MobileUi ? size * 1.3f : size, Life = 1.1f, MaxLife = 1.1f });
    }

    private Vector2 PlayerWorldPosition => new(_player.Car.Position.X * CellSize, _player.Car.Position.Y * CellSize);

    // ---------- Atualização da partida ----------

    /// <summary>A corrida sempre avança em passos de exatamente 1/60 s, qualquer que seja a taxa de quadros da tela
    /// (60, 120, 144 Hz, ou um celular engasgando): física, IA, tempos e pontuação saem iguais em qualquer máquina
    /// — e iguais ao desktop, que já roda a 60 passos por segundo.</summary>
    private const double SimulationStep = 1.0 / 60.0;

    /// <summary>Depois de um engasgo longo (aba em segundo plano, celular lento) a corrida não tenta "recuperar"
    /// mais que isso de uma vez — continua de onde parou, como o desktop fazia.</summary>
    private const int MaxSimulationStepsPerFrame = 6;

    private readonly FixedStepClock _simulationClock = new(SimulationStep, MaxSimulationStepsPerFrame);

    private void UpdateRacing(float frameSeconds)
    {
        if (_input.Pause || TouchPausePressed())
        {
            OpenPause();
            return;
        }

        CarInput input = ReadCarInput();
        int steps = _simulationClock.Advance(frameSeconds);
        for (int i = 0; i < steps && _state == State.Racing; i++)
        {
            StepRace((float)SimulationStep, input);
        }
    }

    /// <summary>Um passo da corrida: contagem, simulação, eventos (sons, textos, eliminações) e o fim da partida.</summary>
    private void StepRace(float dt, CarInput input)
    {
        if (CountingDown)
        {
            UpdateCountdown(dt, input);
            return;
        }

        _goTimer = MathF.Max(0f, _goTimer - dt);
        _race.Update(dt, input);
        _raceTracker.Observe(_race, _player, dt, input);
        UpdateRaceFeedback(dt);

        _audio.UpdateEngine(_player.Car.Speed, _player.Car.Settings.MaxForwardSpeed, _player.Car.IsBoosting);
        UpdateParticleSpawns();
        _particles.Update(dt);

        bool collidingNow = _player.Car.HadHeadOnCollisionThisTick || _player.Car.HadCarCollisionThisTick || _player.Car.HadHazardCollisionThisTick;
        if (collidingNow && !_playerWasCollidingLastTick)
        {
            _audio.PlayCollision();
            Haptic(Game.Haptic.Collision);
            TriggerScreenShake(_player.Car.HadHeadOnCollisionThisTick ? 6f : 3.5f);
        }

        _playerWasCollidingLastTick = collidingNow;
        DetectRaceEvents();

        // Espectador depois de eliminado: ENTER pula direto pro resultado (a colocação dele já está decidida).
        if (_player.Eliminated && !_race.IsRaceOver && (_input.Confirm || MouseClicked))
        {
            for (int i = 0; i < 20000 && !_race.IsRaceOver; i++)
            {
                _race.Update(0.05f, CarInput.None);
                _raceTracker.Observe(_race, _player, 0.05f, CarInput.None);
            }
        }

        if (_race.IsRaceOver)
        {
            _audio.StopMusic();
            _audio.StopEngine();
            ProcessRaceEndRecords();
            RaceOutcome outcome = DetermineOutcome();
            _audio.PlayResultJingle(outcome);
            if (outcome == RaceOutcome.Victory)
            {
                SpawnConfetti();
                Haptic(Game.Haptic.Victory);
            }

            _resultFocus = 0;
            _resultsTime = 0f;
            _state = State.Results;
        }
    }

    /// <summary>Transforma o que aconteceu neste quadro em feedback visível sem parar a ação.</summary>
    private void DetectRaceEvents()
    {
        Car car = _player.Car;
        Vector2 playerPos = PlayerWorldPosition;

        bool lapCompleted = car.LapsCompleted > _lastPlayerLaps;
        if (car.CheckpointCrossedThisTick || lapCompleted)
        {
            if (lapCompleted)
            {
                _audio.PlayLap();
            }
            else
            {
                _audio.PlayCheckpoint();
            }

            Haptic(Game.Haptic.Checkpoint);

            _ringFlashes.Add(new RingFlash { Position = playerPos, Color = WorldTheme.Scenery.Checkpoint, Life = lapCompleted ? 0.8f : 0.5f });
        }

        if (_race.Mode == RaceMode.TimeAttack)
        {
            if (_race.TimeGainedThisTick > 0f)
            {
                AddFloatingText($"+{_race.TimeGainedThisTick:0.0} S", playerPos + new Vector2(0f, -18f), RecordColor, 2.2f);
            }

            if (_race.PointsGainedThisTick > 0f)
            {
                AddFloatingText($"+{_race.PointsGainedThisTick:0}", playerPos + new Vector2(24f, -6f), AccentColor, 1.8f);
            }

            if (_race.TimeLostThisTick > 0f)
            {
                AddFloatingText($"-{_race.TimeLostThisTick:0.0} S", playerPos + new Vector2(0f, -18f), DangerColor, 2.2f);
            }

            if (!_recordBannerShown && _saveData.BestScoreTimeAttack is > 0f and { } best && _player.Score > best)
            {
                _recordBannerShown = true;
                ShowBanner(L.T("NOVO RECORDE!", "NEW RECORD!"), RecordColor, L.T("CONTINUE PONTUANDO", "KEEP SCORING"));
                _audio.PlayUnlock(isSkin: false);
                Haptic(Game.Haptic.Achievement);
            }

            if (_race.TimeRemaining is { } timeRemaining && timeRemaining <= 3f)
            {
                int currentSecond = Math.Max(0, (int)MathF.Ceiling(timeRemaining));
                if (currentSecond >= 1 && currentSecond != _lastCountdownTickSecond)
                {
                    _audio.PlayCountdownTick();
                    _lastCountdownTickSecond = currentSecond;
                }
            }
        }
        else if (car.LapsCompleted > _lastPlayerLaps && !_player.Eliminated && !_player.Finished)
        {
            AddFloatingText(L.T($"VOLTA {car.LapsCompleted + 1}", $"LAP {car.LapsCompleted + 1}"), playerPos + new Vector2(0f, -20f), TextColor, 2f);
        }

        _lastPlayerLaps = car.LapsCompleted;

        if (_race.EliminationsThisTick.Count > 0)
        {
            _audio.PlayElimination();
            foreach (RaceEntrant eliminated in _race.EliminationsThisTick)
            {
                var at = new Vector2(eliminated.Car.Position.X * CellSize, eliminated.Car.Position.Y * CellSize);
                for (int i = 0; i < 10; i++)
                {
                    _particles.Spawn(at, RandomSpread(70f), life: 0.7f, size: 5f, new Color(90, 90, 100, 200));
                }
            }

            RaceEntrant last = _race.EliminationsThisTick[^1];
            int left = _race.Entrants.Count(e => !e.Eliminated);
            // Assistindo depois de eliminado, as quedas dos outros não vibram mais.
            if (_race.EliminationsThisTick.Contains(_player))
            {
                Haptic(Game.Haptic.PlayerEliminated);
            }
            else if (!_player.Eliminated)
            {
                Haptic(Game.Haptic.Elimination);
            }

            if (_race.EliminationsThisTick.Contains(_player))
            {
                TriggerScreenShake(8f);
                ShowBanner(L.T("VOCE FOI ELIMINADO!", "YOU'VE BEEN ELIMINATED!"), DangerColor, (ShowTouchControls ? L.T("TOQUE: VER RESULTADO", "TAP: SEE RESULTS") : L.T("ENTER: VER RESULTADO", "ENTER: SEE RESULTS")), seconds: 3.5f);
            }
            else if (!_player.Eliminated && _race.EliminationsThisTick.Count > 1)
            {
                ShowBanner(L.T($"{_race.EliminationsThisTick.Count} ELIMINADOS!", $"{_race.EliminationsThisTick.Count} ELIMINATED!"), DangerColor, left > 1 ? L.T($"RESTAM {left}", $"{left} LEFT") : null);
            }
            else if (!_player.Eliminated)
            {
                ShowBanner(L.T($"{_carNames[last]} ELIMINADO!", $"{_carNames[last]} ELIMINATED!"), _carColors[last], left > 1 ? L.T($"RESTAM {left}", $"{left} LEFT") : null);
            }
        }

        if (_raceTracker.SecretFoundThisTick)
        {
            _audio.PlayUnlock(isSkin: false);
            Haptic(Game.Haptic.Unlock);
            ShowBanner(L.T("SEGREDO ENCONTRADO!", "SECRET FOUND!"), new Color(215, 125, 235), SelectedTrack.SecretName);
            for (int i = 0; i < 12; i++)
            {
                _particles.Spawn(playerPos, RandomSpread(120f), life: 0.8f, size: 3f, new Color(255, 230, 120));
            }
        }
    }

    private void UpdateRaceFeedback(float dt)
    {
        _bannerTimer = MathF.Max(0f, _bannerTimer - dt);
        for (int i = _floatingTexts.Count - 1; i >= 0; i--)
        {
            FloatingText text = _floatingTexts[i];
            text.Life -= dt;
            text.Position.Y -= 30f * dt;
            if (text.Life <= 0f)
            {
                _floatingTexts.RemoveAt(i);
            }
        }

        for (int i = _ringFlashes.Count - 1; i >= 0; i--)
        {
            _ringFlashes[i].Life -= dt;
            if (_ringFlashes[i].Life <= 0f)
            {
                _ringFlashes.RemoveAt(i);
            }
        }
    }

    // ---------- Pausa ----------

    private Rectangle PauseButtonRect(int i) => MobileUi
        ? new Rectangle((int)(AreaWidth / 2f) - 220, 104 + (i * 86), 440, 72)
        : new Rectangle((int)(AreaWidth / 2f) - 140, 176 + (i * 50), 280, 40);

    private void OpenPause(bool countAsPause = true)
    {
        if (countAsPause)
        {
            _raceTracker.RegisterPause();
        }

        _audio.SetPaused(true);
        _audio.PlayMenuConfirm();
        if (countAsPause)
        {
            Haptic(Game.Haptic.Tap);
        }

        _pauseFocus = 0;
        _state = State.Paused;
    }

    private void ResumeRace()
    {
        _audio.SetPaused(false);
        _state = State.Racing;
    }

    private void UpdatePaused()
    {
        if (_input.Pause || _input.Back)
        {
            ResumeRace();
            return;
        }

        Point mouse = LogicalMousePoint();
        if (_input.MouseMoved)
        {
            for (int i = 0; i < PauseItems.Length; i++)
            {
                if (PauseButtonRect(i).Contains(mouse) && _pauseFocus != i)
                {
                    _pauseFocus = i;
                    _audio.PlayHover();
                }
            }
        }

        if (_input.MenuDown)
        {
            _pauseFocus = (_pauseFocus + 1) % PauseItems.Length;
            _audio.PlayMenuMove();
        }
        else if (_input.MenuUp)
        {
            _pauseFocus = (_pauseFocus - 1 + PauseItems.Length) % PauseItems.Length;
            _audio.PlayMenuMove();
        }

        bool clicked = MouseClicked && PauseButtonRect(_pauseFocus).Contains(mouse);
        if (!_input.Confirm && !clicked)
        {
            return;
        }

        switch (PauseItems[_pauseFocus])
        {
            case PauseItem.Resume:
                ResumeRace();
                break;
            case PauseItem.Restart:
                _audio.SetPaused(false);
                BeginRace(_race.Mode);
                break;
            case PauseItem.Settings:
                OpenSettings(State.Paused);
                break;
            case PauseItem.Quit:
                _audio.SetPaused(false);
                _audio.StopMusic();
                _audio.StopEngine();
                _audio.PlayMenuConfirm();
                StartNewRace(_race.Mode);
                _state = State.MainMenu;
                break;
        }
    }

    private void DrawPauseMenu()
    {
        DimScreen(OverlayDimColor);
        Rectangle panel = MobileUi
            ? new Rectangle((int)(AreaWidth / 2f) - 260, 12, 520, 454)
            : new Rectangle((int)(AreaWidth / 2f) - 180, 96, 360, 310);
        DrawAccentPanel(panel, AccentColor);
        DrawCenteredText(panel, L.T("PAUSADO", "PAUSED"), panel.Y + (MobileUi ? 30f : 24f), MobileUi ? 5f : TitleSize, AccentColor, shadow: true);
        string[] labels = [L.T("CONTINUAR", "RESUME"), L.T("REINICIAR", "RESTART"), L.T("CONFIGURACOES", "SETTINGS"), L.T("SAIR PARA O MENU", "QUIT TO MENU")];
        for (int i = 0; i < labels.Length; i++)
        {
            DrawButton(PauseButtonRect(i), labels[i], _pauseFocus == i, primary: i == 0, textSize: MobileUi ? 3f : 2f);
        }

        DrawKeyHints((L.T("SETAS", "ARROWS"), L.T("NAVEGAR", "NAVIGATE")), ("ENTER", L.T("CONFIRMAR", "CONFIRM")), ("ESC", L.T("CONTINUAR", "RESUME")));
    }

    // ---------- HUD ----------

    /// <summary>HUD enxuto: só o que importa no modo atual, numa faixa fina acima da pista (e o turbo na faixa de
    /// baixo) — nada fica por cima do asfalto tapando os carros.</summary>
    private void DrawLiveHud()
    {
        if (MobileUi)
        {
            if (_race.Mode == RaceMode.TimeAttack)
            {
                DrawMobileTimeAttackHud();
            }
            else
            {
                DrawMobileDeathRaceHud();
            }
        }
        else if (_race.Mode == RaceMode.TimeAttack)
        {
            DrawTimeAttackHud();
        }
        else
        {
            DrawDeathRaceHud();
        }

        // Com os controles de toque o combustível aparece no anel do botão TURBO.
        if (ShowTouchControls)
        {
            return;
        }

        DrawBoostBar(new Vector2(14f, AreaHeight + 13f));
        string pauseHint = L.T("ESC: PAUSA", "ESC: PAUSE");
        PixelFont.DrawShadowed(_spriteBatch, _pixel, pauseHint, new Vector2(AreaWidth - PixelFont.Measure(pauseHint, 1.4f) - 12f, AreaHeight + 15f), 1.4f, StatBadgeLabelColor);
    }

    // ----- HUD do celular: os mesmos dados, em painéis mais altos e texto maior (a faixa de cima tem 40 px lógicos) -----

    private const int MobileHudTop = -39;
    private const int MobileHudHeight = 42;

    /// <summary>Corrida Mortal: posição, volta e carros restantes à esquerda; a próxima eliminação no meio.</summary>
    private void DrawMobileDeathRaceHud()
    {
        IReadOnlyList<RaceEntrant> standings = _race.GetStandings();
        int position = standings.ToList().IndexOf(_player) + 1;
        int remaining = _race.Entrants.Count(e => !e.Eliminated);
        bool isOut = _player.Eliminated;

        var panel = new Rectangle(8, MobileHudTop, 430, MobileHudHeight);
        DrawHudPanel(panel);
        Color placeColor = isOut ? DangerColor : position == 1 ? AccentColor : TextColor;
        string place = isOut ? "X" : L.T($"{position}º", L.Ordinal(position));
        PixelFont.DrawShadowed(_spriteBatch, _pixel, place, new Vector2(panel.X + 12f, panel.Y + 8f), 3.4f, placeColor);
        float x = panel.X + 16f + PixelFont.Measure(place, 3.4f);
        PixelFont.Draw(_spriteBatch, _pixel, $"/{_race.Entrants.Count}", new Vector2(x, panel.Y + 18f), 2f, StatBadgeLabelColor);
        string lap = isOut ? L.T("ELIMINADO", "ELIMINATED") : L.T($"VOLTA {_player.Car.LapsCompleted + 1}", $"LAP {_player.Car.LapsCompleted + 1}");
        PixelFont.DrawShadowed(_spriteBatch, _pixel, lap, new Vector2(panel.X + 132f, panel.Y + 12f), 2.6f, isOut ? DangerColor : TextColor);
        string left = L.T($"RESTAM {remaining}", $"{remaining} LEFT");
        PixelFont.Draw(_spriteBatch, _pixel, left, new Vector2(panel.Right - 14f - PixelFont.Measure(left, 2.1f), panel.Y + 14f), 2.1f, StatBadgeLabelColor);

        RaceEntrant leader = standings.FirstOrDefault(e => !e.Eliminated && !e.Finished);
        if (leader is null || remaining <= 1)
        {
            return;
        }

        bool inDanger = !isOut && ReferenceEquals(standings.Where(e => !e.Eliminated && !e.Finished).LastOrDefault(), _player) && _race.ElapsedTime > 3f;
        var box = new Rectangle((int)(AreaWidth / 2f) - 150, MobileHudTop, 340, MobileHudHeight);
        if (inDanger)
        {
            float blink = (MathF.Sin(_visualTime * 10f) + 1f) / 2f;
            DrawRoundedRect(box, DangerColor * (0.6f + (0.3f * blink)), 7f);
        }
        else
        {
            DrawHudPanel(box);
        }

        string label = inDanger ? L.T("VOCE ESTA EM ULTIMO!", "YOU'RE IN LAST PLACE!") : L.T("PROXIMA ELIMINACAO", "NEXT ELIMINATION");
        DrawCenteredText(box, label, box.Y + 6f, FitTextSize(label, box.Width - 24f, 2f), inDanger ? Color.White : StatBadgeLabelColor);
        DrawProgressBar(new Rectangle(box.X + 18, box.Y + 26, box.Width - 36, 9), LapFraction(leader), inDanger ? Color.White : DangerColor * 0.9f, 4f);
    }

    /// <summary>Contra o Relógio: o tempo grande no meio (com quanto o próximo checkpoint dá), pontos e recorde à esquerda.</summary>
    private void DrawMobileTimeAttackHud()
    {
        float time = _race.TimeRemaining ?? 0f;
        bool low = time <= 5f;
        float pulse = low ? (MathF.Sin(_visualTime * 12f) + 1f) / 2f : 0f;

        var timer = new Rectangle((int)(AreaWidth / 2f) - 150, MobileHudTop, 330, MobileHudHeight);
        DrawHudPanel(timer);
        string timeText = $"{time:0.0}";
        float timeSize = 4f + (pulse * 0.25f);
        Color timeColor = low ? Color.Lerp(DangerColor, Color.White, pulse * 0.4f) : TextColor;
        PixelFont.DrawShadowed(_spriteBatch, _pixel, timeText, new Vector2(timer.X + 16f, timer.Y + 21f - (3.5f * timeSize)), timeSize, timeColor);
        PixelFont.Draw(_spriteBatch, _pixel, L.T("PROXIMO", "NEXT"), new Vector2(timer.X + 168f, timer.Y + 6f), 1.7f, StatBadgeLabelColor);
        PixelFont.DrawShadowed(_spriteBatch, _pixel, $"+{_race.NextTimeBonus:0.0} S", new Vector2(timer.X + 168f, timer.Y + 20f), 2.4f, RecordColor);

        var score = new Rectangle(8, MobileHudTop, 400, MobileHudHeight);
        DrawHudPanel(score);
        PixelFont.Draw(_spriteBatch, _pixel, L.T("PONTOS", "POINTS"), new Vector2(score.X + 12f, score.Y + 6f), 1.7f, StatBadgeLabelColor);
        PixelFont.DrawShadowed(_spriteBatch, _pixel, $"{_player.Score:0}", new Vector2(score.X + 12f, score.Y + 19f), 2.6f, AccentColor);
        string record = _saveData.BestScoreTimeAttack is > 0f and { } best ? L.T($"RECORDE {best:0}", $"RECORD {best:0}") : L.T("SEM RECORDE", "NO RECORD");
        PixelFont.Draw(_spriteBatch, _pixel, record, new Vector2(score.X + 150f, score.Y + 6f), 1.8f, _recordBannerShown ? RecordColor : StatBadgeLabelColor);
        PixelFont.Draw(_spriteBatch, _pixel, L.T($"VOLTA {_player.Car.LapsCompleted + 1}", $"LAP {_player.Car.LapsCompleted + 1}"), new Vector2(score.X + 150f, score.Y + 21f), 2.2f, TextColor);
    }

    /// <summary>O HUD mora nas faixas de cenário (TrackMargin) acima e abaixo da pista, fora do asfalto.</summary>
    private const int HudTop = -TrackMargin + 4;
    private const int HudPanelHeight = 32;

    private void DrawHudPanel(Rectangle rect)
    {
        DrawRoundedRect(rect, HudFrameFill, 7f);
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X + 6, rect.Bottom - 3, rect.Width - 12, 2), AccentColor * 0.8f);
    }

    private void DrawDeathRaceHud()
    {
        IReadOnlyList<RaceEntrant> standings = _race.GetStandings();
        int position = standings.ToList().IndexOf(_player) + 1;
        int remaining = _race.Entrants.Count(e => !e.Eliminated);

        var panel = new Rectangle(10, HudTop, 330, HudPanelHeight);
        DrawHudPanel(panel);
        bool isOut = _player.Eliminated;
        Color placeColor = isOut ? DangerColor : position == 1 ? AccentColor : TextColor;
        string place = isOut ? "X" : position.ToString();
        PixelFont.DrawShadowed(_spriteBatch, _pixel, place, new Vector2(panel.X + 12f, panel.Y + 6f), 2.8f, placeColor);
        float afterNumber = panel.X + 12f + PixelFont.Measure(place, 2.8f) + 2f;
        if (!isOut)
        {
            PixelFont.Draw(_spriteBatch, _pixel, L.OrdinalSuffix(position), new Vector2(afterNumber, panel.Y + 6f), 1.4f, placeColor);
        }

        PixelFont.Draw(_spriteBatch, _pixel, L.T($"DE {_race.Entrants.Count}", $"OF {_race.Entrants.Count}"), new Vector2(afterNumber, panel.Y + 18f), 1.2f, StatBadgeLabelColor);
        PixelFont.Draw(_spriteBatch, _pixel, isOut ? L.T("ELIMINADO", "ELIMINATED") : L.T($"VOLTA {_player.Car.LapsCompleted + 1}", $"LAP {_player.Car.LapsCompleted + 1}"), new Vector2(panel.X + 104f, panel.Y + 9f), 1.8f, isOut ? DangerColor : TextColor);
        PixelFont.Draw(_spriteBatch, _pixel, L.T($"RESTAM {remaining}", $"{remaining} LEFT"), new Vector2(panel.X + 222f, panel.Y + 11f), 1.4f, StatBadgeLabelColor);

        // Próxima eliminação: acontece quando o líder completar a volta — a barra mostra o quanto falta.
        // Quando o jogador é o último, o próprio painel pisca em vermelho com o aviso.
        RaceEntrant leader = standings.FirstOrDefault(e => !e.Eliminated && !e.Finished);
        if (leader is not null && remaining > 1)
        {
            bool inDanger = !_player.Eliminated && ReferenceEquals(standings.Where(e => !e.Eliminated && !e.Finished).LastOrDefault(), _player) && _race.ElapsedTime > 3f;
            var box = new Rectangle((int)(AreaWidth / 2f) - 150, HudTop, 300, HudPanelHeight);
            if (inDanger)
            {
                float blink = (MathF.Sin(_visualTime * 10f) + 1f) / 2f;
                DrawRoundedRect(box, DangerColor * (0.6f + (0.3f * blink)), 7f);
            }
            else
            {
                DrawHudPanel(box);
            }

            string label = inDanger ? L.T("VOCE ESTA EM ULTIMO!", "YOU'RE IN LAST PLACE!") : L.T("PROXIMA ELIMINACAO", "NEXT ELIMINATION");
            DrawCenteredText(box, label, box.Y + 5f, 1.3f, inDanger ? Color.White : StatBadgeLabelColor);
            var bar = new Rectangle(box.X + 20, box.Y + 18, box.Width - 40, 7);
            DrawProgressBar(bar, LapFraction(leader), inDanger ? Color.White : DangerColor * 0.9f, 3f);
        }
    }

    /// <summary>Quanto da volta atual um carro já percorreu (0-1), pelos checkpoints e pela distância ao próximo.</summary>
    private float LapFraction(RaceEntrant entrant)
    {
        Track track = _race.Track;
        int segments = track.CheckpointCount + 1;
        int next = Math.Min(entrant.Car.NextCheckpointIndex, segments);
        Vector2D target = next > track.CheckpointCount ? track.FinishLineCenter : track.CheckpointCenters[next];
        Vector2D previous = next <= 1 ? track.FinishLineCenter : track.CheckpointCenters[next - 1];
        float segmentLength = MathF.Max(1f, previous.DistanceTo(target));
        float inSegment = Math.Clamp(1f - (entrant.Car.Position.DistanceTo(target) / segmentLength), 0f, 1f);
        return Math.Clamp((next - 1 + inSegment) / segments, 0f, 1f);
    }

    private void DrawTimeAttackHud()
    {
        float time = _race.TimeRemaining ?? 0f;
        bool low = time <= 5f;
        float pulse = low ? (MathF.Sin(_visualTime * 12f) + 1f) / 2f : 0f;

        var timer = new Rectangle((int)(AreaWidth / 2f) - 120, HudTop, 240, HudPanelHeight);
        DrawHudPanel(timer);
        string timeText = $"{time:0.0}";
        Color timeColor = low ? Color.Lerp(DangerColor, Color.White, pulse * 0.4f) : TextColor;
        float timeSize = 3f + (pulse * 0.2f);
        PixelFont.DrawShadowed(_spriteBatch, _pixel, timeText, new Vector2(timer.X + 14f, timer.Y + 16f - (3.5f * timeSize)), timeSize, timeColor);
        PixelFont.Draw(_spriteBatch, _pixel, "BONUS", new Vector2(timer.X + 128f, timer.Y + 6f), 1.2f, StatBadgeLabelColor);
        PixelFont.Draw(_spriteBatch, _pixel, $"{_race.TimeBonusFactor * 100f:0}%", new Vector2(timer.X + 128f, timer.Y + 17f), 1.4f, TextColor);

        var score = new Rectangle(10, HudTop, 340, HudPanelHeight);
        DrawHudPanel(score);
        PixelFont.Draw(_spriteBatch, _pixel, L.T("PONTOS", "POINTS"), new Vector2(score.X + 12f, score.Y + 6f), 1.2f, StatBadgeLabelColor);
        PixelFont.DrawShadowed(_spriteBatch, _pixel, $"{_player.Score:0}", new Vector2(score.X + 64f, score.Y + 7f), 2.4f, AccentColor);
        string record = _saveData.BestScoreTimeAttack is > 0f and { } best ? L.T($"RECORDE {best:0}", $"RECORD {best:0}") : L.T("SEM RECORDE", "NO RECORD");
        PixelFont.Draw(_spriteBatch, _pixel, record, new Vector2(score.X + 170f, score.Y + 6f), 1.2f, _recordBannerShown ? RecordColor : StatBadgeLabelColor);
        PixelFont.Draw(_spriteBatch, _pixel, L.T($"VOLTA {_player.Car.LapsCompleted + 1}", $"LAP {_player.Car.LapsCompleted + 1}"), new Vector2(score.X + 170f, score.Y + 17f), 1.4f, TextColor);
    }

    private void DrawBoostBar(Vector2 position)
    {
        const float width = 140f;
        const float height = 10f;
        var back = new Rectangle((int)position.X - 2, (int)position.Y - 2, (int)width + 4, (int)height + 4);
        DrawRoundedRect(back, HudFrameFill, 4f);
        float fraction = Math.Clamp(_player.Car.BoostFuel / _player.Car.Settings.BoostMaxFuel, 0f, 1f);
        _spriteBatch.Draw(_pixel, new Rectangle((int)position.X, (int)position.Y, (int)(width * fraction), (int)height), _player.Car.IsBoosting ? BoostActiveColor : BoostFillColor);
        PixelFont.DrawShadowed(_spriteBatch, _pixel, ShowTouchControls ? "TURBO" : L.T("TURBO (SHIFT)", "BOOST (SHIFT)"), new Vector2(position.X + width + 12f, position.Y + 1f), 1.3f, TextColor);
    }

    /// <summary>Textos subindo a partir do carro, anéis de checkpoint e a faixa de aviso no centro da tela.</summary>
    private void DrawRaceFeedback()
    {
        foreach (RingFlash ring in _ringFlashes)
        {
            float t = 1f - (ring.Life / 0.5f);
            float radius = 10f + (t * 26f);
            for (int i = 0; i < 16; i++)
            {
                float a = i * MathF.Tau / 16f;
                DrawCircle(ring.Position + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius, 2f, ring.Color * (1f - t));
            }
        }

        foreach (FloatingText text in _floatingTexts)
        {
            float alpha = Math.Clamp(text.Life / text.MaxLife * 1.5f, 0f, 1f);
            float width = PixelFont.Measure(text.Text, text.Size);
            PixelFont.DrawShadowed(_spriteBatch, _pixel, text.Text, text.Position - new Vector2(width / 2f, 0f), text.Size, text.Color * alpha, Color.Black * (0.5f * alpha));
        }

        if (_bannerTimer <= 0f)
        {
            return;
        }

        float elapsed = _bannerDuration - _bannerTimer;
        float appear = Math.Clamp(elapsed / 0.2f, 0f, 1f);
        float fade = Math.Clamp(_bannerTimer / 0.3f, 0f, 1f);
        float alphaBanner = MathF.Min(appear, fade);
        float scale = 1f + ((1f - appear) * 0.4f);
        const float size = 3.2f;
        float bannerWidth = PixelFont.Measure(_bannerText, size * scale) + 40f;
        var rect = new Rectangle((int)((AreaWidth - bannerWidth) / 2f), 112, (int)bannerWidth, _bannerSubtext is null ? 44 : 62);
        DrawRoundedRect(rect, HudFrameFill * alphaBanner, 10f);
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X + 8, rect.Y + 4, rect.Width - 16, 3), _bannerColor * alphaBanner);
        DrawCenteredText(rect, _bannerText, rect.Y + 12f, size * scale, _bannerColor * alphaBanner, shadow: true);
        if (_bannerSubtext is not null)
        {
            DrawCenteredText(rect, _bannerSubtext, rect.Y + 42f, MobileUi ? 2f : 1.6f, TextColor * alphaBanner);
        }
    }

    // ---------- Resultado ----------

    /// <summary>Fica abaixo da área do aviso de desbloqueio (que desce do topo), pra não cobrir nada.</summary>
    private static Rectangle ResultsPanel => MobileUi ? new Rectangle(214, 104, 760, 378) : new Rectangle(294, 112, 600, 344);

    private Rectangle ResultButtonRect(int i) => MobileUi
        ? new Rectangle(ResultsPanel.X + 40 + (i * 350), ResultsPanel.Bottom - 88, 330, 74)
        : new Rectangle(ResultsPanel.X + 70 + (i * 240), ResultsPanel.Bottom - 60, 220, 40);

    private void UpdateResults(float frameSeconds)
    {
        _resultsTime += frameSeconds;
        _bannerTimer = 0f;
        UpdateConfetti(frameSeconds);

        // Um instante pra ver o resultado: ENTER apertado repetidamente no fim da corrida não pula direto pra outra.
        if (_resultsTime < ResultsInputDelay)
        {
            return;
        }


        Point mouse = LogicalMousePoint();
        if (_input.MouseMoved)
        {
            for (int i = 0; i < ResultItems.Length; i++)
            {
                if (ResultButtonRect(i).Contains(mouse) && _resultFocus != i)
                {
                    _resultFocus = i;
                    _audio.PlayHover();
                }
            }
        }

        if (_input.MenuRight || _input.MenuLeft)
        {
            _resultFocus = (_resultFocus + 1) % ResultItems.Length;
            _audio.PlayMenuMove();
        }

        if (_input.WasJustPressed(Keys.R))
        {
            BeginRace(_race.Mode);
            return;
        }

        if (_input.Back || _input.WasJustPressed(Keys.M))
        {
            GoToMainMenu();
            return;
        }

        bool clicked = MouseClicked && ResultButtonRect(_resultFocus).Contains(mouse);
        if (!_input.Confirm && !clicked)
        {
            return;
        }

        if (ResultItems[_resultFocus] == ResultItem.Replay)
        {
            BeginRace(_race.Mode);
        }
        else
        {
            GoToMainMenu();
        }
    }

    private void GoToMainMenu()
    {
        _audio.PlayMenuConfirm();
        StartNewRace(_race.Mode);
        _mainFocus = 0;
        _state = State.MainMenu;
    }

    private void DrawResults()
    {
        DimScreen(OverlayDimColor);
        Rectangle panel = ResultsPanel;

        bool timeAttack = _race.Mode == RaceMode.TimeAttack;
        (string headline, Color color) = timeAttack
            ? (_records.NewScoreRecord ? L.T("NOVO RECORDE!", "NEW RECORD!") : L.T("TEMPO ESGOTADO!", "TIME'S UP!"), _records.NewScoreRecord ? RecordColor : BoostFillColor)
            : _player.Finished ? (L.T("VITORIA!", "VICTORY!"), AccentColor) : (L.T("ELIMINADO", "ELIMINATED"), DangerColor);

        DrawAccentPanel(panel, color);
        float pop = 1f + (MathF.Max(0f, 1f - (_resultsTime / 0.3f)) * 0.3f);
        DrawCenteredText(panel, headline, panel.Y + (MobileUi ? 18f : 26f), 4.5f * pop, color, shadow: true);

        var rows = new Rectangle(panel.X + 40, panel.Y + (MobileUi ? 66 : 84), panel.Width - 80, 0);
        foreach ((string label, string value, bool highlight) in BuildResultRows())
        {
            DrawResultRow(rows, label, value, highlight);
            rows.Y += MobileUi ? 36 : 32;
        }

        string[] labels = [L.T("JOGAR NOVAMENTE", "PLAY AGAIN"), "MENU"];
        for (int i = 0; i < labels.Length; i++)
        {
            DrawButton(ResultButtonRect(i), labels[i], _resultFocus == i, primary: i == 0, textSize: MobileUi ? 2.8f : 1.8f);
        }

        DrawKeyHints(("ENTER", L.T("CONFIRMAR", "CONFIRM")), ("R", L.T("JOGAR DE NOVO", "PLAY AGAIN")), ("ESC", "MENU"));
        DrawConfetti();
    }

    private List<(string Label, string Value, bool Highlight)> BuildResultRows()
    {
        string lap = _player.Car.BestLapTime is { } bestLap ? TimeFormat.Precise(bestLap) : "--";
        if (_race.Mode == RaceMode.TimeAttack)
        {
            string record = _saveData.BestScoreTimeAttack is { } best ? $"{best:0} PTS" : "--";
            return
            [
                (L.T("PONTUACAO", "SCORE"), $"{_player.Score:0} PTS", true),
                (_records.NewScoreRecord ? L.T("NOVO RECORDE PESSOAL", "NEW PERSONAL BEST") : L.T("RECORDE PESSOAL", "PERSONAL BEST"), record, _records.NewScoreRecord),
                (L.T("VOLTAS", "LAPS"), _player.Car.LapsCompleted.ToString(), false),
                (L.T("MELHOR VOLTA", "BEST LAP"), lap, _records.NewLapRecord),
                (L.T("BATIDAS", "CRASHES"), _raceTracker.Collisions.ToString(), false),
                (L.T("TEMPO JOGADO", "TIME PLAYED"), TimeFormat.Short(_race.ElapsedTime), false),
            ];
        }

        int place = _player.FinishPlace ?? _race.Entrants.Count;
        return
        [
            (L.T("POSICAO FINAL", "FINAL POSITION"), L.T($"{place}º DE {_race.Entrants.Count}", $"{L.Ordinal(place)} OF {_race.Entrants.Count}"), _player.Finished),
            (L.T("VITORIAS NA MORTAL", "DEATH RACE WINS"), _saveData.EliminationWins.ToString(), _player.Finished),
            (L.T("SEQUENCIA DE VITORIAS", "WIN STREAK"), _saveData.EliminationWinStreak.ToString(), false),
            (L.T("MELHOR VOLTA", "BEST LAP"), lap, _records.NewLapRecord),
            (L.T("BATIDAS", "CRASHES"), _raceTracker.Collisions.ToString(), false),
            (L.T("TEMPO DE CORRIDA", "RACE TIME"), TimeFormat.Short(_player.FinishTime ?? _race.ElapsedTime), false),
        ];
    }

    private void DrawResultRow(Rectangle area, string label, string value, bool highlight)
    {
        int height = MobileUi ? 32 : 26;
        float valueSize = MobileUi ? 2.6f : 2f;
        DrawRoundedRect(new Rectangle(area.X, area.Y, area.Width, height), highlight ? StatBadgeFill : new Color(28, 32, 46), 6f);
        if (highlight)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(area.X, area.Y, 4, height), RecordColor);
        }

        PixelFont.Draw(_spriteBatch, _pixel, label, new Vector2(area.X + 12f, area.Y + ((height - PixelFont.LineHeight(TextSmall)) / 2f)), TextSmall, StatBadgeLabelColor);
        float valueWidth = PixelFont.Measure(value, valueSize);
        PixelFont.DrawShadowed(_spriteBatch, _pixel, value, new Vector2(area.Right - 12f - valueWidth, area.Y + ((height - PixelFont.LineHeight(valueSize)) / 2f)), valueSize, highlight ? RecordColor : TextColor);
    }
}
