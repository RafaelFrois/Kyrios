using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kyrios.Game;

/// <summary>Recordes pessoais salvos localmente entre sessões — o gancho de "quero bater meu próprio tempo".</summary>
public sealed class SaveData
{
    public float? BestLapTimeSprint { get; set; }
    public float? BestRaceTimeSprint { get; set; }
    public int EliminationWins { get; set; }
    public int EliminationRaces { get; set; }
    public float? BestScoreTimeAttack { get; set; }

    public float MusicVolume { get; set; } = 1f;
    public float SfxVolume { get; set; } = 1f;
    public bool MusicMuted { get; set; }
    public bool SfxMuted { get; set; }

    /// <summary>Id da <see cref="CarSkin"/> escolhida pro carro do jogador (null = a padrão).</summary>
    public string SelectedSkinId { get; set; }

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
