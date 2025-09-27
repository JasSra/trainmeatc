using PilotSim.Core;

namespace PilotSim.Server.Services;

public class StubSttService : ISttService
{
    public Task<SttResult> TranscribeAsync(Stream wavStream, string biasPrompt, CancellationToken cancellationToken)
    {
        // Stub implementation - returns dummy transcript
        var result = new SttResult(
            "Tower, VH-ABC requesting taxi for departure",
            new List<SttWord>
            {
                new("Tower", 0.0, 0.5),
                new("VH-ABC", 0.6, 1.2),
                new("requesting", 1.3, 1.8),
                new("taxi", 1.9, 2.2),
                new("for", 2.3, 2.5),
                new("departure", 2.6, 3.2)
            }
        );
        return Task.FromResult(result);
    }
}

public class StubInstructorService : IInstructorService
{
    public Task<InstructorVerdict> ScoreAsync(string transcript, object state, Difficulty difficulty, CancellationToken cancellationToken)
    {
        // Stub implementation - returns dummy verdict
        var verdict = new InstructorVerdict(
            Critical: new List<string>(),
            Improvements: new List<string> { "Good callsign usage" },
            ExemplarReadback: "Tower, VH-ABC ready for departure",
            Normalized: 0.85,
            ScoreDelta: 8,
            BlockReason: ""
        );
        return Task.FromResult(verdict);
    }
}

public class StubAtcService : IAtcService
{
    public Task<AtcReply> NextAsync(string transcript, object state, Difficulty difficulty, Load load, CancellationToken cancellationToken)
    {
        // Stub implementation - returns dummy ATC response
        var reply = new AtcReply(
            Transmission: "VH-ABC, taxi via taxiway Alpha to holding point runway 34, QNH 1013",
            ExpectedReadback: new List<string> { "Taxi via Alpha to holding point runway 34, QNH 1013, VH-ABC" },
            NextState: new { Position = "TaxiwayAlpha", Runway = "34", QNH = 1013 },
            HoldShort: true,
            TtsTone: "professional"
        );
        return Task.FromResult(reply);
    }
}

public class StubTtsService : ITtsService
{
    public Task<string> SynthesizeAsync(string text, string voice, string style, CancellationToken cancellationToken)
    {
        // Stub implementation - returns dummy audio path
        return Task.FromResult("/audio/tts_stub.wav");
    }
}