using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PilotSim.Core;
using PilotSim.Data;
using PilotSim.Data.Models;
using PilotSim.Server.Hubs;
using PilotSim.Server.Services;
using System.Text.Json;

namespace PilotSim.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SimulationController : ControllerBase
{
    private readonly ISttService _sttService;
    private readonly ITtsService _ttsService;
    private readonly ITurnService _turnService;
    private readonly SimDbContext _context;
    private readonly IHubContext<LiveHub> _hubContext;
    private readonly ILogger<SimulationController> _logger;

    public SimulationController(
        ISttService sttService,
        ITtsService ttsService,
        SimDbContext context,
        IHubContext<LiveHub> hubContext,
        ILogger<SimulationController> logger,
        ITurnService turnService)
    {
        _sttService = sttService;
        _ttsService = ttsService;
        _turnService = turnService;
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }

    public record ProcessTurnRequest(int SessionId, IFormFile Audio, string? PartialTranscript = null);
    public record TurnResult(string Transcript, InstructorVerdict Verdict, AtcReply AtcResponse, string? TtsAudioPath);

    // Map flexible difficulty strings coming from scenarios/UI to internal enum
    private static Difficulty MapDifficulty(string? value)
        => (value ?? "Basic").Trim().ToLowerInvariant() switch
        {
            "basic" => Difficulty.Basic,
            "intermediate" => Difficulty.Medium,
            "medium" => Difficulty.Medium,
            "adv" => Difficulty.Advanced,
            "advanced" => Difficulty.Advanced,
            // Future higher tiers collapse to Advanced until enum expanded
            "expert" => Difficulty.Advanced,
            "master" => Difficulty.Advanced,
            _ => Difficulty.Basic
        };

    [HttpPost("turn")]
    public async Task<ActionResult<TurnResult>> ProcessTurnAsync([FromForm] ProcessTurnRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Audio == null || request.Audio.Length == 0)
            return BadRequest("Audio file is required");

        try
        {
            var overallSw = System.Diagnostics.Stopwatch.StartNew();
            var turnStartIso = DateTime.UtcNow.ToString("O");
            // Get session and current state
            var session = await _context.Sessions
                .Include(s => s.Turns)
                .Include(s => s.Scenario)
                .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);

            if (session == null)
                return NotFound("Session not found");

            // All scenarios must use ScenarioWorkbookV2 format
            if (string.IsNullOrEmpty(session.Scenario?.InitialStateJson))
                return BadRequest("Scenario does not have workbook configuration (InitialStateJson is required)");

            ScenarioWorkbookV2 workbook;
            try
            {
                workbook = JsonSerializer.Deserialize<ScenarioWorkbookV2>(session.Scenario.InitialStateJson)
                    ?? throw new JsonException("Failed to deserialize workbook");
                
                if (workbook.Phases == null || !workbook.Phases.Any())
                    throw new JsonException("Workbook must have at least one phase");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Scenario {ScenarioId} has invalid ScenarioWorkbookV2 format", session.Scenario.Id);
                return BadRequest($"Invalid ScenarioWorkbookV2 format: {ex.Message}");
            }

            // Use TurnService path for all scenarios
            return await ProcessTurnWithWorkbookAsync(request, session, workbook, cancellationToken);
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

