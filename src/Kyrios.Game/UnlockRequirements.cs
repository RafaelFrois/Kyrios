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
    public override string Description => L.T("DISPONIVEL DESDE O INICIO", "AVAILABLE FROM THE START");

    public override bool IsMet(SaveData progress) => true;
}

/// <summary>Uma estatística que só cresce (vitórias, pontuação, partidas jogadas...) precisa chegar a um alvo.
/// Descrição e alvo podem ser calculados na hora (ex.: "todas as skins do catálogo", nome de uma pista, idioma),
/// pra não depender da ordem em que os catálogos são montados.</summary>
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
    public override string Description => description();

    public override bool IsMet(SaveData progress) => bestTime(progress) is { } time && time < targetSeconds;

    public override string ProgressText(SaveData progress) =>
        bestTime(progress) is { } time
            ? L.T($"SEU MELHOR: {TimeFormat.Short(time)}", $"YOUR BEST: {TimeFormat.Short(time)}")
            : L.T("AINDA SEM TEMPO", "NO TIME YET");
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
    public override string Description => L.Tr(description);

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
/// Idioma: frases fixas escritas no catálogo passam pela tabela de tradução (<see cref="L.Tr"/>); as frases
/// montadas aqui com números e nomes já vêm nos dois idiomas (<see cref="L.T"/>), sempre calculadas na hora.
/// </summary>
public static class Unlock
{
    public static UnlockRequirement FromStart { get; } = new AlwaysUnlockedRequirement();

    /// <summary>Estatística com uma frase fixa do catálogo (traduzida pela tabela).</summary>
    public static UnlockRequirement Stat(string description, Func<SaveData, float> stat, float target) =>
        new StatAtLeastRequirement(() => L.Tr(description), stat, () => target);

    public static UnlockRequirement Stat(Func<string> description, Func<SaveData, float> stat, Func<float> target) =>
        new StatAtLeastRequirement(description, stat, target);

    private static UnlockRequirement Stat(Func<string> description, Func<SaveData, float> stat, float target) =>
        new StatAtLeastRequirement(description, stat, () => target);

    /// <summary>Condição com uma frase fixa do catálogo (traduzida pela tabela).</summary>
    public static UnlockRequirement Condition(string description, Func<SaveData, bool> condition) =>
        new ConditionRequirement(() => L.Tr(description), condition);

    public static UnlockRequirement All(string description, params UnlockRequirement[] parts) =>
        new AllOfRequirement(description, parts);

    private static string Plural(int count, string singular, string plural) => count == 1 ? $"1 {singular}" : $"{count} {plural}";

    private static string TrackName(string trackId) => TrackThemes.Find(trackId)?.Name ?? trackId;

    private static string SkinName(string skinId) => CarSkins.Find(skinId)?.Name ?? skinId;

    private static string DeathRaceCount(int count) => L.T(
        Plural(count, "CORRIDA MORTAL", "CORRIDAS MORTAIS"),
        Plural(count, "DEATH RACE", "DEATH RACES"));

    private static string TrackCount(int count) => L.T(
        Plural(count, "PISTA", "PISTAS DIFERENTES"),
        Plural(count, "TRACK", "DIFFERENT TRACKS"));

    // ----- Corrida Mortal -----
    public static UnlockRequirement DeathRaceWins(int wins) => Stat(
        () => L.T($"VENCA {DeathRaceCount(wins)}", $"WIN {DeathRaceCount(wins)}"), p => p.EliminationWins, wins);

    public static UnlockRequirement DeathRaceWinStreak(int wins) => Stat(
        () => L.T($"VENCA {wins} CORRIDAS MORTAIS SEGUIDAS", $"WIN {wins} DEATH RACES IN A ROW"), p => p.BestEliminationWinStreak, wins);

    public static UnlockRequirement DeathRaces(int races) => Stat(
        () => L.T($"JOGUE {DeathRaceCount(races)}", $"PLAY {DeathRaceCount(races)}"), p => p.EliminationRaces, races);

    public static UnlockRequirement RoundsSurvived(int rounds) => Stat(
        () => L.T($"SOBREVIVA A {Plural(rounds, "ELIMINACAO", "ELIMINACOES")}", $"SURVIVE {Plural(rounds, "ELIMINATION", "ELIMINATIONS")}"),
        p => p.EliminationRoundsSurvived, rounds);

