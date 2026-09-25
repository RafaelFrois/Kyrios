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
            Mode = RaceMode.Elimination, SkinId = "padrao", TrackId = "autodromo", Won = true, Place = 1, EntrantCount = 10,
            RoundsSurvived = 9, Collisions = 0, BestLapTime = 9.4f, RaceSeconds = 40f, Checkpoints = 12, BestCleanCheckpointStreak = 12,
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
        Assert.Contains("no_podio", unlocked);
        Assert.Contains("estreia_vitoriosa", unlocked);
        Assert.Contains("galinha", unlocked);
        Assert.Contains("sapo", unlocked);
        Assert.Contains("tartaruga", unlocked);
        Assert.Contains("floresta", unlocked);
        Assert.DoesNotContain("de_ponta_a_ponta", unlocked);

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
        Assert.Contains("maquina_lavar", unlocked);
        Assert.Contains("estreia_vitoriosa", unlocked);
        Assert.DoesNotContain("tubarao", unlocked);
        Assert.DoesNotContain("em_sequencia", unlocked);
    }

    [Fact]
    public void TimeAttack_AccumulatesScore_AndAZeroScoreRevealsTheSecretSkin()
    {
        var save = new SaveData();
        Progression.RecordRace(save, new RaceReport { Mode = RaceMode.TimeAttack, TrackId = "autodromo", SkinId = "padrao", Score = 0f });
        RecordFlags records = Progression.RecordRace(save, new RaceReport
        {
            Mode = RaceMode.TimeAttack, TrackId = "praia", SkinId = "batata", Score = 1200f, ClutchCheckpoints = 1,
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
        Assert.False(report.FullThrottle);
        Assert.True(report.NeverSteeredRight);
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

/// <summary>As estatísticas novas (habilidade, sequências, descobertas) e como elas liberam conteúdo.</summary>
public class NewStatsTests
{
    private static List<string> Unlocked(SaveData save) =>
        [.. Progression.CheckUnlocks(save).Select(n => n.Achievement?.Id ?? n.Skin?.Id ?? n.Track.Id)];

    private static RaceReport Win(string skin, string track = "autodromo") => new()
    {
        Mode = RaceMode.Elimination, SkinId = skin, TrackId = track, Won = true, Place = 1, EntrantCount = 10, RoundsSurvived = 9, Collisions = 2, BoostSeconds = 1f,
    };

    [Fact]
    public void SameSkinStreaks_ResetWhenTheSkinChangesOrTheRaceIsLost()
    {
        var save = new SaveData();
        Progression.RecordRace(save, Win("pato"));
        Progression.RecordRace(save, Win("pato"));
        Assert.Equal(2, save.SameSkinWinStreak);
        Assert.Equal(1, save.FreshSkinWins);

        Progression.RecordRace(save, Win("galinha"));
        Assert.Equal(1, save.SameSkinWinStreak);
        Assert.Equal(2, save.FreshSkinWins);
        Assert.Equal(2, save.BestSameSkinWinStreak);

        Progression.RecordRace(save, Win("galinha"));
        Progression.RecordRace(save, Win("galinha"));
        Assert.Equal(3, save.BestSameSkinWinStreak);
        Assert.Contains("time_que_ganha", Unlocked(save));

        Progression.RecordRace(save, new RaceReport { Mode = RaceMode.Elimination, SkinId = "galinha", Place = 5, EntrantCount = 10 });
        Assert.Equal(0, save.SameSkinWinStreak);
        Assert.Equal(4, save.SameSkinGameStreak);
        Assert.Equal(4, save.GamesBySkin["galinha"]);
    }

    [Fact]
    public void SkillFlags_AreCountedOnlyInWins()
    {
        var save = new SaveData();
        Progression.RecordRace(save, new RaceReport
        {
            Mode = RaceMode.Elimination, SkinId = "padrao", Won = true, Place = 1, EntrantCount = 10,
            LedWholeRace = true, WasLastInFinalThree = true, PhotoFinish = true, FullThrottle = true, NeverSteeredRight = true,
        });
        Progression.RecordRace(save, new RaceReport { Mode = RaceMode.Elimination, SkinId = "padrao", Place = 3, EntrantCount = 10, FullThrottle = true, NeverSteeredRight = true });

        Assert.Equal(1, save.EliminationWireToWireWins);
        Assert.Equal(1, save.EliminationClutchWins);
        Assert.Equal(1, save.EliminationPhotoFinishWins);
        Assert.Equal(1, save.EliminationFullThrottleWins);
        Assert.Equal(1, save.EliminationLeftOnlyWins);
        Assert.Equal(2, save.EliminationPodiums);

        List<string> unlocked = Unlocked(save);
        Assert.Contains("de_ponta_a_ponta", unlocked);
        Assert.Contains("onibus", unlocked);
        Assert.Contains("tanque", unlocked);
        Assert.Contains("no_limite", unlocked);
        Assert.Contains("foto_de_chegada", unlocked);
        Assert.Contains("pe_na_tabua", unlocked);
        Assert.Contains("so_viro_pra_esquerda", unlocked);
    }

    [Fact]
    public void TimeAttackDetails_ClosestCallNoBoostAndHazards()
    {
        var save = new SaveData();
        Progression.RecordRace(save, new RaceReport { Mode = RaceMode.TimeAttack, Score = 900f, ClutchCheckpoints = 3, ClosestCall = 0.15f, HazardHits = 21, BoostSeconds = 0f });
        Progression.RecordRace(save, new RaceReport { Mode = RaceMode.TimeAttack, Score = 400f, ClosestCall = 0.6f, HazardHits = 30, BoostSeconds = 3f });

        Assert.Equal(900f, save.BestNoBoostTimeAttackScore);
        Assert.Equal(3, save.MostClutchCheckpointsInRun);
        Assert.Equal(0.15f, save.ClosestTimeAttackCall);
        Assert.Equal(51, save.TimeAttackHazardHits);

        List<string> unlocked = Unlocked(save);
        Assert.Contains("economico", unlocked);
        Assert.Contains("coracao_na_mao", unlocked);
        Assert.Contains("por_um_triz", unlocked);
        Assert.Contains("ima_de_obstaculos", unlocked);
        Assert.Contains("obstaculo", unlocked);
    }

    [Fact]
    public void DaysAndNightOwlGames_UseTheLocalTimeOfEachGame()
    {
        var save = new SaveData();
        RaceReport At(int day, int hour) => new() { Mode = RaceMode.TimeAttack, Score = 10f, PlayedAt = new DateTime(2026, 3, day, hour, 30, 0) };

        Progression.RecordRace(save, At(1, 2));
        Progression.RecordRace(save, At(1, 14));
        Progression.RecordRace(save, At(2, 3));
        Progression.RecordRace(save, At(4, 4));
        Progression.RecordRace(save, new RaceReport { Mode = RaceMode.TimeAttack, Score = 10f });

        Assert.Equal(3, save.DaysPlayed);
        Assert.Equal(3, save.NightOwlGames);
        List<string> unlocked = Unlocked(save);
        Assert.Contains("dia_apos_dia", unlocked);
        Assert.Contains("coruja", unlocked);
        Assert.Contains("fantasma", unlocked);
    }

    [Fact]
    public void PerTrackLapsAndSkinTrackGames_FeedTheirRequirements()
    {
        var save = new SaveData();
        Progression.RecordRace(save, new RaceReport { Mode = RaceMode.TimeAttack, SkinId = "cubo_gelo", TrackId = "vulcao", Score = 100f, BestLapTime = 9.6f });
        Progression.RecordRace(save, new RaceReport { Mode = RaceMode.TimeAttack, SkinId = "padrao", TrackId = "vulcao", Score = 100f, BestLapTime = 9.1f });
        Progression.RecordRace(save, new RaceReport { Mode = RaceMode.TimeAttack, SkinId = "padrao", TrackId = "vulcao", Score = 100f, BestLapTime = 9.9f });

        Assert.Equal(9.1f, save.BestLapByTrack["vulcao"]);
        Assert.Equal(1, save.GamesBySkinOnTrack["cubo_gelo@vulcao"]);
        Assert.Contains("nao_era_pra_isso", Unlocked(save));
    }

    [Fact]
    public void MenuDiscoveries_UnlockTheirSecrets()
    {
        var save = new SaveData();
        Assert.True(save.AddDiscovery(Discovery.Konami));
        Assert.False(save.AddDiscovery(Discovery.Konami));

        List<string> unlocked = Unlocked(save);
        Assert.Contains("o_codigo", unlocked);
        Assert.Contains("pixel", unlocked);
        Assert.Contains("retro", unlocked);
    }

    [Fact]
    public void CategoryRequirements_CountDistinctSkinsOfTheCategory()
    {
        var save = new SaveData();
        foreach (string animal in new[] { "pato", "galinha", "jacare" })
        {
            Progression.RecordRace(save, Win(animal));
        }

        Assert.Contains("polvo", Unlocked(save));
        Assert.True(Unlock.CategoriesWon(1).IsMet(save));
        Assert.False(Unlock.CategoriesWon(2).IsMet(save));
    }

    [Fact]
    public void UnlockingAnAchievementAndTheSkinItGives_HappensInOneCheck_InOrder()
    {
        var save = new SaveData { MostCollisionsInOneRace = 12 };
        List<UnlockNotice> notices = Progression.CheckUnlocks(save);

        int achievement = notices.FindIndex(n => n.Achievement?.Id == Achievements.WhereAreTheBrakesId);
        int skin = notices.FindIndex(n => n.Skin?.Id == "tijolo");
        Assert.True(achievement >= 0 && skin > achievement);
        Assert.Contains("tijolo", save.UnlockedSkinIds);
    }
}

/// <summary>O que fica gravado em disco: fechar e abrir o jogo não pode perder nada.</summary>
public class PersistenceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "megrace-tests-" + Guid.NewGuid().ToString("N"));

    public PersistenceTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void NewGame_UnlockThings_CloseAndReopen_EverythingIsStillThere()
    {
        string path = Path.Combine(_dir, "records.json");
        SaveData save = SaveData.LoadFrom(path);
        Assert.Equal(0, save.GamesPlayed);

        Progression.RecordRace(save, new RaceReport { Mode = RaceMode.Elimination, SkinId = "padrao", TrackId = "autodromo", Won = true, Place = 1, EntrantCount = 10, RoundsSurvived = 9, BestLapTime = 9.3f, FoundSecret = true });
        Progression.RecordRace(save, new RaceReport { Mode = RaceMode.TimeAttack, SkinId = "padrao", TrackId = "autodromo", Score = 1100f, BestLapTime = 9.1f });
        Progression.RecordRace(save, new RaceReport { Mode = RaceMode.TimeAttack, SkinId = "padrao", TrackId = "autodromo", Score = 200f });
        save.AddDiscovery(Discovery.Cocorico);
        Progression.CheckUnlocks(save);
        save.SelectedSkinId = "galinha";
        save.SelectedTrackId = "praia";
        save.LastMode = RaceMode.TimeAttack;
        save.SaveTo(path);

        SaveData reopened = SaveData.LoadFrom(path);
        Progression.Normalize(reopened);
        Assert.Equal(save.UnlockedSkinIds, reopened.UnlockedSkinIds);
        Assert.Equal(save.UnlockedTrackIds, reopened.UnlockedTrackIds);
        Assert.Equal(save.UnlockedAchievementIds, reopened.UnlockedAchievementIds);
        Assert.Contains("galinha", reopened.UnlockedSkinIds);
        Assert.Contains("praia", reopened.UnlockedTrackIds);
        Assert.Contains("cocorico", reopened.UnlockedAchievementIds);
        Assert.Equal("galinha", reopened.SelectedSkinId);
        Assert.Equal("praia", reopened.SelectedTrackId);
        Assert.Equal(RaceMode.TimeAttack, reopened.LastMode);
        Assert.Equal(1100f, reopened.BestScoreTimeAttack);
        Assert.Equal(9.1f, reopened.BestLapTime);
        Assert.Equal(9.1f, reopened.BestLapByTrack["autodromo"]);
        Assert.Equal(3, reopened.GamesPlayed);
        Assert.Equal(["autodromo"], reopened.FoundTrackSecrets);
        Assert.Empty(Progression.CheckUnlocks(reopened));
    }

    [Fact]
    public void CorruptedSave_FallsBackToThePreviousCopy()
    {
        string path = Path.Combine(_dir, "records.json");
        new SaveData { EliminationWins = 1 }.SaveTo(path);
        new SaveData { EliminationWins = 2 }.SaveTo(path);
        File.WriteAllText(path, "{ corrompido");

        Assert.Equal(1, SaveData.LoadFrom(path).EliminationWins);
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void MissingSave_StartsFresh()
    {
        SaveData save = SaveData.LoadFrom(Path.Combine(_dir, "nao-existe.json"));
        Assert.Equal(0, save.GamesPlayed);
        Assert.Empty(save.UnlockedSkinIds);
    }

    [Fact]
    public void Fullscreen_IsTheDefault_EvenForOldSavesThatStoredItAsOff()
    {
        Assert.False(new SaveData().Windowed);
        Assert.False(SaveData.FromJson("""{"Fullscreen":false,"GamesPlayed":3}""").Windowed);
        Assert.True(SaveData.FromJson(new SaveData { Windowed = true }.ToJson()).Windowed);
    }

    [Fact]
    public void InvalidLastMode_FromAHandEditedSave_FallsBackToTheDeathRace()
    {
        SaveData save = SaveData.FromJson("""{"LastMode":7}""");
        Assert.Equal(RaceMode.Elimination, save.LastMode);
    }
}
