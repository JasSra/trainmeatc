using Microsoft.EntityFrameworkCore;
using PilotSim.Core;
using PilotSim.Data;
using PilotSim.Data.Models;
using System.Collections.Concurrent;

namespace PilotSim.Server.Services;

public class SimConnectService : ISimConnectService, IDisposable
{
    private readonly SimDbContext _context;
    private readonly ILogger<SimConnectService> _logger;
    private readonly ConcurrentDictionary<string, SimConnectAircraftData> _activeAircraft = new();
    private readonly Timer _updateTimer;
    private bool _isConnected = false;
    private bool _disposed = false;

    public event EventHandler<SimConnectAircraftData>? AircraftPositionUpdated;
    public event EventHandler<string>? AircraftConnected;
    public event EventHandler<string>? AircraftDisconnected;

    public SimConnectService(SimDbContext context, ILogger<SimConnectService> logger)
    {
        _context = context;
        _logger = logger;
        
        // Update every 5 seconds when connected
        _updateTimer = new Timer(UpdateAircraftData, null, Timeout.Infinite, Timeout.Infinite);
    }

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Connecting to MSFS SimConnect...");
            
            // Simulate SimConnect connection - in real implementation this would:
            // 1. Initialize SimConnect SDK
            // 2. Set up data definitions for aircraft position, attitude, etc.
            // 3. Request periodic updates
            // 4. Handle connection events
            
            _isConnected = true;
            _updateTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(5));
            
            _logger.LogInformation("Successfully connected to MSFS SimConnect");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to MSFS SimConnect");
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            _logger.LogInformation("Disconnecting from MSFS SimConnect...");
            
            _isConnected = false;
            _updateTimer.Change(Timeout.Infinite, Timeout.Infinite);
            
            // Update all active pilots to disconnected status
            var activePilots = await _context.PilotProfiles
                .Where(p => p.IsLive)
                .ToListAsync();
                
            foreach (var pilot in activePilots)
            {
                pilot.IsLive = false;
                pilot.SimConnectStatus = "Disconnected";
                pilot.LastUpdate = DateTime.UtcNow;
            }
            
            await _context.SaveChangesAsync();
            _activeAircraft.Clear();
            
            _logger.LogInformation("Disconnected from MSFS SimConnect");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting from MSFS SimConnect");
        }
    }

    public Task<SimConnectStatus> GetStatusAsync()
    {
        return Task.FromResult(new SimConnectStatus(
            _isConnected,
            _isConnected ? "Connected" : "Disconnected",
            DateTime.UtcNow,
            _activeAircraft.Count
        ));
    }

    public Task<IReadOnlyList<SimConnectAircraftData>> GetActiveAircraftAsync()
    {
        return Task.FromResult<IReadOnlyList<SimConnectAircraftData>>(_activeAircraft.Values.ToList());
    }

    public Task<SimConnectAircraftData?> GetAircraftDataAsync(string callsign)
    {
        _activeAircraft.TryGetValue(callsign, out var data);
        return Task.FromResult(data);
    }

    public async Task<bool> SendAtcCommandAsync(string callsign, string command, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_isConnected)
                return false;

            _logger.LogInformation("Sending ATC command to {Callsign}: {Command}", callsign, command);
            
            // In real implementation, this would:
            // 1. Send text-to-speech command to MSFS
            // 2. Potentially trigger aircraft system changes
            // 3. Update flight plan or navigation data
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send ATC command to {Callsign}: {Command}", callsign, command);
            return false;
        }
    }

    private async void UpdateAircraftData(object? state)
    {
        if (!_isConnected || _disposed)
            return;

        try
        {
            // In real implementation, this would poll SimConnect for aircraft data
            // For now, simulate some aircraft data for demo purposes
            await SimulateAircraftData();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating aircraft data");
        }
    }

    private async Task SimulateAircraftData()
    {
        var livePilots = await _context.PilotProfiles
            .Where(p => p.IsLive)
            .Include(p => p.Aircraft)
            .ToListAsync();

        var random = new Random();
        
        foreach (var pilot in livePilots)
        {
            // Simulate realistic position changes
            var currentLat = pilot.CurrentLatitude ?? -37.8136; // Melbourne default
            var currentLon = pilot.CurrentLongitude ?? 144.9631;
            var currentAlt = pilot.CurrentAltitude ?? 1000;
            var currentHdg = pilot.CurrentHeading ?? 90;
            var currentSpd = pilot.CurrentSpeed ?? 120;

            // Small random changes to simulate movement
            var newLat = currentLat + (random.NextDouble() - 0.5) * 0.001; // ~100m movement
            var newLon = currentLon + (random.NextDouble() - 0.5) * 0.001;
            var newAlt = Math.Max(0, currentAlt + (random.NextDouble() - 0.5) * 100); // +/- 50ft
            var newHdg = (currentHdg + (random.NextDouble() - 0.5) * 10) % 360; // +/- 5 degrees
            var newSpd = Math.Max(0, currentSpd + (random.NextDouble() - 0.5) * 20); // +/- 10 knots

            // Update pilot profile
            pilot.CurrentLatitude = newLat;
            pilot.CurrentLongitude = newLon;
            pilot.CurrentAltitude = newAlt;
            pilot.CurrentHeading = newHdg;
            pilot.CurrentSpeed = newSpd;
            pilot.LastUpdate = DateTime.UtcNow;
            pilot.SimConnectStatus = "Connected";

            // Update flight phase based on altitude and speed
            pilot.CurrentPhase = newAlt < 200 && newSpd < 50 ? "Ground" :
                                newAlt < 500 && newSpd < 100 ? "Taxi" :
                                newAlt < 1000 ? "Takeoff" :
                                newAlt > 5000 && newSpd > 200 ? "Cruise" : "Climb";

            var aircraftData = new SimConnectAircraftData(
                pilot.Callsign,
                newLat,
                newLon,
                newAlt,
                newHdg,
                newSpd,
                pilot.CurrentPhase,
                pilot.AssignedFrequency
            );

            _activeAircraft.AddOrUpdate(pilot.Callsign, aircraftData, (key, oldValue) => aircraftData);
            
            // Fire event for live updates
            AircraftPositionUpdated?.Invoke(this, aircraftData);
        }

        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _updateTimer?.Dispose();
        
        // Disconnect from SimConnect if still connected
        if (_isConnected)
        {
            _ = Task.Run(async () => await DisconnectAsync());
        }
    }
}