    // ----- Contra o Relógio -----
    public static UnlockRequirement TimeAttackRaces(int races) => Stat(
        () => races == 1
            ? L.T("JOGUE O CONTRA O RELOGIO", "PLAY TIME ATTACK")
            : L.T($"JOGUE {races} VEZES O CONTRA O RELOGIO", $"PLAY TIME ATTACK {races} TIMES"),
        p => p.TimeAttackRaces, races);

    /// <summary>Pontuação numa única partida (o recorde).</summary>
    public static UnlockRequirement TimeAttackScore(int points) => Stat(
        () => L.T($"FACA {points} PTS NUMA PARTIDA DO RELOGIO", $"SCORE {points} PTS IN ONE TIME ATTACK RUN"),
        p => p.BestScoreTimeAttack ?? 0f, points);

    /// <summary>Pontos somados de todas as partidas — metas de longo prazo.</summary>
    public static UnlockRequirement TimeAttackTotal(int points) => Stat(
        () => L.T($"ACUMULE {points} PTS NO CONTRA O RELOGIO", $"EARN {points} PTS IN TIME ATTACK IN TOTAL"),
        p => p.TotalTimeAttackScore, points);

    // ----- Gerais -----
    public static UnlockRequirement GamesPlayed(int games) => Stat(
        () => games == 1
            ? L.T("TERMINE SUA PRIMEIRA PARTIDA", "FINISH YOUR FIRST GAME")
            : L.T($"JOGUE {games} PARTIDAS", $"PLAY {games} GAMES"),
        p => p.GamesPlayed, games);

    public static UnlockRequirement LapUnder(float seconds) => new TimeBelowRequirement(
        () => L.T($"FACA UMA VOLTA EM MENOS DE {seconds:0.#} S", $"DO A LAP UNDER {seconds:0.#} S"), p => p.BestLapTime, seconds);

    // ----- Pistas -----
    public static UnlockRequirement TimeAttackScoreOnTrack(string trackId, int points) => Stat(
        () => L.T($"FACA {points} PTS NO RELOGIO NA PISTA {TrackName(trackId)}", $"SCORE {points} PTS IN TIME ATTACK ON THE {TrackName(trackId)} TRACK"),
        p => p.BestScoreByTrack.GetValueOrDefault(trackId),
        () => points);

    public static UnlockRequirement DeathRaceWinsOnTrack(string trackId, int wins = 1) => Stat(
        () => L.T($"VENCA {DeathRaceCount(wins)} NA PISTA {TrackName(trackId)}", $"WIN {DeathRaceCount(wins)} ON THE {TrackName(trackId)} TRACK"),
        p => p.WinsByTrack.GetValueOrDefault(trackId),
        () => wins);

    public static UnlockRequirement WinWithSkinOnTrack(string skinId, string trackId) => Stat(
        () => L.T($"VENCA COM {SkinName(skinId)} NA PISTA {TrackName(trackId)}", $"WIN WITH {SkinName(skinId)} ON THE {TrackName(trackId)} TRACK"),
        p => p.WinsBySkinOnTrack.GetValueOrDefault(Progression.SkinOnTrackKey(skinId, trackId)),
        () => 1);

    public static UnlockRequirement TracksPlayed(int count) => Stat(
        () => L.T($"JOGUE EM {TrackCount(count)}", $"PLAY ON {TrackCount(count)}"),
        p => TrackThemes.All.Count(t => p.GamesByTrack.ContainsKey(t.Id)),
        count);

    public static UnlockRequirement AllTracksPlayed() => Stat(
        () => L.T("JOGUE EM TODAS AS PISTAS", "PLAY ON EVERY TRACK"),
        p => TrackThemes.All.Count(t => p.GamesByTrack.ContainsKey(t.Id)),
        () => TrackThemes.All.Count);

    public static UnlockRequirement TracksWon(int count) => Stat(
        () => L.T($"VENCA A MORTAL EM {TrackCount(count)}", $"WIN A DEATH RACE ON {TrackCount(count)}"),
        p => TrackThemes.All.Count(t => p.WinsByTrack.ContainsKey(t.Id)),
        count);