    // TurnService-based processing for ScenarioWorkbookV2
    private async Task<ActionResult<TurnResult>> ProcessTurnWithWorkbookAsync(
        ProcessTurnRequest request, 
        Session session, 
        ScenarioWorkbookV2 workbook,
        CancellationToken cancellationToken)
    {
        var overallSw = System.Diagnostics.Stopwatch.StartNew();
        var turnStartIso = DateTime.UtcNow.ToString("O");
        int sttMs = 0, turnServiceMs = 0, ttsMs = 0;

        try
        {
            // STT: Transcribe audio
            string transcript;
            string? userAudioPath = null;
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var ext = Path.GetExtension(request.Audio.FileName);
                if (string.IsNullOrWhiteSpace(ext) || ext.Length > 5) ext = ".webm";
                var audioDir = Path.Combine("wwwroot", "useraudio");
                Directory.CreateDirectory(audioDir);
                userAudioPath = Path.Combine(audioDir, $"u_{Guid.NewGuid():N}{ext}");
                
                await using (var target = System.IO.File.Create(userAudioPath))
                await using (var upload = request.Audio.OpenReadStream())
                {
                    await upload.CopyToAsync(target, cancellationToken);
                }

                await using var sttStream = System.IO.File.OpenRead(userAudioPath);
                var publicUserAudioPath = "/" + userAudioPath.Replace("\\", "/");
                var biasPrompt = "Use Australian aviation terms. Airport identifiers YSSY, YBBN, YMML, YPAD. Words: QNH, runway, squawk, kilo, papa, hPa. Callsign format VH-XXX.";
                var sttResult = await _sttService.TranscribeAsync(sttStream, biasPrompt, cancellationToken);
                sw.Stop();
                sttMs = (int)sw.ElapsedMilliseconds;
                transcript = sttResult.Text;
                userAudioPath = publicUserAudioPath;
            }

            // Get current state and phase
            var currentState = GetCurrentStateWorkbook(session);
            var currentPhaseId = ExtractPhaseId(currentState);
            var phase = workbook.Phases.FirstOrDefault(p => p.Id == currentPhaseId) ?? workbook.Phases.First();
            
            // Build TurnRequest
            var turnRequest = new PilotSim.Server.Services.TurnRequest
            {
                UserId = session.UserId?.ToString() ?? "",
                SessionId = session.Id.ToString(),
                TurnIndex = session.Turns.Count,
                PhaseId = phase.Id,
                Callsign = ExtractCallsign(workbook, session),
                Transcript = transcript,
                Workbook = workbook,
                CurrentState = currentState,
                Difficulty = MapDifficultyProfile(session.Difficulty),
                Seed = session.Scenario?.Seed,
                ControllerPersona = null
            };

            // Process turn with TurnService
            PilotSim.Server.Services.TurnResponse turnResponse;
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                turnResponse = await _turnService.ProcessTurnAsync(turnRequest, cancellationToken);
                sw.Stop();
                turnServiceMs = (int)sw.ElapsedMilliseconds;
            }

            // Send instructor verdict if available
            if (turnResponse.Instructor != null)
            {
                await _hubContext.Clients.Group($"session-{request.SessionId}")
                    .SendAsync("instructorVerdict", turnResponse.Instructor, cancellationToken);
            }

            // Send ATC transmission from timeline
            var atcTransmission = turnResponse.Timeline.FirstOrDefault(t => t.Source == "ATC");
            AtcReply? atcReply = null;
            if (atcTransmission != null)
            {
                atcReply = new AtcReply(
                    atcTransmission.Text,
                    turnResponse.Atc?.ExpectedReadback ?? new List<string>(),
                    turnResponse.UpdatedState,
                    turnResponse.Atc?.HoldShort,
                    atcTransmission.Tone
                );
                await _hubContext.Clients.Group($"session-{request.SessionId}")
                    .SendAsync("atcTransmission", atcReply, cancellationToken);
            }

            // TTS: Synthesize first transmission
            string? ttsAudioPath = null;
            if (atcTransmission != null && !string.IsNullOrEmpty(atcTransmission.Text))
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                ttsAudioPath = await _ttsService.SynthesizeAsync(
                    atcTransmission.Text,
                    atcTransmission.Persona ?? "professional",
                    atcTransmission.Tone,
                    cancellationToken);
                sw.Stop();
                ttsMs = (int)sw.ElapsedMilliseconds;
                
