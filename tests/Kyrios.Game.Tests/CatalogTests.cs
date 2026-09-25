using Kyrios.Core;
using Kyrios.Game;
using Xunit;

namespace Kyrios.Game.Tests;

/// <summary>Integridade dos catálogos (conquistas, skins, pistas) e garantia de que tudo pode ser desbloqueado.</summary>
public class CatalogTests
{
    private static IEnumerable<string> AllTexts() =>
        Achievements.All.SelectMany(a => new[] { a.Name, a.Description, a.Condition.Description })
            .Concat(CarSkins.All.SelectMany(s => new[] { s.Name, s.Requirement.Description }))
            .Concat(TrackThemes.All.SelectMany(t => new[] { t.Name, t.Tagline, t.Requirement.Description, t.SecretName, t.SecretDescription }));

    [Fact]
    public void Ids_AreUnique_AndThereAreLotsOfThings()
    {
        Assert.True(Achievements.All.Count >= 200);
        Assert.True(CarSkins.All.Count >= 70);
        Assert.True(TrackThemes.All.Count >= 25);
        Assert.Equal(Achievements.All.Count, Achievements.All.Select(a => a.Id).Distinct().Count());
        Assert.Equal(CarSkins.All.Count, CarSkins.All.Select(s => s.Id).Distinct().Count());
        Assert.Equal(TrackThemes.All.Count, TrackThemes.All.Select(t => t.Id).Distinct().Count());
    }

    [Fact]
    public void EveryText_UsesOnlyCharactersTheFontCanDraw()
    {
        Assert.All(AllTexts(), text => Assert.All(text, c => Assert.True(PixelFont.Supports(c), $"'{c}' em \"{text}\"")));
    }

    [Fact]
    public void NothingMentionsTheRemovedClassicMode()
    {
        Assert.All(AllTexts(), text => Assert.DoesNotContain("CLASSIC", text));
        Assert.DoesNotContain(CarSkins.All, s => s.Id.Contains("classic"));
    }

    [Fact]
    public void EveryIcon_IsAValidPixelGridOrAnExistingSkin()
    {
        IEnumerable<AchievementIcon> icons = Achievements.All.Select(a => a.Icon).Concat(TrackThemes.All.Select(t => AchievementIcons.Art(t.Icon)));
        foreach (AchievementIcon icon in icons)
        {
            if (icon.SkinId is not null)
            {
                Assert.NotNull(CarSkins.Find(icon.SkinId));
                continue;
            }

            Assert.NotNull(icon.Pixels);
            Assert.All(icon.Pixels, row => Assert.Equal(icon.Pixels[0].Length, row.Length));
            Assert.All(icon.Pixels.SelectMany(row => row), c => Assert.True(c == '.' || AchievementIcons.Palette.ContainsKey(c), $"cor '{c}'"));
        }
    }

    [Fact]
    public void Difficulties_AreMixed_AndThereAreSecretsAndEveryCategory()
    {
        foreach (Difficulty difficulty in Enum.GetValues<Difficulty>())
        {
            Assert.True(Achievements.All.Count(a => a.Difficulty == difficulty) >= 15, difficulty.ToString());
        }

        Assert.True(Achievements.All.Count(a => a.IsSecret) >= 30);
        Assert.True(Achievements.All.Count(a => a.Category == AchievementCategory.Silly) >= 40);
        Assert.All(ProgressionStyle.CategoryOrder, category => Assert.Contains(Achievements.All, a => a.Category == category));

        foreach (Difficulty difficulty in Enum.GetValues<Difficulty>())
        {
            Assert.Contains(CarSkins.All, s => s.Difficulty == difficulty);
            Assert.Contains(TrackThemes.All, t => t.Difficulty == difficulty);
        }
    }

