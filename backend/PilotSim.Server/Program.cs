using Microsoft.EntityFrameworkCore;
using OpenAI;
using PilotSim.Core;
using PilotSim.Data;
using PilotSim.Server.Components;
using PilotSim.Server.Hubs;
using PilotSim.Server.Services;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add SignalR
builder.Services.AddSignalR();

// Add HttpClient for Blazor components
builder.Services.AddHttpClient();

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

// Add OpenAI client
var openAiApiKey = builder.Configuration["OPENAI_API_KEY"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (!string.IsNullOrEmpty(openAiApiKey))
{
    builder.Services.AddSingleton(new OpenAIClient(openAiApiKey));
    
    // Add OpenAI service implementations
    builder.Services.AddSingleton<ISttService, OpenAiSttService>();
    builder.Services.AddScoped<IInstructorService, OpenAiInstructorService>();
    builder.Services.AddScoped<IAtcService, OpenAiAtcService>();
    builder.Services.AddSingleton<ITtsService, OpenAiTtsService>();
}
else
{
    // Fallback to stub implementations if no API key
    builder.Services.AddSingleton<ISttService, StubSttService>();
    builder.Services.AddScoped<IInstructorService, StubInstructorService>();
    builder.Services.AddScoped<IAtcService, StubAtcService>();
    builder.Services.AddSingleton<ITtsService, StubTtsService>();
}

// Add API controllers
builder.Services.AddControllers();

// Add caching service
builder.Services.AddScoped<ICachingService, CachingService>();

// Add Milestone 4 services
builder.Services.AddScoped<IBackgroundTrafficService, BackgroundTrafficService>();
builder.Services.AddScoped<IMetarService, MetarService>();

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

app.UseAntiforgery();

// Map API controllers
app.MapControllers();

// Map SignalR hub
app.MapHub<LiveHub>("/hubs/live");

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
