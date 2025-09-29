using OpenAI;
using OpenAI.Audio;
using OpenAI.Chat;
using PilotSim.Core;
using System.Text.Json;

namespace PilotSim.Server.Services;

public class OpenAiSttService : ISttService
{
    private readonly OpenAIClient _client;
    private readonly ILogger<OpenAiSttService> _logger;

    public OpenAiSttService(OpenAIClient client, ILogger<OpenAiSttService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<SttResult> TranscribeAsync(Stream wavStream, string biasPrompt, CancellationToken cancellationToken)
    {
        try
        {
            var audioClient = _client.GetAudioClient("whisper-1");
            
            var options = new AudioTranscriptionOptions
            {
                ResponseFormat = AudioTranscriptionFormat.Text,
                Prompt = biasPrompt
            };

            var result = await audioClient.TranscribeAudioAsync(wavStream, "audio.wav", options, cancellationToken);
            
            // Since we can't get word timestamps with this version, create a simple word list
            var words = result.Value.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select((word, index) => new SttWord(word, index * 0.5, (index + 1) * 0.5))
                .ToList();

            return new SttResult(result.Value.Text ?? "", words);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to transcribe audio");
            // Return empty result on error
            return new SttResult("", new List<SttWord>());
        }
    }
}

public class OpenAiInstructorService : IInstructorService
{
    private readonly OpenAIClient _client;
        private readonly ILogger<OpenAiInstructorService> _logger;
        private const string SystemPrompt = """
You are "Instructor". Task: score and coach pilot radio calls per AIP Australia.

Evaluate against rubric dimensions:
- Phrase Accuracy (accuracy of phraseology & callsign)
- Ordering (logical sequence / structure)
- Omissions (missing required elements)
- Safety (potential safety impact)

Return structured JSON with keys:
{
    "critical": [..],
    "improvements": [..],
    "exemplarReadback": string|null,
    "normalized": 0.0-1.0,
    "scoreDelta": int,
    "blockReason": string,
    "rubricVersion": "v1",
    "phraseAccuracy": 0.0-1.0,
    "ordering": 0.0-1.0,
    "omissions": 0.0-1.0,
    "safety": 0.0-1.0,
    "safetyFlag": true/false,
    "components": [
        {"code":"PA_CALLSIGN","category":"PhraseAccuracy","severity":"minor","weight":0.25,"score":0.8,"delta":3,"detail":"Correct prefix; minor phonetics lapses"},
        {"code":"OM_ALT","category":"Omissions","severity":"major","weight":0.20,"score":0.2,"delta":-4,"detail":"Altitude missing"}
    ]
}
Rules:
- Keep list sizes small (<=5 critical). Use Australian aviation phraseology.
- Block if any safety-critical lapse endangers separation or clarity.
""";