                if (!string.IsNullOrEmpty(ttsAudioPath))
                {
                    await _hubContext.Clients.Group($"session-{request.SessionId}")
                        .SendAsync("ttsReady", ttsAudioPath, cancellationToken);
                }
            }

            // Update score if not blocked
            var verdict = turnResponse.Instructor;
            if (verdict != null && !turnResponse.Blocked && verdict.ScoreDelta != 0)
            {
                session.ScoreTotal += verdict.ScoreDelta;
                await _context.SaveChangesAsync(cancellationToken);
                await _hubContext.Clients.Group($"session-{request.SessionId}")
                    .SendAsync("scoreTick", session.ScoreTotal, cancellationToken);
            }

            overallSw.Stop();
            var totalMs = (int)overallSw.ElapsedMilliseconds;

            // Save turn
            await SaveTurnWorkbook(
                session, transcript, verdict, atcReply, ttsAudioPath, userAudioPath,
                turnStartIso, sttMs, turnServiceMs, ttsMs, totalMs, 
                turnResponse.Blocked, turnResponse.UpdatedState, cancellationToken);

            // Step 13: Completion evaluation (check success/fail conditions)
            var completionResult = EvaluateCompletion(workbook, session, turnResponse);
            if (completionResult.IsComplete)
            {
                session.Outcome = completionResult.Outcome;
                session.EndedUtc = DateTime.UtcNow.ToString("O");
                await _context.SaveChangesAsync(cancellationToken);
                
                // Send completion event to client
                await _hubContext.Clients.Group($"session-{request.SessionId}")
                    .SendAsync("sessionComplete", new { 
                        outcome = completionResult.Outcome, 
                        reason = completionResult.Reason 
                    }, cancellationToken);
                
                _logger.LogInformation("Session {SessionId} completed with outcome: {Outcome}", 
                    request.SessionId, completionResult.Outcome);
            }

            return Ok(new TurnResult(transcript, verdict ?? new InstructorVerdict(
                new List<string>(), new List<string>(), null, 0.5, 0, ""), atcReply!, ttsAudioPath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process turn with workbook for session {SessionId}", request.SessionId);
            return StatusCode(500, "Internal server error processing turn");
        }
    }

    private static PilotSim.Server.Services.DifficultyProfile MapDifficultyProfile(string? difficulty)
    {
        var level = (difficulty ?? "Basic").Trim().ToLowerInvariant() switch
        {
            "basic" => PilotSim.Server.Services.DifficultyLevel.Easy,
            "intermediate" or "medium" => PilotSim.Server.Services.DifficultyLevel.Medium,
            _ => PilotSim.Server.Services.DifficultyLevel.Hard
        };

        return new PilotSim.Server.Services.DifficultyProfile
        {
            Level = level,
            ParsingStrictness = level == PilotSim.Server.Services.DifficultyLevel.Easy ? 0.4 : 
                               level == PilotSim.Server.Services.DifficultyLevel.Medium ? 0.6 : 0.8,
            Congestion = level == PilotSim.Server.Services.DifficultyLevel.Easy ? 0.2 :
                        level == PilotSim.Server.Services.DifficultyLevel.Medium ? 0.4 : 0.6,
            Variability = level == PilotSim.Server.Services.DifficultyLevel.Easy ? 0.1 :
                         level == PilotSim.Server.Services.DifficultyLevel.Medium ? 0.3 : 0.5
        };
    }

    private JsonElement GetCurrentStateWorkbook(Session session)
    {
        var lastTurn = session.Turns.OrderBy(t => t.Idx).LastOrDefault();
        if (lastTurn?.AtcJson != null)
        {
            try
            {
                var atcReply = JsonSerializer.Deserialize<AtcReply>(lastTurn.AtcJson);
                if (atcReply?.NextState != null)
                {
                    return JsonSerializer.SerializeToElement(atcReply.NextState);
                }
            }
            catch { }
        }

        return GetInitialStateWorkbook(session);
    }

    private JsonElement GetInitialStateWorkbook(Session session)
    {
        if (!string.IsNullOrEmpty(session.Scenario?.InitialStateJson))
        {
            try
            {
                var workbook = JsonSerializer.Deserialize<ScenarioWorkbookV2>(session.Scenario.InitialStateJson);
                if (workbook != null)
                {
                    // Return a state element with the first phase
                    return JsonSerializer.SerializeToElement(new
                    {
                        phase = workbook.Phases.FirstOrDefault()?.Id ?? "initial",
                        position = "gate",
                        ready = false
                    });
                }
            }
            catch { }
        }

        return JsonSerializer.SerializeToElement(new { phase = "initial", position = "gate", ready = false });
    }

    private static string ExtractPhaseId(JsonElement state)
    {
        if (state.TryGetProperty("phase", out var phase))
        {
            return phase.GetString() ?? "initial";
        }
        return "initial";
    }

    private static string ExtractCallsign(ScenarioWorkbookV2 workbook, Session session)
    {
        // Try to get callsign from workbook aircraft or generate one
        var aircraft = workbook.Inputs?.Aircraft;
        if (!string.IsNullOrEmpty(aircraft))
        {
            return $"VH-{aircraft.ToUpper().Take(3).Concat(new[] { (char)('A' + (session.Id % 26)) })}";
        }
        return $"VH-{session.Id:000}";
    }

    private async Task SaveTurnWorkbook(
        Session session, string transcript, InstructorVerdict? verdict, AtcReply? atcResponse, 
        string? ttsAudioPath, string? userAudioPath, string startedUtc, int sttMs, 
        int turnServiceMs, int ttsMs, int totalMs, bool blocked, JsonElement updatedState,
        CancellationToken cancellationToken)
    {
        var turnIndex = session.Turns.Count;
        
        var turn = new Turn
        {
            SessionId = session.Id,
            Idx = turnIndex,
            UserTranscript = transcript,
            InstructorJson = verdict != null ? JsonSerializer.Serialize(verdict) : null,
            AtcJson = atcResponse != null ? JsonSerializer.Serialize(atcResponse) : null,
            TtsAudioPath = ttsAudioPath,
            UserAudioPath = userAudioPath,
            Verdict = verdict?.BlockReason,
            StartedUtc = startedUtc,
            SttMs = sttMs,
            InstructorMs = turnServiceMs, // TurnService includes instructor + ATC time
            AtcMs = 0, // Already included in TurnService time
            TtsMs = ttsMs,
            TotalMs = totalMs,
            ScoreDelta = verdict?.ScoreDelta ?? 0,
            Blocked = blocked
        };

        _context.Turns.Add(turn);
        await _context.SaveChangesAsync(cancellationToken);

        // Persist component breakdown if available
        if (verdict?.Components != null && verdict.Components.Any())
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

        // Store metrics
        var metricTime = DateTime.UtcNow.ToString("O");
        if (verdict != null)
        {
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

    // Completion evaluation result
    private record CompletionEvaluationResult(
        bool IsComplete,
        string? Outcome,
        string? Reason);

    // Step 13: Evaluate completion criteria
    private CompletionEvaluationResult EvaluateCompletion(
        ScenarioWorkbookV2 workbook,
        Session session,
        PilotSim.Server.Services.TurnResponse turnResponse)
    {
        if (workbook.Completion == null)
            return new CompletionEvaluationResult(false, null, null);

        var state = turnResponse.UpdatedState;
        var currentPhaseId = turnResponse.NextPhaseId;
        var isLastPhase = workbook.Phases.LastOrDefault()?.Id == currentPhaseId;

        // Check fail_major conditions (immediate failure)
        if (workbook.Completion.FailMajorWhenAny?.Any() == true)
        {
            foreach (var criterion in workbook.Completion.FailMajorWhenAny)
            {
                if (EvaluateCriterion(criterion, state, session, turnResponse))
                {
                    return new CompletionEvaluationResult(
                        true,
                        "fail_major",
                        $"Major failure: {criterion.Lhs} {criterion.Op} {criterion.Rhs}");
                }
            }
        }

        // Check fail_minor conditions (only on last phase)
        if (isLastPhase && workbook.Completion.FailMinorWhenAny?.Any() == true)
        {
            foreach (var criterion in workbook.Completion.FailMinorWhenAny)
            {
                if (EvaluateCriterion(criterion, state, session, turnResponse))
                {
                    return new CompletionEvaluationResult(
                        true,
                        "fail_minor",
                        $"Minor failure: {criterion.Lhs} {criterion.Op} {criterion.Rhs}");
                }
            }
        }

        // Check success conditions (all must be true)
        if (workbook.Completion.SuccessWhenAll?.Any() == true)
        {
            var allSuccess = workbook.Completion.SuccessWhenAll.All(criterion =>
                EvaluateCriterion(criterion, state, session, turnResponse));

            if (allSuccess)
            {
                return new CompletionEvaluationResult(
                    true,
                    "success",
                    "All completion criteria met");
            }
        }

        // Not complete yet
        return new CompletionEvaluationResult(false, null, null);
    }

    // Evaluate a single criterion
    private bool EvaluateCriterion(
        Criterion criterion,
        JsonElement state,
        Session session,
        PilotSim.Server.Services.TurnResponse turnResponse)
    {
        try
        {
            // Get the left-hand side value from state
            object? lhsValue = GetValueFromState(criterion.Lhs, state, session, turnResponse);
            
            // Handle different operators
            switch (criterion.Op.ToLowerInvariant())
            {
                case "exists":
                    return lhsValue != null;
                    
                case "missing":
                    return lhsValue == null;
                    
                case "contains":
                    if (lhsValue is string str && criterion.Rhs.ValueKind == JsonValueKind.String)
                        return str.Contains(criterion.Rhs.GetString() ?? "", StringComparison.OrdinalIgnoreCase);
                    return false;
                    
                case "==":
                case "equals":
                    return CompareValues(lhsValue, criterion.Rhs, "==");
                    
                case ">=":
                    return CompareValues(lhsValue, criterion.Rhs, ">=");
                    
                case "<=":
                    return CompareValues(lhsValue, criterion.Rhs, "<=");
                    
                case ">":
                    return CompareValues(lhsValue, criterion.Rhs, ">");
                    
                case "<":
                    return CompareValues(lhsValue, criterion.Rhs, "<");
                    
                default:
                    _logger.LogWarning("Unknown criterion operator: {Op}", criterion.Op);
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating criterion: {Lhs} {Op}", criterion.Lhs, criterion.Op);
            return false;
        }
    }

    // Get value from state by path (e.g., "phase", "metrics.coverage", "safety_flag")
    private object? GetValueFromState(
        string path,
        JsonElement state,
        Session session,
        PilotSim.Server.Services.TurnResponse turnResponse)
    {
        // Handle special paths
        switch (path.ToLowerInvariant())
        {
            case "phase":
                return turnResponse.NextPhaseId;
                
            case "safety_flag":
            case "safetyflag":
                return turnResponse.Instructor?.SafetyFlag ?? false;
                
            case "blocked":
                return turnResponse.Blocked;
                
            case "readback_coverage":
            case "coverage":
                return turnResponse.ReadbackCoverage ?? 0.0;
                
            case "mandatory_missing_count":
                return turnResponse.MandatoryMissing?.Count ?? 0;
                
            case "score_total":
            case "score":
                return session.ScoreTotal;
                
            case "turn_count":
                return session.Turns.Count;
        }

        // Try to get from state JSON
        try
        {
            var parts = path.Split('.');
            var current = state;
            
            foreach (var part in parts)
            {
                if (current.TryGetProperty(part, out var next))
                    current = next;
                else
                    return null;
            }

            // Convert JsonElement to appropriate type
            return current.ValueKind switch
            {
                JsonValueKind.String => current.GetString(),
                JsonValueKind.Number => current.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => current.GetRawText()
            };
        }
        catch
        {
            return null;
        }
    }

    // Compare values based on operator
    private bool CompareValues(object? lhs, JsonElement rhs, string op)
    {
        if (lhs == null) return false;

        try
        {
            // Try numeric comparison
            if (lhs is double || lhs is int || lhs is float)
            {
                var lhsNum = Convert.ToDouble(lhs);
                var rhsNum = rhs.GetDouble();
                
                return op switch
                {
                    "==" => Math.Abs(lhsNum - rhsNum) < 0.0001,
                    ">=" => lhsNum >= rhsNum,
                    "<=" => lhsNum <= rhsNum,
                    ">" => lhsNum > rhsNum,
                    "<" => lhsNum < rhsNum,
                    _ => false
                };
            }
            
            // String comparison
            if (lhs is string lhsStr && rhs.ValueKind == JsonValueKind.String)
            {
                var rhsStr = rhs.GetString();
                return op == "==" && lhsStr.Equals(rhsStr, StringComparison.OrdinalIgnoreCase);
            }
            
            // Boolean comparison
            if (lhs is bool lhsBool)
            {
                var rhsBool = rhs.GetBoolean();
                return op == "==" && lhsBool == rhsBool;
            }
        }
        catch
        {
            // Comparison failed
        }
        
        return false;
    }
}
