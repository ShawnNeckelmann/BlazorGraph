using BlazorGraph.Persistence.Seed;
using BlazorGraph.UI.Components;
using BlazorGraph.UI.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.RegisterServices();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.SeedGremlinDatabase();
var scope = app.Services.CreateScope();
var seedService = scope.ServiceProvider.GetRequiredService<GraphSeedingService>();
await seedService.SeedAsync();


await app.RunAsync();