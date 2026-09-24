using Kyrios.Core;

namespace Kyrios.Game;

/// <summary>Um desbloqueio a anunciar: uma conquista, uma skin ou uma pista (exatamente um dos três).</summary>
public sealed record UnlockNotice(Achievement Achievement = null, CarSkin Skin = null, TrackTheme Track = null);

/// <summary>Quais recordes a partida acabou de bater (pra tela de resultado destacar).</summary>
public readonly record struct RecordFlags(bool NewScoreRecord, bool NewLapRecord)
{
    public bool Any => NewScoreRecord || NewLapRecord;
}

/// <summary>Qualquer coisa desbloqueável por progresso (skin, pista, conquista): um id estável e uma condição.</summary>
public interface IUnlockable
{
    string Id { get; }

    UnlockRequirement Requirement { get; }
}

/// <summary>A regra de desbloqueio compartilhada por skins, pistas e conquistas — uma implementação só.</summary>
public static class Unlockables
{
    public static bool IsUnlocked(IUnlockable item, ICollection<string> unlockedIds) =>
        item.Requirement is AlwaysUnlockedRequirement || unlockedIds.Contains(item.Id);

    /// <summary>Anota em <paramref name="unlockedIds"/> todo item ainda bloqueado cuja condição já foi cumprida e
    /// devolve quais foram, na ordem do catálogo. Repete até estabilizar (um item pode depender de outro do
    /// mesmo catálogo).</summary>
    public static List<T> UnlockNewlyEarned<T>(IReadOnlyList<T> catalog, List<string> unlockedIds, SaveData progress)
        where T : IUnlockable
    {
        var earned = new List<T>();
        bool changed;
        do
        {
            changed = false;
            foreach (T item in catalog)
            {
                if (!IsUnlocked(item, unlockedIds) && item.Requirement.IsMet(progress))
                {
                    unlockedIds.Add(item.Id);
                    earned.Add(item);
                    changed = true;
                }
            }
        }
        while (changed);

        return earned;
    }

    /// <summary>Quantos itens do catálogo exigem ser conquistados, e quantos já foram.</summary>
    public static (int Earned, int Earnable) Count<T>(IReadOnlyList<T> catalog, ICollection<string> unlockedIds)
        where T : IUnlockable
    {
        int earnable = catalog.Count(item => item.Requirement is not AlwaysUnlockedRequirement);
        int earned = catalog.Count(item => item.Requirement is not AlwaysUnlockedRequirement && unlockedIds.Contains(item.Id));
        return (earned, earnable);
    }
}

/// <summary>
/// Sistema central de progressão. O fluxo de fim de partida é sempre o mesmo, pra qualquer modo:
/// ação do jogador → <see cref="RecordRace"/> (atualiza as estatísticas) → <see cref="CheckUnlocks"/>
/// (confere conquistas, skins e pistas até estabilizar e libera o que foi cumprido) → quem chamou salva e mostra.
/// Nenhuma conquista, skin ou pista tem código próprio: todas são dados nos catálogos.
/// </summary>
public static class Progression
{
    /// <summary>Soma a partida às estatísticas salvas e devolve os recordes batidos.</summary>
    public static RecordFlags RecordRace(SaveData save, RaceReport report)
    {
        bool newScore = false;
        bool newLap = false;

        switch (report.Mode)
        {
            case RaceMode.Elimination:
                save.EliminationRaces++;
                save.EliminationRoundsSurvived += report.RoundsSurvived;
                if (report.Won)
                {
                    save.EliminationWins++;
                    save.EliminationWinStreak++;
                    save.BestEliminationWinStreak = Math.Max(save.BestEliminationWinStreak, save.EliminationWinStreak);
                    save.MostCollisionsInAWin = Math.Max(save.MostCollisionsInAWin, report.Collisions);
                    save.EliminationFlawlessWins += report.WasEverLast ? 0 : 1;
                    save.EliminationComebackWins += report.WasEverLast ? 1 : 0;
                    save.EliminationCleanWins += report.Collisions == 0 ? 1 : 0;
                    save.EliminationNoBoostWins += report.BoostSeconds <= 0f ? 1 : 0;
                    save.EliminationLazyWins += report.LazyWin ? 1 : 0;
                    Increment(save.WinsBySkin, report.SkinId);
                    Increment(save.WinsByTrack, report.TrackId);
                    if (report.SkinId is not null && report.TrackId is not null)
                    {
                        Increment(save.WinsBySkinOnTrack, SkinOnTrackKey(report.SkinId, report.TrackId));
                    }
                }
                else
                {
                    save.EliminationWinStreak = 0;
                }

                if (report.Place == report.EntrantCount)
                {
                    save.EliminationFirstOuts++;
                }
                else if (report.Place == 2)
                {
                    save.EliminationRunnerUps++;
                }

                save.EliminatedWhileBoosting += report.EliminatedWhileBoosting ? 1 : 0;
                break;

            case RaceMode.TimeAttack:
                save.TimeAttackRaces++;
                save.TotalTimeAttackScore += report.Score;
                if (report.Score > 0f && (save.BestScoreTimeAttack is null || report.Score > save.BestScoreTimeAttack))
                {
                    save.RecordsBeaten += save.BestScoreTimeAttack is > 0f ? 1 : 0;
                    save.RecordsSet++;
                    Increment(save.RecordsBySkin, report.SkinId);
                    newScore = true;
                }

                if (save.BestScoreTimeAttack is null || report.Score > save.BestScoreTimeAttack)
                {
                    save.BestScoreTimeAttack = report.Score;
                }

                if (report.Collisions == 0)
                {
                    save.BestCleanTimeAttackScore = MathF.Max(save.BestCleanTimeAttackScore, report.Score);
                }

                KeepMax(save.BestScoreBySkin, report.SkinId, report.Score);
                KeepMax(save.BestScoreByTrack, report.TrackId, report.Score);
                save.TimeAttackClutchCheckpoints += report.ClutchCheckpoint ? 1 : 0;
                save.TimeAttackZeroScores += report.Score <= 0f ? 1 : 0;
                save.MostTimeAttackLaps = Math.Max(save.MostTimeAttackLaps, report.LapsCompleted);
                save.MostTimeBanked = MathF.Max(save.MostTimeBanked, report.MaxTimeBanked);
                save.MostTimeLostInOneRun = MathF.Max(save.MostTimeLostInOneRun, report.TimeLostToCrashes);
                break;
        }

        if (report.BestLapTime is { } lap && (save.BestLapTime is null || lap < save.BestLapTime))
        {
            newLap = save.BestLapTime is not null;
            save.BestLapTime = lap;
        }

        Increment(save.GamesByTrack, report.TrackId);
        AddOnce(save.SkinsUsed, report.SkinId);
        if (report.Collisions == 0)
        {
            AddOnce(save.CleanTracks, report.TrackId);
        }

        if (report.FoundSecret)
        {
            AddOnce(save.FoundTrackSecrets, report.TrackId);
        }

        save.BestCleanCheckpointStreak = Math.Max(save.BestCleanCheckpointStreak, report.BestCleanCheckpointStreak);
        save.TotalCheckpoints += report.Checkpoints;
        save.TotalCollisions += report.Collisions;
        save.MostCollisionsInOneRace = Math.Max(save.MostCollisionsInOneRace, report.Collisions);
        save.StartLineCrashes += report.StartLineCrash ? 1 : 0;
        save.LongestStandstillSeconds = MathF.Max(save.LongestStandstillSeconds, report.LongestStandstillSeconds);
        save.LongestReverseSeconds = MathF.Max(save.LongestReverseSeconds, report.LongestReverseSeconds);
        save.TotalBoostSeconds += report.BoostSeconds;
        save.TotalRaceSeconds += report.RaceSeconds;
        save.TotalPauses += report.Pauses;
        save.MostPausesInOneRace = Math.Max(save.MostPausesInOneRace, report.Pauses);
        save.SilentGames += report.Silent ? 1 : 0;

        return new RecordFlags(newScore, newLap);
    }

