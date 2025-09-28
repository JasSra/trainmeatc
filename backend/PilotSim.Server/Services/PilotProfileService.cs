using Microsoft.EntityFrameworkCore;
using PilotSim.Core;
using PilotSim.Data;
using PilotSim.Data.Models;

namespace PilotSim.Server.Services;

public interface IPilotProfileService
{
    Task<IReadOnlyList<PilotProfile>> GetAllPilotProfilesAsync();
    Task<IReadOnlyList<PilotProfile>> GetLivePilotProfilesAsync();
    Task<PilotProfile?> GetPilotProfileAsync(int id);
    Task<PilotProfile?> GetPilotProfileByCallsignAsync(string callsign);
    Task<PilotProfile> CreatePilotProfileAsync(PilotProfile profile);
    Task<PilotProfile> UpdatePilotProfileAsync(PilotProfile profile);
    Task<bool> DeletePilotProfileAsync(int id);
    Task<bool> StartLiveSessionAsync(int profileId, string? frequency = null);
    Task<bool> EndLiveSessionAsync(int profileId);
    Task<bool> UpdateLivePositionAsync(string callsign, SimConnectAircraftData data);
}

public class PilotProfileService : IPilotProfileService
{
    private readonly SimDbContext _context;
    private readonly ISimConnectService _simConnectService;
    private readonly ILogger<PilotProfileService> _logger;

    public PilotProfileService(
        SimDbContext context, 
        ISimConnectService simConnectService,
        ILogger<PilotProfileService> logger)
    {
        _context = context;
        _simConnectService = simConnectService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PilotProfile>> GetAllPilotProfilesAsync()
    {
        return await _context.PilotProfiles
            .Include(p => p.Aircraft)
            .OrderBy(p => p.Callsign)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PilotProfile>> GetLivePilotProfilesAsync()
    {
        return await _context.PilotProfiles
            .Include(p => p.Aircraft)
            .Where(p => p.IsLive)
            .OrderBy(p => p.Callsign)
            .ToListAsync();
    }

    public async Task<PilotProfile?> GetPilotProfileAsync(int id)
    {
        return await _context.PilotProfiles
            .Include(p => p.Aircraft)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PilotProfile?> GetPilotProfileByCallsignAsync(string callsign)
    {
        return await _context.PilotProfiles
            .Include(p => p.Aircraft)
            .FirstOrDefaultAsync(p => p.Callsign == callsign);
    }

    public async Task<PilotProfile> CreatePilotProfileAsync(PilotProfile profile)
    {
        // Ensure callsign is unique
        var existing = await _context.PilotProfiles
            .FirstOrDefaultAsync(p => p.Callsign == profile.Callsign);
            
        if (existing != null)
        {
            throw new InvalidOperationException($"A pilot profile with callsign '{profile.Callsign}' already exists");
        }

        profile.SimConnectStatus = "Disconnected";
        profile.IsLive = false;
        
        _context.PilotProfiles.Add(profile);
        await _context.SaveChangesAsync();
        
        return profile;
    }

    public async Task<PilotProfile> UpdatePilotProfileAsync(PilotProfile profile)
    {
        var existing = await _context.PilotProfiles.FindAsync(profile.Id);
        if (existing == null)
        {
            throw new InvalidOperationException($"Pilot profile with ID {profile.Id} not found");
        }

        // Update non-live properties
        existing.PilotName = profile.PilotName;
        existing.ExperienceLevel = profile.ExperienceLevel;
        existing.PreferredAirports = profile.PreferredAirports;
        existing.CertificatesRatings = profile.CertificatesRatings;
        existing.AircraftId = profile.AircraftId;
        
        // Don't allow direct updates to live properties - they should be managed by SimConnect
        
        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeletePilotProfileAsync(int id)
    {
        var profile = await _context.PilotProfiles.FindAsync(id);
        if (profile == null)
            return false;

        // End live session if active
        if (profile.IsLive)
        {
            await EndLiveSessionAsync(id);
        }

        _context.PilotProfiles.Remove(profile);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> StartLiveSessionAsync(int profileId, string? frequency = null)
    {
        var profile = await _context.PilotProfiles
            .Include(p => p.Aircraft)
            .FirstOrDefaultAsync(p => p.Id == profileId);
            
        if (profile == null)
            return false;

        // Check if aircraft supports SimConnect
        if (!profile.Aircraft.SupportsSimConnect)
        {
            _logger.LogWarning("Cannot start live session for {Callsign} - aircraft {AircraftType} does not support SimConnect", 
                profile.Callsign, profile.Aircraft.Type);
            return false;
        }

        // Ensure SimConnect is connected
        var status = await _simConnectService.GetStatusAsync();
        if (!status.IsConnected)
        {
            _logger.LogWarning("Cannot start live session for {Callsign} - SimConnect not connected", profile.Callsign);
            return false;
        }

        // Start live session
        profile.IsLive = true;
        profile.SimConnectStatus = "Connected";
        profile.AssignedFrequency = frequency ?? "118.100"; // Default tower frequency
        profile.CurrentPhase = "Ground";
        profile.LastUpdate = DateTime.UtcNow;
        
        // Set initial position (default to Melbourne airport for demo)
        profile.CurrentLatitude = -37.6733;
        profile.CurrentLongitude = 144.8433;
        profile.CurrentAltitude = 434; // YMML elevation
        profile.CurrentHeading = 90;
        profile.CurrentSpeed = 0;

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Started live session for pilot {Callsign} on frequency {Frequency}", 
            profile.Callsign, profile.AssignedFrequency);
        
        return true;
    }

    public async Task<bool> EndLiveSessionAsync(int profileId)
    {
        var profile = await _context.PilotProfiles.FindAsync(profileId);
        if (profile == null)
            return false;

        profile.IsLive = false;
        profile.SimConnectStatus = "Disconnected";
        profile.LastUpdate = DateTime.UtcNow;
        
        // Clear live position data
        profile.CurrentLatitude = null;
        profile.CurrentLongitude = null;
        profile.CurrentAltitude = null;
        profile.CurrentHeading = null;
        profile.CurrentSpeed = null;
        profile.CurrentPhase = null;
        profile.AssignedFrequency = null;

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Ended live session for pilot {Callsign}", profile.Callsign);
        return true;
    }

    public async Task<bool> UpdateLivePositionAsync(string callsign, SimConnectAircraftData data)
    {
        var profile = await _context.PilotProfiles
            .FirstOrDefaultAsync(p => p.Callsign == callsign && p.IsLive);
            
        if (profile == null)
            return false;

        profile.CurrentLatitude = data.Latitude;
        profile.CurrentLongitude = data.Longitude;
        profile.CurrentAltitude = data.Altitude;
        profile.CurrentHeading = data.Heading;
        profile.CurrentSpeed = data.Speed;
        profile.CurrentPhase = data.FlightPhase;
        profile.AssignedFrequency = data.FrequencyAssigned ?? profile.AssignedFrequency;
        profile.LastUpdate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }
}