using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PilotSim.Data;
using System.Reflection;

namespace PilotSim.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly SimDbContext _context;

    public HealthController(SimDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult> CheckHealth()
    {
        try
        {
            // Check database connectivity
            await _context.Database.CanConnectAsync();
            
            var response = new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                Database = "Connected",
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            var response = new
            {
                Status = "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Error = ex.Message,
                Database = "Disconnected"
            };

            return StatusCode(503, response);
        }
    }

    [HttpGet("ready")]
    public ActionResult CheckReadiness()
    {
        // Simple readiness check
        return Ok(new { Status = "Ready", Timestamp = DateTime.UtcNow });
    }

    [HttpGet("live")]
    public ActionResult CheckLiveness()
    {
        // Simple liveness check
        return Ok(new { Status = "Alive", Timestamp = DateTime.UtcNow });
    }
}