    [Fact]
    public void TheCollection_IsLongTerm_MostSkinsAndTracksAreHard()
    {
        int EasyOrMedium<T>(IEnumerable<T> items, Func<T, Difficulty> difficulty) => items.Count(i => difficulty(i) is Difficulty.Easy or Difficulty.Medium);
        int HardOrMore<T>(IEnumerable<T> items, Func<T, Difficulty> difficulty) => items.Count(i => difficulty(i) is Difficulty.Hard or Difficulty.VeryHard or Difficulty.Rare);

        Assert.True(HardOrMore(CarSkins.All, s => s.Difficulty) > 2 * EasyOrMedium(CarSkins.All, s => s.Difficulty));
        Assert.True(CarSkins.All.Count(s => s.Difficulty == Difficulty.Easy) <= CarSkins.All.Count / 8);
        Assert.True(CarSkins.All.Count(s => s.Difficulty == Difficulty.VeryHard) >= 20);
        Assert.True(HardOrMore(TrackThemes.All, t => t.Difficulty) > EasyOrMedium(TrackThemes.All, t => t.Difficulty));
        Assert.All(Enum.GetValues<SkinCategory>(), category => Assert.True(CarSkins.All.Count(s => s.Category == category) >= 5, category.ToString()));

        // Nada de metas repetitivas: poucas skins pedem só "vença N vezes" ou "jogue N partidas".
        int plainCounts = CarSkins.All.Count(s => s.Requirement.Description.StartsWith("VENCA ") && s.Requirement.Description.EndsWith("CORRIDAS MORTAIS")
            || s.Requirement.Description.StartsWith("JOGUE ") && s.Requirement.Description.EndsWith(" PARTIDAS"));
        Assert.True(plainCounts <= 4, plainCounts.ToString());
    }

    [Fact]
    public void EveryTrack_HasItsSecretAchievement_AndTheSecretSpotIsOnTheTrackSurface()
    {
        Track track = TrackFactory.CreateRingTrack();
        foreach (TrackTheme theme in TrackThemes.All)
        {
            Achievement secret = Achievements.Find(Achievements.TrackSecretId(theme));
            Assert.NotNull(secret);
            Assert.True(secret.IsSecret);
            Assert.False(track.IsWall((int)theme.SecretSpot.X, (int)theme.SecretSpot.Y), theme.Id);
            Assert.NotNull(theme.Scenery.PaintStatic);
            Assert.NotNull(theme.Scenery.PaintSecret);
        }
    }

    [Fact]
    public void RequirementsThatPointToOtherItems_PointToRealOnes()
    {
        Assert.NotNull(Achievements.Find(Achievements.WhereAreTheBrakesId));
        Assert.NotNull(Achievements.Find(Achievements.PredatorId));
        Assert.NotNull(Achievements.Find(Achievements.WasThatSupposedToHappenId));

        // Descrições que citam outros itens usam o nome deles (se o id estivesse errado, apareceria o id cru).
        Assert.All(AllTexts(), text => Assert.DoesNotContain("_", text));
    }

    [Fact]
    public void NewPlayer_StartsWithOnlyTheDefaultCarAndTheFirstTrack()
    {
        var save = new SaveData();

        Assert.Empty(Progression.CheckUnlocks(save));
        Assert.True(SkinUnlocks.IsUnlocked(CarSkins.Default, save));
        Assert.True(TrackThemes.IsUnlocked(TrackThemes.Default, save));
        Assert.All(CarSkins.All.Skip(1), s => Assert.False(SkinUnlocks.IsUnlocked(s, save)));
        Assert.All(TrackThemes.All.Skip(1), t => Assert.False(TrackThemes.IsUnlocked(t, save)));
    }

