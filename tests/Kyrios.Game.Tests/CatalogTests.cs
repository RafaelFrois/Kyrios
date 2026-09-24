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
        Assert.True(Achievements.All.Count >= 100);
        Assert.True(CarSkins.All.Count >= 20);
        Assert.True(TrackThemes.All.Count >= 10);
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
        int easy = Achievements.All.Count(a => a.Difficulty == Difficulty.Easy);
        int medium = Achievements.All.Count(a => a.Difficulty == Difficulty.Medium);
        int hard = Achievements.All.Count(a => a.Difficulty == Difficulty.Hard);
        int rare = Achievements.All.Count(a => a.Difficulty == Difficulty.Rare);

        Assert.True(easy >= rare && medium >= hard && hard > 0 && rare > 0, $"{easy}/{medium}/{hard}/{rare}");
        Assert.True(Achievements.All.Count(a => a.IsSecret) >= 15);
        Assert.All(ProgressionStyle.CategoryOrder, category => Assert.Contains(Achievements.All, a => a.Category == category));

        foreach (Difficulty difficulty in Enum.GetValues<Difficulty>())
        {
            Assert.Contains(CarSkins.All, s => s.Difficulty == difficulty);
            Assert.Contains(TrackThemes.All, t => t.Difficulty == difficulty);
        }
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
            MostCollisionsInAWin = 20,
            EliminationLazyWins = 1,
            EliminationFirstOuts = 1,
            EliminationRunnerUps = 1,
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
            MostCollisionsInOneRace = 50,
            TotalCheckpoints = 100000,
            LongestStandstillSeconds = 60,
            LongestReverseSeconds = 60,
            TotalBoostSeconds = 100000,
            TotalRaceSeconds = 1000000,
            StartLineCrashes = 1,
            TotalPauses = 100,
            MostPausesInOneRace = 10,
            SilentGames = 1,
            WinsBySkin = skinIds.ToDictionary(id => id, _ => 5),
            RecordsBySkin = skinIds.ToDictionary(id => id, _ => 5),
            BestScoreBySkin = skinIds.ToDictionary(id => id, _ => 5000f),
            SkinsUsed = skinIds,
            GamesByTrack = trackIds.ToDictionary(id => id, _ => 5),
            WinsByTrack = trackIds.ToDictionary(id => id, _ => 5),
            BestScoreByTrack = trackIds.ToDictionary(id => id, _ => 5000f),
            CleanTracks = trackIds,
            FoundTrackSecrets = trackIds,
            WinsBySkinOnTrack = skinIds.SelectMany(s => trackIds.Select(t => Progression.SkinOnTrackKey(s, t))).ToDictionary(key => key, _ => 1),
        };

        Progression.CheckUnlocks(save);

        Assert.Equal(Achievements.All.Count, Achievements.UnlockedCount(save));
        Assert.All(CarSkins.All, s => Assert.True(SkinUnlocks.IsUnlocked(s, save), s.Id));
        Assert.All(TrackThemes.All, t => Assert.True(TrackThemes.IsUnlocked(t, save), t.Id));
    }
}