    public static UnlockRequirement AllTracksWon() => Stat(
        () => L.T("VENCA A MORTAL EM TODAS AS PISTAS", "WIN A DEATH RACE ON EVERY TRACK"),
        p => TrackThemes.All.Count(t => p.WinsByTrack.ContainsKey(t.Id)),
        () => TrackThemes.All.Count);

    public static UnlockRequirement CleanTracks(int count) => Stat(
        () => L.T($"TERMINE SEM BATER EM {TrackCount(count)}", $"FINISH WITHOUT CRASHING ON {TrackCount(count)}"),
        p => TrackThemes.All.Count(t => p.CleanTracks.Contains(t.Id)),
        count);

    public static UnlockRequirement GamesOnTrack(string trackId, int games) => Stat(
        () => L.T($"JOGUE {games} PARTIDAS NA PISTA {TrackName(trackId)}", $"PLAY {games} GAMES ON THE {TrackName(trackId)} TRACK"),
        p => p.GamesByTrack.GetValueOrDefault(trackId),
        () => games);

    public static UnlockRequirement CleanOnTrack(string trackId) => new ConditionRequirement(
        () => L.T($"TERMINE UMA PARTIDA SEM BATER NA PISTA {TrackName(trackId)}", $"FINISH A GAME WITHOUT CRASHING ON THE {TrackName(trackId)} TRACK"),
        p => p.CleanTracks.Contains(trackId));

    public static UnlockRequirement TrackSecretsFound(int count) => Stat(
        () => L.T(
            $"ENCONTRE {Plural(count, "SEGREDO ESCONDIDO", "SEGREDOS ESCONDIDOS")} NAS PISTAS",
            $"FIND {Plural(count, "HIDDEN SECRET", "HIDDEN SECRETS")} ON THE TRACKS"),
        p => TrackThemes.All.Count(t => p.FoundTrackSecrets.Contains(t.Id)),
        count);

    public static UnlockRequirement AllTrackSecretsFound() => Stat(
        () => L.T("ENCONTRE O SEGREDO DE TODAS AS PISTAS", "FIND THE SECRET OF EVERY TRACK"),
        p => TrackThemes.All.Count(t => p.FoundTrackSecrets.Contains(t.Id)),
        () => TrackThemes.All.Count);

    public static UnlockRequirement TrackSecretFound(string trackId) => new ConditionRequirement(
        () => L.T($"ENCONTRE O SEGREDO DA PISTA {TrackName(trackId)}", $"FIND THE SECRET OF THE {TrackName(trackId)} TRACK"),
        p => p.FoundTrackSecrets.Contains(trackId));

    public static UnlockRequirement TracksUnlocked(int count) => Stat(
        () => L.T($"DESBLOQUEIE {Plural(count, "PISTA", "PISTAS")}", $"UNLOCK {Plural(count, "TRACK", "TRACKS")}"),
        p => Unlockables.Count(TrackThemes.All, p.UnlockedTrackIds).Earned,
        count);

    public static UnlockRequirement AllTracksUnlocked() => Stat(
        () => L.T("DESBLOQUEIE TODAS AS PISTAS", "UNLOCK EVERY TRACK"),
        p => Unlockables.Count(TrackThemes.All, p.UnlockedTrackIds).Earned,
        () => Unlockables.Count(TrackThemes.All, []).Earnable);

    public static UnlockRequirement TrackEarned(string trackId) => new ConditionRequirement(
        () => L.T($"DESBLOQUEIE A PISTA {TrackName(trackId)}", $"UNLOCK THE {TrackName(trackId)} TRACK"),
        p => p.UnlockedTrackIds.Contains(trackId));

    // ----- Skins -----
    /// <summary>Quantas skins além da padrão já foram liberadas.</summary>
    public static UnlockRequirement SkinsUnlocked(int count) => Stat(
        () => L.T($"DESBLOQUEIE {Plural(count, "SKIN", "SKINS")}", $"UNLOCK {Plural(count, "SKIN", "SKINS")}"),
        p => Unlockables.Count(CarSkins.All, p.UnlockedSkinIds).Earned,
        count);

    public static UnlockRequirement AllSkins() => Stat(
        () => L.T("DESBLOQUEIE TODAS AS SKINS", "UNLOCK EVERY SKIN"),
        p => Unlockables.Count(CarSkins.All, p.UnlockedSkinIds).Earned,
        () => Unlockables.Count(CarSkins.All, []).Earnable);

