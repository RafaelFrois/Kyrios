using static Kyrios.Game.AchievementIcons;

namespace Kyrios.Game;

/// <summary>
/// Catálogo de conquistas, na ordem em que aparecem na página (agrupadas por categoria). Pra criar uma nova
/// basta uma linha em <see cref="Handmade"/>: id estável (é o que fica salvo), nome, descrição, categoria,
/// dificuldade, condição (atalhos em <see cref="Unlock"/>) e ícone (<see cref="AchievementIcons"/>) — sem
/// código específico em lugar nenhum. Se a condição precisar de uma estatística que ainda não existe, ela entra
/// no <see cref="SaveData"/> e é atualizada em <see cref="Progression.RecordRace"/>. A conquista secreta de cada
/// pista é gerada a partir de <see cref="TrackThemes"/>, então uma pista nova já vem com a dela.
/// Textos em maiúsculas e sem acento (limite da fonte pixelizada).
/// </summary>
public static class Achievements
{
    // Ids usados como requisito de skins/pistas.
    public const string WhereAreTheBrakesId = "onde_estao_os_freios";
    public const string PredatorId = "predador";
    public const string WasThatSupposedToHappenId = "era_para_isso";

    private const AchievementCategory DeathRace = AchievementCategory.DeathRace;
    private const AchievementCategory TimeAttack = AchievementCategory.TimeAttack;
    private const AchievementCategory Tracks = AchievementCategory.Tracks;
    private const AchievementCategory Skins = AchievementCategory.Skins;
    private const AchievementCategory General = AchievementCategory.General;
    private const AchievementCategory Silly = AchievementCategory.Silly;

    private const Difficulty Easy = Difficulty.Easy;
    private const Difficulty Medium = Difficulty.Medium;
    private const Difficulty Hard = Difficulty.Hard;
    private const Difficulty Rare = Difficulty.Rare;

