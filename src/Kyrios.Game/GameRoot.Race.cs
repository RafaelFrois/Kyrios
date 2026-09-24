using Kyrios.Core;
using Microsoft.Xna.Framework;
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
        ChooseTrack,
        Menu,
    }

    private static readonly ResultItem[] ResultItems = Enum.GetValues<ResultItem>();
    private int _resultFocus;
    private float _resultsTime;
    private int _unlocksRevealed;

    private const float BannerSeconds = 1.8f;
    private const float UnlockRevealInterval = 0.35f;
    private const float UnlockRevealDelay = 0.6f;

    private void ResetRaceFeedback()
    {
        _floatingTexts.Clear();
        _ringFlashes.Clear();
        _bannerTimer = 0f;
        _lastPlayerLaps = 0;
        _recordBannerShown = false;
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
        _floatingTexts.Add(new FloatingText { Text = text, Position = position, Color = color, Size = size, Life = 1.1f, MaxLife = 1.1f });
    }

    private Vector2 PlayerWorldPosition => new(_player.Car.Position.X * CellSize, _player.Car.Position.Y * CellSize);

    // ---------- Atualização da partida ----------

    private void UpdateRacing(float frameSeconds)
    {
        if (_input.Pause)
        {
            OpenPause();
            return;
        }

        float dt = Math.Min(frameSeconds, 0.1f);
        _race.Update(dt, _input.BuildCarInput());
        _raceTracker.Observe(_race, _player, dt);
        UpdateRaceFeedback(dt);

        _audio.UpdateEngine(_player.Car.Speed, _player.Car.Settings.MaxForwardSpeed, _player.Car.IsBoosting);
        UpdateParticleSpawns();
        _particles.Update(dt);

        bool collidingNow = _player.Car.HadHeadOnCollisionThisTick || _player.Car.HadCarCollisionThisTick || _player.Car.HadHazardCollisionThisTick;
        if (collidingNow && !_playerWasCollidingLastTick)
        {
            _audio.PlayCollision();
            TriggerScreenShake(_player.Car.HadHeadOnCollisionThisTick ? 6f : 3.5f);
        }

        _playerWasCollidingLastTick = collidingNow;
        DetectRaceEvents();

        // Espectador depois de eliminado: ENTER pula direto pro resultado (a colocação dele já está decidida).
        if (_player.Eliminated && !_race.IsRaceOver && _input.Confirm)
        {
            for (int i = 0; i < 20000 && !_race.IsRaceOver; i++)
            {
                _race.Update(0.05f, CarInput.None);
                _raceTracker.Observe(_race, _player, 0.05f);
            }
        }

        if (_race.IsRaceOver)
        {
            _audio.StopMusic();
            _audio.StopEngine();
            ProcessRaceEndRecords();
            _audio.PlayResultJingle(DetermineOutcome());
            _resultFocus = 0;
            _state = State.Results;
        }
    }

    /// <summary>Transforma o que aconteceu neste quadro em feedback visível sem parar a ação.</summary>
    private void DetectRaceEvents()
    {
        Car car = _player.Car;
        Vector2 playerPos = PlayerWorldPosition;

        if (car.CheckpointCrossedThisTick || car.LapsCompleted > _lastPlayerLaps)
        {
            _audio.PlayCheckpoint();
            _ringFlashes.Add(new RingFlash { Position = playerPos, Color = WorldTheme.Scenery.Checkpoint, Life = 0.5f });
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
                ShowBanner("NOVO RECORDE!", RecordColor, "CONTINUE PONTUANDO");
                _audio.PlayUnlock(isSkin: false);
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
            AddFloatingText($"VOLTA {car.LapsCompleted + 1}", playerPos + new Vector2(0f, -20f), TextColor, 2f);
        }

        _lastPlayerLaps = car.LapsCompleted;

        if (_race.EliminatedThisTick is { } eliminated)
        {
            _audio.PlayElimination();
            if (ReferenceEquals(eliminated, _player))
            {
                TriggerScreenShake(8f);
                ShowBanner("VOCE FOI ELIMINADO!", DangerColor, "ENTER: VER RESULTADO", seconds: 3.5f);
            }
            else if (!_player.Eliminated)
            {
                int left = _race.Entrants.Count(e => !e.Eliminated);
                ShowBanner($"{_carNames[eliminated]} ELIMINADO!", _carColors[eliminated], left > 1 ? $"RESTAM {left}" : null);
            }
        }

        if (_raceTracker.SecretFoundThisTick)
        {
            _audio.PlayUnlock(isSkin: false);
            ShowBanner("SEGREDO ENCONTRADO!", new Color(215, 125, 235), SelectedTrack.SecretName);
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

    private Rectangle PauseButtonRect(int i) => new((int)(AreaWidth / 2f) - 140, 176 + (i * 50), 280, 40);

    private void OpenPause()
    {
        _raceTracker.RegisterPause();
        _audio.SetPaused(true);
        _audio.PlayMenuConfirm();
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
                if (PauseButtonRect(i).Contains(mouse))
                {
                    _pauseFocus = i;
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

        bool clicked = _input.WasMouseLeftJustPressed && PauseButtonRect(_pauseFocus).Contains(mouse);
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
        var panel = new Rectangle((int)(AreaWidth / 2f) - 180, 96, 360, 310);
        DrawAccentPanel(panel, AccentColor);
        DrawCenteredText(panel, "PAUSADO", panel.Y + 24f, TitleSize, AccentColor, shadow: true);
        string[] labels = ["CONTINUAR", "REINICIAR", "CONFIGURACOES", "SAIR PARA O MENU"];
        for (int i = 0; i < labels.Length; i++)
        {
            DrawButton(PauseButtonRect(i), labels[i], _pauseFocus == i, primary: i == 0);
        }

        DrawKeyHints(("SETAS", "NAVEGAR"), ("ENTER", "CONFIRMAR"), ("ESC", "CONTINUAR"));
    }

    // ---------- HUD ----------

    /// <summary>HUD enxuto: só o que importa no modo atual, com a informação principal maior.</summary>
    private void DrawLiveHud()
    {
        if (_race.Mode == RaceMode.TimeAttack)
        {
            DrawTimeAttackHud();
        }
        else
        {
            DrawDeathRaceHud();
        }

        DrawBoostBar(new Vector2(14f, AreaHeight - 40f));
        const string pauseHint = "ESC: PAUSA";
        PixelFont.DrawShadowed(_spriteBatch, _pixel, pauseHint, new Vector2(AreaWidth - PixelFont.Measure(pauseHint, 1.4f) - 12f, AreaHeight - 18f), 1.4f, StatBadgeLabelColor);
    }

    private void DrawHudPanel(Rectangle rect)
    {
        DrawRoundedRect(rect, HudFrameFill, 8f);
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X + 6, rect.Bottom - 3, rect.Width - 12, 2), AccentColor * 0.8f);
    }

    private void DrawDeathRaceHud()
    {
        IReadOnlyList<RaceEntrant> standings = _race.GetStandings();
        int position = standings.ToList().IndexOf(_player) + 1;
        int remaining = _race.Entrants.Count(e => !e.Eliminated);

        var panel = new Rectangle(10, 8, 250, 62);
        DrawHudPanel(panel);
        bool isOut = _player.Eliminated;
        Color placeColor = isOut ? DangerColor : position == 1 ? AccentColor : TextColor;
        string place = isOut ? "X" : position.ToString();
        PixelFont.DrawShadowed(_spriteBatch, _pixel, place, new Vector2(panel.X + 14f, panel.Y + 12f), 5f, placeColor);
        float afterNumber = panel.X + 14f + PixelFont.Measure(place, 5f) + 3f;
        if (!isOut)
        {
            PixelFont.Draw(_spriteBatch, _pixel, "º", new Vector2(afterNumber, panel.Y + 12f), 2f, placeColor);
        }

        PixelFont.Draw(_spriteBatch, _pixel, $"DE {_race.Entrants.Count}", new Vector2(afterNumber, panel.Y + 32f), 1.5f, StatBadgeLabelColor);
        PixelFont.Draw(_spriteBatch, _pixel, isOut ? "ELIMINADO" : $"VOLTA {_player.Car.LapsCompleted + 1}", new Vector2(panel.X + 118f, panel.Y + 14f), 2f, isOut ? DangerColor : TextColor);
        PixelFont.Draw(_spriteBatch, _pixel, $"RESTAM {remaining} CARROS", new Vector2(panel.X + 118f, panel.Y + 38f), 1.5f, StatBadgeLabelColor);

        // Próxima eliminação: acontece quando o líder completar a volta — a barra mostra o quanto falta.
        RaceEntrant leader = standings.FirstOrDefault(e => !e.Eliminated && !e.Finished);
        if (leader is not null && remaining > 1)
        {
            var bar = new Rectangle((int)(AreaWidth / 2f) - 130, 14, 260, 8);
            string label = "PROXIMA ELIMINACAO";
            DrawCenteredText(new Rectangle(bar.X, 0, bar.Width, 0), label, 26f, 1.4f, StatBadgeLabelColor);
            DrawProgressBar(bar, LapFraction(leader), DangerColor * 0.9f, 4f);

            bool inDanger = !_player.Eliminated && ReferenceEquals(standings.Where(e => !e.Eliminated && !e.Finished).LastOrDefault(), _player) && _race.ElapsedTime > 3f;
            if (inDanger)
            {
                float blink = (MathF.Sin(_visualTime * 10f) + 1f) / 2f;
                var warn = new Rectangle((int)(AreaWidth / 2f) - 120, 42, 240, 24);
                DrawRoundedRect(warn, DangerColor * (0.55f + (0.35f * blink)), 6f);
                DrawCenteredText(warn, "VOCE ESTA EM ULTIMO!", warn.Y + 6f, 1.8f, Color.White);
            }
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

        var timer = new Rectangle((int)(AreaWidth / 2f) - 90, 6, 180, 62);
        DrawHudPanel(timer);
        string timeText = $"{time:0.0}";
        Color timeColor = low ? Color.Lerp(DangerColor, Color.White, pulse * 0.4f) : TextColor;
        DrawCenteredText(timer, timeText, timer.Y + 8f, 4.5f + (pulse * 0.4f), timeColor, shadow: true);
        DrawCenteredText(timer, $"BONUS DE TEMPO {_race.TimeBonusFactor * 100f:0}%", timer.Y + 46f, 1.3f, StatBadgeLabelColor);

        var score = new Rectangle(10, 8, 220, 62);
        DrawHudPanel(score);
        PixelFont.Draw(_spriteBatch, _pixel, "PONTOS", new Vector2(score.X + 14f, score.Y + 10f), 1.4f, StatBadgeLabelColor);
        PixelFont.DrawShadowed(_spriteBatch, _pixel, $"{_player.Score:0}", new Vector2(score.X + 14f, score.Y + 24f), 3.2f, AccentColor);
        string record = _saveData.BestScoreTimeAttack is > 0f and { } best ? $"RECORDE {best:0}" : "SEM RECORDE";
        PixelFont.Draw(_spriteBatch, _pixel, record, new Vector2(score.X + 118f, score.Y + 12f), 1.4f, _recordBannerShown ? RecordColor : StatBadgeLabelColor);
        PixelFont.Draw(_spriteBatch, _pixel, $"VOLTA {_player.Car.LapsCompleted + 1}", new Vector2(score.X + 118f, score.Y + 34f), 1.6f, TextColor);
    }

    private void DrawBoostBar(Vector2 position)
    {
        const float width = 140f;
        const float height = 10f;
        var back = new Rectangle((int)position.X - 2, (int)position.Y - 2, (int)width + 4, (int)height + 4);
        DrawRoundedRect(back, HudFrameFill, 4f);
        float fraction = Math.Clamp(_player.Car.BoostFuel / _player.Car.Settings.BoostMaxFuel, 0f, 1f);
        _spriteBatch.Draw(_pixel, new Rectangle((int)position.X, (int)position.Y, (int)(width * fraction), (int)height), _player.Car.IsBoosting ? BoostActiveColor : BoostFillColor);
        PixelFont.DrawShadowed(_spriteBatch, _pixel, "TURBO (SHIFT)", new Vector2(position.X, position.Y + height + 5f), 1.3f, TextColor);
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
            DrawCenteredText(rect, _bannerSubtext, rect.Y + 42f, 1.6f, TextColor * alphaBanner);
        }
    }

    // ---------- Resultado ----------

    private void StartResultsReveal()
    {
        _resultsTime = 0f;
        _unlocksRevealed = 0;
    }

    private static readonly Rectangle ResultsPanel = new(120, 20, 948, 438);

    private Rectangle ResultButtonRect(int i) => new(ResultsPanel.X + 150 + (i * 220), ResultsPanel.Bottom - 56, 200, 40);

    private void UpdateResults(float frameSeconds)
    {
        _resultsTime += frameSeconds;
        _bannerTimer = 0f;

        // Os desbloqueios aparecem um de cada vez, cada um com seu som — é a "notificação" do fim da partida.
        int shouldShow = Math.Min(_raceUnlocks.Count, Math.Max(0, (int)((_resultsTime - UnlockRevealDelay) / UnlockRevealInterval) + 1));
        if (_resultsTime >= UnlockRevealDelay && shouldShow > _unlocksRevealed)
        {
            _unlocksRevealed = shouldShow;
            if (_unlocksRevealed <= 5)
            {
                UnlockNotice notice = _raceUnlocks[_unlocksRevealed - 1];
                _audio.PlayUnlock(isSkin: notice.Achievement is null);
            }
        }

        Point mouse = LogicalMousePoint();
        if (_input.MouseMoved)
        {
            for (int i = 0; i < ResultItems.Length; i++)
            {
                if (ResultButtonRect(i).Contains(mouse))
                {
                    _resultFocus = i;
                }
            }
        }

        if (_input.MenuRight)
        {
            _resultFocus = (_resultFocus + 1) % ResultItems.Length;
            _audio.PlayMenuMove();
        }
        else if (_input.MenuLeft)
        {
            _resultFocus = (_resultFocus - 1 + ResultItems.Length) % ResultItems.Length;
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

        bool clicked = _input.WasMouseLeftJustPressed && ResultButtonRect(_resultFocus).Contains(mouse);
        if (!_input.Confirm && !clicked)
        {
            return;
        }

        switch (ResultItems[_resultFocus])
        {
            case ResultItem.Replay:
                BeginRace(_race.Mode);
                break;
            case ResultItem.ChooseTrack:
                _selectedMode = _race.Mode;
                StartNewRace(_race.Mode);
                OpenCarousel(State.TrackSelect, playFlow: true);
                break;
            case ResultItem.Menu:
                GoToMainMenu();
                break;
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
        bool won = !timeAttack && _player.Finished;
        (string headline, Color color, string[] icon) = timeAttack
            ? (_records.NewScoreRecord ? "NOVO RECORDE!" : "TEMPO ESGOTADO!", _records.NewScoreRecord ? RecordColor : BoostFillColor, AchievementIcons.Stopwatch)
            : won ? ("VITORIA!", AccentColor, AchievementIcons.Trophy) : ("ELIMINADO", DangerColor, AchievementIcons.Skull);

        DrawAccentPanel(panel, color);
        float pop = 1f + (MathF.Max(0f, 1f - (_resultsTime / 0.3f)) * 0.3f);
        var iconTile = new Rectangle(panel.X + 28, panel.Y + 22, 58, 58);
        DrawIconTile(iconTile, AchievementIcons.Art(icon), color, colored: true);
        PixelFont.DrawShadowed(_spriteBatch, _pixel, headline, new Vector2(panel.X + 104f, panel.Y + 24f), 4.5f * pop, color);
        string subtitle = $"{(timeAttack ? "CONTRA O RELOGIO" : "CORRIDA MORTAL")}  -  {SelectedTrack.Name}  -  {SelectedSkin.Name}";
        PixelFont.Draw(_spriteBatch, _pixel, subtitle, new Vector2(panel.X + 106f, panel.Y + 64f), FitTextSize(subtitle, panel.Width - 140f, 1.6f), StatBadgeLabelColor);

        // Coluna da esquerda: números da partida.
        var left = new Rectangle(panel.X + 28, panel.Y + 100, 390, 250);
        foreach ((string label, string value, bool highlight) row in BuildResultRows())
        {
            DrawResultRow(left, row.label, row.value, row.highlight);
            left.Y += 34;
        }

        _spriteBatch.Draw(_pixel, new Rectangle(panel.X + 440, panel.Y + 100, 2, 262), PanelBorderColor);

        // Coluna da direita: o que foi liberado agora e o que está perto.
        float x = panel.X + 464f;
        float y = panel.Y + 100f;
        PixelFont.Draw(_spriteBatch, _pixel, "NOVOS DESBLOQUEIOS", new Vector2(x, y), BodySize, AccentColor);
        y += 24f;
        if (_raceUnlocks.Count == 0)
        {
            PixelFont.Draw(_spriteBatch, _pixel, "NENHUM DESTA VEZ", new Vector2(x, y + 4f), SmallSize, StatBadgeLabelColor);
            y += 30f;
        }
        else
        {
            int shown = Math.Min(Math.Min(_unlocksRevealed, _raceUnlocks.Count), 5);
            for (int i = 0; i < shown; i++)
            {
                DrawUnlockLine(_raceUnlocks[i], x, y, i == shown - 1 ? MathF.Max(0f, 1f - ((_resultsTime - UnlockRevealDelay - (i * UnlockRevealInterval)) / 0.25f)) : 0f);
                y += 34f;
            }

            if (_raceUnlocks.Count > 5 && _unlocksRevealed >= 5)
            {
                PixelFont.Draw(_spriteBatch, _pixel, $"+ {_raceUnlocks.Count - 5} MAIS (VEJA EM CONQUISTAS)", new Vector2(x, y), SmallSize, StatBadgeLabelColor);
                y += 20f;
            }

        }

        y = MathF.Max(y + 8f, panel.Y + 262f);
        PixelFont.Draw(_spriteBatch, _pixel, "PROXIMOS DESBLOQUEIOS", new Vector2(x, y), 1.6f, StatBadgeLabelColor);
        y += 18f;
        foreach ((string kind, string name, UnlockRequirement requirement, float fraction) in Progression.NextUnlocks(_saveData, 2))
        {
            string line = $"{kind}: {name}";
            PixelFont.Draw(_spriteBatch, _pixel, line, new Vector2(x, y), FitTextSize(line, 250f, 1.5f), TextColor);
            DrawProgressBar(new Rectangle((int)x + 262, (int)y + 2, 180, 7), fraction, RecordColor, 3f);
            string progress = requirement.ProgressText(_saveData) ?? string.Empty;
            PixelFont.Draw(_spriteBatch, _pixel, progress, new Vector2(x + 450f, y), 1.3f, RecordColor);
            y += 20f;
        }

        string[] labels = ["JOGAR NOVAMENTE", "ESCOLHER PISTA", "MENU"];
        for (int i = 0; i < labels.Length; i++)
        {
            DrawButton(ResultButtonRect(i), labels[i], _resultFocus == i, primary: i == 0, textSize: 1.7f);
        }
    }

    private List<(string Label, string Value, bool Highlight)> BuildResultRows()
    {
        string lap = _player.Car.BestLapTime is { } bestLap ? TimeFormat.Precise(bestLap) : "--";
        if (_race.Mode == RaceMode.TimeAttack)
        {
            string record = _saveData.BestScoreTimeAttack is { } best ? $"{best:0} PTS" : "--";
            return
            [
                ("PONTUACAO", $"{_player.Score:0} PTS", true),
                (_records.NewScoreRecord ? "NOVO RECORDE PESSOAL" : "RECORDE PESSOAL", record, _records.NewScoreRecord),
                ("VOLTAS", _player.Car.LapsCompleted.ToString(), false),
                ("MELHOR VOLTA", lap, _records.NewLapRecord),
                ("BATIDAS", _raceTracker.Collisions.ToString(), false),
                ("TEMPO JOGADO", TimeFormat.Short(_race.ElapsedTime), false),
            ];
        }

        int place = _player.FinishPlace ?? _race.Entrants.Count;
        return
        [
            ("POSICAO FINAL", $"{place}º DE {_race.Entrants.Count}", _player.Finished),
            ("VITORIAS NA MORTAL", _saveData.EliminationWins.ToString(), _player.Finished),
            ("SEQUENCIA DE VITORIAS", _saveData.EliminationWinStreak.ToString(), false),
            ("MELHOR VOLTA", lap, _records.NewLapRecord),
            ("BATIDAS", _raceTracker.Collisions.ToString(), false),
            ("TEMPO DE CORRIDA", TimeFormat.Short(_player.FinishTime ?? _race.ElapsedTime), false),
        ];
    }

    private void DrawResultRow(Rectangle area, string label, string value, bool highlight)
    {
        DrawRoundedRect(new Rectangle(area.X, area.Y, area.Width, 28), highlight ? StatBadgeFill : new Color(28, 32, 46), 6f);
        if (highlight)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(area.X, area.Y, 4, 28), RecordColor);
        }

        PixelFont.Draw(_spriteBatch, _pixel, label, new Vector2(area.X + 12f, area.Y + 9f), SmallSize, StatBadgeLabelColor);
        float valueWidth = PixelFont.Measure(value, 2f);
        PixelFont.DrawShadowed(_spriteBatch, _pixel, value, new Vector2(area.Right - 12f - valueWidth, area.Y + 7f), 2f, highlight ? RecordColor : TextColor);
    }

    /// <summary>Uma linha de desbloqueio no resultado: ícone + tipo + nome (salta ao aparecer).</summary>
    private void DrawUnlockLine(UnlockNotice notice, float x, float y, float pop)
    {
        (string kind, string name, AchievementIcon icon, Color color) = notice switch
        {
            { Achievement: { } a } => ("CONQUISTA", a.Name, a.Icon, ProgressionStyle.CategoryColor(a.Category)),
            { Skin: { } s } => ("NOVA SKIN", s.Name, AchievementIcons.Skin(s.Id), AccentColor),
            { Track: { } t } => ("NOVA PISTA", t.Name, AchievementIcons.Art(t.Icon), new Color(120, 230, 130)),
            _ => ("?", "?", AchievementIcons.Secret, TextColor),
        };

        int size = (int)(28f * (1f + (pop * 0.4f)));
        DrawIconTile(new Rectangle((int)x, (int)y - ((size - 28) / 2), size, size), icon, color, colored: true);
        PixelFont.Draw(_spriteBatch, _pixel, kind, new Vector2(x + 38f, y + 1f), 1.3f, color);
        PixelFont.DrawShadowed(_spriteBatch, _pixel, name, new Vector2(x + 38f, y + 13f), FitTextSize(name, 420f, 1.8f), TextColor);
    }
}
