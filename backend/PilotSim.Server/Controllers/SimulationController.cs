using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PilotSim.Core;
using PilotSim.Data;
using PilotSim.Data.Models;
using PilotSim.Server.Hubs;
using System.Text.Json;

namespace PilotSim.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SimulationController : ControllerBase
{
    private readonly ISttService _sttService;
    private readonly IInstructorService _instructorService;
    private readonly IAtcService _atcService;
    private readonly ITtsService _ttsService;
    private readonly SimDbContext _context;
    private readonly IHubContext<LiveHub> _hubContext;
    private readonly ILogger<SimulationController> _logger;

    public SimulationController(
        ISttService sttService,
        IInstructorService instructorService,
        IAtcService atcService,
        ITtsService ttsService,
        SimDbContext context,
        IHubContext<LiveHub> hubContext,
        ILogger<SimulationController> logger)
    {
        _sttService = sttService;
        _instructorService = instructorService;
        _atcService = atcService;
        _ttsService = ttsService;
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }

    public record ProcessTurnRequest(int SessionId, IFormFile Audio, string? PartialTranscript = null);
    public record TurnResult(string Transcript, InstructorVerdict Verdict, AtcReply AtcResponse, string? TtsAudioPath);

    [HttpPost("turn")]
    public async Task<ActionResult<TurnResult>> ProcessTurnAsync([FromForm] ProcessTurnRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Audio == null || request.Audio.Length == 0)
            return BadRequest("Audio file is required");

        try
        {
            // Get session and current state
            var session = await _context.Sessions
                .Include(s => s.Turns)
                .Include(s => s.Scenario)
                .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);

            if (session == null)
                return NotFound("Session not found");

            // Send partial transcript update if provided
            if (!string.IsNullOrEmpty(request.PartialTranscript))
            {
                await _hubContext.Clients.Group($"session-{request.SessionId}")
                    .SendAsync("partialTranscript", request.PartialTranscript, cancellationToken);
            }

            // STT: Transcribe audio
            string transcript;
            using (var audioStream = request.Audio.OpenReadStream())
            {
                var biasPrompt = "Use Australian aviation terms. Airport identifiers YSSY, YBBN, YMML, YPAD. Words: QNH, runway, squawk, kilo, papa, hPa. Callsign format VH-XXX.";
                var sttResult = await _sttService.TranscribeAsync(audioStream, biasPrompt, cancellationToken);
                transcript = sttResult.Text;
            }

            // Get current simulation state
            var currentState = GetCurrentState(session);
            var difficulty = Enum.Parse<Difficulty>(session.Difficulty ?? "Basic");

            // Instructor: Score the transmission
            var verdict = await _instructorService.ScoreAsync(transcript, currentState, difficulty, cancellationToken);
            
            // Send instructor verdict
            await _hubContext.Clients.Group($"session-{request.SessionId}")
                .SendAsync("instructorVerdict", verdict, cancellationToken);

            // Check if we should block (critical errors)
            if (verdict.Critical.Any() && !string.IsNullOrEmpty(verdict.BlockReason))
            {
                // Save turn and return without ATC response
                await SaveTurn(session, transcript, verdict, null, null, cancellationToken);
                return Ok(new TurnResult(transcript, verdict, null!, null));
            }

            // ATC: Generate response
            var load = new Load(0.5f, 0.8f, "Professional", "Clear");
            var atcResponse = await _atcService.NextAsync(transcript, currentState, difficulty, load, cancellationToken);

            // Send ATC transmission
            await _hubContext.Clients.Group($"session-{request.SessionId}")
                .SendAsync("atcTransmission", atcResponse, cancellationToken);

            // TTS: Synthesize ATC response
            string? ttsAudioPath = null;
            if (!string.IsNullOrEmpty(atcResponse.Transmission))
            {
                ttsAudioPath = await _ttsService.SynthesizeAsync(
                    atcResponse.Transmission, 
                    "professional", 
                    atcResponse.TtsTone ?? "neutral", 
                    cancellationToken);
                
                if (!string.IsNullOrEmpty(ttsAudioPath))
                {
                    await _hubContext.Clients.Group($"session-{request.SessionId}")
                        .SendAsync("ttsReady", ttsAudioPath, cancellationToken);
                }
            }

            // Update score
            session.ScoreTotal += verdict.ScoreDelta;
            await _context.SaveChangesAsync(cancellationToken);

            // Send score update
            await _hubContext.Clients.Group($"session-{request.SessionId}")
                .SendAsync("scoreTick", session.ScoreTotal, cancellationToken);

            // Save turn
            await SaveTurn(session, transcript, verdict, atcResponse, ttsAudioPath, cancellationToken);

            return Ok(new TurnResult(transcript, verdict, atcResponse, ttsAudioPath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process turn for session {SessionId}", request.SessionId);
            return StatusCode(500, "Internal server error processing turn");
        }
    }

    [HttpGet("session/{sessionId}/scenario")]
    public async Task<ActionResult<object>> GetScenarioAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _context.Sessions
            .Include(s => s.Scenario)
            .ThenInclude(sc => sc!.Airport)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session?.Scenario == null)
            return NotFound();

        return Ok(new
        {
            ScenarioId = session.Scenario.Id,
            Name = session.Scenario.Name,
            Airport = session.Scenario.Airport?.Name,
            AirportIcao = session.Scenario.AirportIcao,
            Difficulty = session.Scenario.Difficulty,
            InitialState = session.Scenario.InitialStateJson
        });
    }

    private object GetCurrentState(Session session)
    {
        // Get the last turn's state or initial state
        var lastTurn = session.Turns.OrderBy(t => t.Idx).LastOrDefault();
        if (lastTurn?.AtcJson != null)
        {
            var atcReply = JsonSerializer.Deserialize<AtcReply>(lastTurn.AtcJson);
            return atcReply?.NextState ?? GetInitialState(session);
        }

        return GetInitialState(session);
    }

    private object GetInitialState(Session session)
    {
        if (!string.IsNullOrEmpty(session.Scenario?.InitialStateJson))
        {
            return JsonSerializer.Deserialize<object>(session.Scenario.InitialStateJson) 
                   ?? new { position = "gate", ready = false };
        }

        return new { position = "gate", ready = false };
    }

    private async Task SaveTurn(Session session, string transcript, InstructorVerdict verdict, AtcReply? atcResponse, string? ttsAudioPath, CancellationToken cancellationToken)
    {
        var turnIndex = session.Turns.Count;
        
        var turn = new Turn
        {
            SessionId = session.Id,
            Idx = turnIndex,
            UserTranscript = transcript,
            InstructorJson = JsonSerializer.Serialize(verdict),
            AtcJson = atcResponse != null ? JsonSerializer.Serialize(atcResponse) : null,
            TtsAudioPath = ttsAudioPath,
            Verdict = verdict.BlockReason
        };

        _context.Turns.Add(turn);
        await _context.SaveChangesAsync(cancellationToken);
    }
}