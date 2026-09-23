using static Kyrios.Game.AchievementIcons;

namespace Kyrios.Game;

/// <summary>
/// Catálogo de conquistas, na ordem em que aparecem na página (agrupadas por categoria). Pra criar uma nova
/// basta uma linha em <see cref="All"/>: id estável (é o que fica salvo), nome, descrição, categoria,
/// dificuldade, condição (atalhos em <see cref="Unlock"/>) e ícone (<see cref="AchievementIcons"/>) — sem
/// código específico em lugar nenhum. Se a condição precisar de uma estatística que ainda não existe, ela entra
/// no <see cref="SaveData"/> e é atualizada em <see cref="Progression.RecordRace"/>.
/// Textos em maiúsculas e sem acento (limite da fonte pixelizada).
/// </summary>
public static class Achievements
{
    // Ids usados como requisito de skins (ver CarSkins).
    public const string WhereAreTheBrakesId = "onde_estao_os_freios";
    public const string PredatorId = "predador";
    public const string WasThatSupposedToHappenId = "era_para_isso";

    private const AchievementCategory Races = AchievementCategory.Races;
    private const AchievementCategory DeathRace = AchievementCategory.DeathRace;
    private const AchievementCategory TimeAttack = AchievementCategory.TimeAttack;
    private const AchievementCategory Records = AchievementCategory.Records;
    private const AchievementCategory Skins = AchievementCategory.Skins;
    private const AchievementCategory Playing = AchievementCategory.Playing;
    private const AchievementCategory Absurd = AchievementCategory.Absurd;

    private const Difficulty Easy = Difficulty.Easy;
    private const Difficulty Medium = Difficulty.Medium;
    private const Difficulty Hard = Difficulty.Hard;

