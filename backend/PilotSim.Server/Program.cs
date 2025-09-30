using Microsoft.EntityFrameworkCore;
using OpenAI;
using PilotSim.Core;
using PilotSim.Data;
using PilotSim.Server.Components;
using PilotSim.Server.Hubs;
using PilotSim.Server.Services;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Components; // Added for NavigationManager

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add SignalR
builder.Services.AddSignalR();

// Add HttpClient for Blazor components (factory)
builder.Services.AddHttpClient();
// Provide a scoped HttpClient with a BaseAddress so relative URLs work in components
builder.Services.AddScoped(sp =>
{
    var nav = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(nav.BaseUri) };
});

// Add Memory Caching
builder.Services.AddMemoryCache();

// Add Redis Caching (optional, fallback to memory cache if not configured)
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
    });
}
else
{
    // Add NullObjectPattern distributed cache when Redis is not available
    builder.Services.AddDistributedMemoryCache();
}

// Add DbContext
builder.Services.AddDbContext<SimDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("SimDb")));

builder.Services.AddSingleton(new OpenAIClient(builder.Configuration["OPENAI_API_KEY"]));
// Register OpenAI-backed services
builder.Services.AddSingleton<ISttService, OpenAiSttService>();
builder.Services.AddScoped<PilotSim.Core.IInstructorService, PilotSim.Server.Services.OpenAiInstructorServiceV2>();
builder.Services.AddScoped<PilotSim.Server.Services.ITrafficAgent, PilotSim.Server.Services.OpenAiTrafficAgentService>();
builder.Services.AddScoped<PilotSim.Server.Services.IResponderRouter, PilotSim.Server.Services.ResponderRouter>();
builder.Services.AddScoped<PilotSim.Server.Services.ITurnService, PilotSim.Server.Services.TurnService>();
// Adapter for backward compatibility with SimulationController
builder.Services.AddScoped<PilotSim.Core.IAtcService, PilotSim.Server.Services.AtcServiceAdapter>();
builder.Services.AddSingleton<ITtsService, OpenAiTtsService>();

// Add API controllers
builder.Services.AddControllers();

// Add caching service
builder.Services.AddScoped<ICachingService, CachingService>();

// Add Milestone 4 services
builder.Services.AddScoped<IBackgroundTrafficService, BackgroundTrafficService>();
builder.Services.AddScoped<IMetarService, MetarService>();


// Add chart service for aviation documentation
builder.Services.AddScoped<IAirserviceChartsService, AirserviceChartsService>();

// Add MSFS SimConnect integration services
builder.Services.AddScoped<ISimConnectService, SimConnectService>();
builder.Services.AddScoped<IPilotProfileService, PilotProfileService>();


var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SimDbContext>();
    await DbInitializer.InitializeAsync(context);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Serve static files (audio, user uploads, etc.)
app.UseStaticFiles();

app.UseAntiforgery();

// Map API controllers
app.MapControllers();

// Map SignalR hub
app.MapHub<LiveHub>("/hubs/live");

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
