namespace Kyrios.Game;

/// <summary>
/// Uma condição de desbloqueio — usada tanto pelas skins (<see cref="CarSkin.Requirement"/>) quanto pelas
/// conquistas (<see cref="Achievement.Condition"/>). Uma condição só LÊ o progresso salvo
/// (<see cref="SaveData"/>) — nunca conhece os modos de jogo nem a tela. Pra criar um tipo novo:
/// (1) se ainda não existir, guarde a estatística nova no SaveData e atualize-a em
/// <see cref="Progression.RecordRace"/>; (2) exponha um atalho em <see cref="Unlock"/> (quase sempre
/// dá pra montar com <see cref="StatAtLeastRequirement"/> ou <see cref="ConditionRequirement"/>).
/// </summary>
public abstract class UnlockRequirement
{
    /// <summary>Frase curta mostrada no menu e na notificação, em maiúsculas e sem acento (limite da fonte).</summary>
    public abstract string Description { get; }

    public abstract bool IsMet(SaveData progress);

    /// <summary>Quanto falta, pra mostrar abaixo do requisito (ex.: "PROGRESSO: 2/5"); null = nada a mostrar.</summary>
    public virtual string ProgressText(SaveData progress) => null;

    /// <summary>Fração já cumprida (0 a 1) pra desenhar uma barrinha; null = não é mensurável assim.</summary>
    public virtual float? ProgressFraction(SaveData progress) => null;
}

/// <summary>Disponível desde o primeiro acesso (a skin clássica).</summary>
public sealed class AlwaysUnlockedRequirement : UnlockRequirement
{
    public override string Description => "DISPONIVEL DESDE O INICIO";

    public override bool IsMet(SaveData progress) => true;
}

/// <summary>Uma estatística que só cresce (vitórias, pontuação, partidas jogadas...) precisa chegar a um alvo.
/// O alvo pode ser calculado na hora (ex.: "todas as skins do catálogo"), pra não depender da ordem em que
/// os catálogos são montados.</summary>
public sealed class StatAtLeastRequirement : UnlockRequirement
{
    private readonly string _description;
    private readonly Func<SaveData, float> _stat;
    private readonly Func<float> _target;

    public StatAtLeastRequirement(string description, Func<SaveData, float> stat, float target)
        : this(description, stat, () => target)
    {
    }

    public StatAtLeastRequirement(string description, Func<SaveData, float> stat, Func<float> target)
    {
        _description = description;
        _stat = stat;
        _target = target;
    }

    public override string Description => _description;

    public override bool IsMet(SaveData progress) => _stat(progress) >= _target();

    public override string ProgressText(SaveData progress)
    {
        float target = _target();
        return $"PROGRESSO: {MathF.Floor(MathF.Min(_stat(progress), target)):0}/{target:0}";
    }

    public override float? ProgressFraction(SaveData progress)
    {
        float target = _target();
        return target <= 0f ? 1f : Math.Clamp(_stat(progress) / target, 0f, 1f);
    }
}

/// <summary>Um melhor tempo salvo (menor = melhor) precisa ficar abaixo de uma meta.</summary>
public sealed class TimeBelowRequirement(string description, Func<SaveData, float?> bestTime, float targetSeconds) : UnlockRequirement
{
    public override string Description => description;

    public override bool IsMet(SaveData progress) => bestTime(progress) is { } time && time < targetSeconds;

    public override string ProgressText(SaveData progress) =>
        bestTime(progress) is { } time ? $"SEU MELHOR: {TimeFormat.Precise(time)}" : "AINDA SEM TEMPO";
}

/// <summary>Qualquer condição sim/não sobre o progresso (ex.: "já desbloqueou a skin X?").</summary>
public sealed class ConditionRequirement(Func<string> description, Func<SaveData, bool> condition) : UnlockRequirement
{
    public override string Description => description();

    public override bool IsMet(SaveData progress) => condition(progress);
}

/// <summary>Combinação: todas as condições precisam estar cumpridas. O progresso mostrado é o da primeira
/// que ainda falta.</summary>
public sealed class AllOfRequirement(string description, params UnlockRequirement[] parts) : UnlockRequirement
{
    public override string Description => description;

    public override bool IsMet(SaveData progress) => parts.All(part => part.IsMet(progress));

    public override string ProgressText(SaveData progress) =>
        parts.FirstOrDefault(part => !part.IsMet(progress))?.ProgressText(progress);

    public override float? ProgressFraction(SaveData progress)
    {
        float[] fractions = [.. parts.Select(part => part.IsMet(progress) ? 1f : part.ProgressFraction(progress) ?? 0f)];
        return fractions.Length == 0 ? 1f : fractions.Average();
    }
}

/// <summary>
/// Atalhos legíveis pra montar as condições dos catálogos (<see cref="CarSkins"/> e
/// <see cref="Achievements"/>) — é lá que ficam os valores de cada item, então ajustar a dificuldade é só
/// trocar o número na linha dele. Referência de escala nessa pista: o vencedor da Corrida Clássica cruza em
/// ~30 s, uma volta rápida leva ~9 s e uma partida muito boa no Contra o Relógio fica em 1500-2000 pts.
/// </summary>
public static class Unlock
{
    public static UnlockRequirement FromStart { get; } = new AlwaysUnlockedRequirement();

    public static UnlockRequirement Stat(string description, Func<SaveData, float> stat, float target) =>
        new StatAtLeastRequirement(description, stat, target);

    public static UnlockRequirement Stat(string description, Func<SaveData, float> stat, Func<float> target) =>
        new StatAtLeastRequirement(description, stat, target);

    public static UnlockRequirement Condition(string description, Func<SaveData, bool> condition) =>
        new ConditionRequirement(() => description, condition);