    public static string SkinOnTrackKey(string skinId, string trackId) => $"{skinId}@{trackId}";

    /// <summary>
    /// Libera tudo o que o progresso atual já cumpre — conquistas, depois skins, depois pistas, repetindo até não
    /// mudar mais nada (uma skin pode exigir uma conquista, uma conquista pode exigir skins, uma pista pode exigir
    /// as duas...). Devolve os desbloqueios na ordem em que aconteceram; quem chamou salva e avisa o jogador.
    /// </summary>
    public static List<UnlockNotice> CheckUnlocks(SaveData save)
    {
        var notices = new List<UnlockNotice>();

        while (true)
        {
            List<Achievement> achievements = Unlockables.UnlockNewlyEarned(Achievements.All, save.UnlockedAchievementIds, save);
            List<CarSkin> skins = Unlockables.UnlockNewlyEarned(CarSkins.All, save.UnlockedSkinIds, save);
            List<TrackTheme> tracks = Unlockables.UnlockNewlyEarned(TrackThemes.All, save.UnlockedTrackIds, save);
            if (achievements.Count == 0 && skins.Count == 0 && tracks.Count == 0)
            {
                return notices;
            }

            notices.AddRange(achievements.Select(achievement => new UnlockNotice(Achievement: achievement)));
            notices.AddRange(skins.Select(skin => new UnlockNotice(Skin: skin)));
            notices.AddRange(tracks.Select(track => new UnlockNotice(Track: track)));
        }
    }

    /// <summary>Ao abrir um save: completa o que faltar e remove ids de itens que não existem mais (ex.: de
    /// conteúdo removido do jogo), pra nada "fantasma" contar nos totais.</summary>
    public static void Normalize(SaveData save)
    {
        save.FillStatsMissingFromOldSaves();
        save.UnlockedAchievementIds.RemoveAll(id => Achievements.Find(id) is null);
        save.UnlockedSkinIds.RemoveAll(id => CarSkins.Find(id) is null);
        save.UnlockedTrackIds.RemoveAll(id => TrackThemes.Find(id) is null);
    }

    private static void Increment(Dictionary<string, int> counters, string key)
    {
        if (key is not null)
        {
            counters[key] = counters.GetValueOrDefault(key) + 1;
        }
    }

    private static void KeepMax(Dictionary<string, float> bests, string key, float value)
    {
        if (key is not null && value > bests.GetValueOrDefault(key))
        {
            bests[key] = value;
        }
    }

    private static void AddOnce(List<string> list, string item)
    {
        if (item is not null && !list.Contains(item))
        {
            list.Add(item);
        }
    }
}
