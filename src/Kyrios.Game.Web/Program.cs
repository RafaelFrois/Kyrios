using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Kyrios.Game.Web;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<GameHost>("#game-host");
        await builder.Build().RunAsync();
    }
}
