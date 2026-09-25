using Kyrios.Core;

namespace Kyrios.Game;

/// <summary>
/// Uma pista/cenário. Todas usam EXATAMENTE o mesmo circuito do <see cref="TrackFactory"/> (curvas, checkpoints,
/// largada, colisores e IA iguais) — o que muda é só o visual (<see cref="Scenery"/>): chão, bordas, objetos,
/// iluminação, partículas e animações. <see cref="Id"/> é o que fica salvo; <see cref="Icon"/> é um pixel-art
/// 12x12 (ver <see cref="AchievementIcons"/>) usado em notificações e conquistas. Cada pista esconde um detalhe
/// em <see cref="SecretSpot"/> (em células, sobre o asfalto, fora da trajetória ideal): passar por cima dele
/// libera a conquista secreta <see cref="SecretName"/> dessa pista. Uma pista <see cref="IsSecret"/> esconde nome,
/// visual e requisito até ser liberada. <see cref="Difficulty"/> só organiza o catálogo (não aparece na tela).
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
    string SecretDescription,
    bool IsSecret = false) : IUnlockable;

/// <summary>
/// Catálogo de pistas, na ordem do seletor. Pra criar uma nova: monte um <see cref="SceneryStyle"/> em
/// <see cref="TrackSceneries"/> (cores + o que desenhar) e acrescente uma linha aqui com requisito,
/// dificuldade, ícone e o segredo. A conquista secreta da pista é gerada sozinha (ver
/// <see cref="Achievements"/>), assim como as que contam "pistas jogadas/vencidas".
/// </summary>
public static class TrackThemes
{
    private const Difficulty Easy = Difficulty.Easy;
    private const Difficulty Medium = Difficulty.Medium;
    private const Difficulty Hard = Difficulty.Hard;
    private const Difficulty VeryHard = Difficulty.VeryHard;
    private const Difficulty Rare = Difficulty.Rare;

