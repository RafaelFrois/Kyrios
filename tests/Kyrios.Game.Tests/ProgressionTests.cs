using Kyrios.Core;
using Kyrios.Game;
using Xunit;

namespace Kyrios.Game.Tests;

public class ProgressionTests
{
    private static List<string> Unlocked(SaveData save) =>
        [.. Progression.CheckUnlocks(save).Select(n => n.Achievement?.Id ?? n.Skin?.Id ?? n.Track.Id)];

    [Fact]
    public void WinningADeathRace_UpdatesStatsPerSkinAndTrack_AndUnlocksTheFirstThings()
    {
        var save = new SaveData();
        Progression.RecordRace(save, new RaceReport
        {
            Mode = RaceMode.Elimination, SkinId = "padrao", TrackId = "autodromo", Won = true, Place = 1, EntrantCount = 4,
            RoundsSurvived = 3, Collisions = 0, BestLapTime = 9.4f, RaceSeconds = 40f, Checkpoints = 12, BestCleanCheckpointStreak = 12,
        });

        Assert.Equal(1, save.EliminationRaces);
        Assert.Equal(1, save.EliminationWins);
        Assert.Equal(1, save.EliminationWinStreak);
        Assert.Equal(1, save.EliminationCleanWins);
        Assert.Equal(1, save.EliminationNoBoostWins);
        Assert.Equal(1, save.EliminationFlawlessWins);
        Assert.Equal(1, save.WinsBySkin["padrao"]);
        Assert.Equal(1, save.WinsByTrack["autodromo"]);
        Assert.Equal(1, save.WinsBySkinOnTrack["padrao@autodromo"]);
        Assert.Contains("autodromo", save.CleanTracks);
        Assert.Equal(9.4f, save.BestLapTime);

        List<string> unlocked = Unlocked(save);
        Assert.Contains("primeira_partida", unlocked);
        Assert.Contains("sobrevivente", unlocked);
        Assert.Contains("ultimo_de_pe", unlocked);
        Assert.Contains("lataria_intacta", unlocked);
        Assert.Contains("motor_original", unlocked);
        Assert.Contains("galinha", unlocked);
        Assert.Contains("sapo", unlocked);
        Assert.Contains("floresta", unlocked);

        // Nada é anunciado duas vezes.
        Assert.Empty(Progression.CheckUnlocks(save));
    }

    [Fact]
    public void DeathRace_TracksStreaksComebacksFirstOutsAndRunnerUps()
    {
        var save = new SaveData();
        RaceReport Win(bool wasLast = false) => new() { Mode = RaceMode.Elimination, SkinId = "jacare", TrackId = "praia", Won = true, Place = 1, EntrantCount = 4, RoundsSurvived = 3, WasEverLast = wasLast, Collisions = 11, BoostSeconds = 2f };

        Progression.RecordRace(save, Win());
        Progression.RecordRace(save, Win(wasLast: true));
        Assert.Equal(2, save.EliminationWinStreak);
        Assert.Equal(1, save.EliminationComebackWins);
        Assert.Equal(11, save.MostCollisionsInAWin);
        Assert.Equal(6, save.EliminationRoundsSurvived);

        Progression.RecordRace(save, new RaceReport { Mode = RaceMode.Elimination, TrackId = "praia", Place = 4, EntrantCount = 4, EliminatedWhileBoosting = true });
        Progression.RecordRace(save, new RaceReport { Mode = RaceMode.Elimination, TrackId = "praia", Place = 2, EntrantCount = 4, RoundsSurvived = 2 });
        Assert.Equal(0, save.EliminationWinStreak);
        Assert.Equal(2, save.BestEliminationWinStreak);
        Assert.Equal(1, save.EliminationFirstOuts);
        Assert.Equal(1, save.EliminationRunnerUps);
        Assert.Equal(1, save.EliminatedWhileBoosting);

        List<string> unlocked = Unlocked(save);
        Assert.Contains("de_virada", unlocked);
        Assert.Contains("problema_seu", unlocked);
        Assert.Contains("o_carro_esta_bem", unlocked);
        Assert.Contains("eu_tinha_um_plano", unlocked);
        Assert.Contains("quase", unlocked);
        Assert.Contains("turbo_pra_lugar_nenhum", unlocked);
        Assert.Contains("tubarao", unlocked);
        Assert.DoesNotContain("em_sequencia", unlocked);
    }