    /// <summary>Todas as skins menos a própria que usa este requisito (o troféu final da coleção).</summary>
    public static UnlockRequirement AllOtherSkins() => Stat(
        () => L.T("DESBLOQUEIE TODAS AS OUTRAS SKINS", "UNLOCK EVERY OTHER SKIN"),
        p => Unlockables.Count(CarSkins.All, p.UnlockedSkinIds).Earned,
        () => Unlockables.Count(CarSkins.All, []).Earnable - 1);

    public static UnlockRequirement SkinEarned(string skinId) => new ConditionRequirement(
        () => L.T($"DESBLOQUEIE A SKIN {SkinName(skinId)}", $"UNLOCK THE {SkinName(skinId)} SKIN"),
        p => p.UnlockedSkinIds.Contains(skinId));

    public static UnlockRequirement WinsWithSkin(string skinId, int wins = 1) => Stat(
        () => wins == 1
            ? L.T($"VENCA A MORTAL COM {SkinName(skinId)}", $"WIN A DEATH RACE WITH {SkinName(skinId)}")
            : L.T($"VENCA {wins} MORTAIS COM {SkinName(skinId)}", $"WIN {wins} DEATH RACES WITH {SkinName(skinId)}"),
        p => p.WinsBySkin.GetValueOrDefault(skinId),
        () => wins);

    public static UnlockRequirement RecordWithSkin(string skinId) => Stat(
        () => L.T($"BATA SEU RECORDE COM {SkinName(skinId)}", $"BEAT YOUR RECORD WITH {SkinName(skinId)}"),
        p => p.RecordsBySkin.GetValueOrDefault(skinId),
        () => 1);

    public static UnlockRequirement ScoreWithSkin(string skinId, int points) => Stat(
        () => L.T($"FACA {points} PTS NO RELOGIO COM {SkinName(skinId)}", $"SCORE {points} PTS IN TIME ATTACK WITH {SkinName(skinId)}"),
        p => p.BestScoreBySkin.GetValueOrDefault(skinId),
        () => points);

    public static UnlockRequirement SkinsUsed(int count) => Stat(
        () => L.T($"JOGUE COM {count} SKINS DIFERENTES", $"PLAY WITH {count} DIFFERENT SKINS"), p => p.SkinsUsed.Count, count);

    public static UnlockRequirement SkinsWonWith(int count) => Stat(
        () => L.T($"VENCA COM {count} SKINS DIFERENTES", $"WIN WITH {count} DIFFERENT SKINS"), p => p.WinsBySkin.Count, count);

    /// <summary>Venceu pelo menos uma vez com cada uma das skins dadas.</summary>
    public static UnlockRequirement WonWithEachSkin(string description, params string[] skinIds) => Stat(
        () => L.Tr(description),
        p => skinIds.Count(id => p.WinsBySkin.ContainsKey(id)),
        () => skinIds.Length);

    public static UnlockRequirement PlayedWithSkinOnTrack(string skinId, string trackId) => Stat(
        () => L.T($"JOGUE NA PISTA {TrackName(trackId)} USANDO {SkinName(skinId)}", $"PLAY ON THE {TrackName(trackId)} TRACK USING {SkinName(skinId)}"),
        p => p.GamesBySkinOnTrack.GetValueOrDefault(Progression.SkinOnTrackKey(skinId, trackId)),
        () => 1);

    /// <summary>Venceu a Mortal na pista usando qualquer skin da categoria.</summary>
    public static UnlockRequirement WinWithCategoryOnTrack(SkinCategory category, string trackId) => new ConditionRequirement(
        () => L.T(
            $"VENCA NA PISTA {TrackName(trackId)} USANDO {SkinCategories.Article(category)} {SkinCategories.Singular(category)}",
            $"WIN ON THE {TrackName(trackId)} TRACK USING {SkinCategories.Article(category)} {SkinCategories.Singular(category)}"),
        p => CarSkins.All.Any(skin => skin.Category == category && p.WinsBySkinOnTrack.ContainsKey(Progression.SkinOnTrackKey(skin.Id, trackId))));

