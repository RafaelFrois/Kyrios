using Microsoft.Xna.Framework;
using static Kyrios.Game.SkinCategory;

namespace Kyrios.Game;

/// <summary>Que tipo de "veículo" uma skin é — aparece no seletor e alguns requisitos pedem vitórias com
/// skins de uma categoria.</summary>
public enum SkinCategory
{
    Vehicle,
    Animal,
    Food,
    Thing,

    /// <summary>Coisas que nem objeto são direito (buraco negro, fantasma, um pixel perdido...).</summary>
    Absurd,
}

/// <summary>Nomes das categorias de skin, já no formato que a fonte pixelizada aceita.</summary>
public static class SkinCategories
{
    public static string Singular(SkinCategory category) => category switch
    {
        Vehicle => L.T("VEICULO", "VEHICLE"),
        Animal => L.T("ANIMAL", "ANIMAL"),
        Food => L.T("COMIDA", "FOOD"),
        Thing => L.T("OBJETO", "OBJECT"),
        _ => L.T("COISA ABSURDA", "ABSURD THING"),
    };

    public static string Plural(SkinCategory category) => category switch
    {
        Vehicle => L.T("VEICULOS", "VEHICLES"),
        Animal => L.T("ANIMAIS", "ANIMALS"),
        Food => L.T("COMIDAS", "FOODS"),
        Thing => L.T("OBJETOS", "OBJECTS"),
        _ => L.T("COISAS ABSURDAS", "ABSURD THINGS"),
    };

    /// <summary>Rótulo curto pra legendas apertadas (a grade da coleção).</summary>
    public static string ShortPlural(SkinCategory category) => category == Absurd ? L.T("ABSURDOS", "ABSURD") : Plural(category);

    /// <summary>Artigo antes do singular ("UM VEICULO", "UMA COMIDA", "A VEHICLE", "AN ANIMAL").</summary>
    public static string Article(SkinCategory category) => L.English
        ? (category is Animal or Thing or Absurd ? "AN" : "A")
        : (category is Food or Absurd ? "UMA" : "UM");

    public static Color Color(SkinCategory category) => category switch
    {
        Vehicle => new Color(255, 200, 40),
        Animal => new Color(120, 230, 130),
        Food => new Color(255, 140, 70),
        Thing => new Color(110, 190, 245),
        _ => new Color(215, 125, 235),
    };
}

/// <summary>Uma aparência pro carro do jogador. <see cref="Id"/> é o que fica salvo (estável mesmo se a lista
/// for reordenada), <see cref="Name"/> é o que aparece no menu, <see cref="Paint"/> desenha o veículo virado
/// pra frente (+x) no espaço local de um <see cref="CarPainter"/> e <see cref="Requirement"/> diz o que é
/// preciso pra liberá-la (o estado bloqueado/desbloqueado em si fica no progresso salvo). <see cref="Difficulty"/>
/// só organiza o catálogo (não aparece na tela); uma skin <see cref="IsSecret"/> esconde nome, visual e requisito
/// até ser liberada. Só muda o visual — a física é a mesma.</summary>
public sealed record CarSkin(
    string Id,
    string Name,
    SkinCategory Category,
    Action<CarPainter> Paint,
    UnlockRequirement Requirement,
    Difficulty Difficulty = Difficulty.Easy,
    bool IsSecret = false) : IUnlockable
{
    private readonly string _name = Name;

    /// <summary>Nome no idioma atual (o catálogo guarda o português; ver <see cref="L.Tr"/>).</summary>
    public string Name { get => L.Tr(_name); init => _name = value; }
}

/// <summary>
/// Catálogo de skins, na ordem do seletor (das mais fáceis às mais raras). Pra adicionar uma nova: escreva um
/// método <c>Paint*(CarPainter p)</c> desenhando o veículo mais ou menos dentro de x ∈ [-0.7, 0.7] e
/// y ∈ [-0.45, 0.45] (o tamanho do carro de corrida padrão), e acrescente uma linha em <see cref="All"/> com a
/// categoria, o requisito (atalhos em <see cref="Unlock"/>) e a dificuldade. Nomes em maiúsculas e sem acento
/// (a fonte pixelizada não tem).
/// <para>Curva da coleção: poucas fáceis (saem nas primeiras partidas), um meio que pede dedicação e alguma
/// habilidade, muitas difíceis/muito difíceis que pedem domínio, combinações e exploração das pistas, e poucos
/// troféus raros — alguns secretos, achados só por quem fuça o jogo.</para>
/// </summary>
public static partial class CarSkins
{
    private static readonly Color Wheel = new(24, 24, 27);

