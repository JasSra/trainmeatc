using Microsoft.AspNetCore.Mvc;
using PilotSim.Core;

namespace PilotSim.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SttController : ControllerBase
{
    private readonly ISttService _sttService;

    public SttController(ISttService sttService)
    {
        _sttService = sttService;
    }

    [HttpPost]
    public async Task<ActionResult<SttResult>> TranscribeAsync(IFormFile audio, [FromForm] string biasPrompt = "", CancellationToken cancellationToken = default)
    {
        if (audio == null || audio.Length == 0)
            return BadRequest("Audio file is required");

        using var stream = audio.OpenReadStream();
        var result = await _sttService.TranscribeAsync(stream, biasPrompt, cancellationToken);
        return Ok(result);
    }
}