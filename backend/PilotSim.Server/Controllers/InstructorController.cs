using Microsoft.AspNetCore.Mvc;
using PilotSim.Core;

namespace PilotSim.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InstructorController : ControllerBase
{
    private readonly IInstructorService _instructorService;

    public InstructorController(IInstructorService instructorService)
    {
        _instructorService = instructorService;
    }

    public record ScoreRequest(string Transcript, object State, Difficulty Difficulty);

    [HttpPost]
    public async Task<ActionResult<InstructorVerdict>> ScoreAsync([FromBody] ScoreRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request.Transcript))
            return BadRequest("Transcript is required");

        var result = await _instructorService.ScoreAsync(request.Transcript, request.State, request.Difficulty, cancellationToken);
        return Ok(result);
    }
}