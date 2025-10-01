using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PilotSim.Data;
using PilotSim.Data.Models;
using PilotSim.Server.Services;
using System.Text.Json;

namespace PilotSim.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionController : ControllerBase
{
    private readonly SimDbContext _context;
    private readonly IWorkbookResolver _resolver;
    private readonly ILogger<SessionController> _log;

    public SessionController(SimDbContext context, IWorkbookResolver resolver, ILogger<SessionController> log)
    {
        _context = context; _resolver = resolver; _log = log;
    }

    public record StartSessionRequest(int ScenarioId, string Difficulty, object? Parameters = null);
    public record EndSessionRequest(string Outcome);

    [HttpPost]
    public async Task<ActionResult<int>> StartSessionAsync([FromBody] StartSessionRequest request, CancellationToken cancellationToken = default)
    {
        var scenario = await _context.Scenarios.Include(s => s.Airport).ThenInclude(a => a!.Runways).FirstOrDefaultAsync(s => s.Id == request.ScenarioId, cancellationToken);
        if (scenario == null) return NotFound("Scenario not found");

        // Resolve workbook
        var workbook = await _resolver.ResolveAsync(scenario, cancellationToken);
        var workbookJson = JsonSerializer.Serialize(workbook, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });

        var session = new Session
        {
            ScenarioId = request.ScenarioId,
            StartedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Difficulty = request.Difficulty,
            ParametersJson = workbookJson // store resolved workbook in session parameters for retrieval
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(session.Id);
    }

    [HttpGet("{sessionId}/workbook")]
    public async Task<ActionResult<object>> GetResolvedWorkbookAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _context.Sessions.Include(s => s.Scenario).FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        if (session == null) return NotFound();
        if (string.IsNullOrWhiteSpace(session.ParametersJson)) return NotFound("No workbook stored on session");
        try
        {
            var doc = JsonDocument.Parse(session.ParametersJson);
            return Ok(doc.RootElement);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Invalid workbook JSON in session {SessionId}", sessionId);
            return StatusCode(500, "Workbook parse error");
        }
    }

    [HttpPost("{sessionId}/end")]
    public async Task<ActionResult> EndSessionAsync(int sessionId, [FromBody] EndSessionRequest request, CancellationToken cancellationToken = default)
    {
        var session = await _context.Sessions.FindAsync(new object?[] { sessionId }, cancellationToken: cancellationToken);
        if (session == null)
            return NotFound();

        session.EndedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        session.Outcome = request.Outcome;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok();
    }
}