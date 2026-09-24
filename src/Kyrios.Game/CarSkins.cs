using Microsoft.Xna.Framework;

namespace Kyrios.Game;

/// <summary>Uma aparência pro carro do jogador. <see cref="Id"/> é o que fica salvo (estável mesmo se a lista
/// for reordenada), <see cref="Name"/> é o que aparece no menu, <see cref="Paint"/> desenha o veículo virado
/// pra frente (+x) no espaço local de um <see cref="CarPainter"/> e <see cref="Requirement"/> diz o que é
/// preciso pra liberá-la (o estado bloqueado/desbloqueado em si fica no progresso salvo). <see cref="Difficulty"/>
/// é só informativa; uma skin <see cref="IsSecret"/> esconde nome, visual e requisito até ser liberada. Só muda
/// o visual — a física é a mesma.</summary>
public sealed record CarSkin(
    string Id,
    string Name,
    Action<CarPainter> Paint,
    UnlockRequirement Requirement,
    Difficulty Difficulty = Difficulty.Easy,
    bool IsSecret = false) : IUnlockable;

/// <summary>
/// Catálogo de skins, na ordem do seletor (das mais fáceis às mais raras). Pra adicionar uma nova: escreva um
/// método <c>Paint*(CarPainter p)</c> desenhando o veículo mais ou menos dentro de x ∈ [-0.7, 0.7] e
/// y ∈ [-0.45, 0.45] (o tamanho do carro de corrida padrão), e acrescente uma linha em <see cref="All"/> com o
/// requisito (atalhos em <see cref="Unlock"/>) e a dificuldade. Nomes em maiúsculas e sem acento (a fonte
/// pixelizada não tem).
/// </summary>
public static class CarSkins
{
    private static readonly Color Wheel = new(24, 24, 27);

