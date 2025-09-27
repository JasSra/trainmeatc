using Microsoft.AspNetCore.Mvc;
using PilotSim.Core;

namespace PilotSim.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TtsController : ControllerBase
{
    private readonly ITtsService _ttsService;

    public TtsController(ITtsService ttsService)
    {
        _ttsService = ttsService;
    }

    public record TtsRequest(string Text, string Voice = "default", string Style = "neutral");

    [HttpPost]
    public async Task<ActionResult<string>> SynthesizeAsync([FromBody] TtsRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request.Text))
            return BadRequest("Text is required");

        var result = await _ttsService.SynthesizeAsync(request.Text, request.Voice, request.Style, cancellationToken);
        return Ok(new { AudioPath = result });
    }
}