    private const Difficulty Medium = Difficulty.Medium;
    private const Difficulty Hard = Difficulty.Hard;
    private const Difficulty VeryHard = Difficulty.VeryHard;
    private const Difficulty Rare = Difficulty.Rare;

    public static IReadOnlyList<CarSkin> All { get; } =
    [
        new("padrao", "CARRO DE CORRIDA", Vehicle, PaintRaceCar, Unlock.FromStart),

        // ----- Fáceis: saem naturalmente nas primeiras partidas -----
        new("pato", "PATO", Animal, PaintDuck, Unlock.GamesPlayed(3)),
        new("galinha", "GALINHA", Animal, PaintChicken, Unlock.DeathRaceWins(1)),
        new("banana", "BANANA", Food, PaintBanana, Unlock.TimeAttackScore(400)),
        new("melancia", "MELANCIA", Food, PaintWatermelon, Unlock.TracksPlayed(2)),
        new("caixa", "CAIXA DE PAPELAO", Thing, PaintCardboardBox, Unlock.RoundsSurvived(15)),
        new("cubo_gelo", "CUBO DE GELO", Thing, PaintIceCube, Unlock.Standstill(5, "CONGELE: FIQUE 5 S PARADO NUMA PARTIDA")),
        new("tijolo", "TIJOLO", Thing, PaintBrick, Unlock.AchievementEarned(Achievements.WhereAreTheBrakesId)),

        // ----- Médias: dedicação ou um pouco de habilidade -----
        new("ovelha", "OVELHA", Animal, PaintSheep, Unlock.SkinsUsed(5), Medium),
        new("jacare", "JACARE", Animal, PaintAlligator, Unlock.DeathRaceWins(5), Medium),
        new("peixe", "PEIXE", Animal, PaintFish, Unlock.TimeAttackScoreOnTrack("praia", 700), Medium),
        new("torradeira", "TORRADEIRA", Thing, PaintToaster, Unlock.TimeAttackScore(1000), Medium),
        new("rosquinha", "ROSQUINHA", Food, PaintDonut, Unlock.Stat("COMPLETE 5 VOLTAS NUMA PARTIDA DO RELOGIO", p => p.MostTimeAttackLaps, 5), Medium),
        new("carrinho", "CARRINHO DE MERCADO", Thing, PaintShoppingCart, Unlock.Stat("PASSE POR 300 CHECKPOINTS", p => p.TotalCheckpoints, 300), Medium),
        new("batata", "BATATA", Food, PaintPotato, Unlock.GamesPlayed(25), Medium),
        new("suco", "CAIXINHA DE SUCO", Food, PaintJuiceBox, Unlock.AchievementsUnlocked(15), Medium),
        new("hamburguer", "HAMBURGUER", Food, PaintBurger, Unlock.SameSkinGameStreak(10), Medium),
        new("chinelo", "CHINELO", Thing, PaintSlipper, Unlock.Stat("BATA NUM RIVAL LOGO NA LARGADA 5 VEZES", p => p.StartLineCrashes, 5), Medium),
        new("porco", "PORCO", Animal, PaintPig, Unlock.Stat("TERMINE 10 CORRIDAS MORTAIS NO PODIO (TOP 3)", p => p.EliminationPodiums, 10), Medium),
        new("skate", "SKATE", Vehicle, PaintSkateboard, Unlock.Stat("PASSE POR UM CHECKPOINT ANDANDO DE RE", p => p.ReverseCheckpoints, 1), Medium),
        new("cone", "CONE", Thing, PaintCone, Unlock.Stat("ANDE 5 S SEGUIDOS NA CONTRAMAO", p => p.LongestWrongWaySeconds, 5), Medium),
        new("pedra", "PEDRA DE ESTIMACAO", Absurd, PaintPetRock, Unlock.Standstill(15), Medium),
        new("pizza", "PIZZA", Food, PaintPizza, Unlock.All("5 VITORIAS NA MORTAL E 3000 PTS NO RELOGIO", Unlock.DeathRaceWins(5), Unlock.TimeAttackTotal(3000)), Medium),

        // ----- Difíceis: domínio de um modo, pistas específicas e combinações -----
        new("tubarao", "TUBARAO", Animal, PaintShark, Unlock.All("VENCA NA PRAIA E FACA 900 PTS NELA", Unlock.DeathRaceWinsOnTrack("praia"), Unlock.TimeAttackScoreOnTrack("praia", 900)), Hard),
        new("dino", "DINOSSAURO", Animal, PaintDinosaur, Unlock.DeathRaceWinStreak(3), Hard),
        new("coxinha", "COXINHA", Food, PaintCoxinha, Unlock.Stat("VENCA 3 CORRIDAS MORTAIS DE VIRADA", p => p.EliminationComebackWins, 3), Hard),
        new("helicoptero", "HELICOPTERO", Vehicle, PaintHelicopter, Unlock.TimeAttackScore(1500), Hard),
        new("maquina_lavar", "MAQUINA DE LAVAR", Thing, PaintWashingMachine, Unlock.Stat("VENCA A MORTAL BATENDO 10 VEZES OU MAIS", p => p.MostCollisionsInAWin, 10), Hard),
        new("polvo", "POLVO", Animal, PaintOctopus, Unlock.WinsWithCategory(Animal, 3), Hard),
        new("cachorro_quente", "CACHORRO-QUENTE", Food, PaintHotDog, Unlock.WinsWithCategory(Food, 3), Hard),
        new("ovni", "OVNI", Vehicle, PaintUfo, Unlock.All("10 SKINS E 15000 PTS NO RELOGIO", Unlock.SkinsUnlocked(10), Unlock.TimeAttackTotal(15000)), Hard),
        new("sofa", "SOFA", Thing, PaintSofa, Unlock.DaysPlayed(5), Hard),
        new("trator", "TRATOR", Vehicle, PaintTractor, Unlock.TracksWon(5), Hard),
        new("ovo_frito", "OVO FRITO", Food, PaintFriedEgg, Unlock.ScoreWithSkin("torradeira", 1000), Hard),
        new("aspirador", "ASPIRADOR ROBO", Thing, PaintRobotVacuum, Unlock.Stat("COMPLETE 10 VOLTAS NUMA PARTIDA DO RELOGIO", p => p.MostTimeAttackLaps, 10), Hard),
        new("pneu", "PNEU", Thing, PaintTire, Unlock.All("ACHE O PNEU DE OURO E FACA UMA VOLTA EM MENOS DE 9 S NO AUTODROMO", Unlock.TrackSecretFound("autodromo"), Unlock.LapUnderOnTrack("autodromo", 9f)), Hard),
        new("fusca", "FUSCA", Vehicle, PaintBeetle, Unlock.WinsWithSkin("padrao", 5), Hard),
        new("bate_bate", "CARRINHO DE BATE-BATE", Vehicle, PaintBumperCar, Unlock.All("BATA 300 VEZES E VENCA NO PARQUE DE DIVERSOES", Unlock.Stat("BATA 300 VEZES NO TOTAL", p => p.TotalCollisions, 300), Unlock.DeathRaceWinsOnTrack("parque")), Hard),
        new("obstaculo", "O OBSTACULO", Absurd, PaintHazardBall, Unlock.Stat("BATA 50 VEZES NOS OBSTACULOS DO RELOGIO", p => p.TimeAttackHazardHits, 50), Hard),
        new("disquete", "DISQUETE", Thing, PaintFloppy, Unlock.Stat("SUPERE SEU RECORDE DO RELOGIO 10 VEZES", p => p.RecordsBeaten, 10), Hard),
        new("lampada", "LAMPADA", Thing, PaintLightBulb, Unlock.SecretAchievements(5), Hard),
        new("pinguim", "PINGUIM", Animal, PaintPenguin, Unlock.All("ACHE O PINGUIM PERDIDO E VENCA NA NEVE", Unlock.TrackSecretFound("neve"), Unlock.DeathRaceWinsOnTrack("neve")), Hard),
        new("formiga", "FORMIGA", Animal, PaintAnt, Unlock.All("ACHE A FORMIGA E FACA 800 PTS NA MESA DA COZINHA", Unlock.TrackSecretFound("mesa"), Unlock.TimeAttackScoreOnTrack("mesa", 800)), Hard),
        new("meia", "MEIA PERDIDA", Thing, PaintSock, Unlock.All("ACHE A MEIA E VENCA NO QUARTO DE CRIANCA", Unlock.TrackSecretFound("quarto"), Unlock.DeathRaceWinsOnTrack("quarto")), Hard),
        new("bola", "BOLA DE FUTEBOL", Thing, PaintSoccerBall, Unlock.DeathRaceWinsOnTrack("estadio", 3), Hard),

        // ----- Muito difíceis: feitos de habilidade, exploração e combinações longas -----
        new("tartaruga", "TARTARUGA", Animal, PaintTurtle, Unlock.Stat("VENCA A MORTAL SEM USAR O TURBO", p => p.EliminationNoBoostWins, 1), VeryHard),
        new("abelha", "ABELHA", Animal, PaintBee, Unlock.Stat("PASSE POR 18 CHECKPOINTS SEGUIDOS SEM BATER", p => p.BestCleanCheckpointStreak, 18), VeryHard),
        new("capivara", "CAPIVARA", Animal, PaintCapybara, Unlock.CleanTracks(5), VeryHard),
        new("privada", "VASO SANITARIO", Thing, PaintToilet, Unlock.DeathRaceWinStreak(5), VeryHard),
        new("geladeira", "GELADEIRA", Thing, PaintFridge, Unlock.Stat("FACA 1000 PTS NO RELOGIO SEM BATER EM NADA", p => p.BestCleanTimeAttackScore, 1000), VeryHard),
        new("banheira", "BANHEIRA", Thing, PaintBathtub, Unlock.WinsWithSkinOnTracks("pato", 3), VeryHard),
        new("onibus", "ONIBUS ESCOLAR", Vehicle, PaintSchoolBus, Unlock.Stat("VENCA A MORTAL SEM SAIR DO 1º LUGAR", p => p.EliminationWireToWireWins, 1), VeryHard),
        new("tanque", "TANQUE", Vehicle, PaintTank, Unlock.Stat("VENCA DEPOIS DE ESTAR EM ULTIMO COM SO 3 CARROS NA PISTA", p => p.EliminationClutchWins, 1), VeryHard),
        new("vaca", "VACA", Animal, PaintCow, Unlock.TimeAttackScore(2000), VeryHard),
        new("sushi", "SUSHI", Food, PaintSushi, Unlock.LapUnder(8.2f), VeryHard),
        new("baleia", "BALEIA", Animal, PaintWhale, Unlock.All("ACUMULE 30000 PTS NO RELOGIO E FACA 1500 NO FUNDO DO MAR", Unlock.TimeAttackTotal(30000), Unlock.TimeAttackScoreOnTrack("fundo_do_mar", 1500)), VeryHard),
        new("sapo", "SAPO", Animal, PaintFrog, Unlock.Stat("VENCA A MORTAL SEM BATER EM NADA", p => p.EliminationCleanWins, 1), VeryHard),
        new("foguete", "FOGUETE", Vehicle, PaintRocket, Unlock.TrackSecretsFound(10), VeryHard),
        new("siri", "SIRI", Animal, PaintCrab, Unlock.All("ACHE O SIRI E FACA 1200 PTS NA PRAIA", Unlock.TrackSecretFound("praia"), Unlock.TimeAttackScoreOnTrack("praia", 1200)), VeryHard),
        new("gato", "GATO", Animal, PaintCat, Unlock.All("ACHE O GATO E FACA UMA VOLTA EM MENOS DE 8.8 S NA CIDADE A NOITE", Unlock.TrackSecretFound("cidade_noite"), Unlock.LapUnderOnTrack("cidade_noite", 8.8f)), VeryHard),
        new("dragao", "DRAGAO", Animal, PaintDragon, Unlock.All("ACHE O OVO DE DRAGAO E VENCA 3 VEZES NO VULCAO", Unlock.TrackSecretFound("vulcao"), Unlock.DeathRaceWinsOnTrack("vulcao", 3)), VeryHard),
        new("robo", "ROBO", Thing, PaintRobot, Unlock.All("ACHE O ROBO E FACA 1500 PTS NA CIDADE NEON", Unlock.TrackSecretFound("cidade_neon"), Unlock.TimeAttackScoreOnTrack("cidade_neon", 1500)), VeryHard),
        new("bombeiro", "CAMINHAO DE BOMBEIRO", Vehicle, PaintFireTruck, Unlock.All("VENCA NO VULCAO E TERMINE UMA PARTIDA LA SEM BATER", Unlock.DeathRaceWinsOnTrack("vulcao"), Unlock.CleanOnTrack("vulcao")), VeryHard),
        new("submarino", "SUBMARINO", Vehicle, PaintSubmarine, Unlock.All("ACHE O SEGREDO DO FUNDO DO MAR E VENCA 2 VEZES LA", Unlock.TrackSecretFound("fundo_do_mar"), Unlock.DeathRaceWinsOnTrack("fundo_do_mar", 2)), VeryHard),
        new("boia", "BOIA DE FLAMINGO", Thing, PaintFlamingoFloat, Unlock.WonWithEachSkin("VENCA COM O PATO, O PEIXE, O TUBARAO E O JACARE", "pato", "peixe", "tubarao", "jacare"), VeryHard),
        new("aviao_papel", "AVIAO DE PAPEL", Thing, PaintPaperPlane, Unlock.TimeAttackScoreOnTrack("caderno", 1500), VeryHard),
        new("tapete", "TAPETE VOADOR", Absurd, PaintMagicCarpet, Unlock.All("JOGUE EM 15 PISTAS E VENCA EM 8 DELAS", Unlock.TracksPlayed(15), Unlock.TracksWon(8)), VeryHard),
        new("saturno", "SATURNO", Absurd, PaintSaturn, Unlock.All("VENCA NO ESPACO E NO PLANETA ALIENIGENA", Unlock.DeathRaceWinsOnTrack("espaco"), Unlock.DeathRaceWinsOnTrack("alienigena")), VeryHard),

        // ----- Troféus: o topo da coleção -----
        new("buraco_negro", "BURACO NEGRO", Absurd, PaintBlackHole, Unlock.All(
            "30 SKINS, 5 CONQUISTAS MUITO DIFICEIS, 15 VITORIAS NA MORTAL E 20000 PTS NO RELOGIO",
            Unlock.SkinsUnlocked(30),
            Unlock.AchievementsOfDifficulty(Difficulty.VeryHard, 5),
            Unlock.DeathRaceWins(15),
            Unlock.TimeAttackTotal(20000)), Rare),
        new("trofeu", "TROFEU", Absurd, PaintTrophy, Unlock.AchievementsUnlocked(150), Rare),
        new("mascote", "GALINHA DE KART", Vehicle, PaintKartChicken, Unlock.AllOtherSkins(), Rare),

        // Secretas: nome, visual e requisito ficam escondidos até serem liberadas.
        new("ursinho", "URSINHO", Animal, PaintTeddyBear, Unlock.AchievementEarned(Achievements.WasThatSupposedToHappenId), Rare, IsSecret: true),
        new("fantasma", "FANTASMA", Absurd, PaintGhost, Unlock.Stat("JOGUE 3 PARTIDAS DE MADRUGADA (0H AS 5H)", p => p.NightOwlGames, 3), Rare, IsSecret: true),
        new("pixel", "PIXEL PERDIDO", Absurd, PaintLostPixel, Unlock.Discovered(Discovery.Konami, "DIGITE O CODIGO SECRETO NO MENU"), Rare, IsSecret: true),
    ];

    /// <summary>O carro de corrida padrão — o dos rivais e o inicial do jogador.</summary>
    public static CarSkin Default => All[0];

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

    public static CarSkin Find(string id) => All.FirstOrDefault(skin => skin.Id == id);
}
