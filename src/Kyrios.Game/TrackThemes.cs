using Kyrios.Core;

namespace Kyrios.Game;

/// <summary>
/// Uma pista/cenário. Todas usam EXATAMENTE o mesmo circuito do <see cref="TrackFactory"/> (curvas, checkpoints,
/// largada, colisores e IA iguais) — o que muda é só o visual (<see cref="Scenery"/>): chão, bordas, objetos,
/// iluminação, partículas e animações. <see cref="Id"/> é o que fica salvo; <see cref="Icon"/> é um pixel-art
/// 12x12 (ver <see cref="AchievementIcons"/>) usado em notificações e conquistas. Cada pista esconde um detalhe
/// em <see cref="SecretSpot"/> (em células, sobre o asfalto, fora da trajetória ideal): passar por cima dele
/// libera a conquista secreta <see cref="SecretName"/> dessa pista.
/// </summary>
public sealed record TrackTheme(
    string Id,
    string Name,
    string Tagline,
    UnlockRequirement Requirement,
    Difficulty Difficulty,
    string[] Icon,
    SceneryStyle Scenery,
    Vector2D SecretSpot,
    string SecretName,
    string SecretDescription) : IUnlockable;

/// <summary>
/// Catálogo de pistas, na ordem do seletor. Pra criar uma nova: monte um <see cref="SceneryStyle"/> em
/// <see cref="TrackSceneries"/> (cores + o que desenhar) e acrescente uma linha aqui com requisito,
/// dificuldade, ícone e o segredo. A conquista secreta da pista é gerada sozinha (ver
/// <see cref="Achievements"/>), assim como as que contam "pistas jogadas/vencidas".
/// </summary>
public static class TrackThemes
{
    public static IReadOnlyList<TrackTheme> All { get; } =
    [
        new("autodromo", "AUTODROMO", "O CIRCUITO OFICIAL. ARQUIBANCADA LOTADA.", Unlock.FromStart, Difficulty.Easy,
            AchievementIcons.Flag, TrackSceneries.Circuit, new Vector2D(51.4f, 19.6f),
            "PNEU DE OURO", "ACHE O PNEU DOURADO ESQUECIDO NO AUTODROMO"),

        new("praia", "PRAIA", "AREIA, MAR, GUARDA-SOL E QUIOSQUE.", Unlock.GamesPlayed(2), Difficulty.Easy,
            AchievementIcons.PalmTree, TrackSceneries.Beach, new Vector2D(1.5f, 1.5f),
            "SIRI TIMIDO", "ENCONTRE O SIRI ESCONDIDO NA PRAIA"),

        new("floresta", "FLORESTA", "ESTRADA DE TERRA NO MEIO DAS ARVORES.", Unlock.RoundsSurvived(3), Difficulty.Easy,
            AchievementIcons.PineTree, TrackSceneries.Forest, new Vector2D(51.5f, 1.5f),
            "CIRCULO DE COGUMELOS", "ACHE O CIRCULO DE COGUMELOS DA FLORESTA"),

        new("cidade_noite", "CIDADE A NOITE", "RUAS ILUMINADAS E PREDIOS ACORDADOS.", Unlock.DeathRaceWins(5), Difficulty.Medium,
            AchievementIcons.CityNight, TrackSceneries.NightCity, new Vector2D(1.5f, 19.5f),
            "GATO DE RUA", "ENCONTRE O GATO DA CIDADE A NOITE"),

        new("deserto", "DESERTO", "DUNAS, CACTOS E UM POSTO ABANDONADO.", Unlock.TimeAttackTotal(4000), Difficulty.Medium,
            AchievementIcons.Cactus, TrackSceneries.Desert, new Vector2D(40.5f, 1.4f),
            "OSSADA NO CAMINHO", "ACHE A CAVEIRA PERDIDA NO DESERTO"),

        new("neve", "NEVE", "GELO, PINHEIROS E UM LAGO CONGELADO.", Unlock.TracksPlayed(4), Difficulty.Medium,
            AchievementIcons.Snowflake, TrackSceneries.Snow, new Vector2D(8.6f, 14.5f),
            "PINGUIM PERDIDO", "ENCONTRE O PINGUIM PERDIDO NA NEVE"),

        new("supermercado", "SUPERMERCADO", "UM CARRO DO TAMANHO DE UMA FORMIGA.", Unlock.SkinEarned("carrinho"), Difficulty.Medium,
            AchievementIcons.Cart, TrackSceneries.Supermarket, new Vector2D(20.5f, 20.3f),
            "CASCA DE BANANA", "PASSE PELA CASCA DE BANANA DO CORREDOR 7"),

        new("cidade_neon", "CIDADE NEON", "CHUVA, NEON E REFLEXOS NO ASFALTO.", Unlock.DeathRaceWinStreak(3), Difficulty.Hard,
            AchievementIcons.NeonCity, TrackSceneries.NeonCity, new Vector2D(51.4f, 13.5f),
            "ROBO SOLITARIO", "ENCONTRE O ROBO NA CIDADE NEON"),

        new("vulcao", "VULCAO", "LAVA, CINZAS E ROCHA QUENTE.", Unlock.TimeAttackScore(1500), Difficulty.Hard,
            AchievementIcons.Volcano, TrackSceneries.Volcano, new Vector2D(1.4f, 6.5f),
            "OVO DE DRAGAO", "ACHE O OVO DE DRAGAO NO VULCAO"),

        new("mesa", "MESA DA COZINHA", "CAFE DA MANHA SERVIDO NA PISTA.", Unlock.AchievementsUnlocked(30), Difficulty.Hard,
            AchievementIcons.Mug, TrackSceneries.KitchenTable, new Vector2D(33.5f, 4.3f),
            "FORMIGA CURIOSA", "ENCONTRE A FORMIGA NA MESA DA COZINHA"),

        new("quarto", "QUARTO DE CRIANCA", "TAPETE, BRINQUEDOS E PISTA DE PLASTICO.", Unlock.TrackSecretsFound(3), Difficulty.Rare,
            AchievementIcons.ToyBlocks, TrackSceneries.KidsRoom, new Vector2D(44.4f, 19.6f),
            "MEIA PERDIDA", "ACHE A MEIA PERDIDA NO QUARTO"),
    ];

    public static TrackTheme Default => All[0];

    public static TrackTheme Find(string id) => All.FirstOrDefault(track => track.Id == id);

    public static int IndexOf(string id)
    {
        for (int i = 0; i < All.Count; i++)
        {
            if (All[i].Id == id)
            {
                return i;
            }
        }

        return 0;
    }

    public static bool IsUnlocked(TrackTheme track, SaveData progress) => Unlockables.IsUnlocked(track, progress.UnlockedTrackIds);
}
