using Kyrios.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Kyrios.Game;

/// <summary>Qual dos três jingles de resultado combina com o desfecho da corrida pro jogador.</summary>
public enum RaceOutcome
{
    Victory,
    Neutral,
    Defeat,
}

/// <summary>
/// Dono de toda a trilha sonora e efeitos do jogo — tudo sintetizado em código (ver <see cref="Synth"/>
/// e <see cref="Soundtrack"/>), sem nenhum arquivo de áudio externo. Se o dispositivo de áudio não
/// estiver disponível (ou falhar por qualquer motivo, ex.: máquina sem placa de som), desativa o som
/// silenciosamente em vez de derrubar o jogo — o resto do jogo funciona normalmente sem áudio.
/// </summary>
public sealed class AudioManager
{
    private const float EngineMinPitch = -0.35f;
    private const float EngineMaxPitch = 0.5f;
    private const float EngineBoostPitchBonus = 0.15f;
    private const float EngineMinVolume = 0.22f;
    private const float EngineMaxVolume = 0.5f;
    private const float SfxPitchJitter = 0.1f;

    private bool _available = true;
    private readonly Random _random = new();

    private float _musicVolume = 1f;
    private float _sfxVolume = 1f;
    private bool _musicMuted;
    private bool _sfxMuted;

    private SoundEffectInstance _eliminationMusic;
    private SoundEffectInstance _timeAttackMusic;
    private SoundEffectInstance _menuMusic;
    private SoundEffectInstance _activeMusic;

    private SoundEffectInstance _engine;
    private SoundEffect _checkpointSfx;
    private SoundEffect _collisionSfx;
    private SoundEffect _victoryJingle;
    private SoundEffect _defeatJingle;
    private SoundEffect _neutralJingle;
    private SoundEffect _menuMoveBlip;
    private SoundEffect _menuConfirmBlip;
    private SoundEffect _countdownTick;
    private SoundEffect _achievementChime;
    private SoundEffect _skinFanfare;
    private SoundEffect _eliminationSting;
    private SoundEffect _denyBuzz;
    private SoundEffect _startBeep;
    private SoundEffect _goBeep;
    private SoundEffect _lapChime;
    private SoundEffect _cluck;

    public void LoadContent()
    {
        try
        {
            _eliminationMusic = CreateLoop(Soundtrack.BuildEliminationTheme());
            _timeAttackMusic = CreateLoop(Soundtrack.BuildTimeAttackTheme());
            _menuMusic = CreateLoop(Soundtrack.BuildMenuTheme());
            _engine = CreateLoop(Soundtrack.BuildEngineLoop());
            _checkpointSfx = Soundtrack.BuildCheckpointChime();
            _collisionSfx = Soundtrack.BuildCollisionThud();
            _victoryJingle = Soundtrack.BuildVictoryJingle();
            _defeatJingle = Soundtrack.BuildDefeatJingle();
            _neutralJingle = Soundtrack.BuildNeutralEndJingle();
            _menuMoveBlip = Soundtrack.BuildMenuMoveBlip();
            _menuConfirmBlip = Soundtrack.BuildMenuConfirmBlip();
            _countdownTick = Soundtrack.BuildCountdownTick();
            _achievementChime = Soundtrack.BuildAchievementChime();
            _skinFanfare = Soundtrack.BuildSkinFanfare();
            _eliminationSting = Soundtrack.BuildEliminationSting();
            _denyBuzz = Soundtrack.BuildDenyBuzz();
            _startBeep = Soundtrack.BuildStartBeep(go: false);
            _goBeep = Soundtrack.BuildStartBeep(go: true);
            _lapChime = Soundtrack.BuildLapChime();
            _cluck = Soundtrack.BuildCluck();
        }
        catch (Exception)
        {
            // Sem hardware/driver de áudio disponível (ou qualquer outra falha ao inicializar o back-end
            // de som) — o jogo continua funcionando normalmente, só que mudo.
            _available = false;
        }
    }

    public float MusicVolume => _musicVolume;

    public float SfxVolume => _sfxVolume;

    public bool MusicMuted => _musicMuted;

    public bool SfxMuted => _sfxMuted;

    private float EffectiveMusicVolume => _musicMuted ? 0f : _musicVolume;

    private float EffectiveSfxVolume => _sfxMuted ? 0f : _sfxVolume;