    public static IReadOnlyList<Achievement> All { get; } =
    [
        // ----- Corridas -----
        new("primeiros_passos", "PRIMEIROS PASSOS", "TERMINE SUA PRIMEIRA CORRIDA CLASSICA", Races, Easy,
            Unlock.ClassicRaces(1), Art(Flag)),
        new("primeira_vitoria", "PRIMEIRA VITORIA", "VENCA SUA PRIMEIRA CORRIDA (CLASSICA OU MORTAL)", Races, Easy,
            Unlock.Wins(1), Art(Trophy)),
        new("piloto", "PILOTO", "VENCA 10 CORRIDAS (CLASSICA OU MORTAL)", Races, Medium,
            Unlock.Wins(10), Art(Trophy, "10")),
        new("de_virada", "DE VIRADA", "VENCA UMA CORRIDA CLASSICA DEPOIS DE JA TER ESTADO EM ULTIMO", Races, Medium,
            Unlock.Stat("VENCA DEPOIS DE ESTAR EM ULTIMO", p => p.SprintComebackWins, 1), Art(ArrowUp)),
        new("veterano", "VETERANO", "VENCA 30 CORRIDAS (CLASSICA OU MORTAL)", Races, Hard,
            Unlock.Wins(30), Art(Recolor(Trophy, "YL", "DE"), "30")),
        new("lenda_das_pistas", "LENDA DAS PISTAS", "VENCA 100 CORRIDAS (CLASSICA OU MORTAL)", Races, Hard,
            Unlock.Wins(100), Art(Recolor(Trophy, "YP", "Db"), "100")),

        // ----- Corrida Mortal -----
        new("sobrevivente", "SOBREVIVENTE", "SOBREVIVA A UMA ELIMINACAO NA CORRIDA MORTAL", DeathRace, Easy,
            Unlock.Stat("SOBREVIVA A 1 ELIMINACAO", p => p.EliminationRoundsSurvived, 1), Art(Shield)),
        new("ultimo_de_pe", "ULTIMO DE PE", "VENCA SUA PRIMEIRA CORRIDA MORTAL", DeathRace, Medium,
            Unlock.DeathRaceWins(1), Art(Crown)),
        new("em_sequencia", "EM SEQUENCIA", "VENCA 3 CORRIDAS MORTAIS SEGUIDAS", DeathRace, Medium,
            Unlock.DeathRaceWinStreak(3), Art(Fire, "3")),
        new("implacavel", "IMPLACAVEL", "VENCA 10 CORRIDAS MORTAIS", DeathRace, Medium,
            Unlock.DeathRaceWins(10), Art(Crown, "10")),
        new("sobrevivente_nato", "SOBREVIVENTE NATO", "SOBREVIVA A 100 ELIMINACOES NA CORRIDA MORTAL", DeathRace, Medium,
            Unlock.Stat("SOBREVIVA A 100 ELIMINACOES", p => p.EliminationRoundsSurvived, 100), Art(Shield, "100")),
        new("so_pode_sobrar_um", "SO PODE SOBRAR UM", "VENCA UMA CORRIDA MORTAL SEM FICAR EM ULTIMO NENHUMA VEZ", DeathRace, Hard,
            Unlock.Stat("VENCA SEM NUNCA FICAR EM ULTIMO", p => p.EliminationFlawlessWins, 1), Art(Recolor(Crown, "YC", "Db", "RW"))),
        new(PredatorId, "PREDADOR", "VENCA 25 CORRIDAS MORTAIS", DeathRace, Hard,
            Unlock.DeathRaceWins(25), Art(Skull, "25")),

        // ----- Contra o Relógio -----
        new("contra_o_tempo", "CONTRA O TEMPO", "JOGUE SUA PRIMEIRA PARTIDA DO CONTRA O RELOGIO", TimeAttack, Easy,
            Unlock.TimeAttackRaces(1), Art(Stopwatch)),
        new("cada_segundo_conta", "CADA SEGUNDO CONTA", "FACA 1000 PTS NUMA UNICA PARTIDA", TimeAttack, Medium,
            Unlock.TimeAttackScore(1000), Art(Stopwatch, "1K")),
        new("sem_tempo_a_perder", "SEM TEMPO A PERDER", "ACUMULE 5000 PTS NO CONTRA O RELOGIO", TimeAttack, Medium,
            Unlock.TimeAttackTotal(5000), Art(Hourglass, "5K")),
        new("intocavel", "INTOCAVEL", "FACA 500 PTS NUMA PARTIDA SEM BATER EM NADA", TimeAttack, Medium,
            Unlock.Stat("500 PTS SEM BATER", p => p.BestCleanTimeAttackScore, 500), Art(Diamond)),
        new("o_tempo_nao_para", "O TEMPO NAO PARA", "ACUMULE 10000 PTS NO CONTRA O RELOGIO", TimeAttack, Hard,
            Unlock.TimeAttackTotal(10000), Art(Recolor(Hourglass, "YR"), "10K")),
        new("contra_todos_os_limites", "CONTRA TODOS OS LIMITES", "FACA 2000 PTS NUMA UNICA PARTIDA", TimeAttack, Hard,
            Unlock.TimeAttackScore(2000), Art(Recolor(Stopwatch, "EY", "LD"), "2K")),

        // ----- Recordes -----
        new("recordista", "RECORDISTA", "ESTABELECA SEU PRIMEIRO RECORDE PESSOAL", Records, Easy,
            Unlock.Stat("1 RECORDE PESSOAL", p => p.RecordsSet, 1), Art(Star)),
        new("nao_consigo_parar", "NAO CONSIGO PARAR", "SUPERE UM RECORDE SEU 5 VEZES", Records, Medium,
            Unlock.Stat("SUPERE 5 RECORDES", p => p.RecordsBeaten, 5), Art(Star, "5")),
        new("volta_rapida", "VOLTA RAPIDA", "FACA UMA VOLTA NA CLASSICA EM MENOS DE 9.5 S", Records, Medium,
            Unlock.ClassicLapUnder(9.5f), Art(Lightning)),
        new("velocista", "VELOCISTA", "TERMINE A CORRIDA CLASSICA EM MENOS DE 31 S", Records, Medium,
            Unlock.ClassicRaceUnder(31f), Art(Speedometer)),
        new("fora_da_curva", "FORA DA CURVA", "TERMINE A CORRIDA CLASSICA EM MENOS DE 28 S", Records, Hard,
            Unlock.ClassicRaceUnder(28f), Art(Recolor(Lightning, "YC", "DB"))),

        // ----- Skins -----
        new("novo_visual", "NOVO VISUAL", "DESBLOQUEIE SUA PRIMEIRA SKIN", Skins, Easy,
            Unlock.SkinsUnlocked(1), Art(PaintPalette)),
        new("quack", "QUACK!", "DESBLOQUEIE O PATO", Skins, Easy,
            Unlock.SkinEarned("pato"), Skin("pato")),
        new("colecionador", "COLECIONADOR", "DESBLOQUEIE 5 SKINS", Skins, Medium,
            Unlock.SkinsUnlocked(5), Art(PaintPalette, "5")),
        new("colecionador_serio", "COLECIONADOR SERIO", "DESBLOQUEIE 10 SKINS", Skins, Medium,
            Unlock.SkinsUnlocked(10), Art(PaintPalette, "10")),
        new("obcecado", "OBCECADO", "DESBLOQUEIE 15 SKINS", Skins, Hard,
            Unlock.SkinsUnlocked(15), Art(PaintPalette, "15")),
        new("colecao_completa", "COLECAO COMPLETA", "DESBLOQUEIE TODAS AS SKINS", Skins, Hard,
            Unlock.AllSkins(), Art(Chest)),

        // ----- Jogando -----
        new("primeira_partida", "PRIMEIRA PARTIDA", "TERMINE SUA PRIMEIRA PARTIDA EM QUALQUER MODO", Playing, Easy,
            Unlock.GamesPlayed(1), Art(Gamepad)),
        new("turista", "TURISTA", "TERMINE UMA PARTIDA EM CADA UM DOS 3 MODOS", Playing, Easy,
            Unlock.All("UMA PARTIDA EM CADA MODO", Unlock.ClassicRaces(1), Unlock.Stat("1 CORRIDA MORTAL", p => p.EliminationRaces, 1), Unlock.TimeAttackRaces(1)),
            Art(Compass)),
        new("fregues", "FREGUES", "JOGUE 10 PARTIDAS", Playing, Easy,
            Unlock.GamesPlayed(10), Art(Gamepad, "10")),
        new("turbinado", "TURBINADO", "USE O TURBO POR 60 SEGUNDOS NO TOTAL", Playing, Easy,
            Unlock.Stat("60 S DE TURBO", p => p.TotalBoostSeconds, 60), Art(Turbo)),
        new("maratonista", "MARATONISTA", "PASSE 30 MINUTOS CORRENDO NO TOTAL", Playing, Medium,
            Unlock.Stat("30 MINUTOS CORRENDO", p => p.TotalRaceSeconds / 60f, 30), Art(Television)),
        new("voce_ainda_esta_aqui", "VOCE AINDA ESTA AQUI?", "JOGUE 100 PARTIDAS", Playing, Medium,
            Unlock.GamesPlayed(100), Art(Gamepad, "100")),
        new("cacador_de_conquistas", "CACADOR DE CONQUISTAS", "DESBLOQUEIE 25 CONQUISTAS", Playing, Medium,
            Unlock.AchievementsUnlocked(25), Art(Medal)),
        new("morador_das_pistas", "MORADOR DAS PISTAS", "JOGUE 500 PARTIDAS", Playing, Hard,
            Unlock.GamesPlayed(500), Art(House)),
        new("platina", "PLATINA", "DESBLOQUEIE TODAS AS OUTRAS CONQUISTAS", Playing, Hard,
            Unlock.AllOtherAchievements(), Art(Recolor(Trophy, "YC", "DB", "WW"))),

        // ----- Absurdas -----
        new("isso_e_uma_galinha", "ISSO E UMA GALINHA?", "DESBLOQUEIE A GALINHA", Absurd, Easy,
            Unlock.SkinEarned("galinha"), Skin("galinha")),
        new(WhereAreTheBrakesId, "ONDE ESTAO OS FREIOS?", "BATA 10 VEZES NUMA MESMA CORRIDA", Absurd, Easy,
            Unlock.Stat("10 BATIDAS NUMA CORRIDA", p => p.MostCollisionsInOneRace, 10), Art(Explosion)),
        new("lanterninha", "LANTERNINHA", "TERMINE UMA CORRIDA CLASSICA EM ULTIMO LUGAR", Absurd, Easy,
            Unlock.Stat("TERMINE EM ULTIMO", p => p.SprintLastPlaces, 1), Art(Lantern)),
        new("eu_tinha_um_plano", "EU TINHA UM PLANO", "SEJA O PRIMEIRO ELIMINADO DA CORRIDA MORTAL", Absurd, Easy,
            Unlock.Stat("PRIMEIRO ELIMINADO", p => p.EliminationFirstOuts, 1), Art(Bomb), IsSecret: true),
        new("parado_no_transito", "PARADO NO TRANSITO", "FIQUE 10 SEGUNDOS PARADO NUMA CORRIDA", Absurd, Easy,
            Unlock.Stat("10 S PARADO", p => p.LongestStandstillSeconds, 10), Art(Cone), IsSecret: true),
        new(WasThatSupposedToHappenId, "ERA PARA ISSO ACONTECER?", "TERMINE O CONTRA O RELOGIO COM 0 PONTOS", Absurd, Easy,
            Unlock.Stat("0 PONTOS", p => p.TimeAttackZeroScores, 1), Art(Bug), IsSecret: true),
        new("quack_quack", "QUACK QUACK", "VENCA UMA CORRIDA USANDO O PATO", Absurd, Medium,
            Unlock.WinsWithSkin("pato", "VENCA COM O PATO"), Skin("pato", "1º")),
        new("problema_seu", "PROBLEMA SEU", "VENCA UMA CORRIDA MORTAL USANDO O JACARE", Absurd, Medium,
            Unlock.DeathRaceWinsWithSkin("jacare", "VENCA A MORTAL COM O JACARE"), Skin("jacare", "1º")),
        new("batata_veloz", "BATATA VELOZ", "BATA UM RECORDE PESSOAL USANDO A BATATA", Absurd, Medium,
            Unlock.RecordsWithSkin("batata", "RECORDE COM A BATATA"), Skin("batata", "REC")),
        new("quase", "QUASE!", "CAIA NA ULTIMA ELIMINACAO DA CORRIDA MORTAL", Absurd, Medium,
            Unlock.Stat("2º LUGAR NA MORTAL", p => p.EliminationRunnerUps, 1), Art(BrokenHeart)),
        new("colecionador_de_amassados", "COLECIONADOR DE AMASSADOS", "BATA 200 VEZES NO TOTAL", Absurd, Medium,
            Unlock.Stat("200 BATIDAS", p => p.TotalCollisions, 200), Art(Wrench, "200")),
        new("eu_tenho_tempo", "EU TENHO TEMPO!", "PASSE NUM CHECKPOINT COM MENOS DE 1 S NO RELOGIO", Absurd, Medium,
            Unlock.Stat("CHECKPOINT NO ULTIMO SEGUNDO", p => p.TimeAttackClutchCheckpoints, 1), Art(Recolor(Stopwatch, "ER", "WY")), IsSecret: true),
        new("isso_conta", "ISSO CONTA?", "CRUZE A CHEGADA DA CORRIDA CLASSICA DE RE", Absurd, Medium,
            Unlock.Stat("CHEGADA DE RE", p => p.SprintReverseFinishes, 1), Art(Reverse), IsSecret: true),
        new("por_que", "POR QUE?", "DESBLOQUEIE O VASO SANITARIO", Absurd, Hard,
            Unlock.SkinEarned("privada"), Skin("privada")),
        new("decisao_questionavel", "DECISAO QUESTIONAVEL", "VENCA UMA CORRIDA USANDO O VASO SANITARIO", Absurd, Hard,
            Unlock.WinsWithSkin("privada", "VENCA COM O VASO SANITARIO"), Skin("privada", "1º"), IsSecret: true),
        new("nao_somos_mais_os_unicos", "NAO SOMOS MAIS OS UNICOS", "DESBLOQUEIE O OVNI", Absurd, Hard,
            Unlock.SkinEarned("ovni"), Skin("ovni"), IsSecret: true),
    ];

    public static Achievement Find(string id) => All.FirstOrDefault(achievement => achievement.Id == id);

    public static bool IsUnlocked(Achievement achievement, SaveData progress) =>
        progress.UnlockedAchievementIds.Contains(achievement.Id);

    public static int UnlockedCount(SaveData progress) => All.Count(achievement => IsUnlocked(achievement, progress));

    /// <summary>Libera toda conquista ainda bloqueada cuja condição já foi cumprida (anotando no progresso) e
    /// devolve quais foram, na ordem do catálogo. Quem repete até estabilizar, junto com as skins, é
    /// <see cref="Progression.CheckUnlocks"/>.</summary>
    public static List<Achievement> UnlockNewlyEarned(SaveData progress, IReadOnlyList<Achievement> catalog = null)
    {
        catalog ??= All;
        var earned = new List<Achievement>();
        foreach (Achievement achievement in catalog)
        {
            if (!progress.UnlockedAchievementIds.Contains(achievement.Id) && achievement.Condition.IsMet(progress))
            {
                progress.UnlockedAchievementIds.Add(achievement.Id);
                earned.Add(achievement);
            }
        }

        return earned;
    }
}
