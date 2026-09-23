using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kyrios.Game;

/// <summary>
/// Progresso do jogador salvo localmente entre sessões (records.json): estatísticas dos três modos,
/// skins e conquistas desbloqueadas, skin escolhida e preferências de som. Só <see cref="Progression"/>
/// ESCREVE as estatísticas (no fim de cada partida); os requisitos de skins e conquistas só as LEEM (ver
/// <see cref="UnlockRequirement"/>).
/// Um campo novo aqui que falte num save antigo simplesmente começa com o valor padrão.
/// </summary>
public sealed class SaveData
{
    // ----- Corrida Clássica -----
    public float? BestLapTimeSprint { get; set; }
    public float? BestRaceTimeSprint { get; set; }
    public int SprintWins { get; set; }
    public int SprintRaces { get; set; }

    /// <summary>Vitórias na Clássica depois de ter estado em último em algum momento da corrida.</summary>
    public int SprintComebackWins { get; set; }

    public int SprintLastPlaces { get; set; }

    /// <summary>Corridas clássicas terminadas cruzando a linha de chegada de ré.</summary>
    public int SprintReverseFinishes { get; set; }

    // ----- Corrida Mortal -----
    public int EliminationWins { get; set; }
    public int EliminationRaces { get; set; }

    /// <summary>Eliminações de rivais que o jogador sobreviveu (somando todas as corridas).</summary>
    public int EliminationRoundsSurvived { get; set; }

    public int EliminationWinStreak { get; set; }
    public int BestEliminationWinStreak { get; set; }

    /// <summary>Vitórias sem nunca ter ficado em último lugar durante a corrida.</summary>
    public int EliminationFlawlessWins { get; set; }

    /// <summary>Vezes em que o jogador foi o primeiro eliminado.</summary>
    public int EliminationFirstOuts { get; set; }

    /// <summary>Vezes em que o jogador caiu só na última eliminação (2º lugar).</summary>
    public int EliminationRunnerUps { get; set; }

    // ----- Contra o Relógio -----
    public float? BestScoreTimeAttack { get; set; }
    public int TimeAttackRaces { get; set; }

    /// <summary>Soma dos pontos de todas as partidas — é o que mede o progresso de longo prazo no modo
    /// (uma partida boa fica na casa de 1000-2000 pts).</summary>
    public float TotalTimeAttackScore { get; set; }

    /// <summary>Melhor pontuação numa partida sem nenhuma batida.</summary>
    public float BestCleanTimeAttackScore { get; set; }

    /// <summary>Checkpoints cruzados com menos de 1 s sobrando no relógio.</summary>
    public int TimeAttackClutchCheckpoints { get; set; }

    public int TimeAttackZeroScores { get; set; }

    // ----- Gerais (todos os modos) -----
    /// <summary>Vezes que algum recorde pessoal foi registrado (inclusive o primeiro de cada tipo).</summary>
    public int RecordsSet { get; set; }

    /// <summary>Vezes que um recorde que já existia foi superado.</summary>
    public int RecordsBeaten { get; set; }

    public int TotalCollisions { get; set; }
    public int MostCollisionsInOneRace { get; set; }
    public float LongestStandstillSeconds { get; set; }
    public float TotalBoostSeconds { get; set; }
    public float TotalRaceSeconds { get; set; }

    /// <summary>Vitórias (Clássica ou Mortal) por id de skin usada.</summary>
    public Dictionary<string, int> WinsBySkin { get; set; } = [];

    public Dictionary<string, int> EliminationWinsBySkin { get; set; } = [];

    /// <summary>Recordes pessoais batidos por id de skin usada.</summary>
    public Dictionary<string, int> RecordsBySkin { get; set; } = [];

    /// <summary>Partidas terminadas em qualquer modo (abandonar no meio com ESC não conta).</summary>
    [JsonIgnore]
    public int GamesPlayed => SprintRaces + EliminationRaces + TimeAttackRaces;

    [JsonIgnore]
    public int TotalWins => SprintWins + EliminationWins;

    // ----- Skins -----
    /// <summary>Id da <see cref="CarSkin"/> equipada no carro do jogador (null = a clássica).</summary>
    public string SelectedSkinId { get; set; }

    /// <summary>Ids das skins já conquistadas. Uma vez aqui, a skin fica liberada pra sempre — mesmo que o
    /// requisito dela seja alterado depois. As que não exigem nada nem precisam aparecer na lista.</summary>
    public List<string> UnlockedSkinIds { get; set; } = [];

    // ----- Conquistas -----
    /// <summary>Ids das <see cref="Achievement"/>s já conquistadas (permanentes, como as skins).</summary>
    public List<string> UnlockedAchievementIds { get; set; } = [];

    // ----- Som -----
    public float MusicVolume { get; set; } = 1f;
    public float SfxVolume { get; set; } = 1f;
    public bool MusicMuted { get; set; }
    public bool SfxMuted { get; set; }

    private static string FilePath
    {
        get
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MegRace");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "records.json");
        }
    }

    public static SaveData Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                SaveData loaded = JsonSerializer.Deserialize(json, SaveDataJsonContext.Default.SaveData);
                if (loaded is not null)
                {
                    loaded.FillStatsMissingFromOldSaves();
                    return loaded;
                }
            }
        }
        catch (IOException)
        {
        }
        catch (JsonException)
        {
        }

        return new SaveData();
    }

    /// <summary>Saves de versões anteriores não tinham os contadores novos — deduz o mínimo garantido a
    /// partir do que já existia (quem venceu N corridas jogou pelo menos N, quem tem recorde no Contra o
    /// Relógio jogou pelo menos uma vez), pra não começar do zero conquistas que já foram cumpridas.</summary>
    public void FillStatsMissingFromOldSaves()
    {
        WinsBySkin ??= [];
        EliminationWinsBySkin ??= [];
        RecordsBySkin ??= [];
        UnlockedSkinIds ??= [];
        UnlockedAchievementIds ??= [];

        SprintRaces = Math.Max(SprintRaces, Math.Max(SprintWins, BestRaceTimeSprint is null ? 0 : 1));
        if (BestScoreTimeAttack is { } bestScore)
        {
            TimeAttackRaces = Math.Max(TimeAttackRaces, 1);
            TotalTimeAttackScore = Math.Max(TotalTimeAttackScore, bestScore);
        }

        int existingRecords = (BestLapTimeSprint is null ? 0 : 1) + (BestRaceTimeSprint is null ? 0 : 1) + (BestScoreTimeAttack is null ? 0 : 1);
        RecordsSet = Math.Max(RecordsSet, existingRecords);
    }

    public void Save()
    {
        try
        {
            string json = JsonSerializer.Serialize(this, SaveDataJsonContext.Default.SaveData);
            File.WriteAllText(FilePath, json);
        }
        catch (IOException)
        {
        }
    }
}

// Gera o (de)serializador em tempo de compilação, sem reflexão — necessário porque o executável
// publicado usa trimming (PublishTrimmed) e IL trimming/AOT desabilitam o JsonSerializer por reflexão.
[JsonSerializable(typeof(SaveData))]
internal sealed partial class SaveDataJsonContext : JsonSerializerContext
{
}
