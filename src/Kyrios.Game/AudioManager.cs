using Kyrios.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Kyrios.Game;

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

    private bool _available = true;

    private SoundEffectInstance _sprintMusic;
    private SoundEffectInstance _eliminationMusic;
    private SoundEffectInstance _timeAttackMusic;
    private SoundEffectInstance _activeMusic;

    private SoundEffectInstance _engine;
    private SoundEffect _checkpointSfx;
    private SoundEffect _collisionSfx;

    public void LoadContent()
    {
        try
        {
            _sprintMusic = CreateLoop(Soundtrack.BuildSprintTheme());
            _eliminationMusic = CreateLoop(Soundtrack.BuildEliminationTheme());
            _timeAttackMusic = CreateLoop(Soundtrack.BuildTimeAttackTheme());
            _engine = CreateLoop(Soundtrack.BuildEngineLoop());
            _checkpointSfx = Soundtrack.BuildCheckpointChime();
            _collisionSfx = Soundtrack.BuildCollisionThud();
        }
        catch (Exception)
        {
            // Sem hardware/driver de áudio disponível (ou qualquer outra falha ao inicializar o back-end
            // de som) — o jogo continua funcionando normalmente, só que mudo.
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
            _activeMusic?.Stop();
            _activeMusic = mode switch
            {
                RaceMode.Elimination => _eliminationMusic,
                RaceMode.TimeAttack => _timeAttackMusic,
                _ => _sprintMusic,
            };
            _activeMusic?.Play();
        }
        catch (Exception)
        {
            _available = false;
        }
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
            _engine.Volume = MathHelper.Lerp(EngineMinVolume, EngineMaxVolume, speedFraction);
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
            _checkpointSfx.Play(0.6f, 0f, 0f);
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
            _collisionSfx.Play(0.7f, 0f, 0f);
        }
        catch (Exception)
        {
            _available = false;
        }
    }

    public void Dispose()
    {
        _sprintMusic?.Dispose();
        _eliminationMusic?.Dispose();
        _timeAttackMusic?.Dispose();
        _engine?.Dispose();
        _checkpointSfx?.Dispose();
        _collisionSfx?.Dispose();
    }
}
