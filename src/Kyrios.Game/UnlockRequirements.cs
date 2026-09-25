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
/// Descrição e alvo podem ser calculados na hora (ex.: "todas as skins do catálogo", nome de uma pista), pra
/// não depender da ordem em que os catálogos são montados.</summary>
public sealed class StatAtLeastRequirement : UnlockRequirement
{
    private readonly Func<string> _description;
    private readonly Func<SaveData, float> _stat;
    private readonly Func<float> _target;

    public StatAtLeastRequirement(Func<string> description, Func<SaveData, float> stat, Func<float> target)
    {
        _description = description;
        _stat = stat;
        _target = target;
    }

    public override string Description => _description();

    public override bool IsMet(SaveData progress) => _stat(progress) >= _target();

    public override string ProgressText(SaveData progress)
    {
        float target = _target();
        return $"{MathF.Floor(MathF.Min(_stat(progress), target)):0}/{target:0}";
    }

    public override float? ProgressFraction(SaveData progress)
    {
        float target = _target();
        return target <= 0f ? 1f : Math.Clamp(_stat(progress) / target, 0f, 1f);
    }
}

/// <summary>Um melhor tempo salvo (menor = melhor) precisa ficar abaixo de uma meta.</summary>
public sealed class TimeBelowRequirement(Func<string> description, Func<SaveData, float?> bestTime, float targetSeconds) : UnlockRequirement
{
    public TimeBelowRequirement(string description, Func<SaveData, float?> bestTime, float targetSeconds)
        : this(() => description, bestTime, targetSeconds)
    {
    }

    public override string Description => description();

    public override bool IsMet(SaveData progress) => bestTime(progress) is { } time && time < targetSeconds;

    public override string ProgressText(SaveData progress) =>
        bestTime(progress) is { } time ? $"SEU MELHOR: {TimeFormat.Short(time)}" : "AINDA SEM TEMPO";
}

/// <summary>Qualquer condição sim/não sobre o progresso (ex.: "já desbloqueou a skin X?").</summary>
public sealed class ConditionRequirement(Func<string> description, Func<SaveData, bool> condition) : UnlockRequirement
{
    public override string Description => description();

    public override bool IsMet(SaveData progress) => condition(progress);
}

/// <summary>Combinação: todas as condições precisam estar cumpridas. O progresso mostrado é o da primeira
/// que ainda falta; as telas podem listar cada parte (<see cref="Parts"/>) com o próprio progresso.</summary>
public sealed class AllOfRequirement(string description, params UnlockRequirement[] parts) : UnlockRequirement
{
    public override string Description => description;

    public IReadOnlyList<UnlockRequirement> Parts => parts;

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
/// Atalhos legíveis pra montar as condições dos catálogos (<see cref="CarSkins"/>, <see cref="TrackThemes"/> e
/// <see cref="Achievements"/>) — é lá que ficam os valores de cada item, então ajustar a dificuldade é só
/// trocar o número na linha dele. Referência de escala: na Corrida Mortal (10 carros) uma corrida dura ~30 s e
/// uma volta rápida ~9 s; uma partida muito boa no Contra o Relógio fica em 1500-2000 pts.
/// </summary>
public static class Unlock
{
    public static UnlockRequirement FromStart { get; } = new AlwaysUnlockedRequirement();

    public static UnlockRequirement Stat(string description, Func<SaveData, float> stat, float target) =>
        new StatAtLeastRequirement(() => description, stat, () => target);

    public static UnlockRequirement Stat(Func<string> description, Func<SaveData, float> stat, Func<float> target) =>
        new StatAtLeastRequirement(description, stat, target);

    public static UnlockRequirement Condition(string description, Func<SaveData, bool> condition) =>
        new ConditionRequirement(() => description, condition);

    public static UnlockRequirement All(string description, params UnlockRequirement[] parts) =>
        new AllOfRequirement(description, parts);

    private static string Plural(int count, string singular, string plural) => count == 1 ? $"1 {singular}" : $"{count} {plural}";

    private static string TrackName(string trackId) => TrackThemes.Find(trackId)?.Name ?? trackId;

    private static string SkinName(string skinId) => CarSkins.Find(skinId)?.Name ?? skinId;

    // ----- Corrida Mortal -----
    public static UnlockRequirement DeathRaceWins(int wins) => Stat(
        $"VENCA {Plural(wins, "CORRIDA MORTAL", "CORRIDAS MORTAIS")}", p => p.EliminationWins, wins);