    /// <summary>Quantas categorias de skin diferentes já venceram a Mortal.</summary>
    public static UnlockRequirement CategoriesWon(int categories) => Stat(
        () => L.T($"VENCA COM SKINS DE {categories} CATEGORIAS DIFERENTES", $"WIN WITH SKINS FROM {categories} DIFFERENT CATEGORIES"),
        p => Enum.GetValues<SkinCategory>().Count(category => CarSkins.All.Any(skin => skin.Category == category && p.WinsBySkin.ContainsKey(skin.Id))),
        categories);

    public static UnlockRequirement MostGamesWithOneSkin(int games) => Stat(
        () => L.T($"JOGUE {games} PARTIDAS COM UMA MESMA SKIN", $"PLAY {games} GAMES WITH THE SAME SKIN"),
        p => p.GamesBySkin.Count == 0 ? 0 : p.GamesBySkin.Values.Max(),
        games);

    /// <summary>Tem todas as skins dadas desbloqueadas.</summary>
    public static UnlockRequirement SkinsEarned(string description, params string[] skinIds) => Stat(
        () => L.Tr(description),
        p => skinIds.Count(id => p.UnlockedSkinIds.Contains(id)),
        () => skinIds.Length);

    public static UnlockRequirement GamesWithSkin(string skinId, int games) => Stat(
        () => L.T($"JOGUE {games} PARTIDAS COM {SkinName(skinId)}", $"PLAY {games} GAMES WITH {SkinName(skinId)}"),
        p => p.GamesBySkin.GetValueOrDefault(skinId),
        () => games);

    /// <summary>Venceu com a skin em N pistas diferentes.</summary>
    public static UnlockRequirement WinsWithSkinOnTracks(string skinId, int tracks) => Stat(
        () => L.T($"VENCA COM {SkinName(skinId)} EM {tracks} PISTAS DIFERENTES", $"WIN WITH {SkinName(skinId)} ON {tracks} DIFFERENT TRACKS"),
        p => TrackThemes.All.Count(t => p.WinsBySkinOnTrack.ContainsKey(Progression.SkinOnTrackKey(skinId, t.Id))),
        () => tracks);

    /// <summary>Venceu a Mortal com N skins diferentes de uma categoria.</summary>
    public static UnlockRequirement WinsWithCategory(SkinCategory category, int skins) => Stat(
        () => skins == 1
            ? L.T(
                $"VENCA A MORTAL COM {SkinCategories.Article(category)} {SkinCategories.Singular(category)}",
                $"WIN A DEATH RACE WITH {SkinCategories.Article(category)} {SkinCategories.Singular(category)}")
            : L.T(
                $"VENCA COM {skins} {SkinCategories.Plural(category)} DIFERENTES",
                $"WIN WITH {skins} DIFFERENT {SkinCategories.Plural(category)}"),
        p => CarSkins.All.Count(skin => skin.Category == category && p.WinsBySkin.ContainsKey(skin.Id)),
        () => skins);

    public static UnlockRequirement SkinsOfCategoryUnlocked(SkinCategory category, int skins) => Stat(
        () => L.T($"DESBLOQUEIE {skins} {SkinCategories.Plural(category)}", $"UNLOCK {skins} {SkinCategories.Plural(category)}"),
        p => CarSkins.All.Count(skin => skin.Category == category && Unlockables.IsUnlocked(skin, p.UnlockedSkinIds)),
        () => skins);

    public static UnlockRequirement SameSkinWinStreak(int wins) => Stat(
        () => L.T($"VENCA {wins} MORTAIS SEGUIDAS COM A MESMA SKIN", $"WIN {wins} DEATH RACES IN A ROW WITH THE SAME SKIN"),
        p => p.BestSameSkinWinStreak, wins);

    public static UnlockRequirement SameSkinGameStreak(int games) => Stat(
        () => L.T($"JOGUE {games} PARTIDAS SEGUIDAS SEM TROCAR DE SKIN", $"PLAY {games} GAMES IN A ROW WITHOUT CHANGING SKIN"),
        p => p.BestSameSkinGameStreak, games);

