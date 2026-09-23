namespace Kyrios.Game;

/// <summary>
/// O que o jogador precisa ter feito pra liberar uma skin. Um requisito só LÊ o progresso salvo
/// (<see cref="SaveData"/>) — nunca conhece os modos de jogo. Por isso, pra criar um tipo novo de requisito
/// basta: (1) se ainda não existir, guardar a estatística nova no SaveData e atualizá-la no fim da partida
/// (GameRoot.ProcessRaceEndRecords); (2) criar uma subclasse aqui que leia essa estatística; (3) expor um
/// atalho em <see cref="Unlock"/>. Exemplos que cabem nesse formato: "complete N corridas sem bater" (um
/// contador novo + <see cref="StatAtLeastRequirement"/>), "desbloqueie a skin X antes" (ler
/// <see cref="SaveData.UnlockedSkinIds"/>), "jogue N partidas", "vença na pista Y" etc.
/// </summary>
public abstract class UnlockRequirement
{
    /// <summary>Frase curta mostrada no menu e na notificação, em maiúsculas e sem acento (limite da fonte).</summary>
    public abstract string Description { get; }

    public abstract bool IsMet(SaveData progress);

    /// <summary>Quanto falta, pra mostrar abaixo do requisito (ex.: "PROGRESSO: 2/5"); null = nada a mostrar.</summary>
    public virtual string ProgressText(SaveData progress) => null;
}

/// <summary>Disponível desde o primeiro acesso (a skin clássica).</summary>
public sealed class AlwaysUnlockedRequirement : UnlockRequirement
{
    public override string Description => "DISPONIVEL DESDE O INICIO";

    public override bool IsMet(SaveData progress) => true;
}

/// <summary>Uma estatística que só cresce (vitórias, pontuação, partidas jogadas...) precisa chegar a um alvo.</summary>
public sealed class StatAtLeastRequirement(string description, Func<SaveData, float> stat, float target) : UnlockRequirement
{
    public override string Description => description;

    public override bool IsMet(SaveData progress) => stat(progress) >= target;

    public override string ProgressText(SaveData progress) =>
        $"PROGRESSO: {MathF.Min(stat(progress), target):0}/{target:0}";
}

/// <summary>Um melhor tempo salvo (menor = melhor) precisa ficar abaixo de uma meta.</summary>
public sealed class TimeBelowRequirement(string description, Func<SaveData, float?> bestTime, float targetSeconds) : UnlockRequirement
{
    public override string Description => description;

    public override bool IsMet(SaveData progress) => bestTime(progress) is { } time && time < targetSeconds;

    public override string ProgressText(SaveData progress) =>
        bestTime(progress) is { } time ? $"SEU MELHOR: {TimeFormat.Precise(time)}" : "AINDA SEM TEMPO";
}

/// <summary>Atalhos legíveis pra montar os requisitos do catálogo em <see cref="CarSkins"/> — é lá que ficam
/// os valores de cada skin, então ajustar a dificuldade é só trocar o número na linha dela.</summary>
public static class Unlock
{
    public static UnlockRequirement FromStart { get; } = new AlwaysUnlockedRequirement();

    public static UnlockRequirement ClassicWins(int wins) => new StatAtLeastRequirement(
        wins == 1 ? "VENCA 1 CORRIDA CLASSICA" : $"VENCA {wins} CORRIDAS CLASSICAS",
        progress => progress.SprintWins,
        wins);

    public static UnlockRequirement DeathRaceWins(int wins) => new StatAtLeastRequirement(
        wins == 1 ? "VENCA 1 CORRIDA MORTAL" : $"VENCA {wins} CORRIDAS MORTAIS",
        progress => progress.EliminationWins,
        wins);

    public static UnlockRequirement TimeAttackScore(int points) => new StatAtLeastRequirement(
        $"FACA {points} PTS NO CONTRA O RELOGIO",
        progress => progress.BestScoreTimeAttack ?? 0f,
        points);

    public static UnlockRequirement ClassicRaceUnder(float seconds) => new TimeBelowRequirement(
        $"CORRIDA CLASSICA EM MENOS DE {TimeFormat.Short(seconds)}",
        progress => progress.BestRaceTimeSprint,
        seconds);

    public static UnlockRequirement ClassicLapUnder(float seconds) => new TimeBelowRequirement(
        $"VOLTA CLASSICA ABAIXO DE {TimeFormat.Short(seconds)}",
        progress => progress.BestLapTimeSprint,
        seconds);
}

/// <summary>Estado de desbloqueio das skins — separado tanto dos modos de jogo quanto do desenho.</summary>
public static class SkinUnlocks
{
    public static bool IsUnlocked(CarSkin skin, SaveData progress) =>
        skin.Requirement is AlwaysUnlockedRequirement || progress.UnlockedSkinIds.Contains(skin.Id);

    /// <summary>
    /// Libera (anotando em <paramref name="progress"/>) toda skin ainda bloqueada cujo requisito já foi
    /// cumprido, e devolve quais foram — na ordem do catálogo — pra quem chamou salvar e avisar o jogador.
    /// Repete até estabilizar, pra que requisitos que dependam de outra skin ("libere X antes") também
    /// fechem na mesma checagem.
    /// </summary>
    public static List<CarSkin> UnlockNewlyEarned(SaveData progress, IReadOnlyList<CarSkin> catalog = null)
    {
        catalog ??= CarSkins.All;
        var earned = new List<CarSkin>();

        bool changed;
        do
        {
            changed = false;
            foreach (CarSkin skin in catalog)
            {
                if (!IsUnlocked(skin, progress) && skin.Requirement.IsMet(progress))
                {
                    progress.UnlockedSkinIds.Add(skin.Id);
                    earned.Add(skin);
                    changed = true;
                }
            }
        }
        while (changed);

        return earned;
    }
}
