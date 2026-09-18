namespace Kyrios.Core;

public enum RaceMode
{
    /// <summary>Corrida clássica: todos correm até completar o número de voltas definido.</summary>
    Sprint,

    /// <summary>A cada volta completada (por qualquer carro), o último colocado é eliminado, até sobrar um campeão.</summary>
    Elimination,

    /// <summary>Contrarrelógio: um cronômetro só corre pra trás, mas checkpoints/voltas somam tempo e pontos. Acaba quando o tempo zera.</summary>
    TimeAttack,
}
