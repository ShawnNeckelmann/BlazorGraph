using BlazorGraph.Persistence.Seed;

namespace BlazorGraph.UI.Infrastructure;

public static class WebApplicationExtensions
{
    public static async Task SeedGremlinDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var seedService = scope.ServiceProvider.GetRequiredService<GraphSeedingService>();
        await seedService.SeedAsync();
    }
}