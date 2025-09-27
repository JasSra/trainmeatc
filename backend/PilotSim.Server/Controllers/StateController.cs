using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PilotSim.Data;

namespace PilotSim.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StateController : ControllerBase
{
    private readonly SimDbContext _context;

    public StateController(SimDbContext context)
    {
        _context = context;
    }

    [HttpGet("{sessionId}")]
    public async Task<ActionResult<object>> GetSessionStateAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _context.Sessions
            .Include(s => s.Turns)
            .Include(s => s.Metrics)
            .Include(s => s.Scenario)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session == null)
            return NotFound();

        var state = new
        {
            SessionId = session.Id,
            ScenarioId = session.ScenarioId,
            ScenarioName = session.Scenario?.Name,
            StartedUtc = session.StartedUtc,
            EndedUtc = session.EndedUtc,
            Difficulty = session.Difficulty,
            ScoreTotal = session.ScoreTotal,
            Outcome = session.Outcome,
            TurnCount = session.Turns.Count,
            LastTurn = session.Turns.OrderBy(t => t.Idx).LastOrDefault()?.UserTranscript,
            Metrics = session.Metrics.Select(m => new { m.K, m.V, m.TUtc }).ToList()
        };

        return Ok(state);
    }
}