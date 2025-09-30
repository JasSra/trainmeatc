// File: TurnService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
 
using PilotSim.Core;
using PilotSim.Server.Services;
using IAtcService = PilotSim.Core.IAtcService;
using IInstructorService = PilotSim.Core.IInstructorService; // your existing InstructorVerdict, AtcReply types

namespace PilotSim.Server.Services
{
    // 1) Difficulty & load
    public enum DifficultyLevel { Easy, Medium, Hard }
    public sealed class DifficultyProfile
    {
        public DifficultyLevel Level { get; init; } = DifficultyLevel.Medium;
        // Parsing tolerance for Instructor/ATC (0 tolerant … 1 strict)
        public double ParsingStrictness { get; init; } = 0.6;
        // Controller workload/clutter (0 clear … 1 congested)
        public double Congestion { get; init; } = 0.4;
        // Branch variability (0 scripted … 1 dynamic)
        public double Variability { get; init; } = 0.3;
        // Gate policy multipliers
        public double SafetyGateBias { get; init; } = 1.0; // >1 = more blocking
    }

    // 2) Turn I/O
    public sealed class TurnRequest
    {
        public string UserId { get; init; } = "";
        public string SessionId { get; init; } = "";
        public int TurnIndex { get; init; }
        public string PhaseId { get; init; } = "";
        public string Callsign { get; init; } = "";
        public string Transcript { get; init; } = "";        // already STT’d
        public ScenarioWorkbookV2 Workbook { get; init; } = new();
        public JsonElement CurrentState { get; init; }       // simulation state bag
        public DifficultyProfile Difficulty { get; init; } = new();
        public int? Seed { get; init; }                      // deterministic branching
        public string? ControllerPersona { get; init; }      // "concise","normal","high_workload"
    }

    public sealed class TurnResponse
    {
        public string PhaseId { get; init; } = "";
        public string NextPhaseId { get; init; } = "";
        public bool Blocked { get; init; }
        public string? BlockReason { get; init; }
        public List<Transmission> Timeline { get; init; } = new(); // ATC + traffic + system prompts
        public InstructorVerdict? Instructor { get; init; }
        public AtcReply? Atc { get; init; }
        public JsonElement UpdatedState { get; init; }
        public List<string> MandatoryMissing { get; init; } = new();
        public double? ReadbackCoverage { get; init; }
        public string? TtsTone { get; init; } // professional|calm|urgent
    }

    public sealed class Transmission
    {
        public string Source { get; init; } = ""; // "ATC","TRAFFIC:QFA456","SYSTEM"
        public double? FreqMhz { get; init; }
        public string Text { get; init; } = "";
        public string Tone { get; init; } = "professional";  // professional|urgent|calm
        public string Persona { get; init; } = "normal";     // concise|normal|high_workload|angry
        public Dictionary<string, string> Attributes { get; init; } = new(); // e.g., {"direction":"northbound"}
    }

    // 3) Service interfaces
    public interface ITurnService
    {
        Task<TurnResponse> ProcessTurnAsync(TurnRequest req, CancellationToken ct = default);
    }

    public interface IAtcService
    {
        Task<AtcReply> NextAsync(string transcript, object state, Difficulty diff, Load load, CancellationToken ct);
    }

    public interface IInstructorService
    {
        Task<InstructorVerdict> ScoreAsync(string transcript, object state, Difficulty difficulty, CancellationToken ct);
    }