    public static UnlockRequirement DeathRaceWinStreak(int wins) => Stat(
        $"VENCA {wins} CORRIDAS MORTAIS SEGUIDAS", p => p.BestEliminationWinStreak, wins);

    public static UnlockRequirement DeathRaces(int races) => Stat(
        $"JOGUE {Plural(races, "CORRIDA MORTAL", "CORRIDAS MORTAIS")}", p => p.EliminationRaces, races);

    public static UnlockRequirement RoundsSurvived(int rounds) => Stat(
        $"SOBREVIVA A {Plural(rounds, "ELIMINACAO", "ELIMINACOES")}", p => p.EliminationRoundsSurvived, rounds);

    // ----- Contra o Relógio -----
    public static UnlockRequirement TimeAttackRaces(int races) => Stat(
        races == 1 ? "JOGUE O CONTRA O RELOGIO" : $"JOGUE {races} VEZES O CONTRA O RELOGIO", p => p.TimeAttackRaces, races);

    /// <summary>Pontuação numa única partida (o recorde).</summary>
    public static UnlockRequirement TimeAttackScore(int points) => Stat(
        $"FACA {points} PTS NUMA PARTIDA DO RELOGIO", p => p.BestScoreTimeAttack ?? 0f, points);

    /// <summary>Pontos somados de todas as partidas — metas de longo prazo.</summary>
    public static UnlockRequirement TimeAttackTotal(int points) => Stat(
        $"ACUMULE {points} PTS NO CONTRA O RELOGIO", p => p.TotalTimeAttackScore, points);

    // ----- Gerais -----
    public static UnlockRequirement GamesPlayed(int games) => Stat(
        games == 1 ? "TERMINE SUA PRIMEIRA PARTIDA" : $"JOGUE {games} PARTIDAS", p => p.GamesPlayed, games);

    public static UnlockRequirement LapUnder(float seconds) => new TimeBelowRequirement(
        $"FACA UMA VOLTA EM MENOS DE {seconds:0.#} S", p => p.BestLapTime, seconds);

    // ----- Pistas -----
    public static UnlockRequirement TimeAttackScoreOnTrack(string trackId, int points) => Stat(
        () => $"FACA {points} PTS NO RELOGIO NA PISTA {TrackName(trackId)}",
        p => p.BestScoreByTrack.GetValueOrDefault(trackId),
        () => points);

    public static UnlockRequirement DeathRaceWinsOnTrack(string trackId, int wins = 1) => Stat(
        () => $"VENCA {Plural(wins, "CORRIDA MORTAL", "CORRIDAS MORTAIS")} NA PISTA {TrackName(trackId)}",
        p => p.WinsByTrack.GetValueOrDefault(trackId),
        () => wins);

    public static UnlockRequirement WinWithSkinOnTrack(string skinId, string trackId) => Stat(
        () => $"VENCA COM {SkinName(skinId)} NA PISTA {TrackName(trackId)}",
        p => p.WinsBySkinOnTrack.GetValueOrDefault(Progression.SkinOnTrackKey(skinId, trackId)),
        () => 1);

    public static UnlockRequirement TracksPlayed(int count) => Stat(
        $"JOGUE EM {Plural(count, "PISTA", "PISTAS DIFERENTES")}",
        p => TrackThemes.All.Count(t => p.GamesByTrack.ContainsKey(t.Id)),
        count);

    public static UnlockRequirement AllTracksPlayed() => Stat(
        () => "JOGUE EM TODAS AS PISTAS",
        p => TrackThemes.All.Count(t => p.GamesByTrack.ContainsKey(t.Id)),
        () => TrackThemes.All.Count);

    public static UnlockRequirement TracksWon(int count) => Stat(
        $"VENCA A MORTAL EM {Plural(count, "PISTA", "PISTAS DIFERENTES")}",
        p => TrackThemes.All.Count(t => p.WinsByTrack.ContainsKey(t.Id)),
        count);

    public static UnlockRequirement AllTracksWon() => Stat(
        () => "VENCA A MORTAL EM TODAS AS PISTAS",
        p => TrackThemes.All.Count(t => p.WinsByTrack.ContainsKey(t.Id)),
        () => TrackThemes.All.Count);

    public static UnlockRequirement CleanTracks(int count) => Stat(
        $"TERMINE SEM BATER EM {Plural(count, "PISTA", "PISTAS DIFERENTES")}",
        p => TrackThemes.All.Count(t => p.CleanTracks.Contains(t.Id)),
        count);