    [Fact]
    public void TimeAttack_AccumulatesScore_AndAZeroScoreRevealsTheSecretSkin()
    {
        var save = new SaveData();
        Progression.RecordRace(save, new RaceReport { Mode = RaceMode.TimeAttack, TrackId = "autodromo", SkinId = "padrao", Score = 0f });
        RecordFlags records = Progression.RecordRace(save, new RaceReport
        {
            Mode = RaceMode.TimeAttack, TrackId = "praia", SkinId = "batata", Score = 1200f, ClutchCheckpoint = true,
            Collisions = 0, LapsCompleted = 8, MaxTimeBanked = 31f, TimeLostToCrashes = 0f,
        });

        Assert.True(records.NewScoreRecord);
        Assert.Equal(2, save.TimeAttackRaces);
        Assert.Equal(1200f, save.TotalTimeAttackScore);
        Assert.Equal(1200f, save.BestScoreTimeAttack);
        Assert.Equal(1200f, save.BestCleanTimeAttackScore);
        Assert.Equal(1200f, save.BestScoreByTrack["praia"]);
        Assert.Equal(1200f, save.BestScoreBySkin["batata"]);
        Assert.Equal(1, save.RecordsBySkin["batata"]);
        Assert.Equal(1, save.RecordsSet);
        Assert.Equal(0, save.RecordsBeaten);

        List<string> unlocked = Unlocked(save);
        Assert.Contains(Achievements.WasThatSupposedToHappenId, unlocked);
        Assert.Contains("ursinho", unlocked);
        Assert.Contains("eu_tenho_tempo", unlocked);
        Assert.Contains("cada_segundo_conta", unlocked);
        Assert.Contains("intocavel", unlocked);
        Assert.Contains("surfista", unlocked);
        Assert.Contains("peixe", unlocked);
        Assert.Contains("batata_veloz", unlocked);
        Assert.Contains("tempo_de_sobra", unlocked);

        // A conquista vem antes da skin que ela libera, e a skin nova já conta pra "NOVO VISUAL".
        Assert.True(unlocked.IndexOf(Achievements.WasThatSupposedToHappenId) < unlocked.IndexOf("ursinho"));
        Assert.True(unlocked.IndexOf("ursinho") < unlocked.IndexOf("novo_visual"));
    }

    [Fact]
    public void BeatingAnExistingScore_CountsAsBeaten_ButAZeroIsNeverARecord()
    {
        var save = new SaveData();
        Assert.False(Progression.RecordRace(save, new RaceReport { Mode = RaceMode.TimeAttack, Score = 0f }).NewScoreRecord);
        Progression.RecordRace(save, new RaceReport { Mode = RaceMode.TimeAttack, Score = 300f });
        Progression.RecordRace(save, new RaceReport { Mode = RaceMode.TimeAttack, Score = 500f });

        Assert.Equal(2, save.RecordsSet);
        Assert.Equal(1, save.RecordsBeaten);
    }

    [Fact]
    public void Tracks_UnlockThroughVariedRequirements()
    {
        var save = new SaveData();
        Progression.RecordRace(save, new RaceReport { Mode = RaceMode.TimeAttack, TrackId = "autodromo", Score = 100f });
        Assert.DoesNotContain("praia", Unlocked(save));

        Progression.RecordRace(save, new RaceReport { Mode = RaceMode.TimeAttack, TrackId = "autodromo", Score = 100f });
        Assert.Contains("praia", Unlocked(save));

        save.FoundTrackSecrets = ["autodromo", "praia", "floresta"];
        Assert.Contains("quarto", Unlocked(save));
        Assert.Contains("segredo_praia", save.UnlockedAchievementIds);
    }

    [Fact]
    public void CombinedRequirement_NeedsEveryPart_AndShowsTheMissingOne()
    {
        CarSkin pizza = CarSkins.Find("pizza");
        Assert.False(pizza.Requirement.IsMet(new SaveData { EliminationWins = 5, TotalTimeAttackScore = 2000 }));
        Assert.True(pizza.Requirement.IsMet(new SaveData { EliminationWins = 5, TotalTimeAttackScore = 3000 }));
        Assert.Equal("2000/3000", pizza.Requirement.ProgressText(new SaveData { EliminationWins = 5, TotalTimeAttackScore = 2000 }));
    }

    [Fact]
    public void Platinum_UnlocksWhenEveryOtherAchievementIsDone()
    {
        var save = new SaveData { UnlockedAchievementIds = [.. Achievements.All.Where(a => a.Id != "platina").Select(a => a.Id)] };
        Assert.Contains(Progression.CheckUnlocks(save), n => n.Achievement?.Id == "platina");
    }

