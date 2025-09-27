using Microsoft.EntityFrameworkCore;
using OpenAI;
using PilotSim.Core;
using PilotSim.Data;
using PilotSim.Server.Components;
using PilotSim.Server.Hubs;
using PilotSim.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add SignalR
builder.Services.AddSignalR();

// Add DbContext
builder.Services.AddDbContext<SimDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("SimDb")));

// Add OpenAI client - required for all service implementations
var openAiApiKey = builder.Configuration["OPENAI_API_KEY"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (string.IsNullOrEmpty(openAiApiKey))
{
    throw new InvalidOperationException(
        "OPENAI_API_KEY is required. Please set it in configuration or as an environment variable.");
}

builder.Services.AddSingleton(new OpenAIClient(openAiApiKey));

// Add OpenAI service implementations
builder.Services.AddSingleton<ISttService, OpenAiSttService>();
builder.Services.AddScoped<IInstructorService, OpenAiInstructorService>();
builder.Services.AddScoped<IAtcService, OpenAiAtcService>();
builder.Services.AddSingleton<ITtsService, OpenAiTtsService>();

// Add API controllers
builder.Services.AddControllers();

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
