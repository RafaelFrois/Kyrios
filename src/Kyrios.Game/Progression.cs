using Kyrios.Core;

namespace Kyrios.Game;

/// <summary>Um desbloqueio a anunciar: uma conquista ou uma skin (exatamente um dos dois).</summary>
public sealed record UnlockNotice(Achievement Achievement = null, CarSkin Skin = null);

/// <summary>Quais recordes a partida acabou de bater (pro pop-up de resultado destacar).</summary>
public readonly record struct RecordFlags(bool NewLapRecord, bool NewRaceRecord, bool NewScoreRecord)
{
    public bool Any => NewLapRecord || NewRaceRecord || NewScoreRecord;
}

/// <summary>
/// Sistema central de progressão. O fluxo de fim de partida é sempre o mesmo, pra qualquer modo:
/// ação do jogador → <see cref="RecordRace"/> (atualiza as estatísticas) → <see cref="CheckUnlocks"/>
/// (confere conquistas e skins até estabilizar e libera o que foi cumprido) → quem chamou salva e mostra as
/// notificações. Nenhuma conquista ou skin tem código próprio: todas são dados nos catálogos.
/// </summary>
public static class Progression
{
    /// <summary>Soma a partida às estatísticas salvas e devolve os recordes batidos.</summary>
    public static RecordFlags RecordRace(SaveData save, RaceReport report)
    {
        bool newLap = false;
        bool newRace = false;
        bool newScore = false;
        bool beatExisting = false;

        switch (report.Mode)
        {
            case RaceMode.Sprint:
                save.SprintRaces++;
                if (report.BestLapTime is { } lap && (save.BestLapTimeSprint is null || lap < save.BestLapTimeSprint))
                {
                    beatExisting |= save.BestLapTimeSprint is not null;
                    save.BestLapTimeSprint = lap;
                    newLap = true;
                }

                if (report.FinishTime is { } raceTime && (save.BestRaceTimeSprint is null || raceTime < save.BestRaceTimeSprint))
                {
                    beatExisting |= save.BestRaceTimeSprint is not null;
                    save.BestRaceTimeSprint = raceTime;
                    newRace = true;
                }

                if (report.Won)
                {
                    save.SprintWins++;
                    if (report.WasEverLast)
                    {
                        save.SprintComebackWins++;
                    }
                }

                if (report.Place == report.EntrantCount)
                {
                    save.SprintLastPlaces++;
                }

                if (report.FinishedInReverse)
                {
                    save.SprintReverseFinishes++;
                }

                break;

            case RaceMode.Elimination:
                save.EliminationRaces++;
                save.EliminationRoundsSurvived += report.RoundsSurvived;
                if (report.Won)
                {
                    save.EliminationWins++;
                    save.EliminationWinStreak++;
                    save.BestEliminationWinStreak = Math.Max(save.BestEliminationWinStreak, save.EliminationWinStreak);
                    Increment(save.EliminationWinsBySkin, report.SkinId);
                    if (!report.WasEverLast)
                    {
                        save.EliminationFlawlessWins++;
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

                break;

            case RaceMode.TimeAttack:
                save.TimeAttackRaces++;
                save.TotalTimeAttackScore += report.Score;
                if (save.BestScoreTimeAttack is null || report.Score > save.BestScoreTimeAttack)
                {
                    // Um 0 na primeira partida vira o "recorde" salvo, mas não conta como recorde conquistado.
                    beatExisting |= save.BestScoreTimeAttack is not null;
                    save.BestScoreTimeAttack = report.Score;
                    newScore = report.Score > 0f;
                }

                if (report.Collisions == 0)
                {
                    save.BestCleanTimeAttackScore = MathF.Max(save.BestCleanTimeAttackScore, report.Score);
                }

                if (report.ClutchCheckpoint)
                {
                    save.TimeAttackClutchCheckpoints++;
                }

                if (report.Score <= 0f)
                {
                    save.TimeAttackZeroScores++;
                }

                break;
        }

        if (report.Won)
        {
            Increment(save.WinsBySkin, report.SkinId);
        }

        var flags = new RecordFlags(newLap, newRace, newScore);
        if (flags.Any)
        {
            save.RecordsSet++;
            Increment(save.RecordsBySkin, report.SkinId);
            if (beatExisting)
            {
                save.RecordsBeaten++;
            }
        }

        save.TotalCollisions += report.Collisions;
        save.MostCollisionsInOneRace = Math.Max(save.MostCollisionsInOneRace, report.Collisions);
        save.LongestStandstillSeconds = MathF.Max(save.LongestStandstillSeconds, report.LongestStandstillSeconds);
        save.TotalBoostSeconds += report.BoostSeconds;
        save.TotalRaceSeconds += report.RaceSeconds;

        return flags;
    }

    /// <summary>
    /// Libera tudo o que o progresso atual já cumpre — conquistas primeiro, depois skins, repetindo até não
    /// mudar mais nada (uma skin pode exigir uma conquista, e uma conquista pode exigir skins). Devolve os
    /// desbloqueios na ordem em que aconteceram; quem chamou salva e avisa o jogador.
    /// </summary>
    public static List<UnlockNotice> CheckUnlocks(SaveData save)
    {
        var notices = new List<UnlockNotice>();

        while (true)
        {
            List<Achievement> achievements = Achievements.UnlockNewlyEarned(save);
            List<CarSkin> skins = SkinUnlocks.UnlockNewlyEarned(save);
            if (achievements.Count == 0 && skins.Count == 0)
            {
                return notices;
            }

            notices.AddRange(achievements.Select(achievement => new UnlockNotice(Achievement: achievement)));
            notices.AddRange(skins.Select(skin => new UnlockNotice(Skin: skin)));
        }
    }

    private static void Increment(Dictionary<string, int> counters, string key)
    {
        if (key is not null)
        {
            counters[key] = counters.GetValueOrDefault(key) + 1;
        }
    }
}
