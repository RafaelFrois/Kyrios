namespace Kyrios.Core;

public enum RaceMode
{
    /// <summary>Corrida Mortal: a cada volta completada (por qualquer carro), o último colocado é eliminado, até sobrar um campeão.</summary>
    Elimination,

    /// <summary>Contra o Relógio: um cronômetro só corre pra trás, mas checkpoints/voltas somam tempo e pontos. Acaba quando o tempo zera.</summary>
    TimeAttack,
}
