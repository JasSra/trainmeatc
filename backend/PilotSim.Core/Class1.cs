namespace PilotSim.Core;

public record SttWord(string Text, double Start, double End);
public record SttResult(string Text, IReadOnlyList<SttWord> Words);

public enum Difficulty { Basic, Medium, Advanced }
public record Load(float TrafficDensity, float Clarity, string ControllerPersona, string RfQuality);

public interface ISttService {
    Task<SttResult> TranscribeAsync(Stream wavStream, string biasPrompt, CancellationToken cancellationToken);
}

public record InstructorVerdict(
    IReadOnlyList<string> Critical,
    IReadOnlyList<string> Improvements,
    string? ExemplarReadback,
    double Normalized,
    int ScoreDelta,
    string BlockReason);

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