    // Optional: per-user client resolution (single app key also supported)
    public interface IOpenAIClientResolver
    {
        OpenAIClient Resolve(string userId);
    }
}   // Place inside your Atc/Instructor services
        // private const string AtcSystemPrompt = """
        //                                        You are "ATC Controller" in Australia. Use AIP-compliant phraseology. JSON only.
        //                                        Rules:
        //                                        1) Enforce required_components. If any block_on_missing item absent in student readback, repeat clearance slowly and request full readback. Do not advance phase.
        //                                        2) Never imply runway clearance. Any enter/line-up/cross/take-off/land/backtrack requires explicit instruction and full readback.
        //                                        3) Use ft/kt/NM in transmissions. Data is SI inside JSON.
        //                                        Return:
        //                                        {
        //                                         "transmission": "...",
        //                                         "expectedReadback": ["...","..."],
        //                                         "requiredComponents": ["RUNWAY","HOLDING_POINT","..."],
        //                                         "safetyGate": {"block_on_missing":["..."], "warn_on_missing":["..."]},
        //                                         "nextState": {"phase":"...", "state_deltas":[...]},
        //                                         "ttsTone":"professional|urgent|calm"
        //                                        }
        //                                        """;
        //
        // private const string InstructorSystemPrompt = """
        //                                               You are "Instructor". Score pilot radio calls per AIP Australia. JSON only.
        //                                               Score slots, not surface strings. Use provided slot_definitions and tolerance.
        //                                               Apply safety_cap and mandatory readbacks gates.
        //                                               Return:
        //                                               {
        //                                                "normalized": 0..1,
        //                                                "scoreDelta": int,
        //                                                "safetyFlag": true|false,
        //                                                "mandatory_readback_missing": ["RUNWAY","HOLDING_POINT"],
        //                                                "components":[{"code":"PA_CALLSIGN","category":"PhraseAccuracy","severity":"minor","weight":0.10,"score":0.9,"delta":+1,"detail":"..."}],
        //                                                "exemplarReadback":"..."
        //                                               }
        //                                               """;
   
   // File: CommsOrchestration.cs
// Purpose: Router + Instructor V2 + Traffic Agent + Updated TurnService
// Notes:
// - SI stored in data; RTF (ft/kt/NM) only in transmissions.
// - CTAF phases use TrafficAgent; ATC phases use AtcService.
// - Silence: router decides ATC nudge or CTAF traffic interject.
// - Branching supported via phase.Branches + Difficulty.Variability.

 
namespace PilotSim.Server.Services
{
    // -------------------------------
    // 0) Router
    // -------------------------------

    public enum Speaker
    {
        ATC,
        CTAF,                // generic CTAF broadcast by TrafficAgent
        TrafficNearest,      // nearest conflict actor
        TrafficRandom        // random relevant actor
    }

    public interface IResponderRouter
    {
        Speaker Choose(ScenarioWorkbookV2 wb, PhaseSpec phase, bool silent, bool conflictImminent, Random rng);
    }

    public sealed class ResponderRouter : IResponderRouter
    {
        public Speaker Choose(ScenarioWorkbookV2 wb, PhaseSpec phase, bool silent, bool conflictImminent, Random rng)
        {
            var tower = wb.ContextResolved?.Airport?.TowerActive == true;

            if (tower) return Speaker.ATC;

            // Tower inactive → CTAF domain
            if (conflictImminent) return Speaker.TrafficNearest;

            var p = phase?.ResponderMap?.RandomInterjectProb ?? 0.0;
            if (rng.NextDouble() < p) return Speaker.TrafficRandom;

            // Default CTAF
            return Speaker.CTAF;
        }
    }

    // -------------------------------
    // 1) Traffic Agent (CTAF)
    // -------------------------------

    public interface ITrafficAgent
    {
        Task<TrafficReply> NextAsync(
            string transcript,
            object state,            // workbook context + phase hints
            Difficulty difficulty,
            CancellationToken ct);
    }

    public sealed record TrafficReply(
        string Transmission,
        string SourceCallsign,
        List<string> ExpectedReadback,
        object NextState,
        string? TtsTone,
        Dictionary<string, string>? Attributes);

    public sealed class OpenAiTrafficAgentService : ITrafficAgent
    {
        private readonly OpenAIClient _client;
        private readonly ILogger<OpenAiTrafficAgentService> _log;