    public OpenAiInstructorService(OpenAIClient client, ILogger<OpenAiInstructorService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<InstructorVerdict> ScoreAsync(string transcript, object state, Difficulty difficulty, CancellationToken cancellationToken)
    {
        try
        {
            var chatClient = _client.GetChatClient("gpt-4o-mini");
            
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(SystemPrompt),
                new UserChatMessage($@"Evaluate this pilot transmission JSON only no prose:
Transcript: ""{transcript}""
Difficulty: {difficulty}
Current State: {JsonSerializer.Serialize(state)}")
            };

            var response = await chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
            var content = response.Value.Content[0].Text;

            // Parse JSON response
            var verdictJson = JsonSerializer.Deserialize<JsonElement>(content);

            List<ComponentScore>? components = null;
            if (verdictJson.TryGetProperty("components", out var comps) && comps.ValueKind == JsonValueKind.Array)
            {
                components = comps.EnumerateArray().Select(c => new ComponentScore(
                    Code: c.GetProperty("code").GetString() ?? "UNKNOWN",
                    Category: c.GetProperty("category").GetString() ?? "General",
                    Severity: c.GetProperty("severity").GetString() ?? "info",
                    Weight: c.TryGetProperty("weight", out var wt) ? wt.GetDouble() : 0,
                    Score: c.TryGetProperty("score", out var sc) ? sc.GetDouble() : 0,
                    Delta: c.TryGetProperty("delta", out var d) ? d.GetDouble() : 0,
                    Detail: c.TryGetProperty("detail", out var dt) ? dt.GetString() : null
                )).ToList();
            }

            double? GetNullableDouble(string name) => verdictJson.TryGetProperty(name, out var p) && p.ValueKind is JsonValueKind.Number ? p.GetDouble() : null;
            bool? GetNullableBool(string name) => verdictJson.TryGetProperty(name, out var p) && p.ValueKind is JsonValueKind.True or JsonValueKind.False ? p.GetBoolean() : null;

            return new InstructorVerdict(
                Critical: verdictJson.GetProperty("critical").EnumerateArray().Select(x => x.GetString() ?? "").ToList(),
                Improvements: verdictJson.GetProperty("improvements").EnumerateArray().Select(x => x.GetString() ?? "").ToList(),
                ExemplarReadback: verdictJson.TryGetProperty("exemplarReadback", out var exemplar) ? exemplar.GetString() : null,
                Normalized: verdictJson.GetProperty("normalized").GetDouble(),
                ScoreDelta: verdictJson.GetProperty("scoreDelta").GetInt32(),
                BlockReason: verdictJson.GetProperty("blockReason").GetString() ?? "",
                Components: components,
                PhraseAccuracy: GetNullableDouble("phraseAccuracy"),
                Ordering: GetNullableDouble("ordering"),
                Omissions: GetNullableDouble("omissions"),
                Safety: GetNullableDouble("safety"),
                SafetyFlag: GetNullableBool("safetyFlag"),
                RubricVersion: verdictJson.TryGetProperty("rubricVersion", out var rv) ? rv.GetString() : null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to score transcript: {Transcript}", transcript);
            // Return neutral score on error
            return new InstructorVerdict(
                Critical: new List<string> { "System error - please try again" },
                Improvements: new List<string>(),
                ExemplarReadback: null,
                Normalized: 0.5,
                ScoreDelta: 0,
                BlockReason: "System error"
            );
        }
    }
}

public class OpenAiAtcService : IAtcService
{
    private readonly OpenAIClient _client;
    private readonly ILogger<OpenAiAtcService> _logger;
    private const string SystemPrompt = @"You are ""ATC Controller"" at an Australian airport. Respond to pilot transmissions with proper ATC phraseology per AIP Australia.

Provide realistic ATC responses considering:
1. Current traffic situation and load
2. Airport operations and runway status
3. Weather conditions (QNH, wind)
4. Standard taxi/takeoff/landing procedures
5. Australian aviation phraseology

Always include expected pilot readbacks and update the simulation state appropriately.

Maintain professional, clear communications as a real ATC controller would.";

    public OpenAiAtcService(OpenAIClient client, ILogger<OpenAiAtcService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<AtcReply> NextAsync(string transcript, object state, Difficulty difficulty, Load load, CancellationToken cancellationToken)
    {
        try
        {
            var chatClient = _client.GetChatClient("gpt-4o-mini");
            
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(SystemPrompt),
                new UserChatMessage($@"Pilot transmission: ""{transcript}""
Current State: {JsonSerializer.Serialize(state)}
Difficulty: {difficulty}
Load: Traffic Density: {load.TrafficDensity}, Clarity: {load.Clarity}, Controller Persona: {load.ControllerPersona}

Provide ATC response in JSON format:
{{
    ""transmission"": ""ATC response text"",
    ""expectedReadback"": [""array of expected pilot readback options""],
    ""nextState"": {{""updated simulation state object""}},
    ""holdShort"": true/false,
    ""ttsTone"": ""professional/urgent/calm""
}}")
            };

            var response = await chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
            var content = response.Value.Content[0].Text;

            // Parse JSON response
            var replyJson = JsonSerializer.Deserialize<JsonElement>(content);
            
            return new AtcReply(
                Transmission: replyJson.GetProperty("transmission").GetString() ?? "",
                ExpectedReadback: replyJson.GetProperty("expectedReadback").EnumerateArray().Select(x => x.GetString() ?? "").ToList(),
                NextState: JsonSerializer.Deserialize<object>(replyJson.GetProperty("nextState").GetRawText()),
                HoldShort: replyJson.TryGetProperty("holdShort", out var hold) ? hold.GetBoolean() : null,
                TtsTone: replyJson.TryGetProperty("ttsTone", out var tone) ? tone.GetString() : null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate ATC response for transcript: {Transcript}", transcript);
            // Return default response on error
            return new AtcReply(
                Transmission: "Say again, transmission unclear",
                ExpectedReadback: new List<string> { "Transmission unclear, [callsign]" },
                NextState: state,
                HoldShort: null,
                TtsTone: "professional"
            );
        }
    }
}

public class OpenAiTtsService : ITtsService
{
    private readonly OpenAIClient _client;
    private readonly ILogger<OpenAiTtsService> _logger;
    private readonly string _audioDirectory;

    public OpenAiTtsService(OpenAIClient client, ILogger<OpenAiTtsService> logger, IWebHostEnvironment environment)
    {
        _client = client;
        _logger = logger;
        _audioDirectory = Path.Combine(environment.WebRootPath, "audio");
        Directory.CreateDirectory(_audioDirectory);
    }

    public async Task<string> SynthesizeAsync(string text, string voice, string style, CancellationToken cancellationToken)
    {
        try
        {
            var audioClient = _client.GetAudioClient("tts-1");
            
            // Map voice and style to OpenAI TTS voices
            var openAiVoice = voice.ToLower() switch
            {
                "professional" => GeneratedSpeechVoice.Alloy,
                "calm" => GeneratedSpeechVoice.Echo,
                "urgent" => GeneratedSpeechVoice.Fable,
                _ => GeneratedSpeechVoice.Alloy
            };

            var options = new SpeechGenerationOptions
            {
                ResponseFormat = GeneratedSpeechFormat.Mp3
            };

            var response = await audioClient.GenerateSpeechAsync(text, openAiVoice, options, cancellationToken);
            
            // Save audio to file
            var fileName = $"tts_{Guid.NewGuid():N}.mp3";
            var filePath = Path.Combine(_audioDirectory, fileName);
            
            await File.WriteAllBytesAsync(filePath, response.Value.ToArray(), cancellationToken);
            
            return $"/audio/{fileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to synthesize speech for text: {Text}", text);
            // Return empty path on error
            return "";
        }
    }
}