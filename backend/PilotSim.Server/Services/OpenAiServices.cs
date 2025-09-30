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

// Old implementations removed - now using OpenAiInstructorServiceV2 and OpenAiTrafficAgentService from TurnService.cs

// Adapter to allow SimulationController to keep working with old API while using new ITrafficAgent
public class AtcServiceAdapter : IAtcService
{
    private readonly PilotSim.Server.Services.ITrafficAgent _trafficAgent;
    private readonly ILogger<AtcServiceAdapter> _logger;

    public AtcServiceAdapter(PilotSim.Server.Services.ITrafficAgent trafficAgent, ILogger<AtcServiceAdapter> logger)
    {
        _trafficAgent = trafficAgent;
        _logger = logger;
    }

    public async Task<AtcReply> NextAsync(string transcript, object state, Difficulty difficulty, Load load, CancellationToken ct)
    {
        try
        {
            // Use traffic agent for now - in the new architecture this would be routed through TurnService
            var trafficReply = await _trafficAgent.NextAsync(transcript, state, difficulty, ct);
            
            return new AtcReply(
                Transmission: trafficReply.Transmission,
                ExpectedReadback: trafficReply.ExpectedReadback,
                NextState: trafficReply.NextState,
                HoldShort: null,
                TtsTone: trafficReply.TtsTone
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate ATC response via adapter");
            return new AtcReply(
                Transmission: "Say again, transmission unclear",
                ExpectedReadback: new List<string> { "Transmission unclear" },
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