        private const string SystemPrompt = """
You are "Traffic Pilot" on CTAF at an Australian non-controlled aerodrome.
Return JSON only:
{
 "transmission": "...",                  // concise CTAF broadcast, ≤8 s speak time
 "source_callsign": "VH-XYZ",
 "expectedReadback": [],
 "nextState": {"phase":"<same_or_next>", "state_deltas":[...]},
 "ttsTone":"professional",
 "attributes":{"direction":"downwind","role":"traffic"}
}
Rules:
1) No clearances. Broadcast only. Standard AU CTAF style.
2) Include broadcast components if provided (location, type, callsign, position, level in metres MSL, intentions, location repeat).
3) Speak ft/kt/NM in text; data remains SI.
4) If a conflict is imminent (≤90 s), include a brief advisory for your own aircraft only.
5) Be concise and professional.
""";

        public OpenAiTrafficAgentService(OpenAIClient client, ILogger<OpenAiTrafficAgentService> log)
        {
            _client = client; _log = log;
        }

        public async Task<TrafficReply> NextAsync(string transcript, object state, Difficulty difficulty, CancellationToken ct)
        {
            try
            {
                var chat = _client.GetChatClient("gpt-4o-mini");

                var userPayload = JsonSerializer.Serialize(state);
                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(SystemPrompt),
                    new UserChatMessage($"STATE:\n{userPayload}\n\nSTUDENT:\"{transcript}\"")
                };

                var resp = await chat.CompleteChatAsync(messages, cancellationToken: ct);
                var content = resp.Value.Content?[0]?.Text ?? "{}";
                var json = JsonSerializer.Deserialize<JsonElement>(content);

                string tx = json.TryGetProperty("transmission", out var t) ? t.GetString() ?? "" : "";
                string src = json.TryGetProperty("source_callsign", out var s) ? s.GetString() ?? "TRAFFIC" : "TRAFFIC";
                var exp = json.TryGetProperty("expectedReadback", out var er) && er.ValueKind == JsonValueKind.Array
                    ? er.EnumerateArray().Select(x => x.GetString() ?? "").ToList()
                    : new List<string>();
                var next = json.TryGetProperty("nextState", out var ns) ? JsonSerializer.Deserialize<object>(ns.GetRawText())! : new { phase = "" };
                string? tone = json.TryGetProperty("ttsTone", out var tn) ? tn.GetString() : "professional";
                Dictionary<string, string>? attrs = null;
                if (json.TryGetProperty("attributes", out var at) && at.ValueKind == JsonValueKind.Object)
                {
                    attrs = at.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.ToString());
                }

