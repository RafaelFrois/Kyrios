using Kyrios.Core;
using Kyrios.Game;
using Xunit;

namespace Kyrios.Game.Tests;

public class ProgressionTests
{
    [Fact]
    public void Catalogs_HaveUniqueIdsAndOnlyTextTheFontCanDraw()
    {
        Assert.True(Achievements.All.Count >= 50);
        Assert.Equal(Achievements.All.Count, Achievements.All.Select(a => a.Id).Distinct().Count());

        IEnumerable<string> texts = Achievements.All.SelectMany(a => new[] { a.Name, a.Description, a.Condition.Description })
            .Concat(CarSkins.All.SelectMany(skin => new[] { skin.Name, skin.Requirement.Description }));
        Assert.All(texts, text => Assert.All(text, c => Assert.True(PixelFont.Supports(c), $"'{c}' em \"{text}\"")));
    }

    [Fact]
    public void EveryIcon_IsAValidPixelGridOrAnExistingSkin()
    {
        foreach (Achievement achievement in Achievements.All)
        {
            AchievementIcon icon = achievement.Icon;
            if (icon.SkinId is not null)
            {
                Assert.NotNull(CarSkins.Find(icon.SkinId));
                continue;
            }

            Assert.NotNull(icon.Pixels);
            Assert.All(icon.Pixels, row => Assert.Equal(icon.Pixels[0].Length, row.Length));
            Assert.All(icon.Pixels.SelectMany(row => row), c => Assert.True(c == '.' || AchievementIcons.Palette.ContainsKey(c), $"cor '{c}' em {achievement.Id}"));
        }
    }

    [Fact]
    public void Difficulties_AreBalancedAndSomeAchievementsAreSecret()
    {
        int easy = Achievements.All.Count(a => a.Difficulty == Difficulty.Easy);
        int medium = Achievements.All.Count(a => a.Difficulty == Difficulty.Medium);
        int hard = Achievements.All.Count(a => a.Difficulty == Difficulty.Hard);

        Assert.True(easy >= hard && medium >= hard && hard > 0);
        Assert.Contains(Achievements.All, a => a.IsSecret);
        Assert.All(ProgressionStyle.CategoryOrder, category => Assert.Contains(Achievements.All, a => a.Category == category));
    }

    [Fact]
    public void SkinsThatDependOnAchievements_PointToRealAchievements()
    {
        Assert.NotNull(Achievements.Find(Achievements.WhereAreTheBrakesId));
        Assert.NotNull(Achievements.Find(Achievements.PredatorId));
        Assert.NotNull(Achievements.Find(Achievements.WasThatSupposedToHappenId));
        Assert.All(CarSkins.All, skin => Assert.DoesNotContain(skin.Id, skin.Requirement.Description));
    }

    [Fact]
    public void NewPlayer_HasNothingToUnlock()
    {
        Assert.Empty(Progression.CheckUnlocks(new SaveData()));
    }

    [Fact]
    public void WinningAClassicRace_UpdatesStatsRecordsAndUnlocksTheFirstAchievements()
    {
        var save = new SaveData();
        RecordFlags records = Progression.RecordRace(save, new RaceReport
        {
            Mode = RaceMode.Sprint, SkinId = "classico", Won = true, Place = 1, EntrantCount = 4,
            FinishTime = 31.5f, BestLapTime = 9.8f, RaceSeconds = 31.5f, WasEverLast = true,
        });

        Assert.True(records.NewLapRecord && records.NewRaceRecord);
        Assert.Equal(1, save.SprintRaces);
        Assert.Equal(1, save.SprintWins);
        Assert.Equal(1, save.SprintComebackWins);
        Assert.Equal(1, save.WinsBySkin["classico"]);
        Assert.Equal(1, save.RecordsSet);
        Assert.Equal(0, save.RecordsBeaten);

        List<string> unlocked = [.. Progression.CheckUnlocks(save).Select(n => n.Achievement?.Id ?? n.Skin.Id)];
        Assert.Contains("primeiros_passos", unlocked);
        Assert.Contains("primeira_vitoria", unlocked);
        Assert.Contains("primeira_partida", unlocked);
        Assert.Contains("recordista", unlocked);
        Assert.Contains("de_virada", unlocked);
        Assert.DoesNotContain("velocista", unlocked);

        // Nada é anunciado duas vezes.
        Assert.Empty(Progression.CheckUnlocks(save));
    }

    [Fact]
    public void BeatingAnExistingRecord_CountsAsBeaten()
    {
        var save = new SaveData { BestRaceTimeSprint = 33f, BestLapTimeSprint = 9f };
        Progression.RecordRace(save, new RaceReport { Mode = RaceMode.Sprint, SkinId = "batata", Place = 2, EntrantCount = 4, FinishTime = 32f, BestLapTime = 9.5f });

        Assert.Equal(32f, save.BestRaceTimeSprint);
        Assert.Equal(9f, save.BestLapTimeSprint);
        Assert.Equal(1, save.RecordsBeaten);
        Assert.Equal(1, save.RecordsBySkin["batata"]);
    }