    public static IReadOnlyList<TrackTheme> All { get; } =
    [
        // ----- Normais: o mundo "de verdade" -----
        new("autodromo", "AUTODROMO", "O CIRCUITO OFICIAL. ARQUIBANCADA LOTADA.", Unlock.FromStart, Easy,
            AchievementIcons.Flag, TrackSceneries.Circuit, new Vector2D(51.4f, 19.6f),
            "PNEU DE OURO", "ACHE O PNEU DOURADO ESQUECIDO NO AUTODROMO"),

        new("praia", "PRAIA", "AREIA, MAR, GUARDA-SOL E QUIOSQUE.", Unlock.GamesPlayed(2), Easy,
            AchievementIcons.PalmTree, TrackSceneries.Beach, new Vector2D(1.5f, 1.5f),
            "SIRI TIMIDO", "ENCONTRE O SIRI ESCONDIDO NA PRAIA"),

        new("floresta", "FLORESTA", "ESTRADA DE TERRA NO MEIO DAS ARVORES.", Unlock.RoundsSurvived(5), Easy,
            AchievementIcons.PineTree, TrackSceneries.Forest, new Vector2D(51.5f, 1.5f),
            "CIRCULO DE COGUMELOS", "ACHE O CIRCULO DE COGUMELOS DA FLORESTA"),

        new("montanha", "MONTANHA", "ESTRADA DE SERRA, PICO NEVADO E RIO.", Unlock.TracksPlayed(3), Easy,
            AchievementIcons.Mountain, TrackSceneries.Mountain, new Vector2D(16.5f, 1.4f),
            "CABRA MONTANHESA", "ACHE A CABRA ESCONDIDA NA MONTANHA"),

        new("estadio", "ESTADIO", "PISTA DE ATLETISMO EM VOLTA DO GRAMADO.", Unlock.DeathRaceWins(2), Medium,
            AchievementIcons.SoccerBall, TrackSceneries.Stadium, new Vector2D(35.5f, 20.6f),
            "CHUTEIRA PERDIDA", "ACHE A CHUTEIRA PERDIDA NO ESTADIO"),

        new("cidade_noite", "CIDADE A NOITE", "RUAS ILUMINADAS E PREDIOS ACORDADOS.",
            Unlock.Stat("TERMINE 5 CORRIDAS MORTAIS NO PODIO (TOP 3)", p => p.EliminationPodiums, 5), Medium,
            AchievementIcons.CityNight, TrackSceneries.NightCity, new Vector2D(1.5f, 19.5f),
            "GATO DE RUA", "ENCONTRE O GATO DA CIDADE A NOITE"),

        new("deserto", "DESERTO", "DUNAS, CACTOS E UM POSTO ABANDONADO.", Unlock.TimeAttackTotal(4000), Medium,
            AchievementIcons.Cactus, TrackSceneries.Desert, new Vector2D(40.5f, 1.4f),
            "OSSADA NO CAMINHO", "ACHE A CAVEIRA PERDIDA NO DESERTO"),

        new("neve", "NEVE", "GELO, PINHEIROS E UM LAGO CONGELADO.",
            Unlock.Stat("PASSE POR 9 CHECKPOINTS SEGUIDOS SEM BATER", p => p.BestCleanCheckpointStreak, 9), Medium,
            AchievementIcons.Snowflake, TrackSceneries.Snow, new Vector2D(8.6f, 14.5f),
            "PINGUIM PERDIDO", "ENCONTRE O PINGUIM PERDIDO NA NEVE"),

        // ----- Absurdos: o mesmo circuito em escala gigante -----
        new("supermercado", "SUPERMERCADO", "UM CARRO DO TAMANHO DE UMA FORMIGA.", Unlock.SkinEarned("carrinho"), Medium,
            AchievementIcons.Cart, TrackSceneries.Supermarket, new Vector2D(20.5f, 20.3f),
            "CASCA DE BANANA", "PASSE PELA CASCA DE BANANA DO CORREDOR 7"),

        new("fundo_do_mar", "FUNDO DO MAR", "CORAIS, NAUFRAGIO E UM BAU DO TESOURO.", Unlock.SkinEarned("peixe"), Medium,
            AchievementIcons.Fish, TrackSceneries.Underwater, new Vector2D(52.5f, 4.5f),
            "OSTRA COM PEROLA", "ACHE A PEROLA ESCONDIDA NO FUNDO DO MAR"),

        new("sala", "SALA DE ESTAR", "SOFA, TAPETE, TV LIGADA E UM GATO DORMINDO.", Unlock.AchievementsUnlocked(25), Medium,
            AchievementIcons.Couch, TrackSceneries.LivingRoom, new Vector2D(6.5f, 1.4f),
            "CONTROLE REMOTO", "ACHE O CONTROLE REMOTO PERDIDO NA SALA"),

        new("escritorio", "ESCRITORIO", "BAIAS, PAPELADA E CAFE REQUENTADO.", Unlock.GamesPlayed(50), Medium,
            AchievementIcons.Computer, TrackSceneries.Office, new Vector2D(52.5f, 17.5f),
            "GRAMPEADOR VERMELHO", "ACHE O GRAMPEADOR VERMELHO NO ESCRITORIO"),

        new("parque", "PARQUE DE DIVERSOES", "RODA-GIGANTE, CARROSSEL E ALGODAO-DOCE.", Unlock.SkinsUnlocked(15), Hard,
            AchievementIcons.FerrisWheel, TrackSceneries.AmusementPark, new Vector2D(24.5f, 1.4f),
            "BALAO FUJAO", "ACHE O BALAO QUE ESCAPOU NO PARQUE"),

        new("cidade_neon", "CIDADE NEON", "CHUVA, NEON E REFLEXOS NO ASFALTO.", Unlock.DeathRaceWinStreak(3), Hard,
            AchievementIcons.NeonCity, TrackSceneries.NeonCity, new Vector2D(51.4f, 13.5f),
            "ROBO SOLITARIO", "ENCONTRE O ROBO NA CIDADE NEON"),

        new("vulcao", "VULCAO", "LAVA, CINZAS E ROCHA QUENTE.", Unlock.TimeAttackScore(1500), Hard,
            AchievementIcons.Volcano, TrackSceneries.Volcano, new Vector2D(1.4f, 6.5f),
            "OVO DE DRAGAO", "ACHE O OVO DE DRAGAO NO VULCAO"),

        new("mesa", "MESA DA COZINHA", "CAFE DA MANHA SERVIDO NA PISTA.", Unlock.AchievementsUnlocked(35), Hard,
            AchievementIcons.Mug, TrackSceneries.KitchenTable, new Vector2D(33.5f, 4.3f),
            "FORMIGA CURIOSA", "ENCONTRE A FORMIGA NA MESA DA COZINHA"),

        new("setup_gamer", "SETUP GAMER", "TECLADO RGB, MOUSE E LATINHA DE ENERGETICO.", Unlock.LapUnder(8.8f), Hard,
            AchievementIcons.Keyboard, TrackSceneries.GamerDesk, new Vector2D(1.4f, 3.5f),
            "TECLA FUJONA", "ACHE A TECLA QUE SOLTOU DO TECLADO"),

        new("lixao", "LIXAO", "RESTOS, LATINHAS E MUITAS MOSCAS.", Unlock.Stat("BATA 400 VEZES NO TOTAL", p => p.TotalCollisions, 400), Hard,
            AchievementIcons.TrashCan, TrackSceneries.Junkyard, new Vector2D(13.5f, 20.5f),
            "RELOGIO DE OURO", "ACHE O RELOGIO DE OURO JOGADO NO LIXAO"),

        new("fabrica", "FABRICA", "ENGRENAGENS, ESTEIRAS E FAISCAS.", Unlock.Stat("SUPERE SEU RECORDE DO RELOGIO 8 VEZES", p => p.RecordsBeaten, 8), Hard,
            AchievementIcons.Gear, TrackSceneries.Factory, new Vector2D(52.5f, 12.5f),
            "PARAFUSO DE OURO", "ACHE O PARAFUSO DE OURO NA FABRICA"),

        new("quarto", "QUARTO DE CRIANCA", "TAPETE, BRINQUEDOS E PISTA DE PLASTICO.", Unlock.TrackSecretsFound(3), Hard,
            AchievementIcons.ToyBlocks, TrackSceneries.KidsRoom, new Vector2D(44.4f, 19.6f),
            "MEIA PERDIDA", "ACHE A MEIA PERDIDA NO QUARTO"),

        // ----- Fantásticos e criativos: o fim da coleção -----
        new("alienigena", "PLANETA ALIENIGENA", "COGUMELOS GIGANTES E CRISTAIS QUE BRILHAM.", Unlock.All("DESBLOQUEIE O OVNI E VENCA A MORTAL EM 4 PISTAS", Unlock.SkinEarned("ovni"), Unlock.TracksWon(4)), VeryHard,
            AchievementIcons.Alien, TrackSceneries.AlienWorld, new Vector2D(45.5f, 1.4f),
            "ALIEN BEBE", "ENCONTRE O ALIEN BEBE NO PLANETA ALIENIGENA"),

        new("nuvens", "ACIMA DAS NUVENS", "ESTRADA ARCO-IRIS NO MEIO DO CEU.", Unlock.TracksWon(6), VeryHard,
            AchievementIcons.Cloud, TrackSceneries.Clouds, new Vector2D(8.5f, 20.5f),
            "PENA DOURADA", "ACHE A PENA DOURADA ACIMA DAS NUVENS"),

        new("espaco", "ESTACAO ESPACIAL", "PAINEIS SOLARES, ASTEROIDES E A TERRA LA EMBAIXO.", Unlock.SkinEarned("foguete"), VeryHard,
            AchievementIcons.Planet, TrackSceneries.Space, new Vector2D(1.4f, 12.5f),
            "ASTRONAUTA PERDIDO", "RESGATE O ASTRONAUTA PERDIDO NO ESPACO"),

        new("placa_mae", "PLACA-MAE", "O CIRCUITO. LITERALMENTE.",
            Unlock.All("VOLTA EM MENOS DE 8.4 S E 5 CONQUISTAS DIFICEIS", Unlock.LapUnder(8.4f), Unlock.AchievementsOfDifficulty(Difficulty.Hard, 5)), VeryHard,
            AchievementIcons.Chip, TrackSceneries.Motherboard, new Vector2D(47.5f, 20.6f),
            "BUG DE VERDADE", "ACHE O BUG (DE VERDADE) NA PLACA-MAE"),

        new("caderno", "CADERNO", "UMA PISTA DESENHADA A LAPIS NA AULA.", Unlock.AchievementsUnlocked(60), VeryHard,
            AchievementIcons.Pencil, TrackSceneries.Notebook, new Vector2D(1.4f, 17.5f),
            "GALINHA DESENHADA", "ACHE A GALINHA DESENHADA NO CADERNO"),

        new("sinuca", "MESA DE SINUCA", "BAR DO BAIRRO, LUZ BAIXA E BOLA 8.", Unlock.SameSkinWinStreak(3), VeryHard,
            AchievementIcons.EightBall, TrackSceneries.PoolTable, new Vector2D(37.5f, 1.4f),
            "BOLA 8 FUJONA", "ACHE A BOLA 8 QUE PULOU DA MESA DE SINUCA"),

        new("bolo", "BOLO DE ANIVERSARIO", "COBERTURA, VELINHAS E CONFETE.", Unlock.DaysPlayed(7), VeryHard,
            AchievementIcons.Cake, TrackSceneries.Cake, new Vector2D(1.5f, 8.5f),
            "CEREJA DO BOLO", "ACHE A CEREJA DO BOLO"),

        new("assombrada", "NOITE ASSOMBRADA", "CEMITERIO, ABOBORAS E NEBLINA.",
            Unlock.All("JOGUE DE MADRUGADA (0H AS 5H) E ENCONTRE 5 SEGREDOS", Unlock.Stat("JOGUE UMA PARTIDA DE MADRUGADA", p => p.NightOwlGames, 1), Unlock.TrackSecretsFound(5)), Rare,
            AchievementIcons.Pumpkin, TrackSceneries.Haunted, new Vector2D(50.5f, 20.6f),
            "ALMA PERDIDA", "ACHE A ALMA PERDIDA NA NOITE ASSOMBRADA"),

        // Secreta: nome e requisito escondidos até ser liberada.
        new("retro", "MUNDO 8 BITS", "4 CORES, 1 CIRCUITO E MUITA NOSTALGIA.", Unlock.Discovered(Discovery.Konami, "DIGITE O CODIGO SECRETO NO MENU"), Rare,
            AchievementIcons.Invader, TrackSceneries.Retro, new Vector2D(22.5f, 1.4f),
            "COGUMELO DE VIDA", "ACHE O COGUMELO DE VIDA EXTRA NO MUNDO 8 BITS", IsSecret: true),
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
