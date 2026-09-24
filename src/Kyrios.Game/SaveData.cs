using System.Text.Json;
using Kyrios.Core;
using System.Text.Json.Serialization;

namespace Kyrios.Game;

/// <summary>
/// Progresso do jogador salvo localmente entre sessões (records.json): estatísticas dos dois modos (Corrida
/// Mortal e Contra o Relógio), estatísticas por pista e por skin, skins/pistas/conquistas desbloqueadas, as
/// escolhas atuais e as preferências de som. Só <see cref="Progression"/> ESCREVE as estatísticas (no fim de
/// cada partida); os requisitos de skins, pistas e conquistas só as LEEM (ver <see cref="UnlockRequirement"/>).
/// Campo novo que falte num save antigo começa com o valor padrão; campo que não existe mais é ignorado.
/// </summary>
public sealed class SaveData
{
    // ----- Corrida Mortal -----
    public int EliminationWins { get; set; }
    public int EliminationRaces { get; set; }

    /// <summary>Eliminações de rivais que o jogador sobreviveu (somando todas as corridas).</summary>
    public int EliminationRoundsSurvived { get; set; }

    public int EliminationWinStreak { get; set; }
    public int BestEliminationWinStreak { get; set; }

    /// <summary>Vitórias sem nunca ter ficado em último lugar durante a corrida.</summary>
    public int EliminationFlawlessWins { get; set; }

    /// <summary>Vitórias depois de ter estado em último em algum momento.</summary>
    public int EliminationComebackWins { get; set; }

    /// <summary>Vitórias sem bater em nada.</summary>
    public int EliminationCleanWins { get; set; }

    /// <summary>Vitórias sem usar o turbo nenhuma vez.</summary>
    public int EliminationNoBoostWins { get; set; }

    /// <summary>Maior número de batidas numa corrida que mesmo assim terminou em vitória.</summary>
    public int MostCollisionsInAWin { get; set; }

    /// <summary>Vitórias com o carro parado ou de ré no instante da última eliminação.</summary>
    public int EliminationLazyWins { get; set; }

    /// <summary>Vezes em que o jogador foi o primeiro eliminado.</summary>
    public int EliminationFirstOuts { get; set; }

    /// <summary>Vezes em que o jogador caiu só na última eliminação (2º lugar).</summary>
    public int EliminationRunnerUps { get; set; }

    /// <summary>Vezes em que o jogador foi eliminado com o turbo ligado.</summary>
    public int EliminatedWhileBoosting { get; set; }

    // ----- Contra o Relógio -----
    public float? BestScoreTimeAttack { get; set; }
    public int TimeAttackRaces { get; set; }

    /// <summary>Soma dos pontos de todas as partidas — o progresso de longo prazo no modo (uma partida boa
    /// fica na casa de 1000-2000 pts).</summary>
    public float TotalTimeAttackScore { get; set; }

    /// <summary>Melhor pontuação numa partida sem nenhuma batida.</summary>
    public float BestCleanTimeAttackScore { get; set; }

    /// <summary>Checkpoints cruzados com menos de 1 s sobrando no relógio.</summary>
    public int TimeAttackClutchCheckpoints { get; set; }

    public int TimeAttackZeroScores { get; set; }

    /// <summary>Mais voltas completadas numa única partida.</summary>
    public int MostTimeAttackLaps { get; set; }

    /// <summary>Maior sequência de checkpoints seguidos sem bater (numa partida).</summary>
    public int BestCleanCheckpointStreak { get; set; }

    /// <summary>Mais tempo que já chegou a ter no relógio ao mesmo tempo.</summary>
    public float MostTimeBanked { get; set; }

    /// <summary>Mais segundos perdidos em batidas numa única partida.</summary>
    public float MostTimeLostInOneRun { get; set; }

    // ----- Gerais (os dois modos) -----
    /// <summary>Vezes que o recorde do Contra o Relógio foi registrado (inclusive o primeiro).</summary>
    public int RecordsSet { get; set; }

    /// <summary>Vezes que um recorde que já existia foi superado.</summary>
    public int RecordsBeaten { get; set; }

    /// <summary>Melhor volta em qualquer modo.</summary>
    public float? BestLapTime { get; set; }

    public int TotalCollisions { get; set; }
    public int MostCollisionsInOneRace { get; set; }
    public int TotalCheckpoints { get; set; }
    public float LongestStandstillSeconds { get; set; }
    public float LongestReverseSeconds { get; set; }
    public float TotalBoostSeconds { get; set; }
    public float TotalRaceSeconds { get; set; }

