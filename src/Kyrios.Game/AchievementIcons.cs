using Microsoft.Xna.Framework;

namespace Kyrios.Game;

/// <summary>
/// Desenhos pixel-art (12x12) dos ícones de conquista. Cada fileira é uma string; cada caractere é uma cor da
/// <see cref="Palette"/> ('.' = transparente). Pra trocar o ícone de uma conquista basta apontar pra outro
/// desenho (ou criar um novo aqui); <see cref="Recolor"/> gera variações de cor de um mesmo desenho.
/// </summary>
public static class AchievementIcons
{
    public static readonly IReadOnlyDictionary<char, Color> Palette = new Dictionary<char, Color>
    {
        ['Y'] = new(255, 205, 60),
        ['D'] = new(200, 130, 30),
        ['W'] = new(245, 245, 250),
        ['L'] = new(190, 195, 205),
        ['E'] = new(95, 100, 115),
        ['K'] = new(30, 30, 38),
        ['R'] = new(225, 65, 55),
        ['r'] = new(150, 35, 35),
        ['O'] = new(245, 140, 45),
        ['G'] = new(95, 205, 95),
        ['g'] = new(40, 130, 60),
        ['B'] = new(70, 150, 245),
        ['b'] = new(40, 80, 170),
        ['C'] = new(110, 225, 240),
        ['P'] = new(175, 100, 230),
        ['N'] = new(150, 95, 55),
        ['F'] = new(245, 150, 170),
    };

    /// <summary>Troca cores de um desenho: <c>Recolor(Trophy, "YC", "DB")</c> = ouro vira ciano, ouro escuro vira azul.</summary>
    public static string[] Recolor(string[] pixels, params string[] swaps) =>
        [.. pixels.Select(row => new string([.. row.Select(c => swaps.FirstOrDefault(s => s[0] == c) is { } swap ? swap[1] : c)]))];

    public static AchievementIcon Art(string[] pixels, string badge = null) => new(Pixels: pixels, Badge: badge);

    public static AchievementIcon Skin(string skinId, string badge = null) => new(SkinId: skinId, Badge: badge);

    /// <summary>Ícone genérico das conquistas secretas ainda bloqueadas.</summary>
    public static AchievementIcon Secret => Art(Question);

    public static readonly string[] Flag =
    [
        "..N.........",
        "..NWEWEWEWE.",
        "..NEWEWEWEW.",
        "..NWEWEWEWE.",
        "..NEWEWEWEW.",
        "..NWEWEWEWE.",
        "..N.........",
        "..N.........",
        "..N.........",
        "..N.........",
        "..N.........",
        ".NNN........",
    ];

    public static readonly string[] Trophy =
    [
        "............",
        "..YYYYYYYY..",
        "Y.YWYYYYDY.Y",
        "Y.YWYYYYDY.Y",
        ".YYWYYYYDYY.",
        "...YYYYYD...",
        "....YYYD....",
        ".....YD.....",
        ".....YD.....",
        "....YYYD....",
        "...DDDDDD...",
        "...NNNNNN...",
    ];

    public static readonly string[] Star =
    [
        ".....YY.....",
        ".....YY.....",
        "....YYYY....",
        "YYYYYYYYYYYY",
        ".YYYYWYYYYY.",
        "..YYYYYYYY..",
        "...YYYYYY...",
        "..YYYYYYYY..",
        "..YYYDDYYY..",
        ".YYYD..DYYY.",
        ".YYD....DYY.",
        "YD........DY",
    ];

    public static readonly string[] Lightning =
    [
        "......YYYY..",
        ".....YYYY...",
        "....YYYY....",
        "...YYYY.....",
        "..YYYYYYYY..",
        "......YYY...",
        ".....YYY....",
        "....YYY.....",
        "...YYD......",
        "..YYD.......",
        ".YD.........",
        "............",
    ];

    public static readonly string[] Stopwatch =
    [
        ".....LL.....",
        "....LLLL....",
        "...EEEEEE...",
        "..EWWWRWWE..",
        ".EWWWWRWWWE.",
        ".EWWWWRWWWE.",
        ".EWWWWRRRWE.",
        ".EWWWWWWWWE.",
        ".EWWWWWWWWE.",
        "..EWWWWWWE..",
        "...EEEEEE...",
        "............",
    ];

    public static readonly string[] Hourglass =
    [
        ".NNNNNNNNNN.",
        "..LYYYYYYL..",
        "..LYYYYYYL..",
        "...LYYYYL...",
        "....LYYL....",
        ".....LL.....",
        ".....LL.....",
        "....L..L....",
        "...L.YY.L...",
        "..LYYYYYYL..",
        "..LYYYYYYL..",
        ".NNNNNNNNNN.",
    ];

