using Kyrios.Game;
using Kyrios.MobilePreview;

// Uso: dotnet run --project tools/Kyrios.MobilePreview -- --device phone-20x9 --script roteiro.txt --out capturas [--save save.json]
PreviewOptions options = PreviewOptions.Parse(args);
var platform = new PreviewPlatform(options);
GamePlatform.Current = platform;

using var game = new GameRoot();
var director = new PreviewDirector(game, platform, options);
game.Components.Add(director);
game.Run();

if (director.Failures.Count > 0)
{
    Console.WriteLine($"{director.Failures.Count} falha(s) no roteiro.");
    return 1;
}

return 0;