    public static UnlockRequirement GamesOnTrack(string trackId, int games) => Stat(
        () => $"JOGUE {games} PARTIDAS NA PISTA {TrackName(trackId)}",
        p => p.GamesByTrack.GetValueOrDefault(trackId),
        () => games);

    public static UnlockRequirement CleanOnTrack(string trackId) => new ConditionRequirement(
        () => $"TERMINE UMA PARTIDA SEM BATER NA PISTA {TrackName(trackId)}",
        p => p.CleanTracks.Contains(trackId));

    public static UnlockRequirement TrackSecretsFound(int count) => Stat(
        $"ENCONTRE {Plural(count, "SEGREDO ESCONDIDO", "SEGREDOS ESCONDIDOS")} NAS PISTAS",
        p => TrackThemes.All.Count(t => p.FoundTrackSecrets.Contains(t.Id)),
        count);

    public static UnlockRequirement AllTrackSecretsFound() => Stat(
        () => "ENCONTRE O SEGREDO DE TODAS AS PISTAS",
        p => TrackThemes.All.Count(t => p.FoundTrackSecrets.Contains(t.Id)),
        () => TrackThemes.All.Count);

    public static UnlockRequirement TrackSecretFound(string trackId) => new ConditionRequirement(
        () => $"ENCONTRE O SEGREDO DA PISTA {TrackName(trackId)}",
        p => p.FoundTrackSecrets.Contains(trackId));

    public static UnlockRequirement TracksUnlocked(int count) => Stat(
        $"DESBLOQUEIE {Plural(count, "PISTA", "PISTAS")}",
        p => Unlockables.Count(TrackThemes.All, p.UnlockedTrackIds).Earned,
        count);

    public static UnlockRequirement AllTracksUnlocked() => Stat(
        () => "DESBLOQUEIE TODAS AS PISTAS",
        p => Unlockables.Count(TrackThemes.All, p.UnlockedTrackIds).Earned,
        () => Unlockables.Count(TrackThemes.All, []).Earnable);

    public static UnlockRequirement TrackEarned(string trackId) => new ConditionRequirement(
        () => $"DESBLOQUEIE A PISTA {TrackName(trackId)}",
        p => p.UnlockedTrackIds.Contains(trackId));

    // ----- Skins -----
    /// <summary>Quantas skins além da padrão já foram liberadas.</summary>
    public static UnlockRequirement SkinsUnlocked(int count) => Stat(
        $"DESBLOQUEIE {Plural(count, "SKIN", "SKINS")}",
        p => Unlockables.Count(CarSkins.All, p.UnlockedSkinIds).Earned,
        count);

    public static UnlockRequirement AllSkins() => Stat(
        () => "DESBLOQUEIE TODAS AS SKINS",
        p => Unlockables.Count(CarSkins.All, p.UnlockedSkinIds).Earned,
        () => Unlockables.Count(CarSkins.All, []).Earnable);

    /// <summary>Todas as skins menos a própria que usa este requisito (o troféu final da coleção).</summary>
    public static UnlockRequirement AllOtherSkins() => Stat(
        () => "DESBLOQUEIE TODAS AS OUTRAS SKINS",
        p => Unlockables.Count(CarSkins.All, p.UnlockedSkinIds).Earned,
        () => Unlockables.Count(CarSkins.All, []).Earnable - 1);

    public static UnlockRequirement SkinEarned(string skinId) => new ConditionRequirement(
        () => $"DESBLOQUEIE A SKIN {SkinName(skinId)}",
        p => p.UnlockedSkinIds.Contains(skinId));

    public static UnlockRequirement WinsWithSkin(string skinId, int wins = 1) => Stat(
        () => wins == 1 ? $"VENCA A MORTAL COM {SkinName(skinId)}" : $"VENCA {wins} MORTAIS COM {SkinName(skinId)}",
        p => p.WinsBySkin.GetValueOrDefault(skinId),
        () => wins);

    public static UnlockRequirement RecordWithSkin(string skinId) => Stat(
        () => $"BATA SEU RECORDE COM {SkinName(skinId)}",
        p => p.RecordsBySkin.GetValueOrDefault(skinId),
        () => 1);