    public void SetMusicVolume(float volume)
    {
        _musicVolume = Math.Clamp(volume, 0f, 1f);
        ApplyMusicVolumeToActiveTrack();
    }

    public void SetSfxVolume(float volume) => _sfxVolume = Math.Clamp(volume, 0f, 1f);

    public void SetMusicMuted(bool muted)
    {
        _musicMuted = muted;
        ApplyMusicVolumeToActiveTrack();
    }

    public void SetSfxMuted(bool muted) => _sfxMuted = muted;

    private void ApplyMusicVolumeToActiveTrack()
    {
        if (!_available || _activeMusic is null)
        {
            return;
        }

        try
        {
            _activeMusic.Volume = EffectiveMusicVolume;
        }
        catch (Exception)
        {
            _available = false;
        }
    }

    private static SoundEffectInstance CreateLoop(SoundEffect effect)
    {
        SoundEffectInstance instance = effect.CreateInstance();
        instance.IsLooped = true;
        return instance;
    }

    public void PlayMusic(RaceMode mode)
    {
        if (!_available)
        {
            return;
        }

        try
        {
            SoundEffectInstance target = mode == RaceMode.TimeAttack ? _timeAttackMusic : _eliminationMusic;
            PlayLoop(target);
        }
        catch (Exception)
        {
            _available = false;
        }
    }

    /// <summary>Toca o tema de menu — chamada idempotente (não reinicia o loop se ele já estiver
    /// tocando), então é seguro chamar em todo quadro enquanto o jogo estiver num estado de menu.</summary>
    public void PlayMenuTheme()
    {
        if (!_available)
        {
            return;
        }

        try
        {
            PlayLoop(_menuMusic);
        }
        catch (Exception)
        {
            _available = false;
        }
    }

    private void PlayLoop(SoundEffectInstance instance)
    {
        if (instance is null)
        {
            return;
        }

        if (ReferenceEquals(_activeMusic, instance) && instance.State == SoundState.Playing)
        {
            return;
        }

        _activeMusic?.Stop();
        _activeMusic = instance;
        _activeMusic.Volume = EffectiveMusicVolume;
        _activeMusic.Play();
    }

    public void StopMusic()
    {
        if (!_available)
        {
            return;
        }

        try
        {
            _activeMusic?.Stop();
            _activeMusic = null;
        }
        catch (Exception)
        {
            _available = false;
        }
    }

    public void StartEngine()
    {
        if (!_available || _engine is null)
        {
            return;
        }

        try
        {
            if (_engine.State != SoundState.Playing)
            {
                _engine.Play();
            }
        }
        catch (Exception)
        {
            _available = false;
        }
    }

    public void StopEngine()
    {
        if (!_available || _engine is null)
        {
            return;
        }

        try
        {
            _engine.Stop();
        }
        catch (Exception)
        {
            _available = false;
        }
    }

    /// <summary>Ajusta o tom/volume do ronco do motor de acordo com a velocidade atual — sem recriar
    /// nenhum buffer, só reaproveita o mesmo loop curto tocando continuamente.</summary>
    public void UpdateEngine(float speed, float maxSpeed, bool isBoosting)
    {
        if (!_available || _engine is null || maxSpeed <= 0f)
        {
            return;
        }

        try
        {
            float speedFraction = Math.Clamp(MathF.Abs(speed) / maxSpeed, 0f, 1f);
            float pitch = MathHelper.Lerp(EngineMinPitch, EngineMaxPitch, speedFraction) + (isBoosting ? EngineBoostPitchBonus : 0f);
            _engine.Pitch = Math.Clamp(pitch, -1f, 1f);
            _engine.Volume = MathHelper.Lerp(EngineMinVolume, EngineMaxVolume, speedFraction) * EffectiveSfxVolume;
        }
        catch (Exception)
        {
            _available = false;
        }
    }

    public void PlayCheckpoint()
    {
        if (!_available || _checkpointSfx is null)
        {
            return;
        }

        try
        {
            _checkpointSfx.Play(0.6f * EffectiveSfxVolume, NextPitchJitter(), 0f);
        }
        catch (Exception)
        {
            _available = false;
        }
    }

    public void PlayCollision()
    {
        if (!_available || _collisionSfx is null)
        {
            return;
        }

        try
        {
            _collisionSfx.Play(0.7f * EffectiveSfxVolume, NextPitchJitter(), 0f);
        }
        catch (Exception)
        {
            _available = false;
        }
    }

