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
            var overallSw = System.Diagnostics.Stopwatch.StartNew();
            var turnStartIso = DateTime.UtcNow.ToString("O");
            int sttMs = 0, instructorMs = 0, atcMs = 0, ttsMs = 0;
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

            // STT: Transcribe audio (+ persist raw upload for replay)
            string transcript;
            string? userAudioPath = null;
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                using var audioStream = request.Audio.OpenReadStream();
                // Persist original audio (WAV/other) for replay
                var ext = Path.GetExtension(request.Audio.FileName);
                if (string.IsNullOrWhiteSpace(ext) || ext.Length > 5) ext = ".wav"; // fallback
                var audioDir = Path.Combine("wwwroot", "useraudio");
                Directory.CreateDirectory(audioDir);
                userAudioPath = Path.Combine(audioDir, $"u_{Guid.NewGuid():N}{ext}");
                using (var fs = System.IO.File.Create(userAudioPath))
                {
                    await request.Audio.CopyToAsync(fs, cancellationToken);
                }
                var publicUserAudioPath = "/" + userAudioPath.Replace("\\", "/");
                var biasPrompt = "Use Australian aviation terms. Airport identifiers YSSY, YBBN, YMML, YPAD. Words: QNH, runway, squawk, kilo, papa, hPa. Callsign format VH-XXX.";
                var sttResult = await _sttService.TranscribeAsync(audioStream, biasPrompt, cancellationToken);
                sw.Stop();
                sttMs = (int)sw.ElapsedMilliseconds;
                transcript = sttResult.Text;
                // Replace local path with public path after save
                userAudioPath = publicUserAudioPath;
            }

            // Get current simulation state
            var currentState = GetCurrentState(session);
            var difficulty = Enum.Parse<Difficulty>(session.Difficulty ?? "Basic");

            // Instructor: Score the transmission
            InstructorVerdict verdict;
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                verdict = await _instructorService.ScoreAsync(transcript, currentState, difficulty, cancellationToken);
                sw.Stop();
                instructorMs = (int)sw.ElapsedMilliseconds;
            }
            
            // Send instructor verdict
            await _hubContext.Clients.Group($"session-{request.SessionId}")
                .SendAsync("instructorVerdict", verdict, cancellationToken);

            // Check if we should block (critical errors)
            var blocked = verdict.Critical.Any() && !string.IsNullOrEmpty(verdict.BlockReason);
            if (blocked)
            {
                // Save blocked turn without ATC response
                overallSw.Stop();
                var totalMsBlocked = (int)overallSw.ElapsedMilliseconds;
                await SaveTurn(session, transcript, verdict, null, null, userAudioPath, turnStartIso, sttMs, instructorMs, 0, 0, totalMsBlocked, blocked, cancellationToken);
                return Ok(new TurnResult(transcript, verdict, null!, null));
            }

            // ATC: Generate response
            var load = new Load(0.5f, 0.8f, "Professional", "Clear");
            AtcReply atcResponse;
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                atcResponse = await _atcService.NextAsync(transcript, currentState, difficulty, load, cancellationToken);
                sw.Stop();
                atcMs = (int)sw.ElapsedMilliseconds;
            }

            // Send ATC transmission
            await _hubContext.Clients.Group($"session-{request.SessionId}")
                .SendAsync("atcTransmission", atcResponse, cancellationToken);

            // TTS: Synthesize ATC response
            string? ttsAudioPath = null;
            if (!string.IsNullOrEmpty(atcResponse.Transmission))
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                ttsAudioPath = await _ttsService.SynthesizeAsync(
                    atcResponse.Transmission, 
                    "professional", 
                    atcResponse.TtsTone ?? "neutral", 
                    cancellationToken);
                sw.Stop();
                ttsMs = (int)sw.ElapsedMilliseconds;
                if (!string.IsNullOrEmpty(ttsAudioPath))
                {
                    await _hubContext.Clients.Group($"session-{request.SessionId}")
                        .SendAsync("ttsReady", ttsAudioPath, cancellationToken);
                }
            }

            // Update score
            if (verdict.ScoreDelta != 0)
            {
                session.ScoreTotal += verdict.ScoreDelta;
                await _context.SaveChangesAsync(cancellationToken);
                await _hubContext.Clients.Group($"session-{request.SessionId}")
                    .SendAsync("scoreTick", session.ScoreTotal, cancellationToken);
            }

            overallSw.Stop();
            var totalMs = (int)overallSw.ElapsedMilliseconds;
            await SaveTurn(session, transcript, verdict, atcResponse, ttsAudioPath, userAudioPath, turnStartIso, sttMs, instructorMs, atcMs, ttsMs, totalMs, false, cancellationToken);

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

    // Phase 2: Session summary + enriched turns API for debrief
    [HttpGet("session/{sessionId}/summary")]
    public async Task<ActionResult<object>> GetSessionSummaryAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _context.Sessions
            .Include(s => s.Turns)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session == null)
            return NotFound();

        var turnIds = session.Turns.Select(t => t.Id).ToList();
        var verdictDetails = await _context.VerdictDetails
            .Where(v => turnIds.Contains(v.TurnId))
            .ToListAsync(cancellationToken);

        // Aggregate metrics
        var totalTurns = session.Turns.Count;
        var blockedTurns = session.Turns.Count(t => t.Blocked);
        var successfulTurns = totalTurns - blockedTurns;
        var safetyIssues = verdictDetails.Count(v => v.Category == "Safety" && (v.Severity == "critical" || v.Severity == "major"));
        var normalizedMetrics = await _context.Metrics
            .Where(m => m.SessionId == sessionId && m.K == "turn.normalized")
            .Select(m => m.V)
            .ToListAsync(cancellationToken);
        var avgNormalized = normalizedMetrics.Any() ? normalizedMetrics.Average() : 0.0;
        var phraseAccuracy = await _context.Metrics.Where(m => m.SessionId == sessionId && m.K == "turn.phraseAccuracy").Select(m => m.V).ToListAsync(cancellationToken);
        var ordering = await _context.Metrics.Where(m => m.SessionId == sessionId && m.K == "turn.ordering").Select(m => m.V).ToListAsync(cancellationToken);
        var omissions = await _context.Metrics.Where(m => m.SessionId == sessionId && m.K == "turn.omissions").Select(m => m.V).ToListAsync(cancellationToken);
        var safety = await _context.Metrics.Where(m => m.SessionId == sessionId && m.K == "turn.safety").Select(m => m.V).ToListAsync(cancellationToken);

        // Component frequency (by code)
        var componentFrequency = verdictDetails
            .GroupBy(v => v.Code)
            .Select(g => new { Code = g.Key, Count = g.Count(), Category = g.First().Category })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList();

        // Enriched turns list
        var enrichedTurns = session.Turns
            .OrderBy(t => t.Idx)
            .Select(t => new
            {
                t.Id,
                t.Idx,
                t.UserTranscript,
                t.UserAudioPath,
                t.TtsAudioPath,
                t.ScoreDelta,
                t.Blocked,
                t.StartedUtc,
                t.SttMs,
                t.InstructorMs,
                t.AtcMs,
                t.TtsMs,
                t.TotalMs,
                Components = verdictDetails.Where(v => v.TurnId == t.Id).Select(v => new
                {
                    v.Code,
                    v.Category,
                    v.Severity,
                    v.Weight,
                    v.Score,
                    v.Delta,
                    v.Detail
                }).ToList(),
                Verdict = t.InstructorJson != null ? JsonSerializer.Deserialize<InstructorVerdict>(t.InstructorJson) : null
            })
            .ToList();

        return Ok(new
        {
            SessionId = session.Id,
            ScoreTotal = session.ScoreTotal,
            totalTurns,
            blockedTurns,
            successfulTurns,
            safetyIssues,
            avgNormalized,
            phraseAccuracyAvg = phraseAccuracy.Any() ? phraseAccuracy.Average() : 0.0,
            orderingAvg = ordering.Any() ? ordering.Average() : 0.0,
            omissionsAvg = omissions.Any() ? omissions.Average() : 0.0,
            safetyAvg = safety.Any() ? safety.Average() : 0.0,
            componentFrequency,
            turns = enrichedTurns
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

    private async Task SaveTurn(Session session, string transcript, InstructorVerdict verdict, AtcReply? atcResponse, string? ttsAudioPath, string? userAudioPath,
        string startedUtc, int sttMs, int instructorMs, int atcMs, int ttsMs, int totalMs, bool blocked, CancellationToken cancellationToken)
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
            UserAudioPath = userAudioPath,
            Verdict = verdict.BlockReason,
            StartedUtc = startedUtc,
            SttMs = sttMs,
            InstructorMs = instructorMs,
            AtcMs = atcMs,
            TtsMs = ttsMs,
            TotalMs = totalMs,
            ScoreDelta = verdict.ScoreDelta,
            Blocked = blocked
        };

        _context.Turns.Add(turn);
        await _context.SaveChangesAsync(cancellationToken);

        // Persist component breakdown as VerdictDetail rows (Phase 2)
        if (verdict.Components != null && verdict.Components.Any())
        {
            foreach (var comp in verdict.Components)
            {
                _context.VerdictDetails.Add(new VerdictDetail
                {
                    TurnId = turn.Id,
                    Code = comp.Code,
                    Category = comp.Category,
                    Severity = comp.Severity,
                    Weight = comp.Weight,
                    Score = comp.Score,
                    Delta = comp.Delta,
                    Detail = comp.Detail,
                    RubricVersion = verdict.RubricVersion
                });
            }
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Store normalized and sub-score metrics if provided
        var metricTime = DateTime.UtcNow.ToString("O");
        void AddMetric(string key, double? value)
        {
            if (value.HasValue)
            {
                _context.Metrics.Add(new Metric
                {
                    SessionId = session.Id,
                    K = key,
                    V = value.Value,
                    TUtc = metricTime
                });
            }
        }

        AddMetric("turn.normalized", verdict.Normalized);
        AddMetric("turn.phraseAccuracy", verdict.PhraseAccuracy);
        AddMetric("turn.ordering", verdict.Ordering);
        AddMetric("turn.omissions", verdict.Omissions);
        AddMetric("turn.safety", verdict.Safety);
        if (verdict.SafetyFlag.HasValue)
            AddMetric("turn.safetyFlag", verdict.SafetyFlag.Value ? 1.0 : 0.0);

        if (_context.ChangeTracker.HasChanges())
            await _context.SaveChangesAsync(cancellationToken);
    }
}