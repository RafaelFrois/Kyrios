using Kyrios.Core;
using Kyrios.ConsoleGame;

const float FixedDeltaTime = 1f / 30f;
const int FrameDelayMs = 33;

if (args.Contains("--simulate") || args.Contains("-s"))
{
    RunHeadlessSimulation(args);
    return;
}

if (Console.IsInputRedirected || Console.IsOutputRedirected)
{
    Console.WriteLine("O modo interativo precisa de um terminal de verdade (teclado + tela).");
    Console.WriteLine("Rode este executável diretamente em um terminal, ou use `--simulate` para uma corrida sem interface.");
    return;
}

RunInteractiveGame();
return;

static void RunInteractiveGame()
{
    Console.OutputEncoding = System.Text.Encoding.UTF8;
    Console.Title = "Kyrios - Corrida Top-Vision";
    Console.CursorVisible = false;

    try
    {
        ShowIntroScreen();

        bool playAgain = true;
        while (playAgain)
        {
            playAgain = PlayOneRace();
        }
    }
    finally
    {
        Console.ResetColor();
        Console.CursorVisible = true;
        Console.Clear();
    }
}

static void ShowIntroScreen()
{
    Console.Clear();
    Console.WriteLine("=========================================");
    Console.WriteLine("   KYRIOS - CORRIDA TOP-VISION (C#)");
    Console.WriteLine("=========================================");
    Console.WriteLine();
    Console.WriteLine("Controles:");
    Console.WriteLine("  Setas ou W A S D  -> acelerar / frear / virar");
    Console.WriteLine("  Espaço             -> freio de mão");
    Console.WriteLine("  Esc                -> sair a qualquer momento");
    Console.WriteLine("  R (no fim)         -> correr de novo");
    Console.WriteLine();
    Console.WriteLine("Complete 3 voltas passando pelos checkpoints na");
    Console.WriteLine("ordem certa antes de cruzar a linha de chegada.");
    Console.WriteLine();
    Console.WriteLine("Pressione qualquer tecla para começar...");
    Console.ReadKey(intercept: true);
}

static bool PlayOneRace()
{
    RaceSimulation race = RaceFactory.CreateDefaultRace(aiOpponents: 3, targetLaps: 3);
    RaceEntrant player = race.Entrants.First(e => e.Kind == DriverKind.Human);
    var renderer = new ConsoleRenderer(race.Track);
    var input = new InputManager();

    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    float accumulator = 0f;
    bool aborted = false;

    Console.Clear();

    while (!race.IsRaceOver)
    {
        input.Poll();
        if (input.QuitRequested)
        {
            aborted = true;
            break;
        }

        float elapsed = (float)stopwatch.Elapsed.TotalSeconds;
        stopwatch.Restart();
        accumulator += Math.Min(elapsed, 0.25f);

        while (accumulator >= FixedDeltaTime)
        {
            race.Update(FixedDeltaTime, input.BuildCarInput());
            accumulator -= FixedDeltaTime;
        }

        renderer.Render(race, BuildHudLines(race, player, raceOver: false));
        Thread.Sleep(FrameDelayMs);
    }

    if (aborted)
    {
        Console.Clear();
        Console.WriteLine("Corrida abortada. Até a próxima!");
        return false;
    }

    renderer.Render(race, BuildHudLines(race, player, raceOver: true));
    Console.SetCursorPosition(0, race.Track.Height + 14);
    Console.WriteLine();
    Console.WriteLine("Pressione R para correr de novo ou qualquer outra tecla para sair...");

    ConsoleKeyInfo key = Console.ReadKey(intercept: true);
    return key.Key == ConsoleKey.R;
}

