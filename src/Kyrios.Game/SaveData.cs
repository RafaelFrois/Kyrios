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

    /// <summary>Corridas terminadas entre os 3 primeiros.</summary>
    public int EliminationPodiums { get; set; }

    /// <summary>Vitórias sem sair do 1º lugar nenhuma vez depois da largada.</summary>
    public int EliminationWireToWireWins { get; set; }

    /// <summary>Vitórias depois de ter estado em último com só 3 carros (ou menos) na pista.</summary>
    public int EliminationClutchWins { get; set; }

    /// <summary>Vitórias em que o último rival caiu colado no jogador.</summary>
    public int EliminationPhotoFinishWins { get; set; }

    /// <summary>Vitórias sem soltar o acelerador depois da largada.</summary>
    public int EliminationFullThrottleWins { get; set; }

    /// <summary>Vitórias sem virar pra direita nenhuma vez.</summary>
    public int EliminationLeftOnlyWins { get; set; }

    /// <summary>Vitórias seguidas com a mesma skin (a atual e a melhor).</summary>
    public int SameSkinWinStreak { get; set; }
    public int BestSameSkinWinStreak { get; set; }
    public string LastWinSkinId { get; set; }

    /// <summary>Vitórias logo na primeira partida com uma skin.</summary>
    public int FreshSkinWins { get; set; }

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

    /// <summary>Melhor pontuação numa partida sem usar o turbo.</summary>
    public float BestNoBoostTimeAttackScore { get; set; }

    /// <summary>Mais checkpoints cruzados com menos de 1 s no relógio numa única partida.</summary>
    public int MostClutchCheckpointsInRun { get; set; }

    /// <summary>O menor tempo que já sobrava no relógio ao cruzar um checkpoint (null = nunca cruzou).</summary>
    public float? ClosestTimeAttackCall { get; set; }

    /// <summary>Batidas nos obstáculos móveis do Contra o Relógio, somando todas as partidas.</summary>
    public int TimeAttackHazardHits { get; set; }

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

    /// <summary>Checkpoints cruzados andando de ré.</summary>
    public int ReverseCheckpoints { get; set; }

    /// <summary>Mais tempo seguido andando na contramão numa partida.</summary>
    public float LongestWrongWaySeconds { get; set; }

    /// <summary>Partidas terminadas de madrugada (0h às 5h).</summary>
    public int NightOwlGames { get; set; }

    /// <summary>Dias diferentes em que jogou pelo menos uma partida.</summary>
    public int DaysPlayed { get; set; }
    public string LastPlayedDay { get; set; }

    /// <summary>Partidas seguidas com a mesma skin (a atual e a melhor).</summary>
    public int SameSkinGameStreak { get; set; }
    public int BestSameSkinGameStreak { get; set; }
    public string LastSkinId { get; set; }

    // ----- Fora das corridas (menus) -----
    /// <summary>Interações escondidas já descobertas nos menus (ids estáveis, ver <see cref="Discovery"/>).</summary>
    public List<string> Discoveries { get; set; } = [];

    /// <summary>Tentativas de equipar algo ainda bloqueado.</summary>
    public int DeniedEquips { get; set; }

    /// <summary>Vezes que trocou a skin equipada.</summary>
    public int SkinChanges { get; set; }

    // ----- Por skin -----
    /// <summary>Vitórias na Corrida Mortal por id de skin usada.</summary>
    public Dictionary<string, int> WinsBySkin { get; set; } = [];

    /// <summary>Recordes do Contra o Relógio por id de skin usada.</summary>
    public Dictionary<string, int> RecordsBySkin { get; set; } = [];

    /// <summary>Melhor pontuação do Contra o Relógio por id de skin usada.</summary>
    public Dictionary<string, float> BestScoreBySkin { get; set; } = [];

    /// <summary>Skins já usadas em alguma partida terminada.</summary>
    public List<string> SkinsUsed { get; set; } = [];

    /// <summary>Partidas terminadas por id de skin usada.</summary>
    public Dictionary<string, int> GamesBySkin { get; set; } = [];

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

    /// <summary>Melhor volta (qualquer modo) por pista.</summary>
    public Dictionary<string, float> BestLapByTrack { get; set; } = [];

    /// <summary>Partidas terminadas por combinação "skin@pista" (qualquer modo).</summary>
    public Dictionary<string, int> GamesBySkinOnTrack { get; set; } = [];

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

    // ----- Vídeo -----
    /// <summary>O jogo abre em tela cheia; só fica em janela se o jogador desligar nas configurações.
    /// (Campo novo de propósito: o antigo "Fullscreen" salvo como falso nos saves velhos é ignorado.)</summary>
    public bool Windowed { get; set; }

    /// <summary>Idioma do jogo: "pt" (padrão) ou "en".</summary>
    public string Language { get; set; } = "pt";

    /// <summary>Versão atual do formato do save. Subir quando um campo mudar de significado e escrever a migração
    /// em <see cref="Migrate"/> (campos novos não precisam: ganham o valor padrão sozinhos).</summary>
    public const int CurrentVersion = 1;

    /// <summary>Versão do formato em que este save foi escrito (0 = saves anteriores ao controle de versão).</summary>
    public int SaveVersion { get; set; } = CurrentVersion;

    /// <summary>Abre o save do jogador no armazenamento da plataforma (arquivo no desktop, navegador na web).</summary>
    public static SaveData Load() => LoadFrom(GamePlatform.Current.SaveStore);

    /// <summary>Abre um save em arquivo (usado pelos testes e pela versão desktop).</summary>
    public static SaveData LoadFrom(string path) => LoadFrom(new FileSaveStore(path));

    /// <summary>Abre o primeiro save legível: se o principal estiver corrompido (ex.: o PC desligou no meio de uma
    /// gravação), tenta a cópia de segurança antes de desistir e começar do zero.</summary>
    public static SaveData LoadFrom(ISaveStore store)
    {
        foreach (string json in store.ReadCandidates())
        {
            if (FromJson(json) is { } loaded)
            {
                return loaded;
            }
        }

        return new SaveData();
    }

    /// <summary>Lê um save em JSON (null se estiver corrompido). Campos que não existem mais são ignorados e os
    /// que faltam ganham o valor padrão, então saves de versões antigas abrem sem erro.</summary>
    public static SaveData FromJson(string json)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            SaveData loaded = JsonSerializer.Deserialize(json, SaveDataJsonContext.Default.SaveData);
            if (loaded is null)
            {
                return null;
            }

            loaded.Migrate();
            loaded.RemoveImpossibleValues();
            loaded.FillStatsMissingFromOldSaves();
            return loaded;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException or InvalidOperationException)
        {
            return null;
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this, SaveDataJsonContext.Default.SaveData);

    /// <summary>Anota uma interação escondida descoberta nos menus. Devolve true se for nova.</summary>
    public bool AddDiscovery(string id)
    {
        if (Discoveries.Contains(id))
        {
            return false;
        }

        Discoveries.Add(id);
        return true;
    }

    /// <summary>Garante que nenhuma coleção venha nula de um save antigo ou editado à mão, e deduz o mínimo
    /// garantido de contadores novos a partir do que já existia (quem tem recorde no Contra o Relógio jogou
    /// pelo menos uma vez), pra não zerar progresso já cumprido.</summary>
    public void FillStatsMissingFromOldSaves()
    {
        Language = L.Code(L.FromCode(Language));
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
        BestLapByTrack ??= [];
        GamesBySkin ??= [];
        GamesBySkinOnTrack ??= [];
        Discoveries ??= [];
        UnlockedSkinIds ??= [];
        UnlockedTrackIds ??= [];
        UnlockedAchievementIds ??= [];

        if (!Enum.IsDefined(LastMode))
        {
            LastMode = RaceMode.Elimination;
        }

        // Skins usadas antes de existir a contagem por skin valem pelo menos uma partida.
        foreach (string skin in SkinsUsed)
        {
            GamesBySkin.TryAdd(skin, 1);
        }

        if (BestScoreTimeAttack is { } bestScore)
        {
            TimeAttackRaces = Math.Max(TimeAttackRaces, 1);
            TotalTimeAttackScore = Math.Max(TotalTimeAttackScore, bestScore);
            RecordsSet = Math.Max(RecordsSet, bestScore > 0f ? 1 : 0);
        }
    }

    /// <summary>Atualiza saves de formatos antigos. Versão 0 → 1: só passou a existir o campo de versão.</summary>
    private void Migrate()
    {
        if (SaveVersion < CurrentVersion)
        {
            SaveVersion = CurrentVersion;
        }
    }

    /// <summary>Um save editado à mão ou corrompido pela metade não pode quebrar o jogo: contadores negativos voltam
    /// a zero, números inválidos (NaN, infinito) somem e listas perdem entradas vazias ou repetidas. Percorre os
    /// campos pelos metadados do próprio serializador, então vale pra qualquer estatística nova sem código extra.</summary>
    private void RemoveImpossibleValues()
    {
        foreach (System.Text.Json.Serialization.Metadata.JsonPropertyInfo property in SaveDataJsonContext.Default.SaveData.Properties)
        {
            if (property.Get is not { } get || property.Set is not { } set)
            {
                continue;
            }

            // float? (recordes e melhores tempos): inválido vira "sem recorde" — nunca 0, que seria um recorde perfeito.
            if (property.PropertyType == typeof(float?))
            {
                if (get(this) is float optional && (!float.IsFinite(optional) || optional < 0f))
                {
                    set(this, null);
                }

                continue;
            }

            switch (get(this))
            {
                case int number when number < 0:
                    set(this, 0);
                    break;
                case float number when !float.IsFinite(number) || number < 0f:
                    set(this, 0f);
                    break;
                case List<string> list:
                    List<string> clean = [.. list.Where(item => !string.IsNullOrEmpty(item)).Distinct()];
                    if (clean.Count != list.Count)
                    {
                        set(this, clean);
                    }

                    break;
                case Dictionary<string, int> counts:
                    foreach (string key in counts.Where(pair => pair.Value < 0).Select(pair => pair.Key).ToList())
                    {
                        counts.Remove(key);
                    }

                    break;
                case Dictionary<string, float> values:
                    foreach (string key in values.Where(pair => !float.IsFinite(pair.Value) || pair.Value < 0f).Select(pair => pair.Key).ToList())
                    {
                        values.Remove(key);
                    }

                    break;
            }
        }

        MusicVolume = Math.Clamp(MusicVolume, 0f, 1f);
        SfxVolume = Math.Clamp(SfxVolume, 0f, 1f);
    }

    public void Save() => SaveTo(GamePlatform.Current.SaveStore);

    /// <summary>Grava em arquivo (usado pelos testes e pela versão desktop).</summary>
    public void SaveTo(string path) => SaveTo(new FileSaveStore(path));

    public void SaveTo(ISaveStore store) => store.Write(ToJson());
}

// Gera o (de)serializador em tempo de compilação, sem reflexão — necessário porque o executável
// publicado usa trimming (PublishTrimmed) e IL trimming/AOT desabilitam o JsonSerializer por reflexão.
[JsonSerializable(typeof(SaveData))]
internal sealed partial class SaveDataJsonContext : JsonSerializerContext
{
}