    // ----- Habilidade -----
    public static UnlockRequirement LapUnderOnTrack(string trackId, float seconds) => new TimeBelowRequirement(
        () => L.T(
            $"FACA UMA VOLTA EM MENOS DE {seconds:0.#} S NA PISTA {TrackName(trackId)}",
            $"DO A LAP UNDER {seconds:0.#} S ON THE {TrackName(trackId)} TRACK"),
        p => p.BestLapByTrack.TryGetValue(trackId, out float lap) ? lap : null,
        seconds);

    /// <summary>Fez a pontuação em N pistas diferentes (cada uma numa partida).</summary>
    public static UnlockRequirement ScoreOnTracks(int points, int tracks) => Stat(
        () => L.T($"FACA {points} PTS NO RELOGIO EM {tracks} PISTAS DIFERENTES", $"SCORE {points} PTS IN TIME ATTACK ON {tracks} DIFFERENT TRACKS"),
        p => p.BestScoreByTrack.Count(pair => pair.Value >= points),
        tracks);

    public static UnlockRequirement Standstill(int seconds, string flavor = null) => Stat(
        () => flavor is not null ? L.Tr(flavor) : L.T($"FIQUE {seconds} S PARADO NUMA PARTIDA", $"STAND STILL FOR {seconds} S IN A GAME"),
        p => p.LongestStandstillSeconds, seconds);

    public static UnlockRequirement DaysPlayed(int days) => Stat(
        () => L.T($"JOGUE EM {days} DIAS DIFERENTES", $"PLAY ON {days} DIFFERENT DAYS"), p => p.DaysPlayed, days);

    /// <summary>Uma interação escondida dos menus (ver <see cref="Discovery"/>).</summary>
    public static UnlockRequirement Discovered(string discoveryId, string description) => new ConditionRequirement(
        () => L.Tr(description),
        p => p.Discoveries.Contains(discoveryId));

    // ----- Conquistas -----
    public static UnlockRequirement AchievementsOfDifficulty(Difficulty difficulty, int count) => Stat(
        () => L.T(
            $"DESBLOQUEIE {count} CONQUISTAS {DifficultyAdjective(difficulty, count)}",
            $"UNLOCK {count} {DifficultyAdjective(difficulty, count)} ACHIEVEMENTS"),
        p => Achievements.All.Count(a => a.Difficulty == difficulty && Achievements.IsUnlocked(a, p)),
        () => count);

    public static UnlockRequirement SecretAchievements(int count) => Stat(
        () => L.T($"DESBLOQUEIE {count} CONQUISTAS SECRETAS", $"UNLOCK {count} SECRET ACHIEVEMENTS"),
        p => Achievements.All.Count(a => a.IsSecret && Achievements.IsUnlocked(a, p)),
        () => count);

    private static string DifficultyAdjective(Difficulty difficulty, int count) => L.English
        ? difficulty switch
        {
            Difficulty.Easy => "EASY",
            Difficulty.Medium => "MEDIUM",
            Difficulty.Hard => "HARD",
            Difficulty.VeryHard => "VERY HARD",
            _ => "RARE",
        }
        : (difficulty, count == 1) switch
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
        () => L.T($"DESBLOQUEIE {count} CONQUISTAS", $"UNLOCK {count} ACHIEVEMENTS"), p => Achievements.UnlockedCount(p), count);

    public static UnlockRequirement HalfOfAchievements() => Stat(
        () => L.T("DESBLOQUEIE METADE DAS CONQUISTAS", "UNLOCK HALF OF THE ACHIEVEMENTS"),
        p => Achievements.UnlockedCount(p),
        () => MathF.Ceiling(Achievements.All.Count / 2f));

    /// <summary>Todas as conquistas menos a própria que usa este requisito (a "platina").</summary>
    public static UnlockRequirement AllOtherAchievements() => Stat(
        () => L.T("DESBLOQUEIE TODAS AS OUTRAS CONQUISTAS", "UNLOCK EVERY OTHER ACHIEVEMENT"),
        p => Achievements.UnlockedCount(p),
        () => Achievements.All.Count - 1);

    public static UnlockRequirement AchievementEarned(string achievementId) => new ConditionRequirement(
        () => L.T(
            $"GANHE A CONQUISTA {Achievements.Find(achievementId)?.Name ?? achievementId}",
            $"EARN THE {Achievements.Find(achievementId)?.Name ?? achievementId} ACHIEVEMENT"),
        p => p.UnlockedAchievementIds.Contains(achievementId));
}
