using System.Text.Json;
using System.Text.Json.Serialization;

namespace PilotSim.Core;

public record SttWord(string Text, double Start, double End);
public record SttResult(string Text, IReadOnlyList<SttWord> Words);

public enum Difficulty { Basic, Medium, Advanced }
public record Load(float TrafficDensity, float Clarity, string ControllerPersona, string RfQuality);

public interface ISttService {
    Task<SttResult> TranscribeAsync(Stream wavStream, string biasPrompt, CancellationToken cancellationToken);
}

public record ComponentScore(
    string Code,
    string Category,
    string Severity, // info, minor, major, critical
    double Weight,
    double Score, // raw component score 0-1
    double Delta, // contribution to total
    string? Detail);

public record InstructorVerdict(
    IReadOnlyList<string> Critical,
    IReadOnlyList<string> Improvements,
    string? ExemplarReadback,
    double Normalized,
    int ScoreDelta,
    string BlockReason,
    // Phase 2 additions (optional; supply defaults for backward compatibility)
    IReadOnlyList<ComponentScore>? Components = null,
    double? PhraseAccuracy = null,
    double? Ordering = null,
    double? Omissions = null,
    double? Safety = null,
    bool? SafetyFlag = null,
    string? RubricVersion = null);

public interface IInstructorService {
    Task<InstructorVerdict> ScoreAsync(string transcript, object state, Difficulty difficulty, CancellationToken cancellationToken);
}

public record AtcReply(
    string Transmission,
    IReadOnlyList<string> ExpectedReadback,
    object? NextState,
    bool? HoldShort,
    string? TtsTone);

public interface IAtcService {
    Task<AtcReply> NextAsync(string transcript, object state, Difficulty difficulty, Load load, CancellationToken cancellationToken);
}

public interface ITtsService {
    Task<string> SynthesizeAsync(string text, string voice, string style, CancellationToken cancellationToken);
}

// SimConnect integration types
public record SimConnectAircraftData(
    string Callsign,
    double Latitude,
    double Longitude, 
    double Altitude,
    double Heading,
    double Speed,
    string FlightPhase,
    string? FrequencyAssigned);

public record SimConnectStatus(
    bool IsConnected,
    string Status,
    DateTime LastUpdate,
    int ActiveAircraftCount);

public interface ISimConnectService {
    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync();
    Task<SimConnectStatus> GetStatusAsync();
    Task<IReadOnlyList<SimConnectAircraftData>> GetActiveAircraftAsync();
    Task<SimConnectAircraftData?> GetAircraftDataAsync(string callsign);
    Task<bool> SendAtcCommandAsync(string callsign, string command, CancellationToken cancellationToken = default);
    
