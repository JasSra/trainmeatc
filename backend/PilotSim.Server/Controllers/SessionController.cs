using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PilotSim.Data;
using PilotSim.Data.Models;

namespace PilotSim.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionController : ControllerBase
{
    private readonly SimDbContext _context;

    public SessionController(SimDbContext context)
    {
        _context = context;
    }

    public record StartSessionRequest(int ScenarioId, string Difficulty, object? Parameters = null);
    public record EndSessionRequest(string Outcome);

    [HttpPost]
    public async Task<ActionResult<int>> StartSessionAsync([FromBody] StartSessionRequest request, CancellationToken cancellationToken = default)
    {
        var session = new Session
        {
            ScenarioId = request.ScenarioId,
            StartedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Difficulty = request.Difficulty,
            ParametersJson = request.Parameters?.ToString() ?? "{}"
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(session.Id);
    }

    [HttpPost("{sessionId}/end")]
    public async Task<ActionResult> EndSessionAsync(int sessionId, [FromBody] EndSessionRequest request, CancellationToken cancellationToken = default)
    {
        var session = await _context.Sessions.FindAsync(sessionId);
        if (session == null)
            return NotFound();

        session.EndedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        session.Outcome = request.Outcome;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok();
    }
}