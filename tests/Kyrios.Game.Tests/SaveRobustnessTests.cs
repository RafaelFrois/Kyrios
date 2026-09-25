using Kyrios.Game;
using Xunit;

namespace Kyrios.Game.Tests;

/// <summary>O save precisa sobreviver a tudo: formato antigo, edição à mão, gravação cortada no meio e armazenamento
/// indisponível (aba anônima no navegador). Nada disso pode impedir o jogo de abrir.</summary>
public class SaveRobustnessTests
{
    /// <summary>Armazenamento em memória com o mesmo contrato do localStorage da versão web (principal + backup).</summary>
    private sealed class MemoryStore : ISaveStore
    {
        public string Main;
        public string Backup;
        public bool Broken;

        public IEnumerable<string> ReadCandidates()
        {
            if (Broken)
            {
                yield break;
            }

            if (Main is not null)
            {
                yield return Main;
            }

            if (Backup is not null)
            {
                yield return Backup;
            }
        }

        public void Write(string json)
        {
            if (Broken)
            {
                return;
            }

            if (Main is not null && Main != json)
            {
                Backup = Main;
            }

            Main = json;
        }
    }

    [Fact]
    public void NewSaves_CarryTheCurrentVersion_AndOldOnesAreMigrated()
    {
        Assert.Equal(SaveData.CurrentVersion, new SaveData().SaveVersion);
        SaveData legacy = SaveData.FromJson("""{"EliminationWins":4}""");
        Assert.Equal(SaveData.CurrentVersion, legacy.SaveVersion);
        Assert.Equal(4, legacy.EliminationWins);
    }

    [Fact]
    public void ImpossibleValues_AreRepaired_WithoutLosingTheRest()
    {
        SaveData save = SaveData.FromJson("""
            {"EliminationWins":-5,"EliminationRaces":12,"MusicVolume":7,"SfxVolume":-1,
             "BestLapTime":-3,"BestScoreTimeAttack":-10,"TotalTimeAttackScore":-4,
             "WinsBySkin":{"pato":-2,"galinha":3},"BestLapByTrack":{"praia":-1,"neve":9.5},
             "UnlockedSkinIds":["pato","","pato","galinha"]}
            """);

        Assert.NotNull(save);
        Assert.Equal(0, save.EliminationWins);
        Assert.Equal(12, save.EliminationRaces);
        Assert.Equal(1f, save.MusicVolume);
        Assert.Equal(0f, save.SfxVolume);
        Assert.Null(save.BestLapTime);
        Assert.Null(save.BestScoreTimeAttack);
        Assert.Equal(0f, save.TotalTimeAttackScore);
        Assert.False(save.WinsBySkin.ContainsKey("pato"));
        Assert.Equal(3, save.WinsBySkin["galinha"]);
        Assert.False(save.BestLapByTrack.ContainsKey("praia"));
        Assert.Equal(["pato", "galinha"], save.UnlockedSkinIds);
    }

    [Fact]
    public void AnInvalidRecord_NeverBecomesAPerfectRecord()
    {
        // NaN/infinito não existem em JSON puro, mas um save corrompido pode ter qualquer número absurdo.
        SaveData save = SaveData.FromJson("""{"BestLapTime":-0.5,"ClosestTimeAttackCall":-1}""");
        Assert.Null(save.BestLapTime);
        Assert.Null(save.ClosestTimeAttackCall);
    }

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("[1,2,3]")]
    [InlineData("{\"EliminationWins\":\"muitas\"}")]
    [InlineData("{ corrompido")]
    public void GarbageSaves_AreRejected_SoTheBackupCanBeUsed(string json) => Assert.Null(SaveData.FromJson(json));

    [Fact]
    public void CorruptedMainSave_FallsBackToTheBackup()
    {
        var store = new MemoryStore();
        new SaveData { EliminationWins = 3 }.SaveTo(store);
        new SaveData { EliminationWins = 4 }.SaveTo(store);
        store.Main = "{ gravação cortada no mei";

        Assert.Equal(3, SaveData.LoadFrom(store).EliminationWins);
    }

    [Fact]
    public void UnavailableStorage_StartsAFreshGame_AndSavingDoesNotThrow()
    {
        var store = new MemoryStore { Broken = true };
        SaveData save = SaveData.LoadFrom(store);
        Assert.Equal(0, save.GamesPlayed);
        save.EliminationRaces = 3;
        save.SaveTo(store);
        Assert.Null(store.Main);
    }

    [Fact]
    public void EverythingSurvivesARoundTrip()
    {
        var store = new MemoryStore();
        var save = new SaveData();
        save.UnlockedSkinIds.Add("pato");
        save.UnlockedTrackIds.Add("praia");
        save.UnlockedAchievementIds.Add("sobrevivente");
        save.BestScoreTimeAttack = 1234f;
        save.BestLapTime = 9.25f;
        save.SelectedSkinId = "pato";
        save.SelectedTrackId = "praia";
        save.Language = "en";
        save.SaveTo(store);

        SaveData reopened = SaveData.LoadFrom(store);
        Assert.Contains("pato", reopened.UnlockedSkinIds);
        Assert.Contains("praia", reopened.UnlockedTrackIds);
        Assert.Contains("sobrevivente", reopened.UnlockedAchievementIds);
        Assert.Equal(1234f, reopened.BestScoreTimeAttack);
        Assert.Equal(9.25f, reopened.BestLapTime);
        Assert.Equal("pato", reopened.SelectedSkinId);
        Assert.Equal("praia", reopened.SelectedTrackId);
        Assert.Equal("en", reopened.Language);
    }
}