    [Fact]
    public void AnUnlockedItemStaysUnlocked_EvenIfNoLongerMet()
    {
        var save = new SaveData { UnlockedSkinIds = ["jacare"] };
        Assert.True(SkinUnlocks.IsUnlocked(CarSkins.Find("jacare"), save));
        Assert.DoesNotContain(Progression.CheckUnlocks(save), n => n.Skin?.Id == "jacare");
    }

    [Fact]
    public void OldSave_WithClassicModeData_LoadsWithoutErrors_AndLosesOnlyTheRemovedStuff()
    {
        const string oldJson = """
            {"BestLapTimeSprint":9.2,"BestRaceTimeSprint":30.5,"SprintWins":4,"SprintRaces":8,"SprintComebackWins":1,
             "EliminationWins":2,"EliminationRaces":6,"BestScoreTimeAttack":1100,"EliminationWinsBySkin":{"pato":1},
             "WinsBySkin":{"pato":3},"SelectedSkinId":"classico","UnlockedSkinIds":["pato","galinha"],
             "UnlockedAchievementIds":["primeiros_passos","primeira_vitoria","ultimo_de_pe"],"MusicVolume":0.3,"SfxMuted":true}
            """;

        SaveData save = SaveData.FromJson(oldJson);
        Assert.NotNull(save);
        Progression.Normalize(save);

        Assert.Equal(2, save.EliminationWins);
        Assert.Equal(1100f, save.BestScoreTimeAttack);
        Assert.Equal(1, save.TimeAttackRaces);
        Assert.Equal(0.3f, save.MusicVolume);
        Assert.True(save.SfxMuted);
        Assert.Equal(["pato", "galinha"], save.UnlockedSkinIds);
        Assert.Equal(["ultimo_de_pe"], save.UnlockedAchievementIds);
        Assert.Equal(0, CarSkins.IndexOf(save.SelectedSkinId));
        Assert.NotNull(save.GamesByTrack);
    }

    [Fact]
    public void Save_RoundTripsThroughJson()
    {
        var save = new SaveData { EliminationWins = 3, SelectedTrackId = "praia", LastMode = RaceMode.TimeAttack };
        save.WinsBySkinOnTrack["pato@praia"] = 2;
        save.FoundTrackSecrets.Add("neve");

        SaveData loaded = SaveData.FromJson(save.ToJson());
        Assert.Equal(3, loaded.EliminationWins);
        Assert.Equal("praia", loaded.SelectedTrackId);
        Assert.Equal(RaceMode.TimeAttack, loaded.LastMode);
        Assert.Equal(2, loaded.WinsBySkinOnTrack["pato@praia"]);
        Assert.Equal(["neve"], loaded.FoundTrackSecrets);
        Assert.Null(SaveData.FromJson("{ isso nao e json"));
    }

    [Fact]
    public void Tracker_CountsStandingStill_InATimeAttackRun()
    {
        RaceSimulation race = RaceFactory.CreateDefaultRace(RaceMode.TimeAttack, aiOpponents: 2);
        RaceEntrant player = race.Entrants.First(e => e.Kind == DriverKind.Human);
        var tracker = new RaceTracker();
        tracker.Reset(race);

        for (int i = 0; i < 12 * 60 && !race.IsRaceOver; i++)
        {
            race.Update(1f / 60f, CarInput.None);
            tracker.Observe(race, player, 1f / 60f);
        }

        RaceReport report = tracker.BuildReport(race, player, "pato", "neve", silent: true);
        Assert.True(report.LongestStandstillSeconds >= 10f);
        Assert.Equal("pato", report.SkinId);
        Assert.Equal("neve", report.TrackId);
        Assert.True(report.Silent);
        Assert.False(report.Won);
    }

    [Fact]
    public void Tracker_FindsTheTrackSecret_WhenThePlayerDrivesOverIt()
    {
        Track track = TrackFactory.CreateRingTrack();
        TrackTheme beach = TrackThemes.Find("praia");
        var car = new Car("Voce", beach.SecretSpot, 0f);
        var race = new RaceSimulation(track, [new RaceEntrant(car, DriverKind.Human)], RaceMode.TimeAttack);
        var tracker = new RaceTracker();
        tracker.Reset(race, beach.SecretSpot);

        race.Update(0.016f, CarInput.None);
        tracker.Observe(race, race.Entrants[0], 0.016f);

        Assert.True(tracker.SecretFoundThisTick);
        Assert.True(tracker.BuildReport(race, race.Entrants[0], "padrao", "praia", silent: false).FoundSecret);
    }
}