    public static UnlockRequirement ScoreWithSkin(string skinId, int points) => Stat(
        () => $"FACA {points} PTS NO RELOGIO COM {SkinName(skinId)}",
        p => p.BestScoreBySkin.GetValueOrDefault(skinId),
        () => points);

    public static UnlockRequirement SkinsUsed(int count) => Stat(
        $"JOGUE COM {count} SKINS DIFERENTES", p => p.SkinsUsed.Count, count);

    public static UnlockRequirement SkinsWonWith(int count) => Stat(
        $"VENCA COM {count} SKINS DIFERENTES", p => p.WinsBySkin.Count, count);

    /// <summary>Venceu pelo menos uma vez com cada uma das skins dadas.</summary>
    public static UnlockRequirement WonWithEachSkin(string description, params string[] skinIds) => Stat(
        () => description,
        p => skinIds.Count(id => p.WinsBySkin.ContainsKey(id)),
        () => skinIds.Length);

    public static UnlockRequirement PlayedWithSkinOnTrack(string skinId, string trackId) => Stat(
        () => $"JOGUE NA PISTA {TrackName(trackId)} USANDO {SkinName(skinId)}",
        p => p.GamesBySkinOnTrack.GetValueOrDefault(Progression.SkinOnTrackKey(skinId, trackId)),
        () => 1);

    /// <summary>Venceu a Mortal na pista usando qualquer skin da categoria.</summary>
    public static UnlockRequirement WinWithCategoryOnTrack(SkinCategory category, string trackId) => new ConditionRequirement(
        () => $"VENCA NA PISTA {TrackName(trackId)} USANDO {SkinCategories.Article(category)} {SkinCategories.Singular(category)}",
        p => CarSkins.All.Any(skin => skin.Category == category && p.WinsBySkinOnTrack.ContainsKey(Progression.SkinOnTrackKey(skin.Id, trackId))));

    /// <summary>Quantas categorias de skin diferentes já venceram a Mortal.</summary>
    public static UnlockRequirement CategoriesWon(int categories) => Stat(
        $"VENCA COM SKINS DE {categories} CATEGORIAS DIFERENTES",
        p => Enum.GetValues<SkinCategory>().Count(category => CarSkins.All.Any(skin => skin.Category == category && p.WinsBySkin.ContainsKey(skin.Id))),
        categories);

    public static UnlockRequirement MostGamesWithOneSkin(int games) => Stat(
        $"JOGUE {games} PARTIDAS COM UMA MESMA SKIN",
        p => p.GamesBySkin.Count == 0 ? 0 : p.GamesBySkin.Values.Max(),
        games);

    /// <summary>Tem todas as skins dadas desbloqueadas.</summary>
    public static UnlockRequirement SkinsEarned(string description, params string[] skinIds) => Stat(
        () => description,
        p => skinIds.Count(id => p.UnlockedSkinIds.Contains(id)),
        () => skinIds.Length);

    public static UnlockRequirement GamesWithSkin(string skinId, int games) => Stat(
        () => $"JOGUE {games} PARTIDAS COM {SkinName(skinId)}",
        p => p.GamesBySkin.GetValueOrDefault(skinId),
        () => games);

    /// <summary>Venceu com a skin em N pistas diferentes.</summary>
    public static UnlockRequirement WinsWithSkinOnTracks(string skinId, int tracks) => Stat(
        () => $"VENCA COM {SkinName(skinId)} EM {tracks} PISTAS DIFERENTES",
        p => TrackThemes.All.Count(t => p.WinsBySkinOnTrack.ContainsKey(Progression.SkinOnTrackKey(skinId, t.Id))),
        () => tracks);

    /// <summary>Venceu a Mortal com N skins diferentes de uma categoria.</summary>
    public static UnlockRequirement WinsWithCategory(SkinCategory category, int skins) => Stat(
        () => skins == 1
            ? $"VENCA A MORTAL COM {SkinCategories.Article(category)} {SkinCategories.Singular(category)}"
            : $"VENCA COM {skins} {SkinCategories.Plural(category)} DIFERENTES",
        p => CarSkins.All.Count(skin => skin.Category == category && p.WinsBySkin.ContainsKey(skin.Id)),
        () => skins);

    public static UnlockRequirement SkinsOfCategoryUnlocked(SkinCategory category, int skins) => Stat(
        () => $"DESBLOQUEIE {skins} {SkinCategories.Plural(category)}",
        p => CarSkins.All.Count(skin => skin.Category == category && Unlockables.IsUnlocked(skin, p.UnlockedSkinIds)),
        () => skins);

