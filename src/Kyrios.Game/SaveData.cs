using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kyrios.Game;

/// <summary>
/// Progresso do jogador salvo localmente entre sessões (records.json): estatísticas dos três modos,
/// skins desbloqueadas/escolhida e preferências de som. Os modos de jogo só ESCREVEM as estatísticas
/// (ver GameRoot.ProcessRaceEndRecords); os requisitos de skin só as LEEM (ver <see cref="UnlockRequirement"/>).
/// Um campo novo aqui que falte num save antigo simplesmente começa com o valor padrão.
/// </summary>
public sealed class SaveData
{
    // ----- Corrida Clássica -----
    public float? BestLapTimeSprint { get; set; }
    public float? BestRaceTimeSprint { get; set; }
    public int SprintWins { get; set; }

    // ----- Corrida Mortal -----
    public int EliminationWins { get; set; }
    public int EliminationRaces { get; set; }

    // ----- Contra o Relógio -----
    public float? BestScoreTimeAttack { get; set; }

    // ----- Skins -----
    /// <summary>Id da <see cref="CarSkin"/> equipada no carro do jogador (null = a clássica).</summary>
    public string SelectedSkinId { get; set; }

    /// <summary>Ids das skins já conquistadas. Uma vez aqui, a skin fica liberada pra sempre — mesmo que o
    /// requisito dela seja alterado depois. As que não exigem nada nem precisam aparecer na lista.</summary>
    public List<string> UnlockedSkinIds { get; set; } = [];

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
