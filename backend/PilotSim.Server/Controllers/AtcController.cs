using Microsoft.AspNetCore.Mvc;
using PilotSim.Core;

namespace PilotSim.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AtcController : ControllerBase
{
    private readonly IAtcService _atcService;

    public AtcController(IAtcService atcService)
    {
        _atcService = atcService;
    }

    public record AtcRequest(string Transcript, object State, Difficulty Difficulty, Load Load);

    [HttpPost]
    public async Task<ActionResult<AtcReply>> NextAsync([FromBody] AtcRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request.Transcript))
            return BadRequest("Transcript is required");

        // Validate input size
        if (request.Transcript.Length > 5000)
            return BadRequest("Transcript too long. Maximum 5000 characters allowed");

        var result = await _atcService.NextAsync(request.Transcript, request.State, request.Difficulty, request.Load, cancellationToken);
        return Ok(result);
    }
}