    public void PlayResultJingle(RaceOutcome outcome)
    {
        if (!_available)
        {
            return;
        }

        try
        {
            SoundEffect jingle = outcome switch
            {
                RaceOutcome.Victory => _victoryJingle,
                RaceOutcome.Defeat => _defeatJingle,
                _ => _neutralJingle,
            };
            jingle?.Play(0.65f * EffectiveSfxVolume, 0f, 0f);
        }
        catch (Exception)
        {
            _available = false;
        }
    }

    public void PlayMenuMove()
    {
        if (!_available)
        {
            return;
        }

        try
        {
            _menuMoveBlip?.Play(0.4f * EffectiveSfxVolume, 0f, 0f);
        }
        catch (Exception)
        {
            _available = false;
        }
    }

    public void PlayMenuConfirm()
    {
        if (!_available)
        {
            return;
        }

        try
        {
            _menuConfirmBlip?.Play(0.5f * EffectiveSfxVolume, 0f, 0f);
        }
        catch (Exception)
        {
            _available = false;
        }
    }

    /// <summary>Som de desbloqueio: fanfarra pra skin nova, brilho curto pra conquista.</summary>
    public void PlayUnlock(bool isSkin)
    {
        if (!_available)
        {
            return;
        }

        try
        {
            (isSkin ? _skinFanfare : _achievementChime)?.Play(0.55f * EffectiveSfxVolume, 0f, 0f);
        }
        catch (Exception)
        {
            _available = false;
        }
    }

    public void PlayElimination() => PlayOneShot(_eliminationSting, 0.5f);

    /// <summary>Bip da contagem de largada (3, 2, 1) ou o "VAI!".</summary>
    public void PlayStartBeep(bool go) => PlayOneShot(go ? _goBeep : _startBeep, 0.45f);

    public void PlayLap() => PlayOneShot(_lapChime, 0.55f);

    public void PlayCluck() => PlayOneShot(_cluck, 0.6f);

    /// <summary>Tique bem baixinho quando o mouse passa pra outro botão.</summary>
    public void PlayHover() => PlayOneShot(_menuMoveBlip, 0.16f);

    public void PlayDeny() => PlayOneShot(_denyBuzz, 0.45f);

    /// <summary>Pausa (ou retoma) a música e o motor — usado pelo menu de pausa da corrida.</summary>
    public void SetPaused(bool paused)
    {
        if (!_available)
        {
            return;
        }

        try
        {
            if (paused)
            {
                _activeMusic?.Pause();
                _engine?.Pause();
            }
            else
            {
                _activeMusic?.Resume();
                _engine?.Resume();
            }
        }
        catch (Exception)
        {
            _available = false;
        }
    }

    private void PlayOneShot(SoundEffect effect, float volume)
    {
        if (!_available)
        {
            return;
        }

        try
        {
            effect?.Play(volume * EffectiveSfxVolume, 0f, 0f);
        }
        catch (Exception)
        {
            _available = false;
        }
    }

    public void PlayCountdownTick()
    {
        if (!_available)
        {
            return;
        }

        try
        {
            _countdownTick?.Play(0.5f * EffectiveSfxVolume, 0f, 0f);
        }
        catch (Exception)
        {
            _available = false;
        }
    }

    /// <summary>Pequena variação aleatória de tom nos efeitos que repetem bastante (checkpoint, colisão)
    /// pra não soarem exatamente iguais toda vez.</summary>
    private float NextPitchJitter() => (((float)_random.NextDouble() * 2f) - 1f) * SfxPitchJitter;

    public void Dispose()
    {
        _eliminationMusic?.Dispose();
        _timeAttackMusic?.Dispose();
        _menuMusic?.Dispose();
        _engine?.Dispose();
        _checkpointSfx?.Dispose();
        _collisionSfx?.Dispose();
        _victoryJingle?.Dispose();
        _defeatJingle?.Dispose();
        _neutralJingle?.Dispose();
        _menuMoveBlip?.Dispose();
        _menuConfirmBlip?.Dispose();
        _countdownTick?.Dispose();
        _achievementChime?.Dispose();
        _skinFanfare?.Dispose();
        _eliminationSting?.Dispose();
        _denyBuzz?.Dispose();
        _startBeep?.Dispose();
        _goBeep?.Dispose();
        _lapChime?.Dispose();
        _cluck?.Dispose();
    }
}
