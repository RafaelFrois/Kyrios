namespace Kyrios.Game;

/// <summary>
/// Converte o tempo real de cada quadro em passos fixos de simulação: a corrida sempre anda de 1/60 s em 1/60 s,
/// seja a tela de 30, 60, 90 ou 120 Hz (ou um celular engasgando) — física, IA, relógio e pontuação saem iguais em
/// qualquer aparelho. Depois de um engasgo longo não tenta "recuperar" mais que alguns passos de uma vez.
/// </summary>
public sealed class FixedStepClock(double step, int maxStepsPerFrame)
{
    private double _accumulated;

    public double Step { get; } = step;

    /// <summary>Soma o tempo do quadro e devolve quantos passos fixos cabem nele (o resto fica pro próximo quadro).</summary>
    public int Advance(double frameSeconds)
    {
        if (!double.IsFinite(frameSeconds) || frameSeconds < 0)
        {
            frameSeconds = 0;
        }

        _accumulated = Math.Min(_accumulated + frameSeconds, Step * maxStepsPerFrame);
        int steps = 0;
        while (_accumulated >= Step)
        {
            _accumulated -= Step;
            steps++;
        }

        return steps;
    }

    /// <summary>Esquece o tempo acumulado (nova corrida, volta de uma pausa ou de uma interrupção).</summary>
    public void Reset() => _accumulated = 0;
}
