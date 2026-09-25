using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace Kyrios.Game;

/// <summary>
/// O que o jogo faz com os avisos da plataforma de celular — os mesmos pontos existem (sem efeito) no desktop e na
/// web: qualidade gráfica (com a automática), vibração, interrupções (ligação, bloqueio, notificação, troca de app),
/// memória baixa e perda do contexto gráfico. Nada aqui muda regra de jogo: física, IA e tempos continuam em passos
/// fixos de 1/60 s em qualquer qualidade ou taxa de quadros.
/// </summary>
public sealed partial class GameRoot
{
    // ---------- Qualidade gráfica ----------

    /// <summary>A qualidade que a opção AUTO está usando agora (começa pelo perfil do aparelho; baixa se engasgar).</summary>
    private GraphicsQuality _autoQuality = GraphicsQuality.High;
    private GraphicsQuality _appliedQuality = GraphicsQuality.Auto;
    private readonly Stopwatch _frameClock = new();
    private double _slowWindowSeconds;
    private int _slowWindowFrames;

    /// <summary>Escolha do jogador (no desktop/web não existe a opção: sempre ALTA, como sempre foi).</summary>
    private GraphicsQuality ChosenQuality => GamePlatform.Current.IsMobile ? MobileSettings.Quality(_saveData.GraphicsQuality) : GraphicsQuality.High;

    /// <summary>A qualidade valendo de fato (AUTO já resolvida).</summary>
    private GraphicsQuality EffectiveQuality => ChosenQuality == GraphicsQuality.Auto ? _autoQuality : ChosenQuality;

    /// <summary>Quadros por segundo desenhados: 30 na BAIXA (bateria e aparelhos fracos), 60 nas outras. A simulação
    /// roda 60 passos por segundo de qualquer jeito.</summary>
    private int TargetFramesPerSecond => EffectiveQuality == GraphicsQuality.Low ? 30 : 60;

    private void InitializeQuality()
    {
        _autoQuality = GamePlatform.Current.DeviceTier switch
        {
            DeviceTier.Low => GraphicsQuality.Low,
            DeviceTier.Medium => GraphicsQuality.Medium,
            _ => GraphicsQuality.High,
        };
        ApplyQualityIfChanged();
    }

    /// <summary>Aplica a qualidade quando ela muda: taxa de quadros (e a pedida à tela) e a densidade dos enfeites.</summary>
    private void ApplyQualityIfChanged()
    {
        GraphicsQuality quality = EffectiveQuality;
        if (quality == _appliedQuality || !GamePlatform.Current.IsMobile)
        {
            _appliedQuality = quality;
            return;
        }

        _appliedQuality = quality;
        int fps = TargetFramesPerSecond;
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / fps);
        GamePlatform.Current.SetPreferredFrameRate(fps);
        _scenery?.SetAmbientDensity(quality switch
        {
            GraphicsQuality.Low => 0.4f,
            GraphicsQuality.Medium => 0.7f,
            _ => 1f,
        });
        _slowWindowSeconds = 0;
        _slowWindowFrames = 0;
    }

    /// <summary>Menos partículas (poeira, rastro de turbo, confete) na qualidade baixa.</summary>
    private float EffectsDensity => EffectiveQuality switch
    {
        GraphicsQuality.Low => 0.5f,
        GraphicsQuality.Medium => 0.8f,
        _ => 1f,
    };

    /// <summary>AUTO: mede o tempo real entre quadros durante a corrida; se ficar abaixo de ~80% da meta por uns
    /// segundos, desce um nível (nunca sobe sozinho na mesma sessão, pra não ficar oscilando).</summary>
    private void MonitorFrameRate()
    {
        double elapsed = _frameClock.Elapsed.TotalSeconds;
        _frameClock.Restart();
        if (!GamePlatform.Current.IsMobile || ChosenQuality != GraphicsQuality.Auto || _state != State.Racing || CountingDown || elapsed > 0.5)
        {
            return;
        }

        _slowWindowSeconds += elapsed;
        _slowWindowFrames++;
        if (_slowWindowSeconds < 4.0)
        {
            return;
        }

        double fps = _slowWindowFrames / _slowWindowSeconds;
        _slowWindowSeconds = 0;
        _slowWindowFrames = 0;
        if (fps < TargetFramesPerSecond * 0.8 && _autoQuality > GraphicsQuality.Low)
        {
            _autoQuality--;
            ApplyQualityIfChanged();
        }
    }

    private static string QualityName(GraphicsQuality quality) => quality switch
    {
        GraphicsQuality.Low => L.T("BAIXA", "LOW"),
        GraphicsQuality.Medium => L.T("MEDIA", "MEDIUM"),
        GraphicsQuality.High => L.T("ALTA", "HIGH"),
        _ => "AUTO",
    };

    // ---------- Vibração ----------

    /// <summary>Vibra se o aparelho souber e o jogador não tiver desligado nas configurações.</summary>
    private void Haptic(Haptic kind)
    {
        if (GamePlatform.Current.IsMobile && _saveData.Vibration && GamePlatform.Current.CanVibrate)
        {
            GamePlatform.Current.Vibrate(kind);
        }
    }

    // ---------- Avisos do sistema ----------

    private bool _graphicsContentLost;

    /// <summary>Chamado no começo de cada quadro: interrupções pausam a corrida (e gravam), memória baixa libera as
    /// pistas guardadas.</summary>
    private void HandlePlatformSignals()
    {
        if (GamePlatform.Current.ConsumeInterruption())
        {
            if (_state == State.Racing)
            {
                OpenPause(countAsPause: false);
            }

            _simulationClock.Reset();
            _saveData.Save();
        }

        if (GamePlatform.Current.ConsumeLowMemoryWarning())
        {
            _scenery?.ReleaseCache();
            GC.Collect();
        }

        ApplyQualityIfChanged();
    }

    /// <summary>O app vai pro fundo: grava agora (o sistema pode fechar o app sem avisar depois disso).</summary>
    private void OnPlatformSuspending() => _saveData?.Save();

    /// <summary>Android: se o contexto OpenGL for recriado, as texturas feitas em código ficam vazias — marca pra
    /// refazer no próximo quadro (o aviso chega de outra thread).</summary>
    private void WatchGraphicsContext()
    {
        if (GamePlatform.Current.CanLoseGraphicsContext)
        {
            GraphicsDevice.DeviceReset += (_, _) => _graphicsContentLost = true;
        }
    }

    private void RebuildGraphicsIfLost()
    {
        if (!_graphicsContentLost)
        {
            return;
        }

        _graphicsContentLost = false;
        _scenery?.ReleaseCache();
        CreateGraphicsResources();
    }
}