    /// <summary>Batidas num rival logo nos primeiros segundos da largada.</summary>
    public int StartLineCrashes { get; set; }

    public int TotalPauses { get; set; }
    public int MostPausesInOneRace { get; set; }

    /// <summary>Partidas jogadas com música e efeitos no mudo.</summary>
    public int SilentGames { get; set; }

    // ----- Por skin -----
    /// <summary>Vitórias na Corrida Mortal por id de skin usada.</summary>
    public Dictionary<string, int> WinsBySkin { get; set; } = [];

    /// <summary>Recordes do Contra o Relógio por id de skin usada.</summary>
    public Dictionary<string, int> RecordsBySkin { get; set; } = [];

    /// <summary>Melhor pontuação do Contra o Relógio por id de skin usada.</summary>
    public Dictionary<string, float> BestScoreBySkin { get; set; } = [];

    /// <summary>Skins já usadas em alguma partida terminada.</summary>
    public List<string> SkinsUsed { get; set; } = [];

    // ----- Por pista -----
    public Dictionary<string, int> GamesByTrack { get; set; } = [];
    public Dictionary<string, int> WinsByTrack { get; set; } = [];
    public Dictionary<string, float> BestScoreByTrack { get; set; } = [];

    /// <summary>Pistas em que o jogador já terminou uma partida sem bater em nada.</summary>
    public List<string> CleanTracks { get; set; } = [];

    /// <summary>Pistas cujo detalhe escondido o jogador já encontrou.</summary>
    public List<string> FoundTrackSecrets { get; set; } = [];

    /// <summary>Vitórias na Corrida Mortal por combinação "skin@pista".</summary>
    public Dictionary<string, int> WinsBySkinOnTrack { get; set; } = [];

    /// <summary>Partidas terminadas nos dois modos (abandonar no meio não conta).</summary>
    [JsonIgnore]
    public int GamesPlayed => EliminationRaces + TimeAttackRaces;

    // ----- Escolhas atuais -----
    /// <summary>Id da <see cref="CarSkin"/> equipada (null = a padrão).</summary>
    public string SelectedSkinId { get; set; }

    /// <summary>Id da <see cref="TrackTheme"/> escolhida (null = a primeira).</summary>
    public string SelectedTrackId { get; set; }

    /// <summary>Último modo jogado, pra seleção já começar nele.</summary>
    public RaceMode LastMode { get; set; } = RaceMode.Elimination;

    // ----- Desbloqueios (permanentes: uma vez na lista, ficam liberados mesmo que o requisito mude) -----
    public List<string> UnlockedSkinIds { get; set; } = [];
    public List<string> UnlockedTrackIds { get; set; } = [];
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
                SaveData loaded = FromJson(File.ReadAllText(FilePath));
                if (loaded is not null)
                {
                    return loaded;
                }
            }
        }
        catch (IOException)
        {
        }

        return new SaveData();
    }

    /// <summary>Lê um save em JSON (null se estiver corrompido). Campos que não existem mais são ignorados e os
    /// que faltam ganham o valor padrão, então saves de versões antigas abrem sem erro.</summary>
    public static SaveData FromJson(string json)
    {
        try
        {
            SaveData loaded = JsonSerializer.Deserialize(json, SaveDataJsonContext.Default.SaveData);
            loaded?.FillStatsMissingFromOldSaves();
            return loaded;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this, SaveDataJsonContext.Default.SaveData);

    /// <summary>Garante que nenhuma coleção venha nula de um save antigo ou editado à mão, e deduz o mínimo
    /// garantido de contadores novos a partir do que já existia (quem tem recorde no Contra o Relógio jogou
    /// pelo menos uma vez), pra não zerar progresso já cumprido.</summary>
    public void FillStatsMissingFromOldSaves()
    {
        WinsBySkin ??= [];
        RecordsBySkin ??= [];
        BestScoreBySkin ??= [];
        SkinsUsed ??= [];
        GamesByTrack ??= [];
        WinsByTrack ??= [];
        BestScoreByTrack ??= [];
        CleanTracks ??= [];
        FoundTrackSecrets ??= [];
        WinsBySkinOnTrack ??= [];
        UnlockedSkinIds ??= [];
        UnlockedTrackIds ??= [];
        UnlockedAchievementIds ??= [];

        if (BestScoreTimeAttack is { } bestScore)
        {
            TimeAttackRaces = Math.Max(TimeAttackRaces, 1);
            TotalTimeAttackScore = Math.Max(TotalTimeAttackScore, bestScore);
            RecordsSet = Math.Max(RecordsSet, bestScore > 0f ? 1 : 0);
        }
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(FilePath, ToJson());
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