    // Events
    event EventHandler<SimConnectAircraftData>? AircraftPositionUpdated;
    event EventHandler<string>? AircraftConnected;
    event EventHandler<string>? AircraftDisconnected;
}

  /// <summary>Store these in Scenario.InitialStateJson / Scenario.RubricJson. SI in data; RTF converts to ft/kt/NM at render.</summary>
    public sealed class ScenarioWorkbookV2
    {
        [JsonPropertyName("completion")] public CompletionSpec Completion { get; set; } = new();

        [JsonPropertyName("meta")] public Meta Meta { get; set; } = new();
        [JsonPropertyName("inputs")] public Inputs Inputs { get; set; } = new();
        [JsonPropertyName("context_resolved")] public ContextResolved ContextResolved { get; set; } = new();
        [JsonPropertyName("phases")] public List<PhaseSpec> Phases { get; set; } = new();
        [JsonPropertyName("rubric")] public RubricSpec? Rubric { get; set; }
        [JsonPropertyName("tolerance")] public ToleranceSpec? Tolerance { get; set; }
        [JsonPropertyName("global_safety_gates")] public List<SafetyGate> GlobalSafetyGates { get; set; } = new();
        [JsonPropertyName("debrief")] public DebriefSpec? Debrief { get; set; }
        [JsonPropertyName("authoring_notes")] public string? AuthoringNotes { get; set; }
    }

    public sealed class Meta
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("version")] public string Version { get; set; } = "2.0";
        [JsonPropertyName("author")] public string Author { get; set; } = "";
        [JsonPropertyName("review_date_utc")] public string? ReviewDateUtc { get; set; }
    }

    public sealed class Inputs
    {
        [JsonPropertyName("icao")] public string Icao { get; set; } = "";
        [JsonPropertyName("aircraft")] public string Aircraft { get; set; } = "";
        [JsonPropertyName("start_utc")] public string StartUtc { get; set; } = ""; // ISO8601
        [JsonPropertyName("time_of_day")] public string TimeOfDay { get; set; } = "day"; // dawn|day|dusk|night
        [JsonPropertyName("variability")] public double Variability { get; set; }
        [JsonPropertyName("load")] public double Load { get; set; }
        [JsonPropertyName("weather_source")] public string WeatherSource { get; set; } = "sim"; // real|sim
        [JsonPropertyName("notam_source")] public string NotamSource { get; set; } = "sim";    // real|sim
    }

    public sealed class ContextResolved
    {
        [JsonPropertyName("airport")] public AirportCtx Airport { get; set; } = new();
        [JsonPropertyName("runway_in_use")] public string RunwayInUse { get; set; } = "";
        [JsonPropertyName("weather_si")] public WeatherSI WeatherSi { get; set; } = new();
        [JsonPropertyName("atis_txt")] public string? AtisTxt { get; set; }
        [JsonPropertyName("notams")] public List<NotamSummary> Notams { get; set; } = new();
        [JsonPropertyName("traffic_snapshot")] public TrafficSnapshot TrafficSnapshot { get; set; } = new();
    }

    public sealed class AirportCtx
    {
        [JsonPropertyName("icao")] public string Icao { get; set; } = "";
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("lat_deg")] public double? LatDeg { get; set; }
        [JsonPropertyName("lon_deg")] public double? LonDeg { get; set; }
        [JsonPropertyName("elevation_m_msl")] public int? ElevationMMsl { get; set; }
        [JsonPropertyName("timezone")] public string Timezone { get; set; } = "";
        [JsonPropertyName("atis_mhz")] public double? AtisMhz { get; set; }
        [JsonPropertyName("tower_mhz")] public double? TowerMhz { get; set; }
        [JsonPropertyName("ground_mhz")] public double? GroundMhz { get; set; }
        [JsonPropertyName("approach_mhz")] public double? ApproachMhz { get; set; }
        [JsonPropertyName("ctaf_mhz")] public double? CtafMhz { get; set; }
        [JsonPropertyName("tower_active")] public bool TowerActive { get; set; }
        [JsonPropertyName("pattern_altitude_m_agl")] public int? PatternAltitudeMAgl { get; set; }
        [JsonPropertyName("circuit_direction")] public string? CircuitDirection { get; set; } // left|right|split
    }

    public sealed class WeatherSI
    {
        [JsonPropertyName("wind_dir_deg")] public int WindDirDeg { get; set; }
        [JsonPropertyName("wind_speed_mps")] public double WindSpeedMps { get; set; }
        [JsonPropertyName("gust_mps")] public double? GustMps { get; set; }
        [JsonPropertyName("vis_km")] public double VisKm { get; set; }
        [JsonPropertyName("cloud_base_m_agl")] public int? CloudBaseMAgl { get; set; }
        [JsonPropertyName("ceiling_m_agl")] public int? CeilingMAgl { get; set; }
        [JsonPropertyName("qnh_hpa")] public int QnhHpa { get; set; }
        [JsonPropertyName("temp_c")] public double TempC { get; set; }
        [JsonPropertyName("dewpoint_c")] public double? DewpointC { get; set; }
    }

    public sealed class NotamSummary
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("summary")] public string Summary { get; set; } = "";
        [JsonPropertyName("active")] public bool Active { get; set; }
    }

    public sealed class TrafficSnapshot
    {
        [JsonPropertyName("density")] public string Density { get; set; } = "light"; // light|moderate|heavy
        [JsonPropertyName("actors")] public List<TrafficActor> Actors { get; set; } = new();
        [JsonPropertyName("conflicts")] public List<Conflict> Conflicts { get; set; } = new();
        
        //to string
        public override string ToString()
        {
            return $"TrafficSnapshot(Density={Density}, Actors=[{string.Join(", ", Actors)}], Conflicts=[{string.Join(", ", Conflicts)}])";
        }
    }

    public sealed class TrafficActor
    {
        [JsonPropertyName("callsign")] public string Callsign { get; set; } = "";
        [JsonPropertyName("type")] public string Type { get; set; } = "";
        [JsonPropertyName("wake")] public string Wake { get; set; } = "Light";
        [JsonPropertyName("intent")] public string Intent { get; set; } = "";
        [JsonPropertyName("lat_deg")] public double? LatDeg { get; set; }
        [JsonPropertyName("lon_deg")] public double? LonDeg { get; set; }
        [JsonPropertyName("alt_m_msl")] public int? AltMMsl { get; set; }
        [JsonPropertyName("gs_mps")] public double? GsMps { get; set; }
        [JsonPropertyName("eta_s")] public int? EtaS { get; set; }
    }

    public sealed class Conflict
    {
        [JsonPropertyName("with_callsign")] public string WithCallsign { get; set; } = "";
        [JsonPropertyName("event")] public string Event { get; set; } = "";
        [JsonPropertyName("time_to_conflict_s")] public int TimeToConflictS { get; set; }
    }

    public sealed class PhaseSpec
    {
        // Extend PhaseSpec
        [JsonPropertyName("responder_map")] public ResponderMap ResponderMap { get; set; } = new();
        [JsonPropertyName("broadcast_required_components")] public List<string> BroadcastRequiredComponents { get; set; } = new();

        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("primary_freq_mhz")] public double PrimaryFreqMhz { get; set; }
        [JsonPropertyName("pilot_cue")] public string PilotCue { get; set; } = "";
        [JsonPropertyName("controller_policy")] public string? ControllerPolicy { get; set; }
        [JsonPropertyName("expected_readback")] public List<string> ExpectedReadback { get; set; } = new();
        [JsonPropertyName("required_components")] public List<String> RequiredComponents { get; set; } = new();
        [JsonPropertyName("entry_criteria")] public List<Criterion> EntryCriteria { get; set; } = new();
        [JsonPropertyName("common_errors")] public List<string> CommonErrors { get; set; } = new();
        [JsonPropertyName("coaching_tips")] public List<string> CoachingTips { get; set; } = new();
        [JsonPropertyName("safety_gates")] public List<SafetyGate> SafetyGates { get; set; } = new();
        [JsonPropertyName("next_state")] public NextState NextState { get; set; } = new();
        [JsonPropertyName("branches")] public List<BranchSpec> Branches { get; set; } = new();
    }

    public sealed class BranchSpec
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("probability")] public double Probability { get; set; }
        [JsonPropertyName("guard")] public List<Criterion> Guard { get; set; } = new();
        [JsonPropertyName("effects")] public List<StateDelta> Effects { get; set; } = new();
        [JsonPropertyName("atc_override")] public AtcOverride? AtcOverride { get; set; }
        [JsonPropertyName("adds_required_components")] public List<string>? AddsRequiredComponents { get; set; }
    }

    public sealed class AtcOverride
    {
        [JsonPropertyName("transmission")] public string? Transmission { get; set; }
        [JsonPropertyName("expected_readback")] public List<string>? ExpectedReadback { get; set; }
        [JsonPropertyName("required_components")] public List<string>? RequiredComponents { get; set; }
    }

    public sealed class NextState
    {
        [JsonPropertyName("phase_id")] public string PhaseId { get; set; } = "";
        [JsonPropertyName("state_deltas")] public List<StateDelta> StateDeltas { get; set; } = new();
    }

    public sealed class StateDelta
    {
        [JsonPropertyName("key")] public string Key { get; set; } = "";
        [JsonPropertyName("value")] public JsonElement Value { get; set; }
    }

    public sealed class Criterion
    {
        [JsonPropertyName("lhs")] public string Lhs { get; set; } = "";
        [JsonPropertyName("op")] public string Op { get; set; } = "=="; // >=,==,<=,missing,exists,contains
        [JsonPropertyName("rhs")] public JsonElement Rhs { get; set; }
    }

    public sealed class RubricSpec
    {
        [JsonPropertyName("rubric_version")] public string RubricVersion { get; set; } = "v2";
        [JsonPropertyName("weights")] public Weights Weights { get; set; } = new();
        [JsonPropertyName("safety_cap")] public double SafetyCap { get; set; } = 0.49;
        [JsonPropertyName("thresholds")] public Thresholds Thresholds { get; set; } = new();
        [JsonPropertyName("readback_policy")] public ReadbackPolicy ReadbackPolicy { get; set; } = new();
        [JsonPropertyName("timing")] public TimingSpec Timing { get; set; } = new();
    }

    public sealed class Weights
    {
        [JsonPropertyName("phrase_accuracy")] public double PhraseAccuracy { get; set; } = 0.35;
        [JsonPropertyName("ordering")] public double Ordering { get; set; } = 0.15;
        [JsonPropertyName("omissions")] public double Omissions { get; set; } = 0.20;
        [JsonPropertyName("safety")] public double Safety { get; set; } = 0.30;
    }

    public sealed class Thresholds
    {
        [JsonPropertyName("pass_norm")] public double PassNorm { get; set; } = 0.70;
        [JsonPropertyName("readback_cov_min")] public double ReadbackCovMin { get; set; } = 0.80;
    }

    public sealed class ReadbackPolicy
    {
        [JsonPropertyName("mandatory_components")] public List<string> MandatoryComponents { get; set; } = new();
        [JsonPropertyName("block_on_missing")] public List<string> BlockOnMissing { get; set; } = new();
        [JsonPropertyName("warn_on_missing")] public List<string> WarnOnMissing { get; set; } = new();
        [JsonPropertyName("slot_definitions")] public Dictionary<string, SlotDefinition> SlotDefinitions { get; set; } = new();
        [JsonPropertyName("broadcast_components")] public List<string> BroadcastComponents { get; set; } = new();

    }

    public sealed class SlotDefinition
    {
        [JsonPropertyName("pattern")] public string Pattern { get; set; } = "";
    }

    public sealed class ToleranceSpec
    {
        [JsonPropertyName("synonyms")] public List<List<string>> Synonyms { get; set; } = new();
        [JsonPropertyName("phonetic_variants")] public Dictionary<string, List<string>> PhoneticVariants { get; set; } = new();
        [JsonPropertyName("fuzzy_edit_distance_max")] public int FuzzyEditDistanceMax { get; set; } = 2;
        [JsonPropertyName("numeric_tolerance")] public NumericTolerance NumericTolerance { get; set; } = new();
        [JsonPropertyName("slot_scoring")] public bool SlotScoring { get; set; } = true;
    }

    public sealed class NumericTolerance
    {
        [JsonPropertyName("freq_decimals_equivalence")] public bool FreqDecimalsEquivalence { get; set; } = true;
    }

    public sealed class TimingSpec
    {
        [JsonPropertyName("response_latency_s_ok")] public List<double> ResponseLatencySOk { get; set; } = new() { 0.5, 3.0 };
        [JsonPropertyName("overlap_penalty_per_event")] public double OverlapPenaltyPerEvent { get; set; } = 0.05;
    }

    public sealed class SafetyGate
    {
        [JsonPropertyName("code")] public string Code { get; set; } = "";
        [JsonPropertyName("trigger")] public Criterion Trigger { get; set; } = new();
        [JsonPropertyName("action")] public string Action { get; set; } = "block"; // block|warn|coach
        [JsonPropertyName("controller_on_block")] public string? ControllerOnBlock { get; set; }
        [JsonPropertyName("debrief_note")] public string? DebriefNote { get; set; }
    }

    public sealed class DebriefSpec
    {
        [JsonPropertyName("bullet_points")] public List<string> BulletPoints { get; set; } = new();
        [JsonPropertyName("exemplar_readbacks")] public List<string> ExemplarReadbacks { get; set; } = new();
        [JsonPropertyName("references")] public List<string> References { get; set; } = new();
        [JsonPropertyName("metrics_to_show")] public List<string> MetricsToShow { get; set; } = new();
    }
