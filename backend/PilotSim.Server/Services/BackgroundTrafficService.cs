using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PilotSim.Data;
using PilotSim.Data.Models;
using PilotSim.Server.Hubs;

namespace PilotSim.Server.Services;

public interface IBackgroundTrafficService
{
    Task StartTrafficForSessionAsync(int sessionId, CancellationToken cancellationToken = default);
    Task StopTrafficForSessionAsync(int sessionId, CancellationToken cancellationToken = default);
    Task<List<TrafficUpdate>> GenerateTrafficUpdatesAsync(int sessionId, CancellationToken cancellationToken = default);
}

public record TrafficUpdate(
    string Callsign,
    string Transmission,
    string FlightType,
    DateTime Timestamp
);

public class BackgroundTrafficService : IBackgroundTrafficService
{
    private readonly SimDbContext _context;
    private readonly IHubContext<LiveHub> _hubContext;
    private readonly ICachingService _cachingService;
    private readonly ILogger<BackgroundTrafficService> _logger;
    private readonly Dictionary<int, CancellationTokenSource> _activeTrafficSessions = new();
    private readonly Random _random = new();

    // Realistic aviation callsigns and phrases
    private readonly string[] _aviationCallsigns = new[]
    {
        "QFA123", "VOZ456", "TGW789", "JQA012", "REX345", "FLX678", "ANZ901", "SIA234",
        "UAL567", "BAW890", "AFR123", "DLH456", "ELH789", "AUA012", "ASA345", "CPA678"
    };

    private readonly string[] _trafficTransmissions = new[]
    {
        "requesting taxi to holding point runway {runway}",
        "ready for departure runway {runway}",
        "requesting vectors for ILS approach",
        "maintaining {altitude}, requesting higher",
        "turning final runway {runway}",
        "request taxi to gate {gate}",
        "requesting startup, gate {gate}",
        "contact ground {frequency}",
        "established on localizer runway {runway}",
        "going around, runway {runway}"
    };

    public BackgroundTrafficService(
        SimDbContext context,
        IHubContext<LiveHub> hubContext,
        ICachingService cachingService,
        ILogger<BackgroundTrafficService> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _cachingService = cachingService;
        _logger = logger;
    }

    public async Task StartTrafficForSessionAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        if (_activeTrafficSessions.ContainsKey(sessionId))
        {
            _logger.LogWarning("Traffic already active for session {SessionId}", sessionId);
            return;
        }

        var cts = new CancellationTokenSource();
        _activeTrafficSessions[sessionId] = cts;

        _logger.LogInformation("Starting background traffic for session {SessionId}", sessionId);

        // Start background task for traffic generation
        _ = Task.Run(async () => await GenerateBackgroundTrafficAsync(sessionId, cts.Token), cancellationToken);
    }

    public async Task StopTrafficForSessionAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        if (_activeTrafficSessions.TryGetValue(sessionId, out var cts))
        {
            cts.Cancel();
            _activeTrafficSessions.Remove(sessionId);
            _logger.LogInformation("Stopped background traffic for session {SessionId}", sessionId);
        }

        await Task.CompletedTask;
    }

    public async Task<List<TrafficUpdate>> GenerateTrafficUpdatesAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"traffic-updates-{sessionId}";
        
        // Check cache first
        var cachedUpdates = await _cachingService.GetAsync<List<TrafficUpdate>>(cacheKey, cancellationToken);
        if (cachedUpdates != null)
        {
            return cachedUpdates;
        }

        // Generate new traffic updates
        var session = await _context.Sessions
            .Include(s => s.Scenario)
            .ThenInclude(sc => sc!.Airport)
            .ThenInclude(a => a!.Runways)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session?.Scenario?.Airport == null)
        {
            return new List<TrafficUpdate>();
        }

        var updates = new List<TrafficUpdate>();
        var airport = session.Scenario.Airport;
        var runways = airport.Runways.ToList();

        // Generate 3-5 traffic updates
        var updateCount = _random.Next(3, 6);
        for (int i = 0; i < updateCount; i++)
        {
            var callsign = _aviationCallsigns[_random.Next(_aviationCallsigns.Length)];
            var transmission = _trafficTransmissions[_random.Next(_trafficTransmissions.Length)];
            
            // Replace placeholders
            if (runways.Any())
            {
                var runway = runways[_random.Next(runways.Count)];
                transmission = transmission.Replace("{runway}", runway.Ident);
            }
            
            transmission = transmission.Replace("{altitude}", $"FL{_random.Next(20, 40):00}");
            transmission = transmission.Replace("{gate}", $"{_random.Next(1, 20)}");
            transmission = transmission.Replace("{frequency}", "121.7");

            var update = new TrafficUpdate(
                callsign,
                $"{callsign}, {transmission}",
                _random.NextDouble() > 0.7 ? "commercial" : "training",
                DateTime.UtcNow.AddMinutes(_random.Next(-5, 15))
            );

            updates.Add(update);
        }

        // Cache for 2 minutes
        await _cachingService.SetAsync(cacheKey, updates, TimeSpan.FromMinutes(2), cancellationToken);

        return updates;
    }

    private async Task GenerateBackgroundTrafficAsync(int sessionId, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Generate traffic every 45-90 seconds
                var delay = TimeSpan.FromSeconds(_random.Next(45, 91));
                await Task.Delay(delay, cancellationToken);

                var updates = await GenerateTrafficUpdatesAsync(sessionId, cancellationToken);
                
                if (updates.Any())
                {
                    var randomUpdate = updates[_random.Next(updates.Count)];
                    
                    // Send to SignalR group
                    await _hubContext.Clients.Group($"session-{sessionId}")
                        .SendAsync("backgroundTraffic", randomUpdate, cancellationToken);

                    _logger.LogDebug("Sent background traffic update for session {SessionId}: {Callsign}", 
                        sessionId, randomUpdate.Callsign);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Background traffic generation cancelled for session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in background traffic generation for session {SessionId}", sessionId);
        }
    }
}