    [Fact]
    public void EverythingIsReachable_AMaxedOutSaveUnlocksAllAchievementsSkinsAndTracks()
    {
        List<string> skinIds = [.. CarSkins.All.Select(s => s.Id)];
        List<string> trackIds = [.. TrackThemes.All.Select(t => t.Id)];
        var save = new SaveData
        {
            EliminationWins = 1000,
            EliminationRaces = 1000,
            EliminationRoundsSurvived = 5000,
            BestEliminationWinStreak = 50,
            EliminationFlawlessWins = 10,
            EliminationComebackWins = 10,
            EliminationCleanWins = 10,
            EliminationNoBoostWins = 10,
            MostCollisionsInAWin = 30,
            EliminationLazyWins = 1,
            EliminationFirstOuts = 10,
            EliminationRunnerUps = 10,
            EliminatedWhileBoosting = 1,
            BestScoreTimeAttack = 5000,
            TimeAttackRaces = 1000,
            TotalTimeAttackScore = 100000,
            BestCleanTimeAttackScore = 5000,
            TimeAttackClutchCheckpoints = 1,
            TimeAttackZeroScores = 1,
            MostTimeAttackLaps = 30,
            BestCleanCheckpointStreak = 100,
            MostTimeBanked = 60,
            MostTimeLostInOneRun = 30,
            RecordsSet = 50,
            RecordsBeaten = 50,
            BestLapTime = 7f,
            
            TotalCollisions = 5000,
            MostCollisionsInOneRace = 60,
            TotalCheckpoints = 100000,
            LongestStandstillSeconds = 60,
            LongestReverseSeconds = 60,
            TotalBoostSeconds = 100000,
            TotalRaceSeconds = 1000000,
            StartLineCrashes = 10,
            TotalPauses = 100,
            MostPausesInOneRace = 10,
            SilentGames = 1,
            EliminationPodiums = 100,
            EliminationWireToWireWins = 5,
            EliminationClutchWins = 5,
            EliminationPhotoFinishWins = 5,
            EliminationFullThrottleWins = 5,
            EliminationLeftOnlyWins = 5,
            BestSameSkinWinStreak = 10,
            FreshSkinWins = 5,
            BestNoBoostTimeAttackScore = 3000,
            MostClutchCheckpointsInRun = 10,
            ClosestTimeAttackCall = 0.01f,
            TimeAttackHazardHits = 500,
            ReverseCheckpoints = 50,
            LongestWrongWaySeconds = 60,
            NightOwlGames = 20,
            DaysPlayed = 60,
            BestSameSkinGameStreak = 50,
            DeniedEquips = 50,
            SkinChanges = 100,
            Discoveries = [Discovery.Konami, Discovery.Cocorico, Discovery.MenuIdle],
            GamesBySkin = skinIds.ToDictionary(id => id, _ => 60),
            BestLapByTrack = trackIds.ToDictionary(id => id, _ => 7f),
            GamesBySkinOnTrack = skinIds.SelectMany(s => trackIds.Select(t => Progression.SkinOnTrackKey(s, t))).ToDictionary(key => key, _ => 1),
            WinsBySkin = skinIds.ToDictionary(id => id, _ => 20),
            RecordsBySkin = skinIds.ToDictionary(id => id, _ => 5),
            BestScoreBySkin = skinIds.ToDictionary(id => id, _ => 5000f),
            SkinsUsed = skinIds,
            GamesByTrack = trackIds.ToDictionary(id => id, _ => 20),
            WinsByTrack = trackIds.ToDictionary(id => id, _ => 5),
            BestScoreByTrack = trackIds.ToDictionary(id => id, _ => 5000f),
            CleanTracks = trackIds,
            FoundTrackSecrets = trackIds,
            WinsBySkinOnTrack = skinIds.SelectMany(s => trackIds.Select(t => Progression.SkinOnTrackKey(s, t))).ToDictionary(key => key, _ => 1),
        };

        Progression.CheckUnlocks(save);

        Assert.Empty(CarSkins.All.Where(s => !SkinUnlocks.IsUnlocked(s, save)).Select(s => s.Id));
        Assert.Empty(TrackThemes.All.Where(t => !TrackThemes.IsUnlocked(t, save)).Select(t => t.Id));
        Assert.Empty(Achievements.All.Where(a => !Achievements.IsUnlocked(a, save)).Select(a => a.Id));
    }
}

/// <summary>Simula um jogador novo "médio" pra conferir o ritmo da progressão: começo recompensador, mas a coleção
/// não acaba em poucas partidas.</summary>
public class ProgressionPacingTests(Xunit.Abstractions.ITestOutputHelper output)
{
    private void Report(string label, SaveData save)
    {
        (int skins, int totalSkins) = Unlockables.Count(CarSkins.All, save.UnlockedSkinIds);
        (int tracks, int totalTracks) = Unlockables.Count(TrackThemes.All, save.UnlockedTrackIds);
        output.WriteLine($"{label}: skins {skins}/{totalSkins}, pistas {tracks}/{totalTracks}, conquistas {Achievements.UnlockedCount(save)}/{Achievements.All.Count}");
        output.WriteLine("  skins: " + string.Join(", ", save.UnlockedSkinIds));
        output.WriteLine("  pistas: " + string.Join(", ", save.UnlockedTrackIds));
    }