    public static UnlockRequirement SameSkinWinStreak(int wins) => Stat(
        $"VENCA {wins} MORTAIS SEGUIDAS COM A MESMA SKIN", p => p.BestSameSkinWinStreak, wins);

    public static UnlockRequirement SameSkinGameStreak(int games) => Stat(
        $"JOGUE {games} PARTIDAS SEGUIDAS SEM TROCAR DE SKIN", p => p.BestSameSkinGameStreak, games);

    // ----- Habilidade -----
    public static UnlockRequirement LapUnderOnTrack(string trackId, float seconds) => new TimeBelowRequirement(
        () => $"FACA UMA VOLTA EM MENOS DE {seconds:0.#} S NA PISTA {TrackName(trackId)}",
        p => p.BestLapByTrack.TryGetValue(trackId, out float lap) ? lap : null,
        seconds);

    /// <summary>Fez a pontuação em N pistas diferentes (cada uma numa partida).</summary>
    public static UnlockRequirement ScoreOnTracks(int points, int tracks) => Stat(
        $"FACA {points} PTS NO RELOGIO EM {tracks} PISTAS DIFERENTES",
        p => p.BestScoreByTrack.Count(pair => pair.Value >= points),
        tracks);

    public static UnlockRequirement Standstill(int seconds, string flavor = null) => Stat(
        flavor ?? $"FIQUE {seconds} S PARADO NUMA PARTIDA", p => p.LongestStandstillSeconds, seconds);

    public static UnlockRequirement DaysPlayed(int days) => Stat(
        $"JOGUE EM {days} DIAS DIFERENTES", p => p.DaysPlayed, days);

    /// <summary>Uma interação escondida dos menus (ver <see cref="Discovery"/>).</summary>
    public static UnlockRequirement Discovered(string discoveryId, string description) => new ConditionRequirement(
        () => description,
        p => p.Discoveries.Contains(discoveryId));

    // ----- Conquistas -----
    public static UnlockRequirement AchievementsOfDifficulty(Difficulty difficulty, int count) => Stat(
        () => $"DESBLOQUEIE {count} CONQUISTAS {DifficultyAdjective(difficulty, count)}",
        p => Achievements.All.Count(a => a.Difficulty == difficulty && Achievements.IsUnlocked(a, p)),
        () => count);

    public static UnlockRequirement SecretAchievements(int count) => Stat(
        () => $"DESBLOQUEIE {count} CONQUISTAS SECRETAS",
        p => Achievements.All.Count(a => a.IsSecret && Achievements.IsUnlocked(a, p)),
        () => count);

    private static string DifficultyAdjective(Difficulty difficulty, int count) => (difficulty, count == 1) switch
    {
        (Difficulty.Easy, true) => "FACIL",
        (Difficulty.Easy, false) => "FACEIS",
        (Difficulty.Medium, true) => "MEDIA",
        (Difficulty.Medium, false) => "MEDIAS",
        (Difficulty.Hard, true) => "DIFICIL",
        (Difficulty.Hard, false) => "DIFICEIS",
        (Difficulty.VeryHard, true) => "MUITO DIFICIL",
        (Difficulty.VeryHard, false) => "MUITO DIFICEIS",
        (_, true) => "RARA",
        _ => "RARAS",
    };

    public static UnlockRequirement AchievementsUnlocked(int count) => Stat(
        $"DESBLOQUEIE {count} CONQUISTAS", p => Achievements.UnlockedCount(p), count);

    public static UnlockRequirement HalfOfAchievements() => Stat(
        () => "DESBLOQUEIE METADE DAS CONQUISTAS",
        p => Achievements.UnlockedCount(p),
        () => MathF.Ceiling(Achievements.All.Count / 2f));

    /// <summary>Todas as conquistas menos a própria que usa este requisito (a "platina").</summary>
    public static UnlockRequirement AllOtherAchievements() => Stat(
        () => "DESBLOQUEIE TODAS AS OUTRAS CONQUISTAS",
        p => Achievements.UnlockedCount(p),
        () => Achievements.All.Count - 1);

    public static UnlockRequirement AchievementEarned(string achievementId) => new ConditionRequirement(
        () => $"GANHE A CONQUISTA {Achievements.Find(achievementId)?.Name ?? achievementId}",
        p => p.UnlockedAchievementIds.Contains(achievementId));
}
