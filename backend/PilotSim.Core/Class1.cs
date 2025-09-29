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