    private static SaveData Play(int games, int seed, float skill)
    {
        var random = new Random(seed);
        var save = new SaveData();
        for (int i = 0; i < games; i++)
        {
            Progression.CheckUnlocks(save);
            List<TrackTheme> tracks = [.. TrackThemes.All.Where(t => TrackThemes.IsUnlocked(t, save))];
            List<CarSkin> skins = [.. CarSkins.All.Where(s => SkinUnlocks.IsUnlocked(s, save))];
            string track = tracks[random.Next(tracks.Count)].Id;
            string skin = random.NextDouble() < 0.6 ? skins[^1].Id : skins[random.Next(skins.Count)].Id;
            if (i % 2 == 0)
            {
                int place = Math.Clamp((int)Math.Round(1 + ((1f - skill) * 9f * random.NextDouble() * 1.6f)), 1, 10);
                Progression.RecordRace(save, new RaceReport
                {
                    Mode = RaceMode.Elimination, SkinId = skin, TrackId = track, Won = place == 1, Place = place, EntrantCount = 10,
                    RoundsSurvived = 10 - place, Collisions = random.Next(1, 9), BoostSeconds = 3f, WasEverLast = random.NextDouble() < 0.3,
                    BestLapTime = 9.2f + ((1f - skill) * 1.5f) + (float)random.NextDouble(), Checkpoints = 3 * (10 - place),
                    BestCleanCheckpointStreak = random.Next(2, 8), RaceSeconds = 30f,
                });
            }
            else
            {
                float score = (300f + (900f * skill)) * (0.6f + (0.6f * (float)random.NextDouble()));
                Progression.RecordRace(save, new RaceReport
                {
                    Mode = RaceMode.TimeAttack, SkinId = skin, TrackId = track, Score = MathF.Round(score / 10f) * 10f, LapsCompleted = (int)(score / 130f),
                    Collisions = random.Next(1, 9), BoostSeconds = 3f, BestLapTime = 9.2f + ((1f - skill) * 1.5f) + (float)random.NextDouble(),
                    Checkpoints = (int)(score / 40f), BestCleanCheckpointStreak = random.Next(2, 9), MaxTimeBanked = 22f, RaceSeconds = 40f,
                });
            }
        }

        Progression.CheckUnlocks(save);
        return save;
    }

    [Fact]
    public void TheFirstGames_AreRewarding()
    {
        SaveData save = Play(10, seed: 1, skill: 0.5f);
        Report("10 partidas", save);
        (int skins, _) = Unlockables.Count(CarSkins.All, save.UnlockedSkinIds);
        (int tracks, _) = Unlockables.Count(TrackThemes.All, save.UnlockedTrackIds);
        Assert.InRange(skins, 3, 12);
        Assert.InRange(tracks, 1, 5);
        Assert.True(Achievements.UnlockedCount(save) >= 8);
    }

    [Fact]
    public void AFewDozenGames_DoNotUnlockTheWholeCollection()
    {
        SaveData save = Play(60, seed: 2, skill: 0.6f);
        Report("60 partidas", save);
        (int skins, int totalSkins) = Unlockables.Count(CarSkins.All, save.UnlockedSkinIds);
        (int tracks, int totalTracks) = Unlockables.Count(TrackThemes.All, save.UnlockedTrackIds);
        Assert.True(skins < totalSkins * 0.45, $"{skins}/{totalSkins} skins");
        Assert.True(tracks < totalTracks * 0.6, $"{tracks}/{totalTracks} pistas");
        Assert.True(Achievements.UnlockedCount(save) < Achievements.All.Count * 0.4);
        Assert.DoesNotContain("mascote", save.UnlockedSkinIds);
        Assert.DoesNotContain("buraco_negro", save.UnlockedSkinIds);
    }

    [Fact]
    public void EvenAVeryGoodPlayer_NeedsManyHoursForTheTrophies()
    {
        SaveData save = Play(200, seed: 3, skill: 0.95f);
        Report("200 partidas (muito bom)", save);
        Assert.DoesNotContain("mascote", save.UnlockedSkinIds);
        Assert.DoesNotContain("trofeu", save.UnlockedSkinIds);
        Assert.DoesNotContain("platina", save.UnlockedAchievementIds);
        Assert.DoesNotContain("pixel", save.UnlockedSkinIds);
    }
}