    public static IReadOnlyList<CarSkin> All { get; } =
    [
        new("padrao", "CARRO DE CORRIDA", PaintRaceCar, Unlock.FromStart),

        // ----- Fáceis: saem naturalmente nas primeiras partidas -----
        new("pato", "PATO", PaintDuck, Unlock.GamesPlayed(3)),
        new("galinha", "GALINHA", PaintChicken, Unlock.DeathRaceWins(1)),
        new("banana", "BANANA", PaintBanana, Unlock.TimeAttackScore(400)),
        new("melancia", "MELANCIA", PaintWatermelon, Unlock.TracksPlayed(2)),
        new("tijolo", "TIJOLO", PaintBrick, Unlock.AchievementEarned(Achievements.WhereAreTheBrakesId)),

        // ----- Médias: dedicação ou alguma habilidade -----
        new("jacare", "JACARE", PaintAlligator, Unlock.DeathRaceWins(10), Difficulty.Medium),
        new("peixe", "PEIXE", PaintFish, Unlock.TimeAttackScoreOnTrack("praia", 700), Difficulty.Medium),
        new("tubarao", "TUBARAO", PaintShark, Unlock.DeathRaceWinsOnTrack("praia"), Difficulty.Medium),
        new("torradeira", "TORRADEIRA", PaintToaster, Unlock.TimeAttackScore(1000), Difficulty.Medium),
        new("carrinho", "CARRINHO DE MERCADO", PaintShoppingCart, Unlock.Stat("PASSE POR 300 CHECKPOINTS", p => p.TotalCheckpoints, 300), Difficulty.Medium),
        new("batata", "BATATA", PaintPotato, Unlock.GamesPlayed(25), Difficulty.Medium),
        new("suco", "CAIXINHA DE SUCO", PaintJuiceBox, Unlock.AchievementsUnlocked(15), Difficulty.Medium),
        new("pizza", "PIZZA", PaintPizza, Unlock.All("5 VITORIAS NA MORTAL E 3000 PTS NO RELOGIO", Unlock.DeathRaceWins(5), Unlock.TimeAttackTotal(3000)), Difficulty.Medium),

        // ----- Difíceis: bastante progresso ou domínio de um modo -----
        new("dino", "DINOSSAURO", PaintDinosaur, Unlock.AchievementEarned(Achievements.PredatorId), Difficulty.Hard),
        new("privada", "VASO SANITARIO", PaintToilet, Unlock.DeathRaceWinStreak(10), Difficulty.Hard),
        new("ovni", "OVNI", PaintUfo, Unlock.All("10 SKINS E 10000 PTS NO RELOGIO", Unlock.SkinsUnlocked(10), Unlock.TimeAttackTotal(10000)), Difficulty.Hard),
        new("sofa", "SOFA", PaintSofa, Unlock.GamesPlayed(100), Difficulty.Hard),

        // ----- Raras: feitos específicos -----
        new("sapo", "SAPO", PaintFrog, Unlock.Stat("VENCA A MORTAL SEM BATER EM NADA", p => p.EliminationCleanWins, 1), Difficulty.Rare),
        new("foguete", "FOGUETE", PaintRocket, Unlock.TrackSecretsFound(5), Difficulty.Rare),

        // Secreta: vem de uma conquista secreta — nome, visual e requisito ficam escondidos até lá.
        new("ursinho", "URSINHO", PaintTeddyBear, Unlock.AchievementEarned(Achievements.WasThatSupposedToHappenId), Difficulty.Rare, IsSecret: true),
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

    private static void PaintRaceCar(CarPainter p)
    {
        const float length = 1.35f;
        const float width = 0.85f;
        const float halfLength = length / 2f;
        const float halfWidth = width / 2f;

        p.Shadow(length, width);

        const float wheelLength = length * 0.4f;
        const float wheelWidth = width * 0.3f;
        foreach (float sideX in new[] { -1f, 1f })
        {
            foreach (float sideY in new[] { -1f, 1f })
            {
                p.Rect(sideX * halfLength * 0.5f, sideY * (halfWidth + (wheelWidth * 0.55f)), wheelLength, wheelWidth, Wheel);
            }
        }

        // Aerofólio traseiro e retrovisores — silhueta de "carro de corrida" em vez de uma cápsula lisa.
        Color trim = CarPainter.Darken(p.BodyColor, 0.55f);
        p.Rect(-halfLength * 0.98f, 0f, width * 0.16f, width * 1.05f, trim);
        foreach (float side in new[] { -1f, 1f })
        {
            p.Rect(halfLength * 0.1f, side * (halfWidth + (width * 0.1f)), width * 0.16f, width * 0.13f, trim);
        }

        p.Capsule(0f, 0f, length, width, p.BodyColor);
        p.Capsule(halfLength * 0.05f, 0f, length * 0.5f, width * 0.6f, CarPainter.Darken(p.BodyColor, 0.5f));

        if (!p.Eliminated)
        {
            p.Circle(halfLength * 0.2f, -width * 0.14f, width * 0.13f, new Color(255, 255, 255, 70));
            p.Circle(halfLength * 0.9f, halfWidth * 0.55f, 0.11f, new Color(255, 250, 210));
            p.Circle(halfLength * 0.9f, -halfWidth * 0.55f, 0.11f, new Color(255, 250, 210));
            p.Circle(-halfLength * 0.92f, 0f, 0.1f, new Color(200, 20, 20));
        }
    }

    private static void PaintChicken(CarPainter p)
    {
        var white = new Color(245, 242, 232);
        var feather = new Color(205, 200, 190);
        var red = new Color(220, 50, 45);
        var orange = new Color(245, 160, 40);
        float flap = MathF.Sin(p.Time * 18f) * 0.35f;

        p.Shadow(1.2f, 0.8f);

        p.Circle(-0.58f, 0.08f, 0.13f, feather);
        p.Circle(-0.58f, -0.08f, 0.13f, feather);
        p.Circle(-0.66f, 0f, 0.11f, white);

        p.Capsule(-0.05f, 0.36f, 0.62f, 0.24f, feather, -0.25f + flap);
        p.Capsule(-0.05f, -0.36f, 0.62f, 0.24f, feather, 0.25f - flap);

        p.Capsule(-0.05f, 0f, 1.0f, 0.72f, white);
        p.Circle(0.45f, 0f, 0.24f, white);

        p.Circle(0.42f, 0f, 0.09f, red);
        p.Circle(0.52f, 0f, 0.1f, red);
        p.Circle(0.6f, 0f, 0.08f, red);

        p.Capsule(0.72f, 0f, 0.2f, 0.12f, orange);
        p.Circle(0.53f, 0.14f, 0.045f, Color.Black);
        p.Circle(0.53f, -0.14f, 0.045f, Color.Black);
    }

    private static void PaintAlligator(CarPainter p)
    {
        var green = new Color(70, 130, 60);
        var dark = new Color(45, 95, 40);
        var light = new Color(125, 180, 95);
        float wag = MathF.Sin(p.Time * 6f) * 0.3f;

        p.Shadow(1.6f, 0.6f);

        foreach (float x in new[] { 0.3f, -0.3f })
        {
            p.Rect(x, 0.32f, 0.2f, 0.14f, dark, 0.5f);
            p.Rect(x, -0.32f, 0.2f, 0.14f, dark, -0.5f);
        }

        p.Capsule(-0.78f, 0f, 0.55f, 0.26f, green, wag);
        p.Circle(-0.78f - (MathF.Cos(wag) * 0.33f), -MathF.Sin(wag) * 0.33f, 0.08f, green);

        p.Capsule(0f, 0f, 1.2f, 0.55f, green);
        p.Capsule(0.72f, 0f, 0.6f, 0.34f, green);

        for (int i = 0; i < 4; i++)
        {
            float x = 0.58f + (i * 0.1f);
            p.Rect(x, 0.17f, 0.05f, 0.06f, Color.White);
            p.Rect(x, -0.17f, 0.05f, 0.06f, Color.White);
        }

        for (float x = -0.4f; x <= 0.3f; x += 0.14f)
        {
            p.Circle(x, 0f, 0.055f, light);
        }

        p.Circle(0.45f, 0.15f, 0.085f, light);
        p.Circle(0.45f, -0.15f, 0.085f, light);
        p.Circle(0.47f, 0.16f, 0.04f, Color.Black);
        p.Circle(0.47f, -0.16f, 0.04f, Color.Black);
    }

    private static void PaintDuck(CarPainter p)
    {
        var yellow = new Color(250, 215, 50);
        var dark = new Color(225, 180, 30);
        var orange = new Color(250, 130, 30);

        p.Shadow(1.15f, 0.8f);

        p.Circle(-0.5f, 0f, 0.15f, dark);
        p.Capsule(-0.05f, 0f, 0.95f, 0.72f, yellow);
        p.Capsule(-0.1f, 0.24f, 0.5f, 0.2f, dark, 0.15f);
        p.Capsule(-0.1f, -0.24f, 0.5f, 0.2f, dark, -0.15f);

        p.Circle(0.42f, 0f, 0.25f, yellow);
        p.Capsule(0.7f, 0f, 0.3f, 0.22f, orange);
        p.Circle(0.5f, 0.13f, 0.05f, Color.Black);
        p.Circle(0.5f, -0.13f, 0.05f, Color.Black);
    }

    private static void PaintBanana(CarPainter p)
    {
        var yellow = new Color(245, 215, 70);
        var edge = new Color(215, 180, 40);
        var brown = new Color(110, 80, 40);
        const int segments = 11;

        p.Shadow(1.4f, 0.4f);

        // Duas passadas (borda mais escura por baixo, miolo amarelo por cima) ao longo de uma curva.
        foreach (bool outline in new[] { true, false })
        {
            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)(segments - 1);
                float x = -0.7f + (1.4f * t);
                float y = (0.28f * (1f - (4f * (t - 0.5f) * (t - 0.5f)))) - 0.1f;
                float radius = 0.07f + (0.11f * MathF.Sin(MathF.PI * t));
                p.Circle(x, y, outline ? radius + 0.025f : radius, outline ? edge : yellow);
            }
        }

