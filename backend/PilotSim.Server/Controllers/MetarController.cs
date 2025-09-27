using Microsoft.AspNetCore.Mvc;
using PilotSim.Server.Services;

namespace PilotSim.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MetarController : ControllerBase
{
    private readonly IMetarService _metarService;

    public MetarController(IMetarService metarService)
    {
        _metarService = metarService;
    }

    [HttpGet("{airportIcao}")]
    public async Task<ActionResult<MetarData>> GetCurrentMetar(string airportIcao, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(airportIcao) || airportIcao.Length != 4)
            return BadRequest("Valid 4-letter ICAO code required");

        var metar = await _metarService.GetMetarAsync(airportIcao.ToUpper(), cancellationToken);
        
        if (metar == null)
            return NotFound($"Airport {airportIcao} not found");

        return Ok(metar);
    }

    [HttpGet("{airportIcao}/history")]
    public async Task<ActionResult<List<MetarData>>> GetMetarHistory(
        string airportIcao, 
        [FromQuery] int hours = 24, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(airportIcao) || airportIcao.Length != 4)
            return BadRequest("Valid 4-letter ICAO code required");

        if (hours < 1 || hours > 72)
            return BadRequest("Hours must be between 1 and 72");

        var metars = await _metarService.GetRecentMetarsAsync(airportIcao.ToUpper(), hours, cancellationToken);
        
        return Ok(metars);
    }
}