    public static readonly string[] Shield =
    [
        ".LLLLLLLLLL.",
        ".LBBBBBBBBL.",
        ".LBBBWWBBBL.",
        ".LBBBWWBBBL.",
        ".LBWWWWWWBL.",
        ".LBWWWWWWBL.",
        ".LBBBWWBBBL.",
        "..LBBWWBBL..",
        "..LBBBBBBL..",
        "...LBBBBL...",
        "....LBBL....",
        ".....LL.....",
    ];

    public static readonly string[] Crown =
    [
        "............",
        "............",
        "Y....YY....Y",
        "YY...YY...YY",
        "YYY.YYYY.YYY",
        "YYYYYYYYYYYY",
        "YYRYYBBYYRYY",
        "YYYYYYYYYYYY",
        "DDDDDDDDDDDD",
        "............",
        "............",
        "............",
    ];

    public static readonly string[] Skull =
    [
        "............",
        "...WWWWWW...",
        "..WWWWWWWW..",
        ".WWWWWWWWWW.",
        ".WRRWWWWRRW.",
        ".WRRWWWWRRW.",
        ".WWWWKKWWWW.",
        "..WWWWWWWW..",
        "...WKWWKW...",
        "...WWWWWW...",
        "............",
        "............",
    ];

    public static readonly string[] Fire =
    [
        ".....R......",
        ".....RR.....",
        "....RRR.....",
        "....RROR....",
        "...RROOR....",
        "..RROOOORR..",
        "..ROOYYOOR..",
        ".RROYYYYORR.",
        ".ROOYYYYOOR.",
        ".ROOYWWYOOR.",
        "..ROOYYOOR..",
        "...RRRRRR...",
    ];

    public static readonly string[] Gamepad =
    [
        "............",
        "............",
        "............",
        "..LLLLLLLL..",
        ".LLKLLLLRLL.",
        ".LKKKLLGLBL.",
        ".LLKLLLLYLL.",
        "LLLLLLLLLLLL",
        "LLL......LLL",
        "LL........LL",
        "............",
        "............",
    ];

    public static readonly string[] House =
    [
        "............",
        ".....RR.....",
        "....RRRR....",
        "...RRRRRR...",
        "..RRRRRRRR..",
        ".RRRRRRRRRR.",
        "..WWWWWWWW..",
        "..WBBWWNNW..",
        "..WBBWWNNW..",
        "..WWWWWNNW..",
        "..WWWWWNNW..",
        "gggggggggggg",
    ];

    public static readonly string[] PaintPalette =
    [
        "............",
        "...NNNNNN...",
        "..NRRNNBBN..",
        ".NNRRNNBBNN.",
        ".NNNNNNNNNN.",
        ".NYYNN..NNN.",
        ".NYYNN..NNN.",
        ".NNNNNNNGGN.",
        "..NNNNNNGGN.",
        "...NNNNNN...",
        "............",
        "............",
    ];

    public static readonly string[] Chest =
    [
        "..W......W..",
        "....W..W....",
        "..NNNNNNNN..",
        ".NNNNNNNNNN.",
        ".NNNNNNNNNN.",
        "YYYYYYYYYYYY",
        ".NNNNYYNNNN.",
        ".NNNNYYNNNN.",
        ".NNNNNNNNNN.",
        ".NNNNNNNNNN.",
        "YYYYYYYYYYYY",
        "............",
    ];

    public static readonly string[] Turbo =
    [
        "............",
        "............",
        "..CC..CC....",
        "...CC..CC...",
        "....CC..CC..",
        ".....CC..CC.",
        ".....CC..CC.",
        "....CC..CC..",
        "...CC..CC...",
        "..CC..CC....",
        "............",
        "............",
    ];

    public static readonly string[] Compass =
    [
        "............",
        "....EEEE....",
        "..EEWWWWEE..",
        ".EWWWRRWWWE.",
        "EWWWWRRWWWWE",
        "EWWWRRRRWWWE",
        "EWWWBBBBWWWE",
        "EWWWWBBWWWWE",
        ".EWWWBBWWWE.",
        "..EEWWWWEE..",
        "....EEEE....",
        "............",
    ];

    public static readonly string[] Television =
    [
        "............",
        "...E....E...",
        "....E..E....",
        ".....EE.....",
        ".EEEEEEEEEE.",
        ".ECCCCCCCCE.",
        ".ECWCCCCCCE.",
        ".ECCCCCCCCE.",
        ".ECCCCCCCCE.",
        ".EEEEEEEEEE.",
        "..E......E..",
        "............",
    ];

    public static readonly string[] Medal =
    [
        "..RR....BB..",
        "...RR..BB...",
        "....RRBB....",
        ".....RB.....",
        "....YYYY....",
        "...YYWYYY...",
        "..YYWYYYYY..",
        "..YYYYYYDY..",
        "..YYYYYYDY..",
        "...YYYYDY...",
        "....YDDD....",
        "............",
    ];