// New
    public sealed class CompletionSpec
    {
        // All must be true for success
        [JsonPropertyName("success_when_all")] public List<Criterion> SuccessWhenAll { get; set; } = new();
        // Any true => failure major/minor
        [JsonPropertyName("fail_major_when_any")] public List<Criterion> FailMajorWhenAny { get; set; } = new();
        [JsonPropertyName("fail_minor_when_any")] public List<Criterion> FailMinorWhenAny { get; set; } = new();
        [JsonPropertyName("end_phase_id")] public string EndPhaseId { get; set; } = "end";
    }


// New
    public sealed class ResponderMap
    {
        // "ATC|CTAF": ATC if tower_active, else CTAF via TrafficAgent
        [JsonPropertyName("default")] public string Default { get; set; } = "ATC|CTAF";
        // "COACH" yields CoachAgent prompt on silence
        [JsonPropertyName("on_silence")] public string OnSilence { get; set; } = "COACH";
        // "TRAFFIC_NEAREST_CONFLICT" for imminent conflicts
        [JsonPropertyName("on_conflict")] public string OnConflict { get; set; } = "TRAFFIC_NEAREST_CONFLICT";
        // Random CTAF interjects
        [JsonPropertyName("random_interject_prob")] public double RandomInterjectProb { get; set; } = 0.15;
    }

// Extend ReadbackPolicy for CTAF broadcasts (optional but tidy)
    public interface ITrafficAgent
    {
        Task<TrafficReply> NextAsync(string transcript, object state, Difficulty difficulty, CancellationToken ct);
    }
    public sealed record TrafficReply(
        string Transmission,
        string SourceCallsign,
        List<string> ExpectedReadback,
        object NextState,
        string? TtsTone,
        Dictionary<string,string>? Attributes
    );

    public interface ICoachAgent
    {
        Task<CoachReply> NextAsync(object state, string hint, CancellationToken ct);
    }
    public sealed record CoachReply(string Transmission, object NextState, string? TtsTone);