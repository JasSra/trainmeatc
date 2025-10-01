using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PilotSim.Core;
using PilotSim.Data;
using PilotSim.Data.Models;

namespace PilotSim.Server.Services;

public interface IWorkbookResolver
{
    Task<ScenarioWorkbookV2> ResolveAsync(Scenario scenario, CancellationToken ct = default);
}

public sealed class WorkbookResolver : IWorkbookResolver
{
    private readonly SimDbContext _db;
    private readonly ILogger<WorkbookResolver> _log;

    public WorkbookResolver(SimDbContext db, ILogger<WorkbookResolver> log)
    {
        _db = db; _log = log;
    }

    public async Task<ScenarioWorkbookV2> ResolveAsync(Scenario scenario, CancellationToken ct = default)
    {
        ScenarioWorkbookV2 workbook;
        try
        {
            if (!string.IsNullOrWhiteSpace(scenario.InitialStateJson))
            {
                workbook = JsonSerializer.Deserialize<ScenarioWorkbookV2>(scenario.InitialStateJson) ?? new ScenarioWorkbookV2();
            }
            else
            {
                workbook = new ScenarioWorkbookV2();
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to parse scenario InitialStateJson for Scenario {Id}. Using empty workbook template.", scenario.Id);
            workbook = new ScenarioWorkbookV2();
        }

        // Ensure meta
        workbook.Meta ??= new Meta { Id = scenario.Id.ToString(), Author = "system" };

        // Ensure at least one phase
        if (workbook.Phases == null || workbook.Phases.Count == 0)
        {
            workbook.Phases = new List<PhaseSpec>
            {
                new PhaseSpec { Id = "phase_1", Name = scenario.Name ?? "Phase 1", PrimaryFreqMhz = ParsePrimaryFreq(scenario) }
            };
        }

        // Context
        workbook.ContextResolved ??= new ContextResolved();
        workbook.ContextResolved.Airport ??= new AirportCtx();

        if (scenario.AirportIcao != null)
        {
            var airport = await _db.Airports.Include(a => a.Runways).FirstOrDefaultAsync(a => a.Icao == scenario.AirportIcao, ct);
            if (airport != null)
            {
                workbook.ContextResolved.Airport.Icao = airport.Icao;
                workbook.ContextResolved.Airport.Name = airport.Name;
                workbook.ContextResolved.Airport.TowerActive = !string.IsNullOrWhiteSpace(airport.TowerFreq);
                workbook.ContextResolved.Airport.TowerMhz = ParseFreq(airport.TowerFreq);
                workbook.ContextResolved.Airport.GroundMhz = ParseFreq(airport.GroundFreq);
                workbook.ContextResolved.Airport.AtisMhz = ParseFreq(airport.AtisFreq);
                workbook.ContextResolved.Airport.ApproachMhz = ParseFreq(airport.AppFreq);
                workbook.ContextResolved.Airport.CtafMhz = ParseFreq(airport.GroundFreq) ?? ParseFreq(airport.TowerFreq) ?? 0;
                workbook.ContextResolved.Airport.LatDeg = airport.Lat;
                workbook.ContextResolved.Airport.LonDeg = airport.Lon;
                workbook.ContextResolved.Airport.ElevationMMsl = airport.ElevationFt.HasValue ? (int?)(airport.ElevationFt.Value * 0.3048) : null;
            }
        }

        if (string.IsNullOrWhiteSpace(workbook.ContextResolved.RunwayInUse))
        {
            workbook.ContextResolved.RunwayInUse = scenario.Airport?.Runways?.FirstOrDefault()?.Ident ?? "UNKNOWN";
        }

        // Weather (SI)
        workbook.ContextResolved.WeatherSi ??= new WeatherSI
        {
            WindDirDeg = Random.Shared.Next(10, 360),
            WindSpeedMps = 4 + Random.Shared.NextDouble() * 6, // ~8-16 kt
            VisKm = 8 + Random.Shared.NextDouble() * 4,
            QnhHpa = 1008 + Random.Shared.Next(0, 12),
            TempC = 18 + Random.Shared.Next(-4, 6),
            CloudBaseMAgl = 600 + Random.Shared.Next(0, 600)
        };

        // ATIS text simple synthesis
        workbook.ContextResolved.AtisTxt ??= $"Wind {workbook.ContextResolved.WeatherSi.WindDirDeg} at {(int)(workbook.ContextResolved.WeatherSi.WindSpeedMps * 1.94)} knots. Visibility {workbook.ContextResolved.WeatherSi.VisKm:0} km. QNH {workbook.ContextResolved.WeatherSi.QnhHpa} hPa.";

        // Traffic snapshot minimal if empty
        workbook.ContextResolved.TrafficSnapshot ??= new TrafficSnapshot();
        workbook.ContextResolved.TrafficSnapshot.Density = scenario.TrafficDensity?.ToLowerInvariant() ?? workbook.ContextResolved.TrafficSnapshot.Density;
        workbook.ContextResolved.TrafficSnapshot.Actors ??= new List<TrafficActor>();
        workbook.ContextResolved.TrafficSnapshot.Conflicts ??= new List<Conflict>();

        // Provide one sample actor if none and density != light
        if (!workbook.ContextResolved.TrafficSnapshot.Actors.Any() && workbook.ContextResolved.TrafficSnapshot.Density != "light")
        {
            workbook.ContextResolved.TrafficSnapshot.Actors.Add(new TrafficActor
            {
                Callsign = "VH-ABC",
                Type = "C172",
                Intent = "circuit_downwind",
                AltMMsl = 1000,
                GsMps = 30,
                EtaS = 90
            });
        }

        // Provide conflict if more than one actor & random pick
        if (workbook.ContextResolved.TrafficSnapshot.Actors.Count > 1 && !workbook.ContextResolved.TrafficSnapshot.Conflicts.Any())
        {
            var other = workbook.ContextResolved.TrafficSnapshot.Actors.First();
            workbook.ContextResolved.TrafficSnapshot.Conflicts.Add(new Conflict
            {
                WithCallsign = other.Callsign,
                Event = "pattern_merge",
                TimeToConflictS = 80
            });
        }

        // Ensure tolerance and rubric
        workbook.Tolerance ??= new ToleranceSpec();
        workbook.Rubric ??= workbook.Rubric ?? new RubricSpec();

        return workbook;
    }

    private static double ParsePrimaryFreq(Scenario scenario)
    {
        var freq = ParseFreq(scenario.PrimaryFrequency);
        if (freq.HasValue) return freq.Value;
        return 118.700; // default
    }

    private static double? ParseFreq(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (double.TryParse(s, out var d)) return d;
        if (s.Contains(' '))
        {
            var part = s.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (double.TryParse(part, out var d2)) return d2;
        }
        return null;
    }
}
