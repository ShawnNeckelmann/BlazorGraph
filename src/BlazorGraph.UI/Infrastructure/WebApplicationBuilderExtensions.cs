using BlazorGraph.Persistence.Repositories;
using BlazorGraph.Persistence.Seed;
using Gremlin.Net.Driver;

namespace BlazorGraph.UI.Infrastructure;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder RegisterServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<UserRepository>();
        builder.Services.AddSingleton<GraphSeedingService>();
        builder.Services.AddSingleton<GremlinClient>(_ => new GremlinClient(new GremlinServer(
            builder.Configuration["Gremlin:Hostname"],
            int.Parse(builder.Configuration["Gremlin:Port"] ?? "0"),
            bool.Parse(builder.Configuration["Gremlin:EnableSsl"] ?? "false")
        )));

        return builder;
    }
}