        p.Circle(-0.7f, -0.1f, 0.06f, brown);
        p.Circle(0.7f, -0.1f, 0.05f, brown);
        p.Rect(0.76f, -0.12f, 0.12f, 0.06f, brown, -0.3f);
    }

    private static void PaintBrick(CarPainter p)
    {
        var brick = new Color(175, 75, 55);
        var dark = new Color(125, 50, 40);
        var speck = new Color(200, 110, 90);
        var hole = new Color(70, 35, 30);

        p.Shadow(1.3f, 0.75f);

        foreach (float x in new[] { 0.42f, -0.42f })
        {
            p.Rect(x, 0.4f, 0.22f, 0.1f, Wheel);
            p.Rect(x, -0.4f, 0.22f, 0.1f, Wheel);
        }

        p.Rect(0f, 0f, 1.3f, 0.74f, dark);
        p.Rect(0f, 0f, 1.22f, 0.66f, brick);

        foreach (float x in new[] { -0.35f, 0f, 0.35f })
        {
            p.Circle(x, 0f, 0.11f, hole);
        }

        p.Rect(0.45f, -0.22f, 0.12f, 0.04f, speck);
        p.Rect(-0.2f, 0.23f, 0.18f, 0.04f, speck);
        p.Rect(-0.5f, -0.2f, 0.08f, 0.04f, speck);
    }

    private static void PaintFish(CarPainter p)
    {
        var body = new Color(80, 150, 210);
        var dark = new Color(50, 105, 160);
        float wag = MathF.Sin(p.Time * 9f) * 0.35f;

        p.Shadow(1.3f, 0.6f);

        p.Rect(-0.72f, 0.13f, 0.36f, 0.16f, dark, 0.6f + wag);
        p.Rect(-0.72f, -0.13f, 0.36f, 0.16f, dark, -0.6f + wag);
        p.Rect(0.1f, 0.3f, 0.28f, 0.12f, dark, -0.5f);
        p.Rect(0.1f, -0.3f, 0.28f, 0.12f, dark, 0.5f);

        p.Capsule(0.02f, 0f, 1.1f, 0.56f, body);
        p.Rect(-0.05f, 0f, 0.55f, 0.07f, dark);

        p.Circle(0.35f, 0.18f, 0.08f, Color.White);
        p.Circle(0.35f, -0.18f, 0.08f, Color.White);
        p.Circle(0.37f, 0.19f, 0.04f, Color.Black);
        p.Circle(0.37f, -0.19f, 0.04f, Color.Black);
        p.Circle(0.56f, 0f, 0.05f, dark);
    }

    private static void PaintPotato(CarPainter p)
    {
        var potato = new Color(185, 140, 85);
        var edge = new Color(150, 105, 60);
        var spot = new Color(120, 85, 50);
        (float X, float Y, float R)[] lumps = [(0f, 0f, 0.4f), (0.3f, 0.05f, 0.32f), (-0.3f, -0.04f, 0.33f), (0.08f, -0.2f, 0.28f), (-0.1f, 0.2f, 0.28f)];

        p.Shadow(1.15f, 0.85f);

        foreach ((float x, float y, float r) in lumps)
        {
            p.Circle(x, y, r + 0.04f, edge);
        }

        foreach ((float x, float y, float r) in lumps)
        {
            p.Circle(x, y, r, potato);
        }

        p.Circle(-0.35f, 0.15f, 0.04f, spot);
        p.Circle(-0.1f, -0.25f, 0.035f, spot);
        p.Circle(0.15f, 0.25f, 0.03f, spot);
        p.Circle(-0.45f, -0.1f, 0.03f, spot);

        // Olhinhos de brinquedo que tremem — sem motivo nenhum.
        float jiggle = MathF.Sin(p.Time * 7f) * 0.03f;
        p.Circle(0.42f, 0.14f, 0.12f, Color.White);
        p.Circle(0.42f, -0.14f, 0.12f, Color.White);
        p.Circle(0.45f + jiggle, 0.14f - jiggle, 0.06f, Color.Black);
        p.Circle(0.45f - jiggle, -0.14f + jiggle, 0.06f, Color.Black);
    }

    private static void PaintJuiceBox(CarPainter p)
    {
        var box = new Color(235, 235, 225);
        var edge = new Color(190, 190, 180);
        var label = new Color(250, 150, 40);
        var fruit = new Color(240, 110, 20);
        var leaf = new Color(70, 170, 60);
        var straw = new Color(240, 90, 140);

        p.Shadow(1.05f, 0.75f);

        p.Rect(0f, 0f, 1.05f, 0.74f, edge);
        p.Rect(0f, 0f, 0.98f, 0.67f, box);
        p.Rect(-0.05f, 0f, 0.5f, 0.67f, label);
        p.Circle(-0.05f, 0f, 0.17f, fruit);
        p.Circle(0.07f, 0.1f, 0.07f, leaf);
        p.Rect(0.43f, 0f, 0.06f, 0.67f, edge);

        p.Circle(0.3f, -0.18f, 0.05f, new Color(120, 120, 120));
        p.Rect(0.44f, -0.3f, 0.36f, 0.06f, straw, -0.7f);
        p.Circle(0.56f, -0.43f, 0.04f, straw);
    }

    private static void PaintToilet(CarPainter p)
    {
        var porcelain = new Color(240, 242, 245);
        var outline = new Color(170, 175, 185);
        var seat = new Color(225, 228, 232);
        var water = new Color(120, 190, 235);
        var metal = new Color(160, 165, 170);

        p.Shadow(1.3f, 0.8f);

        p.Rect(-0.47f, 0f, 0.36f, 0.82f, outline);
        p.Rect(-0.47f, 0f, 0.3f, 0.76f, porcelain);
        p.Rect(-0.47f, -0.3f, 0.14f, 0.06f, metal);

        p.Capsule(0.15f, 0f, 0.92f, 0.68f, outline);
        p.Capsule(0.15f, 0f, 0.86f, 0.62f, seat);
        p.Capsule(0.18f, 0f, 0.6f, 0.4f, water);
        p.Circle(0.22f, 0f, 0.08f + (MathF.Sin(p.Time * 5f) * 0.03f), new Color(170, 220, 250));
        p.Circle(0.22f, 0f, 0.05f, new Color(90, 160, 215));
    }

    private static void PaintShark(CarPainter p)
    {
        var body = new Color(115, 130, 150);
        var dark = new Color(80, 95, 115);
        float wag = MathF.Sin(p.Time * 7f) * 0.3f;

        p.Shadow(1.55f, 0.6f);

        p.Rect(0.05f, 0.34f, 0.42f, 0.14f, dark, -0.7f);
        p.Rect(0.05f, -0.34f, 0.42f, 0.14f, dark, 0.7f);
        p.Rect(-0.86f, 0.14f, 0.38f, 0.13f, dark, 0.8f + wag);
        p.Rect(-0.86f, -0.14f, 0.38f, 0.13f, dark, -0.8f + wag);

        p.Capsule(-0.55f, 0f, 0.5f, 0.26f, body, wag * 0.5f);
        p.Capsule(0.05f, 0f, 1.3f, 0.55f, body);

        p.Rect(-0.05f, 0f, 0.36f, 0.12f, dark);
        p.Circle(0.12f, 0f, 0.06f, dark);

        for (int i = 0; i < 3; i++)
        {
            float x = 0.3f + (i * 0.06f);
            p.Rect(x, 0.23f, 0.03f, 0.1f, dark);
            p.Rect(x, -0.23f, 0.03f, 0.1f, dark);
        }

        p.Circle(0.48f, 0.19f, 0.05f, Color.Black);
        p.Circle(0.48f, -0.19f, 0.05f, Color.Black);
    }

    private static void PaintPizza(CarPainter p)
    {
        var crust = new Color(215, 160, 80);
        var crustDark = new Color(180, 120, 55);
        var cheese = new Color(250, 205, 80);
        var sauce = new Color(215, 70, 40);
        var pepperoni = new Color(185, 40, 35);
        const int steps = 12;

        p.Shadow(1.3f, 0.9f);

        // Fatia em triângulo: faixas cada vez mais estreitas da borda (atrás) até a ponta (frente).
        foreach (bool outline in new[] { true, false })
        {
            for (int i = 0; i < steps; i++)
            {
                float t = i / (float)(steps - 1);
                float x = -0.45f + (1.15f * t);
                float width = (0.85f * (1f - t)) + 0.04f;
                p.Rect(x, 0f, 0.13f, outline ? width + 0.06f : width, outline ? sauce : cheese);
            }
        }

        p.Capsule(-0.52f, 0f, 0.98f, 0.24f, crustDark, MathF.PI / 2f);
        p.Capsule(-0.5f, 0f, 0.9f, 0.17f, crust, MathF.PI / 2f);

        p.Circle(-0.2f, 0.18f, 0.1f, pepperoni);
        p.Circle(-0.2f, -0.18f, 0.1f, pepperoni);
        p.Circle(0.12f, 0.05f, 0.09f, pepperoni);
        p.Circle(0.38f, -0.02f, 0.06f, pepperoni);
        p.Circle(0f, -0.06f, 0.05f, new Color(60, 150, 60));
    }

    private static void PaintDinosaur(CarPainter p)
    {
        var green = new Color(95, 170, 75);
        var dark = new Color(65, 125, 55);
        var light = new Color(150, 210, 110);
        float stomp = MathF.Sin(p.Time * 10f) * 0.06f;

        p.Shadow(1.6f, 0.7f);

        p.Capsule(-0.12f + stomp, 0.33f, 0.3f, 0.16f, dark);
        p.Capsule(-0.12f - stomp, -0.33f, 0.3f, 0.16f, dark);

        p.Capsule(-0.62f, 0f, 0.62f, 0.3f, green);
        p.Circle(-0.95f, 0f, 0.09f, green);
        p.Capsule(-0.05f, 0f, 0.8f, 0.58f, green);
        p.Capsule(0.5f, 0f, 0.52f, 0.44f, green);

        // Os bracinhos ridículos de T-rex.
        p.Rect(0.24f, 0.3f, 0.14f, 0.06f, dark, 0.4f);
        p.Rect(0.24f, -0.3f, 0.14f, 0.06f, dark, -0.4f);

        for (float x = -0.55f; x <= 0.12f; x += 0.13f)
        {
            p.Circle(x, 0f, 0.06f, light);
        }

        p.Circle(0.58f, 0.15f, 0.06f, Color.White);
        p.Circle(0.58f, -0.15f, 0.06f, Color.White);
        p.Circle(0.6f, 0.15f, 0.03f, Color.Black);
        p.Circle(0.6f, -0.15f, 0.03f, Color.Black);
        p.Rect(0.72f, 0.19f, 0.05f, 0.05f, Color.White);
        p.Rect(0.72f, -0.19f, 0.05f, 0.05f, Color.White);
    }

    private static void PaintFrog(CarPainter p)
    {
        var green = new Color(90, 185, 70);
        var dark = new Color(60, 140, 50);
        var spot = new Color(55, 120, 45);
        float kick = MathF.Max(0f, MathF.Sin(p.Time * 5f)) * 0.12f;

        p.Shadow(1.1f, 0.9f);

        p.Capsule(-0.32f - kick, 0.36f, 0.5f, 0.2f, dark, 0.5f);
        p.Capsule(-0.32f - kick, -0.36f, 0.5f, 0.2f, dark, -0.5f);
        p.Circle(-0.55f - kick, 0.46f, 0.1f, dark);
        p.Circle(-0.55f - kick, -0.46f, 0.1f, dark);
        p.Capsule(0.28f, 0.34f, 0.28f, 0.12f, dark, -0.5f);
        p.Capsule(0.28f, -0.34f, 0.28f, 0.12f, dark, 0.5f);

        p.Circle(0f, 0f, 0.42f, green);
        p.Circle(0.18f, 0f, 0.34f, green);

        p.Circle(-0.15f, 0.12f, 0.06f, spot);
        p.Circle(-0.05f, -0.18f, 0.05f, spot);
        p.Circle(-0.25f, -0.05f, 0.045f, spot);

        foreach (float side in new[] { 1f, -1f })
        {
            p.Circle(0.36f, side * 0.2f, 0.14f, green);
            p.Circle(0.38f, side * 0.2f, 0.1f, Color.White);
            p.Circle(0.41f, side * 0.21f, 0.05f, Color.Black);
        }
    }

    private static void PaintShoppingCart(CarPainter p)
    {
        var metal = new Color(170, 175, 182);
        var basket = new Color(70, 72, 80);
        var handle = new Color(210, 45, 45);

        p.Shadow(1.2f, 0.85f);

        foreach (float x in new[] { 0.45f, -0.3f })
        {
            p.Circle(x, 0.36f, 0.09f, Wheel);
            p.Circle(x, -0.36f, 0.09f, Wheel);
        }

        p.Rect(0.08f, 0f, 0.95f, 0.76f, metal);
        p.Rect(0.08f, 0f, 0.85f, 0.66f, basket);

        for (int i = 1; i <= 4; i++)
        {
            p.Rect(-0.34f + (i * 0.17f), 0f, 0.025f, 0.66f, metal);
        }

        foreach (float y in new[] { -0.22f, 0f, 0.22f })
        {
            p.Rect(0.08f, y, 0.85f, 0.025f, metal);
        }

        p.Circle(0.25f, 0.12f, 0.12f, new Color(240, 210, 60));
        p.Rect(-0.05f, -0.15f, 0.22f, 0.16f, new Color(80, 160, 230));
        p.Circle(0.3f, -0.15f, 0.08f, new Color(220, 60, 60));

        p.Rect(-0.47f, 0f, 0.1f, 0.84f, handle);
    }

    private static void PaintTeddyBear(CarPainter p)
    {
        var fur = new Color(175, 115, 65);
        var light = new Color(215, 170, 120);
        var dark = new Color(120, 75, 40);
        var bow = new Color(220, 50, 70);
        float wave = MathF.Sin(p.Time * 6f) * 0.05f;

        p.Shadow(1.2f, 0.85f);

        foreach (float side in new[] { 1f, -1f })
        {
            p.Circle(-0.42f, side * 0.24f, 0.14f, fur);
            p.Circle(-0.48f, side * 0.24f, 0.08f, light);
        }

        p.Circle(0.05f + wave, 0.37f, 0.12f, fur);
        p.Circle(0.05f - wave, -0.37f, 0.12f, fur);

        p.Circle(-0.1f, 0f, 0.36f, fur);
        p.Circle(-0.12f, 0f, 0.2f, light);

        foreach (float side in new[] { 1f, -1f })
        {
            p.Circle(0.42f, side * 0.24f, 0.1f, fur);
            p.Circle(0.42f, side * 0.24f, 0.055f, light);
        }

        p.Circle(0.36f, 0f, 0.27f, fur);
        p.Circle(0.56f, 0f, 0.1f, light);
        p.Circle(0.63f, 0f, 0.045f, dark);
        p.Circle(0.44f, 0.1f, 0.04f, Color.Black);
        p.Circle(0.44f, -0.1f, 0.04f, Color.Black);

        p.Circle(0.12f, 0.07f, 0.06f, bow);
        p.Circle(0.12f, -0.07f, 0.06f, bow);
        p.Circle(0.12f, 0f, 0.035f, CarPainter.Darken(bow, 0.7f));
    }

    private static void PaintUfo(CarPainter p)
    {
        var hull = new Color(165, 172, 185);
        var rim = new Color(110, 118, 132);
        Color[] lights = [new Color(255, 220, 60), new Color(255, 80, 80), new Color(80, 230, 255)];

        p.ShadowCircle(0.6f);

        p.Circle(0f, 0f, 0.6f, rim);
        p.Circle(0f, 0f, 0.53f, hull);
        p.Circle(0f, 0f, 0.38f, rim);
        p.Circle(0f, 0f, 0.34f, hull);

        for (int i = 0; i < 8; i++)
        {
            float angle = (i * MathF.PI / 4f) + (p.Time * 2.5f);
            p.Circle(MathF.Cos(angle) * 0.46f, MathF.Sin(angle) * 0.46f, 0.055f, lights[i % lights.Length]);
        }

        p.Circle(0f, 0f, 0.26f, new Color(150, 225, 255, 210));
        p.Circle(0.02f, 0f, 0.12f, new Color(110, 220, 90));
        p.Circle(0.08f, 0.05f, 0.035f, Color.Black);
        p.Circle(0.08f, -0.05f, 0.035f, Color.Black);
    }

    private static void PaintWatermelon(CarPainter p)
    {
        var rind = new Color(40, 120, 50);
        var stripe = new Color(70, 160, 70);
        var white = new Color(235, 240, 210);
        var flesh = new Color(235, 70, 80);
        var seed = new Color(30, 25, 25);

        p.Shadow(1.35f, 0.85f);
        p.Capsule(0f, 0f, 1.35f, 0.85f, rind);
        p.Rect(-0.3f, 0f, 0.08f, 0.85f, stripe);
        p.Rect(0.15f, 0f, 0.08f, 0.85f, stripe);
        p.Capsule(0.04f, 0f, 1.18f, 0.66f, white);
        p.Capsule(0.06f, 0f, 1.08f, 0.56f, flesh);
        foreach ((float x, float y) in new[] { (-0.25f, -0.12f), (0f, 0.1f), (0.25f, -0.1f), (0.35f, 0.12f), (-0.1f, -0.02f) })
        {
            p.Rect(x, y, 0.08f, 0.045f, seed, 0.4f);
        }
    }

    private static void PaintToaster(CarPainter p)
    {
        var body = new Color(200, 205, 215);
        var edge = new Color(140, 145, 160);
        var slot = new Color(40, 40, 45);
        var toast = new Color(215, 160, 80);
        var crust = new Color(150, 95, 40);

        p.Shadow(1.2f, 0.8f);
        p.Capsule(0f, 0f, 1.2f, 0.8f, edge);
        p.Capsule(0f, 0f, 1.12f, 0.72f, body);
        p.Rect(-0.3f, -0.2f, 0.25f, 0.08f, Color.White);

        // Torradas pulando pra fora e voltando, como se a torradeira estivesse ligada.
        float bounce = MathF.Max(0f, MathF.Sin(p.Time * 4f)) * 0.08f;
        foreach (float y in new[] { -0.15f, 0.15f })
        {
            p.Rect(0.05f, y, 0.78f, 0.16f, slot);
            p.Rect(0.05f + bounce, y, 0.7f, 0.1f, crust);
            p.Rect(0.05f + bounce, y, 0.62f, 0.06f, toast);
        }

        p.Rect(-0.45f, 0.42f, 0.2f, 0.08f, slot);
    }

    private static void PaintSofa(CarPainter p)
    {
        var frame = new Color(140, 40, 45);
        var cushion = new Color(200, 70, 70);
        var seam = new Color(160, 50, 55);
        var remote = new Color(35, 35, 40);

        p.Shadow(1.3f, 0.95f);
        p.Rect(-0.05f, 0f, 1.3f, 0.95f, frame);
        p.Rect(-0.5f, 0f, 0.28f, 0.95f, CarPainter.Darken(frame, 0.85f));
        p.Rect(0.08f, -0.39f, 1.02f, 0.17f, CarPainter.Darken(frame, 0.9f));
        p.Rect(0.08f, 0.39f, 1.02f, 0.17f, CarPainter.Darken(frame, 0.9f));
        p.Rect(0.13f, -0.15f, 0.84f, 0.28f, cushion);
        p.Rect(0.13f, 0.15f, 0.84f, 0.28f, cushion);
        p.Rect(0.13f, 0f, 0.84f, 0.03f, seam);
        p.Rect(0.2f, 0.18f, 0.2f, 0.08f, remote, 0.3f);
        p.Circle(0.25f, 0.2f, 0.02f, new Color(220, 50, 50));
    }

    private static void PaintRocket(CarPainter p)
    {
        var hull = new Color(235, 238, 245);
        var hullShade = new Color(185, 190, 205);
        var red = new Color(220, 55, 50);
        var glass = new Color(90, 180, 240);

        p.Shadow(1.4f, 0.6f);

        // Chama tremulando atrás (sempre visível, é um foguete).
        float flicker = 0.12f + (MathF.Sin(p.Time * 25f) * 0.05f);
        p.Circle(-0.78f - flicker, 0f, 0.2f, new Color(255, 130, 30));
        p.Circle(-0.7f - (flicker * 0.5f), 0f, 0.13f, new Color(255, 230, 90));

        p.Rect(-0.5f, -0.33f, 0.3f, 0.2f, red, 0.5f);
        p.Rect(-0.5f, 0.33f, 0.3f, 0.2f, red, -0.5f);
        p.Capsule(-0.02f, 0f, 1.3f, 0.5f, hullShade);
        p.Capsule(0f, -0.03f, 1.24f, 0.4f, hull);
        p.Circle(0.52f, 0f, 0.2f, red);
        p.Rect(0.38f, 0f, 0.14f, 0.5f, red);
        p.Circle(0.1f, 0f, 0.13f, hullShade);
        p.Circle(0.1f, 0f, 0.1f, glass);
        p.Circle(0.07f, -0.03f, 0.03f, Color.White);
    }
}