static List<string> BuildHudLines(RaceSimulation race, RaceEntrant player, bool raceOver)
{
    var lines = new List<string>
    {
        string.Empty,
        raceOver
            ? "=== CORRIDA FINALIZADA ==="
            : $"Volta {Math.Min(player.Car.LapsCompleted + 1, race.TargetLaps)}/{race.TargetLaps}  " +
              $"Tempo da volta: {FormatTime(player.Car.CurrentLapTime)}  " +
              $"Velocidade: {player.Car.Speed:0.0}",
        $"Melhor volta: {(player.Car.BestLapTime is { } best ? FormatTime(best) : "--:--.---")}   " +
        $"Tempo total: {FormatTime(player.Car.TotalRaceTime)}",
        string.Empty,
        "Classificação:",
    };

    int position = 1;
    foreach (RaceEntrant entrant in race.GetStandings())
    {
        string marker = entrant.Kind == DriverKind.Human ? "* " : "  ";
        string status = entrant.Finished
            ? $"chegou em {FormatTime(entrant.FinishTime ?? 0f)}"
            : $"volta {entrant.Car.LapsCompleted}/{race.TargetLaps}";
        lines.Add($" {position,2}. {marker}{entrant.Car.Name,-8} {status}");
        position++;
    }

    if (!raceOver)
    {
        lines.Add(string.Empty);
        lines.Add("Setas/WASD: dirigir | Espaço: freio de mão | Esc: sair");
    }

    return lines;
}

static string FormatTime(float seconds)
{
    if (seconds < 0f)
    {
        seconds = 0f;
    }

    var span = TimeSpan.FromSeconds(seconds);
    return $"{(int)span.TotalMinutes:00}:{span.Seconds:00}.{span.Milliseconds:000}";
}

static void RunHeadlessSimulation(string[] args)
{
    int aiOpponents = 3;
    int targetLaps = 3;
    int? seed = null;
    RaceMode mode = RaceMode.Sprint;

    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] == "--ai" && i + 1 < args.Length && int.TryParse(args[i + 1], out int ai))
        {
            aiOpponents = ai;
        }
        else if (args[i] == "--laps" && i + 1 < args.Length && int.TryParse(args[i + 1], out int laps))
        {
            targetLaps = laps;
        }
        else if (args[i] == "--seed" && i + 1 < args.Length && int.TryParse(args[i + 1], out int s))
        {
            seed = s;
        }
        else if (args[i] == "--mode" && i + 1 < args.Length && args[i + 1].Equals("elimination", StringComparison.OrdinalIgnoreCase))
        {
            mode = RaceMode.Elimination;
        }
        else if (args[i] == "--mode" && i + 1 < args.Length && args[i + 1].Equals("timeattack", StringComparison.OrdinalIgnoreCase))
        {
            mode = RaceMode.TimeAttack;
        }
    }

    Console.WriteLine($"Simulação sem interface: {aiOpponents} IA(s), {targetLaps} volta(s), modo={mode}, semente={(seed?.ToString() ?? "aleatória")}");

    RaceSimulation race = RaceFactory.CreateDefaultRace(aiOpponents: aiOpponents, targetLaps: targetLaps, includeHuman: false, randomSeed: seed, mode: mode);

    const float dt = 0.05f;
    const float maxSimSeconds = 180f;
    float elapsed = 0f;

    while (!race.IsRaceOver && elapsed < maxSimSeconds)
    {
        race.Update(dt, CarInput.None);
        elapsed += dt;
    }

    Console.WriteLine(race.IsRaceOver
        ? $"Corrida concluída em {elapsed:0.0}s (tempo simulado)."
        : $"Tempo limite de simulação atingido ({maxSimSeconds:0}s) sem todos terminarem.");

    if (mode == RaceMode.TimeAttack)
    {
        Console.WriteLine($"Pontuação final de {race.ScoredEntrant.Car.Name}: {race.ScoredEntrant.Score:0} pontos " +
            $"(voltas: {race.ScoredEntrant.Car.LapsCompleted}, tempo restante: {race.TimeRemaining:0.00}s)");
        return;
    }

    Console.WriteLine();
    Console.WriteLine("Classificação final:");

    int position = 1;
    foreach (RaceEntrant entrant in race.GetStandings())
    {
        string status = entrant.Finished
            ? $"chegou em {FormatTime(entrant.FinishTime ?? 0f)}"
            : entrant.Eliminated
                ? "eliminado"
                : $"não terminou (volta {entrant.Car.LapsCompleted}/{race.TargetLaps})";
        string best = entrant.Car.BestLapTime is { } b ? FormatTime(b) : "--:--.---";
        Console.WriteLine($"  {position,2}. {entrant.Car.Name,-8} {status}  | melhor volta: {best}");
        position++;
    }
}