    public static readonly string[] Diamond =
    [
        "............",
        "............",
        "..CCCCCCCC..",
        ".CWWCCCCCBC.",
        "CCCCCCCCCCCC",
        ".CCCCCCCCCB.",
        "..CCCCCCCB..",
        "...CCCCCB...",
        "....CCCB....",
        ".....CB.....",
        "............",
        "............",
    ];

    public static readonly string[] ArrowUp =
    [
        ".....GG.....",
        "....GGGG....",
        "...GGGGGG...",
        "..GGGGGGGG..",
        ".GGGGGGGGGG.",
        "....GGGG....",
        "....GGGG....",
        "....GGGG....",
        "....GGGG....",
        "....GGGG....",
        "....gggg....",
        "............",
    ];

    public static readonly string[] Speedometer =
    [
        "............",
        "............",
        "...EEEEEE...",
        "..EGWWWWRE..",
        ".EGWWWWWWRE.",
        ".EWWWWWWKWE.",
        ".EWWWWWKWWE.",
        ".EWWWKKWWWE.",
        ".EEEEEEEEEE.",
        "............",
        "............",
        "............",
    ];

    public static readonly string[] Explosion =
    [
        ".....R......",
        ".R...RR...R.",
        "..RR.RR.RR..",
        "..RROOOORR..",
        "...ROYYOR...",
        "RRROYWWYORRR",
        ".RROYWWYORR.",
        "...ROYYOR...",
        "..RROOOORR..",
        "..RR.RR.RR..",
        ".R...RR...R.",
        "......R.....",
    ];

    public static readonly string[] Question =
    [
        "............",
        "...WWWWWW...",
        "..WW....WW..",
        "..WW....WW..",
        "........WW..",
        ".......WW...",
        ".....WWW....",
        ".....WW.....",
        ".....WW.....",
        "............",
        ".....WW.....",
        ".....WW.....",
    ];

    public static readonly string[] Bug =
    [
        "..E......E..",
        "...E....E...",
        "....GGGG....",
        "...GKGGKG...",
        "E..GGGGGG..E",
        ".EGGGggGGGE.",
        "..GGGggGGG..",
        "EEGGGggGGGEE",
        "..GGGggGGG..",
        ".E.GGggGG.E.",
        "E...GGGG...E",
        "............",
    ];

    public static readonly string[] Lantern =
    [
        ".....EE.....",
        "....E..E....",
        "...EEEEEE...",
        "...EYYYYE...",
        "..OEYWWYEO..",
        "..OEYWWYEO..",
        "...EYYYYE...",
        "...EYYYYE...",
        "...EEEEEE...",
        "....EEEE....",
        "............",
        "............",
    ];

    public static readonly string[] BrokenHeart =
    [
        "............",
        "..RR....RR..",
        ".RRRR..RRRR.",
        "RRRRR.RRRRRR",
        "RRRRRR.RRRRR",
        "RRRRR.RRRRRR",
        ".RRRRR.RRRR.",
        "..RRR.RRRR..",
        "...RRR.RR...",
        "....RR.R....",
        ".....RR.....",
        "............",
    ];

    public static readonly string[] Cone =
    [
        ".....OO.....",
        ".....OO.....",
        "....OOOO....",
        "....WWWW....",
        "....OOOO....",
        "...OOOOOO...",
        "...WWWWWW...",
        "...OOOOOO...",
        "..OOOOOOOO..",
        "..OOOOOOOO..",
        "EEEEEEEEEEEE",
        "............",
    ];

    public static readonly string[] Reverse =
    [
        "............",
        "...G........",
        "..GG........",
        ".GGGGGGGGGG.",
        "GGGGGGGGGGG.",
        ".GGGGGGGGGG.",
        "..GG........",
        "...G........",
        "............",
        "WEWEWEWEWEWE",
        "EWEWEWEWEWEW",
        "WEWEWEWEWEWE",
    ];

    public static readonly string[] Bomb =
    [
        "..........O.",
        ".........Y..",
        "........N...",
        ".......N....",
        "....EEEE....",
        "...EEEEEE...",
        "..EEWEEEEE..",
        "..EWEEEEEE..",
        "..EEEEEEEE..",
        "..EEEEEEEE..",
        "...EEEEEE...",
        "....EEEE....",
    ];

    public static readonly string[] Wrench =
    [
        "...LL..LL...",
        "...LL..LL...",
        "...LLLLLL...",
        "....LLLL....",
        ".....LL.....",
        ".....LL.....",
        ".....LL.....",
        ".....LL.....",
        ".....LL.....",
        "....LLLL....",
        "....LEEL....",
        ".....LL.....",
    ];
}
