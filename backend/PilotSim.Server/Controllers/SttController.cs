using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PilotSim.Core;
using PilotSim.Server.Hubs;

namespace PilotSim.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SttController : ControllerBase
{
    private readonly ISttService _sttService;
    private readonly IHubContext<LiveHub> _hubContext;
    private const int MaxAudioSizeBytes = 25 * 1024 * 1024; // 25MB limit

    public SttController(ISttService sttService, IHubContext<LiveHub> hubContext)
    {
        _sttService = sttService;
        _hubContext = hubContext;
    }

    [HttpPost]
    [RequestSizeLimit(MaxAudioSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxAudioSizeBytes)]
    public async Task<ActionResult<SttResult>> TranscribeAsync(IFormFile audio, [FromForm] string biasPrompt = "", [FromForm] string? sessionId = null, CancellationToken cancellationToken = default)
    {
        if (audio == null || audio.Length == 0)
            return BadRequest("Audio file is required");

        // Validate file size
        if (audio.Length > MaxAudioSizeBytes)
            return BadRequest($"Audio file too large. Maximum size is {MaxAudioSizeBytes / (1024 * 1024)}MB");

        // Validate content type
        var allowedTypes = new[] { "audio/wav", "audio/mpeg", "audio/mp4", "audio/webm", "audio/ogg" };
        if (!allowedTypes.Contains(audio.ContentType?.ToLower()))
            return BadRequest($"Unsupported audio format. Allowed types: {string.Join(", ", allowedTypes)}");

        using var stream = audio.OpenReadStream();
        var result = await _sttService.TranscribeAsync(stream, biasPrompt, cancellationToken);
        
        // Send real-time update via SignalR if sessionId provided
        if (!string.IsNullOrEmpty(sessionId) && result.Text != null)
        {
            // Calculate confidence based on word count and quality (simple heuristic)
            double confidence = Math.Min(1.0, result.Words?.Count * 0.1 ?? 0.5);
            
            await _hubContext.Clients.Group($"session-{sessionId}")
                .SendAsync("partialTranscript", result.Text, confidence, cancellationToken);
        }
        
        return Ok(result);
    }
}