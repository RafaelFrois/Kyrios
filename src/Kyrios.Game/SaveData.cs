using System.Text.Json;

namespace Kyrios.Game;

/// <summary>Recordes pessoais salvos localmente entre sessões — o gancho de "quero bater meu próprio tempo".</summary>
public sealed class SaveData
{
    public float? BestLapTimeSprint { get; set; }
    public float? BestRaceTimeSprint { get; set; }
    public int EliminationWins { get; set; }
    public int EliminationRaces { get; set; }

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
                SaveData loaded = JsonSerializer.Deserialize<SaveData>(json);
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
            string json = JsonSerializer.Serialize(this);
            File.WriteAllText(FilePath, json);
        }
        catch (IOException)
        {
        }
    }
}