    public static UnlockRequirement All(string description, params UnlockRequirement[] parts) =>
        new AllOfRequirement(description, parts);

    // ----- Vitórias e partidas -----
    public static UnlockRequirement ClassicWins(int wins) => Stat(
        wins == 1 ? "VENCA 1 CORRIDA CLASSICA" : $"VENCA {wins} CORRIDAS CLASSICAS",
        progress => progress.SprintWins,
        wins);

    public static UnlockRequirement DeathRaceWins(int wins) => Stat(
        wins == 1 ? "VENCA 1 CORRIDA MORTAL" : $"VENCA {wins} CORRIDAS MORTAIS",
        progress => progress.EliminationWins,
        wins);

    /// <summary>Vitórias somando Corrida Clássica e Corrida Mortal (o Contra o Relógio não tem vencedor).</summary>
    public static UnlockRequirement Wins(int wins) => Stat(
        wins == 1 ? "VENCA 1 CORRIDA (CLASSICA OU MORTAL)" : $"VENCA {wins} CORRIDAS (CLASSICA OU MORTAL)",
        progress => progress.TotalWins,
        wins);

    public static UnlockRequirement DeathRaceWinStreak(int wins) => Stat(
        $"VENCA {wins} CORRIDAS MORTAIS SEGUIDAS",
        progress => progress.BestEliminationWinStreak,
        wins);

    public static UnlockRequirement GamesPlayed(int games) => Stat(
        games == 1 ? "TERMINE 1 PARTIDA EM QUALQUER MODO" : $"JOGUE {games} PARTIDAS",
        progress => progress.GamesPlayed,
        games);

    public static UnlockRequirement ClassicRaces(int races) => Stat(
        races == 1 ? "TERMINE 1 CORRIDA CLASSICA" : $"TERMINE {races} CORRIDAS CLASSICAS",
        progress => progress.SprintRaces,
        races);

    public static UnlockRequirement TimeAttackRaces(int races) => Stat(
        races == 1 ? "JOGUE 1 VEZ O CONTRA O RELOGIO" : $"JOGUE {races} VEZES O CONTRA O RELOGIO",
        progress => progress.TimeAttackRaces,
        races);

    // ----- Contra o Relógio -----
    /// <summary>Pontuação numa única partida (o recorde).</summary>
    public static UnlockRequirement TimeAttackScore(int points) => Stat(
        $"FACA {points} PTS NUMA PARTIDA DO RELOGIO",
        progress => progress.BestScoreTimeAttack ?? 0f,
        points);

    /// <summary>Pontos somados de todas as partidas — metas de longo prazo.</summary>
    public static UnlockRequirement TimeAttackTotal(int points) => Stat(
        $"ACUMULE {points} PTS NO CONTRA O RELOGIO",
        progress => progress.TotalTimeAttackScore,
        points);

    // ----- Tempos -----
    public static UnlockRequirement ClassicRaceUnder(float seconds) => new TimeBelowRequirement(
        $"CORRIDA CLASSICA EM MENOS DE {TimeFormat.Short(seconds)}",
        progress => progress.BestRaceTimeSprint,
        seconds);

    public static UnlockRequirement ClassicLapUnder(float seconds) => new TimeBelowRequirement(
        $"VOLTA CLASSICA ABAIXO DE {TimeFormat.Short(seconds)}",
        progress => progress.BestLapTimeSprint,
        seconds);

    // ----- Skins e conquistas -----
    /// <summary>Quantas skins além da clássica já foram liberadas.</summary>
    public static UnlockRequirement SkinsUnlocked(int count) => Stat(
        count == 1 ? "DESBLOQUEIE 1 SKIN" : $"DESBLOQUEIE {count} SKINS",
        progress => SkinUnlocks.EarnedCount(progress),
        count);

    public static UnlockRequirement AllSkins() => Stat(
        "DESBLOQUEIE TODAS AS SKINS",
        progress => SkinUnlocks.EarnedCount(progress),
        () => SkinUnlocks.EarnableCount);

    public static UnlockRequirement SkinEarned(string skinId) => new ConditionRequirement(
        () => $"DESBLOQUEIE A SKIN {CarSkins.Find(skinId)?.Name ?? skinId}",
        progress => progress.UnlockedSkinIds.Contains(skinId));

    public static UnlockRequirement AchievementsUnlocked(int count) => Stat(
        $"DESBLOQUEIE {count} CONQUISTAS",
        progress => Achievements.UnlockedCount(progress),
        count);

    /// <summary>Todas as conquistas menos a própria que usa este requisito (a "platina").</summary>
    public static UnlockRequirement AllOtherAchievements() => Stat(
        "DESBLOQUEIE TODAS AS OUTRAS CONQUISTAS",
        progress => Achievements.UnlockedCount(progress),
        () => Achievements.All.Count - 1);

    public static UnlockRequirement AchievementEarned(string achievementId) => new ConditionRequirement(
        () => $"GANHE A CONQUISTA {Achievements.Find(achievementId)?.Name ?? achievementId}",
        progress => progress.UnlockedAchievementIds.Contains(achievementId));

    // ----- Estatísticas por skin -----
    public static UnlockRequirement WinsWithSkin(string skinId, string description) => Stat(
        description,
        progress => progress.WinsBySkin.GetValueOrDefault(skinId),
        1);

    public static UnlockRequirement DeathRaceWinsWithSkin(string skinId, string description) => Stat(
        description,
        progress => progress.EliminationWinsBySkin.GetValueOrDefault(skinId),
        1);

    public static UnlockRequirement RecordsWithSkin(string skinId, string description) => Stat(
        description,
        progress => progress.RecordsBySkin.GetValueOrDefault(skinId),
        1);
}
