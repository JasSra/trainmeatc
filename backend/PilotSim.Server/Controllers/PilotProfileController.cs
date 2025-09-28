using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PilotSim.Core;
using PilotSim.Data.Models;
using PilotSim.Server.Hubs;
using PilotSim.Server.Services;

namespace PilotSim.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PilotProfileController : ControllerBase
{
    private readonly IPilotProfileService _pilotProfileService;
    private readonly ISimConnectService _simConnectService;
    private readonly IHubContext<LiveHub> _hubContext;
    private readonly ILogger<PilotProfileController> _logger;

    public PilotProfileController(
        IPilotProfileService pilotProfileService,
        ISimConnectService simConnectService,
        IHubContext<LiveHub> hubContext,
        ILogger<PilotProfileController> logger)
    {
        _pilotProfileService = pilotProfileService;
        _simConnectService = simConnectService;
        _hubContext = hubContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PilotProfile>>> GetPilotProfiles()
    {
        var profiles = await _pilotProfileService.GetAllPilotProfilesAsync();
        return Ok(profiles);
    }

    [HttpGet("live")]
    public async Task<ActionResult<IReadOnlyList<PilotProfile>>> GetLivePilotProfiles()
    {
        var profiles = await _pilotProfileService.GetLivePilotProfilesAsync();
        return Ok(profiles);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PilotProfile>> GetPilotProfile(int id)
    {
        var profile = await _pilotProfileService.GetPilotProfileAsync(id);
        if (profile == null)
            return NotFound();
        
        return Ok(profile);
    }

    [HttpGet("callsign/{callsign}")]
    public async Task<ActionResult<PilotProfile>> GetPilotProfileByCallsign(string callsign)
    {
        var profile = await _pilotProfileService.GetPilotProfileByCallsignAsync(callsign);
        if (profile == null)
            return NotFound();
        
        return Ok(profile);
    }

    [HttpPost]
    public async Task<ActionResult<PilotProfile>> CreatePilotProfile(CreatePilotProfileRequest request)
    {
        try
        {
            var profile = new PilotProfile
            {
                Callsign = request.Callsign,
                AircraftId = request.AircraftId,
                PilotName = request.PilotName,
                ExperienceLevel = request.ExperienceLevel,
                PreferredAirports = request.PreferredAirports,
                CertificatesRatings = request.CertificatesRatings
            };

            var created = await _pilotProfileService.CreatePilotProfileAsync(profile);
            return CreatedAtAction(nameof(GetPilotProfile), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PilotProfile>> UpdatePilotProfile(int id, UpdatePilotProfileRequest request)
    {
        try
        {
            var profile = new PilotProfile
            {
                Id = id,
                Callsign = request.Callsign,
                AircraftId = request.AircraftId,
                PilotName = request.PilotName,
                ExperienceLevel = request.ExperienceLevel,
                PreferredAirports = request.PreferredAirports,
                CertificatesRatings = request.CertificatesRatings
            };

            var updated = await _pilotProfileService.UpdatePilotProfileAsync(profile);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeletePilotProfile(int id)
    {
        var deleted = await _pilotProfileService.DeletePilotProfileAsync(id);
        if (!deleted)
            return NotFound();
        
        return NoContent();
    }

    [HttpPost("{id}/start-live")]
    public async Task<ActionResult> StartLiveSession(int id, StartLiveSessionRequest request)
    {
        var started = await _pilotProfileService.StartLiveSessionAsync(id, request.Frequency);
        if (!started)
            return BadRequest("Could not start live session. Check that SimConnect is connected and aircraft supports integration.");
        
        // Notify clients about the live session start
        await _hubContext.Clients.All.SendAsync("pilotLiveSessionStarted", id);
        
        return Ok();
    }

    [HttpPost("{id}/end-live")]
    public async Task<ActionResult> EndLiveSession(int id)
    {
        var ended = await _pilotProfileService.EndLiveSessionAsync(id);
        if (!ended)
            return NotFound();
        
        // Notify clients about the live session end
        await _hubContext.Clients.All.SendAsync("pilotLiveSessionEnded", id);
        
        return Ok();
    }

    [HttpGet("simconnect/status")]
    public async Task<ActionResult<SimConnectStatus>> GetSimConnectStatus()
    {
        var status = await _simConnectService.GetStatusAsync();
        return Ok(status);
    }

    [HttpPost("simconnect/connect")]
    public async Task<ActionResult> ConnectSimConnect()
    {
        var connected = await _simConnectService.ConnectAsync();
        if (!connected)
            return BadRequest("Failed to connect to MSFS SimConnect");
        
        return Ok();
    }

    [HttpPost("simconnect/disconnect")]
    public async Task<ActionResult> DisconnectSimConnect()
    {
        await _simConnectService.DisconnectAsync();
        return Ok();
    }

    [HttpGet("simconnect/aircraft")]
    public async Task<ActionResult<IReadOnlyList<SimConnectAircraftData>>> GetActiveAircraft()
    {
        var aircraft = await _simConnectService.GetActiveAircraftAsync();
        return Ok(aircraft);
    }

    [HttpPost("{callsign}/atc-command")]
    public async Task<ActionResult> SendAtcCommand(string callsign, SendAtcCommandRequest request)
    {
        var sent = await _simConnectService.SendAtcCommandAsync(callsign, request.Command);
        if (!sent)
            return BadRequest("Failed to send ATC command. Check SimConnect connection.");
        
        return Ok();
    }

    public record CreatePilotProfileRequest(
        string Callsign,
        int AircraftId,
        string? PilotName,
        string? ExperienceLevel,
        string? PreferredAirports,
        string? CertificatesRatings);

    public record UpdatePilotProfileRequest(
        string Callsign,
        int AircraftId,
        string? PilotName,
        string? ExperienceLevel,
        string? PreferredAirports,
        string? CertificatesRatings);

    public record StartLiveSessionRequest(string? Frequency = null);
    public record SendAtcCommandRequest(string Command);
}