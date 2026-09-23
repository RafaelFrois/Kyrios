using Kyrios.Game;
using Xunit;

namespace Kyrios.Game.Tests;

public class SkinUnlockTests
{
    [Fact]
    public void NewPlayer_OnlyTheClassicSkinIsUnlocked()
    {
        var progress = new SaveData();

        Assert.True(SkinUnlocks.IsUnlocked(CarSkins.Classic, progress));
        Assert.All(CarSkins.All.Skip(1), skin => Assert.False(SkinUnlocks.IsUnlocked(skin, progress)));
        Assert.Empty(SkinUnlocks.UnlockNewlyEarned(progress));
    }

    [Fact]
    public void ClassicIsTheFallbackForAnUnknownOrMissingSkinId()
    {
        Assert.Equal(0, CarSkins.IndexOf(null));
        Assert.Equal(0, CarSkins.IndexOf("skin-que-nao-existe"));
    }

    [Fact]
    public void SkinIdsAreUniqueAndEverySkinHasARequirement()
    {
        Assert.Equal(CarSkins.All.Count, CarSkins.All.Select(skin => skin.Id).Distinct().Count());
        Assert.All(CarSkins.All, skin => Assert.NotNull(skin.Requirement));
        Assert.All(CarSkins.All, skin => Assert.False(string.IsNullOrWhiteSpace(skin.Requirement.Description)));
    }

    [Fact]
    public void ReachingAWinTarget_UnlocksTheSkinOnce()
    {
        var catalog = new[] { Skin("a", Unlock.DeathRaceWins(5)) };
        var progress = new SaveData { EliminationWins = 4 };

        Assert.Empty(SkinUnlocks.UnlockNewlyEarned(progress, catalog));

        progress.EliminationWins = 5;
        Assert.Equal(["a"], SkinUnlocks.UnlockNewlyEarned(progress, catalog).Select(skin => skin.Id));
        Assert.Contains("a", progress.UnlockedSkinIds);

        // Já liberada: não é anunciada de novo.
        Assert.Empty(SkinUnlocks.UnlockNewlyEarned(progress, catalog));
    }

    [Fact]
    public void TimeRequirement_NeedsARecordedTimeStrictlyBelowTheTarget()
    {
        UnlockRequirement requirement = Unlock.ClassicRaceUnder(30f);

        Assert.False(requirement.IsMet(new SaveData { BestRaceTimeSprint = null }));
        Assert.False(requirement.IsMet(new SaveData { BestRaceTimeSprint = 30f }));
        Assert.True(requirement.IsMet(new SaveData { BestRaceTimeSprint = 29.9f }));
    }

    [Fact]
    public void ScoreRequirement_ReadsTheBestTimeAttackScore()
    {
        UnlockRequirement requirement = Unlock.TimeAttackScore(1000);

        Assert.False(requirement.IsMet(new SaveData()));
        Assert.True(requirement.IsMet(new SaveData { BestScoreTimeAttack = 1000f }));
        Assert.Equal("PROGRESSO: 400/1000", requirement.ProgressText(new SaveData { BestScoreTimeAttack = 400f }));
    }

    [Fact]
    public void AnUnlockedSkinStaysUnlocked_EvenIfItsRequirementChangesLater()
    {
        var progress = new SaveData { UnlockedSkinIds = ["a"] };
        var harderCatalog = new[] { Skin("a", Unlock.ClassicWins(999)) };

        Assert.True(SkinUnlocks.IsUnlocked(harderCatalog[0], progress));
        Assert.Empty(SkinUnlocks.UnlockNewlyEarned(progress, harderCatalog));
    }

    [Fact]
    public void RequirementsThatDependOnOtherUnlocks_AreResolvedInTheSameCheck()
    {
        // Exemplo de requisito futuro ("libere outra skin antes"): cabe no mesmo formato sem mexer no resto.
        var catalog = new[]
        {
            Skin("depois", new AfterSkinRequirement("antes")),
            Skin("antes", Unlock.ClassicWins(1)),
        };
        var progress = new SaveData { SprintWins = 1 };

        List<CarSkin> earned = SkinUnlocks.UnlockNewlyEarned(progress, catalog);

        Assert.Equal(["antes", "depois"], earned.Select(skin => skin.Id));
    }

    private static CarSkin Skin(string id, UnlockRequirement requirement) => new(id, id.ToUpperInvariant(), _ => { }, requirement);

    private sealed class AfterSkinRequirement(string otherSkinId) : UnlockRequirement
    {
        public override string Description => $"LIBERE {otherSkinId} ANTES";

        public override bool IsMet(SaveData progress) => progress.UnlockedSkinIds.Contains(otherSkinId);
    }
}