    [Fact]
    public void DeathRace_TracksStreaksFlawlessWinsFirstOutsAndRunnerUps()
    {
        var save = new SaveData();
        RaceReport Win(bool wasLast = false) => new() { Mode = RaceMode.Elimination, SkinId = "jacare", Won = true, Place = 1, EntrantCount = 10, RoundsSurvived = 9, WasEverLast = wasLast };

        Progression.RecordRace(save, Win());
        Progression.RecordRace(save, Win(wasLast: true));
        Assert.Equal(2, save.EliminationWinStreak);
        Assert.Equal(1, save.EliminationFlawlessWins);
        Assert.Equal(18, save.EliminationRoundsSurvived);
        Assert.Equal(2, save.EliminationWinsBySkin["jacare"]);

        Progression.RecordRace(save, new RaceReport { Mode = RaceMode.Elimination, Place = 10, EntrantCount = 10 });
        Progression.RecordRace(save, new RaceReport { Mode = RaceMode.Elimination, Place = 2, EntrantCount = 10, RoundsSurvived = 8 });
        Assert.Equal(0, save.EliminationWinStreak);
        Assert.Equal(2, save.BestEliminationWinStreak);
        Assert.Equal(1, save.EliminationFirstOuts);
        Assert.Equal(1, save.EliminationRunnerUps);

        List<string> unlocked = [.. Progression.CheckUnlocks(save).Select(n => n.Achievement?.Id ?? n.Skin.Id)];
        Assert.Contains("so_pode_sobrar_um", unlocked);
        Assert.Contains("problema_seu", unlocked);
        Assert.Contains("eu_tinha_um_plano", unlocked);
        Assert.Contains("quase", unlocked);
        Assert.DoesNotContain("em_sequencia", unlocked);
    }

    [Fact]
    public void TimeAttack_AccumulatesScoreAndAZeroScoreRevealsTheSecretSkin()
    {
        var save = new SaveData();
        Progression.RecordRace(save, new RaceReport { Mode = RaceMode.TimeAttack, Score = 0f });
        Progression.RecordRace(save, new RaceReport { Mode = RaceMode.TimeAttack, Score = 1200f, ClutchCheckpoint = true, Collisions = 0 });

        Assert.Equal(2, save.TimeAttackRaces);
        Assert.Equal(1200f, save.TotalTimeAttackScore);
        Assert.Equal(1200f, save.BestScoreTimeAttack);
        Assert.Equal(1200f, save.BestCleanTimeAttackScore);

        List<string> unlocked = [.. Progression.CheckUnlocks(save).Select(n => n.Achievement?.Id ?? n.Skin.Id)];
        Assert.Contains(Achievements.WasThatSupposedToHappenId, unlocked);
        Assert.Contains("ursinho", unlocked);
        Assert.Contains("eu_tenho_tempo", unlocked);
        Assert.Contains("cada_segundo_conta", unlocked);
        Assert.Contains("intocavel", unlocked);

        // A conquista vem antes da skin que ela libera, e a skin nova já conta pra "NOVO VISUAL".
        Assert.True(unlocked.IndexOf(Achievements.WasThatSupposedToHappenId) < unlocked.IndexOf("ursinho"));
        Assert.True(unlocked.IndexOf("ursinho") < unlocked.IndexOf("novo_visual"));
    }

    [Fact]
    public void CrashingTenTimes_UnlocksTheBrakesAchievementAndTheBrickSkin()
    {
        var save = new SaveData();
        Progression.RecordRace(save, new RaceReport { Mode = RaceMode.Sprint, Place = 4, EntrantCount = 4, Collisions = 10 });

        List<string> unlocked = [.. Progression.CheckUnlocks(save).Select(n => n.Achievement?.Id ?? n.Skin.Id)];
        Assert.Contains(Achievements.WhereAreTheBrakesId, unlocked);
        Assert.Contains("tijolo", unlocked);
        Assert.Contains("lanterninha", unlocked);
    }

    [Fact]
    public void CombinedSkinRequirement_NeedsEveryPart()
    {
        CarSkin pizza = CarSkins.Find("pizza");
        Assert.False(pizza.Requirement.IsMet(new SaveData { SprintWins = 5, EliminationWins = 4 }));
        Assert.True(pizza.Requirement.IsMet(new SaveData { SprintWins = 5, EliminationWins = 5 }));
        Assert.Equal("PROGRESSO: 4/5", pizza.Requirement.ProgressText(new SaveData { SprintWins = 5, EliminationWins = 4 }));
    }

    [Fact]
    public void Platinum_UnlocksWhenEveryOtherAchievementIsDone()
    {
        var save = new SaveData { UnlockedAchievementIds = [.. Achievements.All.Where(a => a.Id != "platina").Select(a => a.Id)] };
        Assert.Contains(Progression.CheckUnlocks(save), n => n.Achievement?.Id == "platina");
    }

    [Fact]
    public void OldSaves_GetTheirMissingCountersFromExistingStats()
    {
        var save = new SaveData { SprintWins = 4, BestRaceTimeSprint = 30f, BestScoreTimeAttack = 900f, WinsBySkin = null };
        save.FillStatsMissingFromOldSaves();

        Assert.Equal(4, save.SprintRaces);
        Assert.Equal(1, save.TimeAttackRaces);
        Assert.Equal(900f, save.TotalTimeAttackScore);
        Assert.Equal(2, save.RecordsSet);
        Assert.NotNull(save.WinsBySkin);
    }

    [Fact]
    public void Tracker_CountsCollisionsAndStandingStill()
    {
        RaceSimulation race = RaceFactory.CreateDefaultRace(aiOpponents: 3, targetLaps: 3, mode: RaceMode.Sprint);
        RaceEntrant player = race.Entrants.First(e => e.Kind == DriverKind.Human);
        var tracker = new RaceTracker();
        tracker.Reset(race);

        for (int i = 0; i < 12 * 60; i++)
        {
            race.Update(1f / 60f, CarInput.None);
            tracker.Observe(race, player, 1f / 60f);
        }

        RaceReport report = tracker.BuildReport(race, player, "pato");
        Assert.True(report.LongestStandstillSeconds >= 10f);
        Assert.Equal("pato", report.SkinId);
        Assert.Equal(RaceMode.Sprint, report.Mode);
        Assert.False(report.Won);
    }
}