                return new TrafficReply(tx, src, exp, next, tone, attrs);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "TrafficAgent failed");
                return new TrafficReply(
                    "Traffic, {{ PLACEHOLDER }} traffic, aircraft on downwind.",
                    "TRAFFIC",
                    new List<string>(),
                    new { phase = "" },
                    "professional",
                    new Dictionary<string, string> { { "role", "traffic" } });
            }
        }
    }

    // -------------------------------
    // 2) Instructor V2 (slot-first, CTAF-aware)
    // -------------------------------

    public sealed class OpenAiInstructorServiceV2 : IInstructorService
    {
        private readonly OpenAIClient _client;
        private readonly ILogger<OpenAiInstructorServiceV2> _log;

        private const string SystemPrompt = """
You are "Instructor". Score Australian radiotelephony at slot-level. JSON only.
Rules:
- Score slots, not surface strings. Use provided slot_definitions and tolerance.
- Apply safety_cap and mandatory readback/broadcast gates (block_on_missing).
- CTAF phases: assess broadcast components, not clearances. No clearances are issued on CTAF.
Return JSON:
{
 "normalized": 0.0-1.0,
 "scoreDelta": int,
 "safetyFlag": true|false,
 "mandatory_readback_missing": ["RUNWAY","HOLDING_POINT", ...],
 "components":[{"code":"PA_CALLSIGN","category":"PhraseAccuracy","severity":"minor","weight":0.10,"score":0.9,"delta":+1,"detail":"..."}],
 "exemplarReadback":"..."
}
""";

        public OpenAiInstructorServiceV2(OpenAIClient client, ILogger<OpenAiInstructorServiceV2> log)
        {
            _client = client; _log = log;
        }

        public async Task<InstructorVerdict> ScoreAsync(string transcript, object state, Difficulty difficulty, CancellationToken ct)
        {
            try
            {
                var chat = _client.GetChatClient("gpt-4o-mini");
                var payload = JsonSerializer.Serialize(state);

                var msgs = new List<ChatMessage>
                {
                    new SystemChatMessage(SystemPrompt),
                    new UserChatMessage($"STUDENT:\"{transcript}\"\nSTATE:\n{payload}")
                };

                var resp = await chat.CompleteChatAsync(msgs, cancellationToken: ct);
                var txt = resp.Value.Content?[0]?.Text ?? "{}";
                var je = JsonSerializer.Deserialize<JsonElement>(txt);

                // Extract components[] for your existing VerdictDetail mapping
                List<ComponentScore>? comps = null;
                if (je.TryGetProperty("components", out var arr) && arr.ValueKind == JsonValueKind.Array)
                {
                    comps = arr.EnumerateArray().Select(c => new ComponentScore(
                        Code: c.GetProperty("code").GetString() ?? "UNKNOWN",
                        Category: c.GetProperty("category").GetString() ?? "General",
                        Severity: c.GetProperty("severity").GetString() ?? "info",
                        Weight: c.TryGetProperty("weight", out var wt) ? wt.GetDouble() : 0,
                        Score: c.TryGetProperty("score", out var sc) ? sc.GetDouble() : 0,
                        Delta: c.TryGetProperty("delta", out var d) ? d.GetDouble() : 0,
                        Detail: c.TryGetProperty("detail", out var dt) ? dt.GetString() : null
                    )).ToList();
                }

                double normalized = je.TryGetProperty("normalized", out var n) ? n.GetDouble() : 0.5;
                int delta = je.TryGetProperty("scoreDelta", out var sd) ? sd.GetInt32() : 0;
                bool safety = je.TryGetProperty("safetyFlag", out var sf) && sf.GetBoolean();
                string? exemplar = je.TryGetProperty("exemplarReadback", out var ex) ? ex.GetString() : null;

                // Missing list
                List<string> missing = new();
                if (je.TryGetProperty("mandatory_readback_missing", out var mm) && mm.ValueKind == JsonValueKind.Array)
                    missing = mm.EnumerateArray().Select(x => x.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();

                return new InstructorVerdict(
                    Critical: new List<string>(),
                    Improvements: new List<string>(),
                    ExemplarReadback: exemplar,
                    Normalized: normalized,
                    ScoreDelta: delta,
                    BlockReason: safety ? "Safety concern" : "",
                    Components: comps,
                    PhraseAccuracy: null,
                    Ordering: null,
                    Omissions: null,
                    Safety: safety ? 0.0 : 1.0,
                    SafetyFlag: safety,
                    RubricVersion: "v2"
                );
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Instructor scoring failed");
                return new InstructorVerdict(
                    Critical: new List<string> { "Scoring error" },
                    Improvements: new List<string>(),
                    ExemplarReadback: null,
                    Normalized: 0.5,
                    ScoreDelta: 0,
                    BlockReason: "System error"
                );
            }
        }
    }

    // -------------------------------
    // 3) Updated TurnService (router + CTAF support)
    // -------------------------------

    public sealed class TurnService : ITurnService
    {
        private readonly IAtcService _atc;
        private readonly IInstructorService _instructor;   // replaced implementation with V2
        private readonly ITrafficAgent _traffic;
        private readonly IResponderRouter _router;
        private readonly ILogger<TurnService> _log;

        public TurnService(
            IAtcService atc,
            IInstructorService instructor,
            ITrafficAgent traffic,
            IResponderRouter router,
            ILogger<TurnService> log)
        {
            _atc = atc; _instructor = instructor; _traffic = traffic; _router = router; _log = log;
        }

        public async Task<TurnResponse> ProcessTurnAsync(TurnRequest req, CancellationToken ct = default)
        {
            // 0) Init
            var rng = new Random(req.Seed ?? HashCode.Combine(req.SessionId, req.TurnIndex));
            var phase = req.Workbook.Phases.First(p => p.Id == req.PhaseId);
            var ctx = BuildPhaseContext(req, phase, rng);
            var timeline = new List<Transmission>();
            InstructorVerdict? instructor = null; AtcReply? atc = null; TrafficReply? traf = null;

            // Conflict check
            bool conflictImminent = req.Workbook.ContextResolved?.TrafficSnapshot?.Conflicts?.Any(c => c.TimeToConflictS <= 90) == true;

            // 1) Silence handling (no student TX)
            if (string.IsNullOrWhiteSpace(req.Transcript))
            {
                var sp = _router.Choose(req.Workbook, phase, silent: true, conflictImminent, rng);
                if (sp == Speaker.ATC)
                {
                    timeline.Add(new Transmission
                    {
                        Source = "ATC",
                        FreqMhz = phase.PrimaryFreqMhz,
                        Text = $"{req.Callsign}, say intentions.",
                        Tone = "professional",
                        Persona = Persona(req)
                    });
                    return new TurnResponse
                    {
                        PhaseId = req.PhaseId,
                        NextPhaseId = req.PhaseId,
                        Blocked = false,
                        Timeline = timeline,
                        UpdatedState = req.CurrentState
                    };
                }
                else
                {
                    // CTAF/Traffic interject
                    var trafCtx = ComposeTrafficState(req, phase, ctx, mode: sp.ToString());
                    traf = await _traffic.NextAsync("", trafCtx, MapDifficulty(req.Difficulty), ct);
                    timeline.Add(ToTransmissionTraffic(req, phase, traf));
                    var stateAfter = ApplyNextState(req.CurrentState, req.PhaseId, req.PhaseId, traf.NextState);
                    return new TurnResponse
                    {
                        PhaseId = req.PhaseId,
                        NextPhaseId = req.PhaseId,
                        Blocked = false,
                        Timeline = timeline,
                        UpdatedState = stateAfter
                    };
                }
            }

            // 2) Instructor scoring (slot-level; applies tolerance/gates from workbook)
            var difficulty = MapDifficulty(req.Difficulty);
            var stateForScoring = ComposeInstructorState(req, phase, ctx);
            instructor = await _instructor.ScoreAsync(req.Transcript, stateForScoring, difficulty, ct);
            var missing = ExtractMandatoryMissing(instructor);
            var coverage = ExtractCoverage(instructor);

            // 3) Gate check (CASA mandatory readbacks/broadcasts + safety)
            var mustBlock = ShouldBlock(phase, req.Workbook, instructor);
            if (mustBlock.Block)
            {
                // Repeat/coach via ATC if tower, otherwise via traffic-style nudge
                if (req.Workbook.ContextResolved.Airport.TowerActive)
                {
                    atc = BuildRepeatClearance(phase, ctx, req, instructor, req.Difficulty);
                    timeline.Add(ToTransmissionATC(phase.PrimaryFreqMhz, atc, Persona(req)));
                    var noAdvance = ApplyNextState(req.CurrentState, phase.Id, phase.Id, atc.NextState);
                    return new TurnResponse
                    {
                        PhaseId = req.PhaseId,
                        NextPhaseId = req.PhaseId,
                        Blocked = true,
                        BlockReason = instructor.BlockReason,
                        Timeline = timeline,
                        Instructor = instructor,
                        Atc = atc,
                        ReadbackCoverage = coverage,
                        MandatoryMissing = missing,
                        TtsTone = atc.TtsTone,
                        UpdatedState = noAdvance
                    };
                }
                else
                {
                    // CTAF: traffic interject to highlight situational cue
                    var trafCtx = ComposeTrafficState(req, phase, ctx, mode: "CTAF");
                    traf = await _traffic.NextAsync(req.Transcript, trafCtx, difficulty, ct);
                    timeline.Add(ToTransmissionTraffic(req, phase, traf));
                    var noAdvance = ApplyNextState(req.CurrentState, phase.Id, phase.Id, traf.NextState);
                    return new TurnResponse
                    {
                        PhaseId = req.PhaseId,
                        NextPhaseId = req.PhaseId,
                        Blocked = true,
                        BlockReason = instructor.BlockReason,
                        Timeline = timeline,
                        Instructor = instructor,
                        ReadbackCoverage = coverage,
                        MandatoryMissing = missing,
                        TtsTone = traf.TtsTone,
                        UpdatedState = noAdvance
                    };
                }
            }

            // 4) Branch resolution
            var branch = PickBranch(phase, ctx, rng, req.Difficulty);
            if (branch != null) ctx = ApplyBranch(ctx, branch);

            // 5) Response generation (ATC vs CTAF)
            var speaker = _router.Choose(req.Workbook, phase, silent: false, conflictImminent, rng);
            if (speaker == Speaker.ATC)
            {
                var load = new Load(
                    (float)req.Difficulty.Congestion,
                    1.0f - (float)req.Difficulty.Congestion,
                    req.ControllerPersona ?? Persona(req),
                     req.Workbook.ContextResolved.TrafficSnapshot.Density);
                atc = await _atc.NextAsync(req.Transcript, ctx, difficulty, load, ct);
                timeline.Add(ToTransmissionATC(phase.PrimaryFreqMhz, atc, load.ControllerPersona));
            }
            else
            {
                var trafCtx = ComposeTrafficState(req, phase, ctx, mode: speaker.ToString());
                traf = await _traffic.NextAsync(req.Transcript, trafCtx, difficulty, ct);
                timeline.Add(ToTransmissionTraffic(req, phase, traf));
            }

            // 6) Optional extra traffic call if conflict imminent and speaker wasn't traffic
            if (speaker == Speaker.ATC && conflictImminent)
            {
                var trigCtx = ComposeTrafficState(req, phase, ctx, mode: "TrafficNearest");
                var extra = await _traffic.NextAsync(req.Transcript, trigCtx, difficulty, ct);
                timeline.Add(ToTransmissionTraffic(req, phase, extra));
                traf ??= extra;
            }

            // 7) Advance phase
            var nextPhaseId = ResolveNextPhaseId(phase, atc, traf) ?? req.PhaseId;
            var updated = ApplyNextState(req.CurrentState, req.PhaseId, nextPhaseId, (object?)atc?.NextState ?? traf?.NextState);

            return new TurnResponse
            {
                PhaseId = req.PhaseId,
                NextPhaseId = nextPhaseId,
                Blocked = false,
                Timeline = timeline,
                Instructor = instructor,
                Atc = atc,
                ReadbackCoverage = coverage,
                MandatoryMissing = missing,
                TtsTone = atc?.TtsTone ?? traf?.TtsTone,
                UpdatedState = updated
            };
        }

        // --- Helpers ---

        private static object BuildPhaseContext(TurnRequest req, PhaseSpec phase, Random rng)
        {
            return new
            {
                req.Workbook.ContextResolved,
                req.Workbook.Rubric?.ReadbackPolicy,
                phase.RequiredComponents,
                phase.BroadcastRequiredComponents,
                phase.SafetyGates,
                GlobalGates = req.Workbook.GlobalSafetyGates,
                PhaseId = phase.Id,
                RandomSeed = rng.Next()
            };
        }

        private static object ComposeTrafficState(TurnRequest req, PhaseSpec phase, object ctx, string mode)
        {
            return new
            {
                phase_id = phase.Id,
                runway_in_use = req.Workbook.ContextResolved.RunwayInUse,
                ctaf_mhz = req.Workbook.ContextResolved.Airport.CtafMhz,
                traffic_snapshot = req.Workbook.ContextResolved.TrafficSnapshot,
                broadcast_required_components = phase.BroadcastRequiredComponents,
                tolerance = req.Workbook.Tolerance,
                mode
            };
        }

        private static Difficulty MapDifficulty(DifficultyProfile p) =>
            p.Level switch
            {
                DifficultyLevel.Easy => Difficulty.Basic,
                DifficultyLevel.Hard => Difficulty.Advanced,
                _ => Difficulty.Medium
            };

        private static object ComposeInstructorState(TurnRequest req, PhaseSpec phase, object ctx)
        {
            return new
            {
                Phase = phase.Id,
                Required = phase.RequiredComponents,
                BroadcastRequired = phase.BroadcastRequiredComponents,
                ExpectedReadback = phase.ExpectedReadback,
                Tolerance = req.Workbook.Tolerance,
                ReadbackPolicy = req.Workbook.Rubric?.ReadbackPolicy,
                Context = ctx,
                TowerActive = req.Workbook.ContextResolved.Airport.TowerActive
            };
        }

        private static List<string> ExtractMandatoryMissing(InstructorVerdict v) =>
            v?.Components?.Where(c => c.Code != null && c.Score is 0).Select(c => c.Code!).Distinct().ToList() ?? new();

        private static double? ExtractCoverage(InstructorVerdict v) => v?.Normalized;

        private static (bool Block, string Reason) ShouldBlock(PhaseSpec phase, ScenarioWorkbookV2 wb, InstructorVerdict v)
        {
            var blockList = new HashSet<string>(wb.Rubric?.ReadbackPolicy.BlockOnMissing ?? new());
            var missing = v?.Components?.Where(c => c.Score is 0 && c.Code != null).Select(c => c.Code!).ToHashSet() ?? new();
            var trip = missing.Intersect(blockList).Any() || (v?.SafetyFlag ?? false);
            return trip ? (true, v?.BlockReason ?? "Mandatory item missing") : (false, "");
        }

        private static BranchSpec? PickBranch(PhaseSpec phase, object ctx, Random rng, DifficultyProfile diff)
        {
            if (phase.Branches is null || phase.Branches.Count == 0) return null;
            if (diff.Variability <= 0) return null;

            var p = rng.NextDouble();
            double acc = 0;
            foreach (var b in phase.Branches)
            {
                acc += b.Probability * diff.Variability;
                if (p <= acc) return b;
            }
            return null;
        }

        private static object ApplyBranch(object ctx, BranchSpec b) => ctx;

        private static Transmission ToTransmissionATC(double freq, AtcReply reply, string persona) =>
            new()
            {
                Source = "ATC",
                FreqMhz = freq,
                Text = reply.Transmission,
                Tone = reply.TtsTone ?? "professional",
                Persona = persona,
                Attributes = new Dictionary<string, string> { { "direction", "N/A" } }
            };

        private static Transmission ToTransmissionTraffic(TurnRequest req, PhaseSpec phase, TrafficReply r) =>
            new()
            {
                Source = $"TRAFFIC:{r.SourceCallsign}",
                FreqMhz = req.Workbook.ContextResolved.Airport.TowerActive
                    ? req.Workbook.ContextResolved.Airport.TowerMhz
                    : req.Workbook.ContextResolved.Airport.CtafMhz,
                Text = r.Transmission,
                Tone = r.TtsTone ?? "professional",
                Persona = "concise",
                Attributes = r.Attributes ?? new Dictionary<string, string> { { "role", "traffic" } }
            };

        private static string Persona(TurnRequest req) =>
            string.IsNullOrWhiteSpace(req.ControllerPersona)
                ? (req.Difficulty.Congestion > 0.65 ? "high_workload" : "normal")
                : req.ControllerPersona!;

        private static string? ResolveNextPhaseId(PhaseSpec phase, AtcReply? atc, TrafficReply? traf)
        {
            if (atc?.NextState != null)
            {
                var bag = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(atc.NextState)) ?? new();
                if (bag.TryGetValue("phase", out var p) && p is string s && !string.IsNullOrWhiteSpace(s)) return s;
            }
            if (traf?.NextState != null)
            {
                var bag = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(traf.NextState)) ?? new();
                if (bag.TryGetValue("phase", out var p) && p is string s && !string.IsNullOrWhiteSpace(s)) return s;
            }
            return phase.Id;
        }

        private static JsonElement ApplyNextState(JsonElement current, string phaseId, string nextPhaseId, object? nextState)
        {
            using var doc = JsonDocument.Parse(current.GetRawText());
            var root = JsonSerializer.Deserialize<Dictionary<string, object>>(doc.RootElement.GetRawText()) ?? new();
            root["phase"] = nextPhaseId;

            if (nextState != null)
            {
                var bag = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(nextState)) ?? new();
                foreach (var kv in bag) root[kv.Key] = kv.Value;
            }
            return JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(root));
        }
        
        
   
        private static (IEnumerable<Transmission> Transmissions, IEnumerable<StateDelta> Deltas) HandleSilence(TurnRequest req, PhaseSpec phase, Random rng)
        {
            // If student was expected to call (e.g., join downwind), ATC poke or other traffic self-announce
            var tx = new List<Transmission>();
            var persona = Persona(req);
            if (req.Workbook.ContextResolved.Airport.TowerActive)
            {
                tx.Add(new Transmission
                {
                    Source = "ATC",
                    FreqMhz = phase.PrimaryFreqMhz,
                    Text = $"{req.Callsign}, say intentions.",
                    Tone = "professional",
                    Persona = persona
                });
            }
            else
            {
                tx.Add(new Transmission
                {
                    Source = "TRAFFIC:GEN",
                    FreqMhz = req.Workbook.ContextResolved.Airport.CtafMhz,
                    Text = "Traffic, downwind runway {{ PLACEHOLDER }}, aircraft {{ PLACEHOLDER }} in the circuit.",
                    Tone = "professional",
                    Persona = "concise"
                });
            }
            return (tx, Array.Empty<StateDelta>());
        }

        

        private static IEnumerable<Transmission> SimulateTraffic(TurnRequest req, object ctx, Random rng)
        {
            var list = new List<Transmission>();
            var snap = req.Workbook.ContextResolved.TrafficSnapshot;
            if (snap is null || snap.Actors.Count == 0) return list;

            // Simple rule: if conflict within 90 s, one actor makes a position call
            var imminent = snap.Conflicts?.FirstOrDefault(c => c.TimeToConflictS <= 90);
            if (imminent != null)
            {
                var actor = snap.Actors.FirstOrDefault(a => a.Callsign == imminent.WithCallsign) ?? snap.Actors[0];
                list.Add(new Transmission
                {
                    Source = $"TRAFFIC:{actor.Callsign}",
                    FreqMhz = req.Workbook.ContextResolved.Airport.TowerActive ? req.Workbook.ContextResolved.Airport.TowerMhz : req.Workbook.ContextResolved.Airport.CtafMhz,
                    Text = $"{actor.Callsign}, {req.Workbook.ContextResolved.RunwayInUse} final, {Math.Round((actor.GsMps ?? 0) * 3.6)} km/h ground speed.",
                    Tone = "professional",
                    Persona = "concise",
                    Attributes = new Dictionary<string, string> { { "direction", "final" } }
                });
            }
            return list;
        }
 
        private static JsonElement ApplyDeltas(JsonElement current, IEnumerable<StateDelta> deltas)
        {
            using var doc = JsonDocument.Parse(current.GetRawText());
            var root = JsonSerializer.Deserialize<Dictionary<string, object>>(doc.RootElement.GetRawText()) ?? new();
            foreach (var d in deltas)
                root[d.Key] = d.Value.Deserialize<object>();
            return JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(root));
        }

        private static string ResolveNextPhaseId(PhaseSpec phase, AtcReply atc) =>
            (atc?.NextState is null) ? phase.Id
            : JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(atc.NextState))?.GetValueOrDefault("phase")?.ToString() ?? phase.Id;

        private static AtcReply BuildRepeatClearance(PhaseSpec phase, object ctx, TurnRequest req, InstructorVerdict v, DifficultyProfile d)
        {
            // Minimal JSON that your IAtcService already supports if you want to bypass LLM on repeats
            return new AtcReply(
                Transmission: "Say again readback. Clearance is as follows, read back in full.",
                ExpectedReadback: phase.ExpectedReadback,
                NextState: new { phase = phase.Id },
                HoldShort: true,
                TtsTone: "professional"
            );
        }
    }
}