    private static readonly Achievement[] Handmade =
    [
        // ----- Corrida Mortal -----
        new("sobrevivente", "SOBREVIVENTE", "SOBREVIVA A UMA ELIMINACAO NA CORRIDA MORTAL", DeathRace, Easy,
            Unlock.RoundsSurvived(1), Art(Shield)),
        new("ultimo_de_pe", "ULTIMO DE PE", "VENCA SUA PRIMEIRA CORRIDA MORTAL", DeathRace, Easy,
            Unlock.DeathRaceWins(1), Art(Crown)),
        new("veterano", "VETERANO", "JOGUE 30 CORRIDAS MORTAIS", DeathRace, Medium,
            Unlock.DeathRaces(30), Art(Recolor(Shield, "BR"), "30")),
        new("em_sequencia", "EM SEQUENCIA", "VENCA 3 CORRIDAS MORTAIS SEGUIDAS", DeathRace, Medium,
            Unlock.DeathRaceWinStreak(3), Art(Fire, "3")),
        new("implacavel", "IMPLACAVEL", "VENCA 10 CORRIDAS MORTAIS", DeathRace, Medium,
            Unlock.DeathRaceWins(10), Art(Crown, "10")),
        new("de_virada", "DE VIRADA", "VENCA DEPOIS DE JA TER ESTADO EM ULTIMO LUGAR", DeathRace, Medium,
            Unlock.Stat("VENCA DEPOIS DE ESTAR EM ULTIMO", p => p.EliminationComebackWins, 1), Art(ArrowUp)),
        new("so_pode_sobrar_um", "SO PODE SOBRAR UM", "VENCA SEM FICAR EM ULTIMO NENHUMA VEZ", DeathRace, Medium,
            Unlock.Stat("VENCA SEM NUNCA FICAR EM ULTIMO", p => p.EliminationFlawlessWins, 1), Art(Recolor(Crown, "YC", "Db", "RW"))),
        new("sobrevivente_nato", "SOBREVIVENTE NATO", "SOBREVIVA A 100 ELIMINACOES NA CORRIDA MORTAL", DeathRace, Hard,
            Unlock.RoundsSurvived(100), Art(Shield, "100")),
        new(PredatorId, "PREDADOR", "VENCA 25 CORRIDAS MORTAIS", DeathRace, Hard,
            Unlock.DeathRaceWins(25), Art(Skull, "25")),
        new("imbativel", "IMBATIVEL", "VENCA 5 CORRIDAS MORTAIS SEGUIDAS", DeathRace, Hard,
            Unlock.DeathRaceWinStreak(5), Art(Recolor(Fire, "RP", "OC", "YW"), "5")),
        new("lenda_da_mortal", "LENDA DA MORTAL", "VENCA 50 CORRIDAS MORTAIS", DeathRace, Hard,
            Unlock.DeathRaceWins(50), Art(Recolor(Trophy, "YP", "Db"), "50")),
        new("motor_original", "MOTOR ORIGINAL", "VENCA UMA CORRIDA MORTAL SEM USAR O TURBO", DeathRace, Rare,
            Unlock.Stat("VENCA SEM TURBO", p => p.EliminationNoBoostWins, 1), Art(Recolor(Turbo, "CL"))),
        new("lataria_intacta", "LATARIA INTACTA", "VENCA UMA CORRIDA MORTAL SEM BATER EM NADA", DeathRace, Rare,
            Unlock.Stat("VENCA SEM BATER", p => p.EliminationCleanWins, 1), Art(Recolor(Shield, "BY", "WD"))),
        new("invencivel", "INVENCIVEL", "VENCA 10 CORRIDAS MORTAIS SEGUIDAS", DeathRace, Rare,
            Unlock.DeathRaceWinStreak(10), Art(Recolor(Fire, "RP", "OP", "YW"), "10")),

        // ----- Contra o Relógio -----
        new("contra_o_tempo", "CONTRA O TEMPO", "JOGUE SUA PRIMEIRA PARTIDA DO CONTRA O RELOGIO", TimeAttack, Easy,
            Unlock.TimeAttackRaces(1), Art(Stopwatch)),
        new("aquecimento", "AQUECIMENTO", "FACA 300 PTS NUMA PARTIDA DO RELOGIO", TimeAttack, Easy,
            Unlock.TimeAttackScore(300), Art(Stopwatch, "300")),
        new("recordista", "RECORDISTA", "ESTABELECA SEU PRIMEIRO RECORDE NO RELOGIO", TimeAttack, Easy,
            Unlock.Stat("1 RECORDE PESSOAL", p => p.RecordsSet, 1), Art(Star)),
        new("cada_segundo_conta", "CADA SEGUNDO CONTA", "FACA 1000 PTS NUMA PARTIDA DO RELOGIO", TimeAttack, Medium,
            Unlock.TimeAttackScore(1000), Art(Stopwatch, "1K")),
        new("sem_tempo_a_perder", "SEM TEMPO A PERDER", "ACUMULE 5000 PTS NO CONTRA O RELOGIO", TimeAttack, Medium,
            Unlock.TimeAttackTotal(5000), Art(Hourglass, "5K")),
        new("nao_consigo_parar", "NAO CONSIGO PARAR", "SUPERE O SEU PROPRIO RECORDE 5 VEZES", TimeAttack, Medium,
            Unlock.Stat("SUPERE SEU RECORDE 5 VEZES", p => p.RecordsBeaten, 5), Art(Star, "5")),
        new("intocavel", "INTOCAVEL", "FACA 500 PTS SEM BATER EM NADA", TimeAttack, Medium,
            Unlock.Stat("500 PTS SEM BATER", p => p.BestCleanTimeAttackScore, 500), Art(Diamond)),
        new("mao_firme", "MAO FIRME", "PASSE POR 12 CHECKPOINTS SEGUIDOS SEM BATER", TimeAttack, Medium,
            Unlock.Stat("12 CHECKPOINTS SEM BATER", p => p.BestCleanCheckpointStreak, 12), Art(Speedometer)),
        new("tempo_de_sobra", "TEMPO DE SOBRA", "TENHA 30 S OU MAIS NO RELOGIO AO MESMO TEMPO", TimeAttack, Medium,
            Unlock.Stat("30 S NO RELOGIO", p => p.MostTimeBanked, 30), Art(Recolor(Hourglass, "YG"))),
        new("maratona", "MARATONA", "COMPLETE 10 VOLTAS NUMA UNICA PARTIDA DO RELOGIO", TimeAttack, Hard,
            Unlock.Stat("10 VOLTAS NUMA PARTIDA", p => p.MostTimeAttackLaps, 10), Art(Flag, "10")),
        new("contra_todos_os_limites", "CONTRA TODOS OS LIMITES", "FACA 2000 PTS NUMA PARTIDA DO RELOGIO", TimeAttack, Hard,
            Unlock.TimeAttackScore(2000), Art(Recolor(Stopwatch, "EY", "LD"), "2K")),
        new("o_tempo_nao_para", "O TEMPO NAO PARA", "ACUMULE 20000 PTS NO CONTRA O RELOGIO", TimeAttack, Hard,
            Unlock.TimeAttackTotal(20000), Art(Recolor(Hourglass, "YR"), "20K")),
        new("perfeicao", "PERFEICAO", "FACA 1000 PTS SEM BATER EM NADA", TimeAttack, Rare,
            Unlock.Stat("1000 PTS SEM BATER", p => p.BestCleanTimeAttackScore, 1000), Art(Recolor(Diamond, "CP", "BP", "WF"))),

        // ----- Pistas -----
        new("primeira_viagem", "PRIMEIRA VIAGEM", "JOGUE EM UMA PISTA DIFERENTE DO AUTODROMO", Tracks, Easy,
            Unlock.TracksPlayed(2), Art(Compass)),
        new("turista", "TURISTA", "JOGUE EM 5 PISTAS DIFERENTES", Tracks, Medium,
            Unlock.TracksPlayed(5), Art(Compass, "5")),
        new("agente_de_viagens", "AGENTE DE VIAGENS", "DESBLOQUEIE 5 PISTAS", Tracks, Medium,
            Unlock.TracksUnlocked(5), Art(Recolor(Compass, "RG", "BY"), "5")),
        new("conquistador", "CONQUISTADOR", "VENCA A CORRIDA MORTAL EM 3 PISTAS DIFERENTES", Tracks, Medium,
            Unlock.TracksWon(3), Art(Recolor(Flag, "WG"), "3")),
        new("explorador", "EXPLORADOR", "ENCONTRE UM SEGREDO ESCONDIDO EM ALGUMA PISTA", Tracks, Medium,
            Unlock.TrackSecretsFound(1), Art(Magnifier)),
        new("volta_ao_mundo", "VOLTA AO MUNDO", "JOGUE EM TODAS AS PISTAS", Tracks, Hard,
            Unlock.AllTracksPlayed(), Art(Globe)),
        new("passaporte_cheio", "PASSAPORTE CHEIO", "DESBLOQUEIE TODAS AS PISTAS", Tracks, Hard,
            Unlock.AllTracksUnlocked(), Art(Recolor(Globe, "GY"))),
        new("motorista_exemplar", "MOTORISTA EXEMPLAR", "TERMINE UMA PARTIDA SEM BATER EM 3 PISTAS DIFERENTES", Tracks, Hard,
            Unlock.CleanTracks(3), Art(Recolor(Shield, "BG"), "3")),
        new("cacador_de_segredos", "CACADOR DE SEGREDOS", "ENCONTRE 5 SEGREDOS ESCONDIDOS NAS PISTAS", Tracks, Hard,
            Unlock.TrackSecretsFound(5), Art(Magnifier, "5")),
        new("surfista", "SURFISTA", "FACA 800 PTS NO RELOGIO NA PRAIA", Tracks, Medium,
            Unlock.TimeAttackScoreOnTrack("praia", 800), Art(PalmTree)),
        new("patinacao_artistica", "PATINACAO ARTISTICA", "VENCA UMA CORRIDA MORTAL NA NEVE", Tracks, Medium,
            Unlock.DeathRaceWinsOnTrack("neve"), Art(Snowflake)),
        new("corredor_7", "CORREDOR 7", "VENCA NO SUPERMERCADO USANDO O CARRINHO DE MERCADO", Tracks, Medium,
            Unlock.WinWithSkinOnTrack("carrinho", "supermercado"), Skin("carrinho", "1º")),
        new("miragem", "MIRAGEM", "FACA 1000 PTS NO RELOGIO NO DESERTO", Tracks, Hard,
            Unlock.TimeAttackScoreOnTrack("deserto", 1000), Art(Cactus)),
        new("promocao_relampago", "PROMOCAO RELAMPAGO", "FACA 1000 PTS NO RELOGIO NO SUPERMERCADO", Tracks, Hard,
            Unlock.TimeAttackScoreOnTrack("supermercado", 1000), Art(Cart, "1K")),
        new("rei_do_vulcao", "REI DO VULCAO", "VENCA 3 CORRIDAS MORTAIS NO VULCAO", Tracks, Hard,
            Unlock.DeathRaceWinsOnTrack("vulcao", 3), Art(Volcano, "3")),
        new("luzes_da_cidade", "LUZES DA CIDADE", "VENCA NA CIDADE A NOITE E NA CIDADE NEON", Tracks, Hard,
            Unlock.All("VENCA NAS DUAS CIDADES", Unlock.DeathRaceWinsOnTrack("cidade_noite"), Unlock.DeathRaceWinsOnTrack("cidade_neon")), Art(NeonCity)),
        new("dono_do_mundo", "DONO DO MUNDO", "VENCA A CORRIDA MORTAL EM TODAS AS PISTAS", Tracks, Rare,
            Unlock.AllTracksWon(), Art(Recolor(Globe, "BY", "GD"))),
        new("olho_de_aguia", "OLHO DE AGUIA", "ENCONTRE O SEGREDO DE TODAS AS PISTAS", Tracks, Rare,
            Unlock.AllTrackSecretsFound(), Art(Recolor(Magnifier, "CY", "ED"))),
        new("cafe_da_manha", "CAFE DA MANHA", "VENCA NA MESA DA COZINHA USANDO A TORRADEIRA", Tracks, Rare,
            Unlock.WinWithSkinOnTrack("torradeira", "mesa"), Skin("torradeira", "1º")),
        new("hora_do_banho", "HORA DO BANHO", "VENCA NO QUARTO DE CRIANCA USANDO O PATO", Tracks, Rare,
            Unlock.WinWithSkinOnTrack("pato", "quarto"), Skin("pato", "1º")),
        new("peixe_fora_da_agua", "PEIXE FORA DA AGUA", "VENCA NO DESERTO USANDO O TUBARAO", Tracks, Rare,
            Unlock.WinWithSkinOnTrack("tubarao", "deserto"), Skin("tubarao", "1º")),
        new("jurassico", "JURASSICO", "VENCA NA FLORESTA USANDO O DINOSSAURO", Tracks, Rare,
            Unlock.WinWithSkinOnTrack("dino", "floresta"), Skin("dino", "1º")),

        // ----- Skins -----
        new("novo_visual", "NOVO VISUAL", "DESBLOQUEIE SUA PRIMEIRA SKIN", Skins, Easy,
            Unlock.SkinsUnlocked(1), Art(PaintPalette)),
        new("quack", "QUACK!", "DESBLOQUEIE O PATO", Skins, Easy,
            Unlock.SkinEarned("pato"), Skin("pato")),
        new("troca_troca", "TROCA-TROCA", "JOGUE COM 5 SKINS DIFERENTES", Skins, Easy,
            Unlock.SkinsUsed(5), Art(Recolor(PaintPalette, "NE"), "5")),
        new("colecionador", "COLECIONADOR", "DESBLOQUEIE 5 SKINS", Skins, Medium,
            Unlock.SkinsUnlocked(5), Art(PaintPalette, "5")),
        new("estilo_proprio", "ESTILO PROPRIO", "VENCA COM 5 SKINS DIFERENTES", Skins, Medium,
            Unlock.SkinsWonWith(5), Art(Recolor(Crown, "YF", "DP"), "5")),
        new("colecionador_serio", "COLECIONADOR SERIO", "DESBLOQUEIE 10 SKINS", Skins, Medium,
            Unlock.SkinsUnlocked(10), Art(PaintPalette, "10")),
        new("batata_veloz", "BATATA VELOZ", "BATA SEU RECORDE NO RELOGIO USANDO A BATATA", Skins, Medium,
            Unlock.RecordWithSkin("batata"), Skin("batata", "REC")),
        new("obcecado", "OBCECADO", "DESBLOQUEIE 15 SKINS", Skins, Hard,
            Unlock.SkinsUnlocked(15), Art(PaintPalette, "15")),
        new("guarda_roupa_lotado", "GUARDA-ROUPA LOTADO", "VENCA COM 10 SKINS DIFERENTES", Skins, Hard,
            Unlock.SkinsWonWith(10), Art(Recolor(Crown, "YF", "DP"), "10")),
        new("abduzido", "ABDUZIDO", "FACA 1500 PTS NO RELOGIO USANDO O OVNI", Skins, Hard,
            Unlock.ScoreWithSkin("ovni", 1500), Skin("ovni", "1K")),
        new("colecao_completa", "COLECAO COMPLETA", "DESBLOQUEIE TODAS AS SKINS", Skins, Rare,
            Unlock.AllSkins(), Art(Chest)),

        // ----- Geral -----
        new("primeira_partida", "PRIMEIRA PARTIDA", "TERMINE SUA PRIMEIRA PARTIDA", General, Easy,
            Unlock.GamesPlayed(1), Art(Gamepad)),
        new("experimente_tudo", "EXPERIMENTE TUDO", "TERMINE UMA PARTIDA EM CADA MODO", General, Easy,
            Unlock.All("UMA PARTIDA EM CADA MODO", Unlock.DeathRaces(1), Unlock.TimeAttackRaces(1)), Art(Recolor(Gamepad, "LC"))),
        new("fregues", "FREGUES", "JOGUE 10 PARTIDAS", General, Easy,
            Unlock.GamesPlayed(10), Art(Gamepad, "10")),
        new("turbinado", "TURBINADO", "USE O TURBO POR 60 SEGUNDOS NO TOTAL", General, Easy,
            Unlock.Stat("60 S DE TURBO", p => p.TotalBoostSeconds, 60), Art(Turbo)),
        new("maratonista", "MARATONISTA", "PASSE 30 MINUTOS CORRENDO NO TOTAL", General, Medium,
            Unlock.Stat("30 MINUTOS CORRENDO", p => p.TotalRaceSeconds / 60f, 30), Art(Television)),
        new("volta_relampago", "VOLTA RELAMPAGO", "FACA UMA VOLTA EM MENOS DE 9 SEGUNDOS", General, Medium,
            Unlock.LapUnder(9f), Art(Lightning)),
        new("viciado_em_turbo", "VICIADO EM TURBO", "USE O TURBO POR 10 MINUTOS NO TOTAL", General, Medium,
            Unlock.Stat("10 MINUTOS DE TURBO", p => p.TotalBoostSeconds / 60f, 10), Art(Turbo, "10M")),
        new("voce_ainda_esta_aqui", "VOCE AINDA ESTA AQUI?", "JOGUE 100 PARTIDAS", General, Medium,
            Unlock.GamesPlayed(100), Art(Gamepad, "100")),
        new("cacador_de_conquistas", "CACADOR DE CONQUISTAS", "DESBLOQUEIE 25 CONQUISTAS", General, Medium,
            Unlock.AchievementsUnlocked(25), Art(Medal)),
        new("mais_rapido_que_a_luz", "MAIS RAPIDO QUE A LUZ", "FACA UMA VOLTA EM MENOS DE 8.4 SEGUNDOS", General, Hard,
            Unlock.LapUnder(8.4f), Art(Recolor(Lightning, "YC", "DB"))),
        new("mil_checkpoints", "MIL CHECKPOINTS", "PASSE POR 1000 CHECKPOINTS NO TOTAL", General, Hard,
            Unlock.Stat("1000 CHECKPOINTS", p => p.TotalCheckpoints, 1000), Art(Flag, "1K")),
        new("meio_caminho", "MEIO CAMINHO", "DESBLOQUEIE METADE DAS CONQUISTAS", General, Hard,
            Unlock.HalfOfAchievements(), Art(Medal, "50%")),
        new("morador_das_pistas", "MORADOR DAS PISTAS", "JOGUE 500 PARTIDAS", General, Hard,
            Unlock.GamesPlayed(500), Art(House)),
        new("platina", "PLATINA", "DESBLOQUEIE TODAS AS OUTRAS CONQUISTAS", General, Rare,
            Unlock.AllOtherAchievements(), Art(Recolor(Trophy, "YC", "DB", "WW"))),

        // ----- Idiotas -----
        new("isso_e_uma_galinha", "ISSO E UMA GALINHA?", "DESBLOQUEIE A GALINHA", Silly, Easy,
            Unlock.SkinEarned("galinha"), Skin("galinha")),
        new(WhereAreTheBrakesId, "ONDE ESTAO OS FREIOS?", "BATA 10 VEZES NUMA MESMA PARTIDA", Silly, Easy,
            Unlock.Stat("10 BATIDAS NUMA PARTIDA", p => p.MostCollisionsInOneRace, 10), Art(Explosion)),
        new("eu_nao_queria_fazer_isso", "EU NAO QUERIA FAZER ISSO", "BATA NUM RIVAL LOGO NA LARGADA", Silly, Easy,
            Unlock.Stat("BATIDA NA LARGADA", p => p.StartLineCrashes, 1), Art(Oops)),
        new("turbo_pra_lugar_nenhum", "TURBO PRA LUGAR NENHUM", "SEJA ELIMINADO COM O TURBO LIGADO", Silly, Easy,
            Unlock.Stat("ELIMINADO NO TURBO", p => p.EliminatedWhileBoosting, 1), Art(Recolor(Turbo, "CR"))),
        new("pausa_pro_lanche", "PAUSA PRO LANCHE", "PAUSE O JOGO 10 VEZES", Silly, Easy,
            Unlock.Stat("10 PAUSAS", p => p.TotalPauses, 10), Art(Pause)),
        new("eu_tinha_um_plano", "EU TINHA UM PLANO", "SEJA O PRIMEIRO ELIMINADO DA CORRIDA MORTAL", Silly, Easy,
            Unlock.Stat("PRIMEIRO ELIMINADO", p => p.EliminationFirstOuts, 1), Art(Bomb), IsSecret: true),
        new("parado_no_transito", "PARADO NO TRANSITO", "FIQUE 10 SEGUNDOS PARADO NUMA PARTIDA", Silly, Easy,
            Unlock.Stat("10 S PARADO", p => p.LongestStandstillSeconds, 10), Art(Cone), IsSecret: true),
        new("isso_foi_de_proposito", "ISSO FOI DE PROPOSITO?", "ANDE DE RE POR 5 SEGUNDOS SEGUIDOS", Silly, Easy,
            Unlock.Stat("5 S DE RE", p => p.LongestReverseSeconds, 5), Art(Reverse), IsSecret: true),
        new("ta_pausando_por_que", "TA PAUSANDO POR QUE?", "PAUSE 5 VEZES NUMA MESMA PARTIDA", Silly, Easy,
            Unlock.Stat("5 PAUSAS NUMA PARTIDA", p => p.MostPausesInOneRace, 5), Art(Recolor(Pause, "WR")), IsSecret: true),
        new(WasThatSupposedToHappenId, "ERA PARA ISSO ACONTECER?", "TERMINE O CONTRA O RELOGIO COM 0 PONTOS", Silly, Easy,
            Unlock.Stat("0 PONTOS", p => p.TimeAttackZeroScores, 1), Art(Bug), IsSecret: true),
        new("quase", "QUASE!", "CAIA NA ULTIMA ELIMINACAO DA CORRIDA MORTAL", Silly, Medium,
            Unlock.Stat("2º LUGAR NA MORTAL", p => p.EliminationRunnerUps, 1), Art(BrokenHeart)),
        new("quack_quack", "QUACK", "VENCA UMA CORRIDA MORTAL USANDO O PATO", Silly, Medium,
            Unlock.WinsWithSkin("pato"), Skin("pato", "1º")),
        new("problema_seu", "PROBLEMA SEU", "VENCA UMA CORRIDA MORTAL USANDO O JACARE", Silly, Medium,
            Unlock.WinsWithSkin("jacare"), Skin("jacare", "1º")),
        new("fazendinha", "FAZENDINHA", "VENCA COM A GALINHA E TAMBEM COM O PATO", Silly, Medium,
            Unlock.WonWithEachSkin("VENCA COM GALINHA E PATO", "galinha", "pato"), Skin("galinha", "1º")),
        new("cade_o_tempo", "CADE O TEMPO?", "PERCA 10 S EM BATIDAS NUMA PARTIDA DO RELOGIO", Silly, Medium,
            Unlock.Stat("10 S PERDIDOS EM BATIDAS", p => p.MostTimeLostInOneRun, 10), Art(Recolor(Stopwatch, "RE", "EK"), "-10")),
        new("colecionador_de_amassados", "COLECIONADOR DE AMASSADOS", "BATA 200 VEZES NO TOTAL", Silly, Medium,
            Unlock.Stat("200 BATIDAS", p => p.TotalCollisions, 200), Art(Wrench, "200")),
        new("eu_tenho_tempo", "EU TENHO TEMPO!", "PASSE NUM CHECKPOINT COM MENOS DE 1 S NO RELOGIO", Silly, Medium,
            Unlock.Stat("CHECKPOINT NO ULTIMO SEGUNDO", p => p.TimeAttackClutchCheckpoints, 1), Art(Recolor(Stopwatch, "ER", "WY")), IsSecret: true),
        new("silencio_no_set", "SILENCIO NO SET", "TERMINE UMA PARTIDA COM MUSICA E EFEITOS NO MUDO", Silly, Medium,
            Unlock.Stat("PARTIDA NO MUDO", p => p.SilentGames, 1), Art(Mute), IsSecret: true),
        new("o_carro_esta_bem", "O CARRO ESTA BEM?", "VENCA UMA CORRIDA MORTAL BATENDO 10 VEZES OU MAIS", Silly, Hard,
            Unlock.Stat("VENCA COM 10 BATIDAS", p => p.MostCollisionsInAWin, 10), Art(Recolor(Wrench, "LR"), "10")),
        new("por_que", "POR QUE?", "DESBLOQUEIE O VASO SANITARIO", Silly, Hard,
            Unlock.SkinEarned("privada"), Skin("privada")),
        new("sedentario", "SEDENTARIO", "VENCA UMA CORRIDA MORTAL USANDO O SOFA", Silly, Hard,
            Unlock.WinsWithSkin("sofa"), Skin("sofa", "1º")),
        new("decisao_questionavel", "DECISAO QUESTIONAVEL", "VENCA UMA CORRIDA MORTAL USANDO O VASO SANITARIO", Silly, Hard,
            Unlock.WinsWithSkin("privada"), Skin("privada", "1º"), IsSecret: true),
        new("nao_somos_mais_os_unicos", "NAO SOMOS MAIS OS UNICOS", "DESBLOQUEIE O OVNI", Silly, Hard,
            Unlock.SkinEarned("ovni"), Skin("ovni"), IsSecret: true),
        new("dieta_balanceada", "DIETA BALANCEADA", "VENCA COM A BANANA, A BATATA, A PIZZA E A MELANCIA", Silly, Rare,
            Unlock.WonWithEachSkin("VENCA COM AS 4 COMIDAS", "banana", "batata", "pizza", "melancia"), Skin("pizza", "4")),
        new("isso_conta_como_estrategia", "ISSO CONTA COMO ESTRATEGIA?", "VENCA A MORTAL PARADO OU DE RE NA ULTIMA ELIMINACAO", Silly, Rare,
            Unlock.Stat("VITORIA PARADO", p => p.EliminationLazyWins, 1), Art(Sleep), IsSecret: true),
    ];

    public static IReadOnlyList<Achievement> All { get; } = [.. Handmade, .. TrackThemes.All.Select(TrackSecretAchievement)];

    /// <summary>A conquista secreta de uma pista: achar o detalhe escondido nela.</summary>
    public static string TrackSecretId(TrackTheme track) => $"segredo_{track.Id}";

    private static Achievement TrackSecretAchievement(TrackTheme track) => new(
        TrackSecretId(track),
        track.SecretName,
        track.SecretDescription,
        Tracks,
        Medium,
        Unlock.TrackSecretFound(track.Id),
        Art(track.Icon, "?"),
        IsSecret: true);

    public static Achievement Find(string id) => All.FirstOrDefault(achievement => achievement.Id == id);

    public static bool IsUnlocked(Achievement achievement, SaveData progress) =>
        progress.UnlockedAchievementIds.Contains(achievement.Id);

    public static int UnlockedCount(SaveData progress) => All.Count(achievement => IsUnlocked(achievement, progress));
}
