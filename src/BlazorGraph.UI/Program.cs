using BlazorGraph.UI.Components;
using Gremlin.Net.Driver;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<GremlinClient>(_ => new GremlinClient(new GremlinServer(
    builder.Configuration["Gremlin:Hostname"],
    int.Parse(builder.Configuration["Gremlin:Port"] ?? "0"),
    bool.Parse(builder.Configuration["Gremlin:EnableSsl"] ?? "false")
)));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();