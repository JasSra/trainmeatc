using Microsoft.EntityFrameworkCore;
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

// Add service implementations (stubs for now)
builder.Services.AddSingleton<ISttService, StubSttService>();
builder.Services.AddScoped<IInstructorService, StubInstructorService>();
builder.Services.AddScoped<IAtcService, StubAtcService>();
builder.Services.AddSingleton<ITtsService, StubTtsService>();

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
