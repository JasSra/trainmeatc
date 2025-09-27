using Microsoft.EntityFrameworkCore;
using PilotSim.Data;

namespace PilotSim.Server.Services;

public interface IMetarService
{
    Task<MetarData?> GetMetarAsync(string airportIcao, CancellationToken cancellationToken = default);
    Task<List<MetarData>> GetRecentMetarsAsync(string airportIcao, int hours = 24, CancellationToken cancellationToken = default);
}

public record MetarData(
    string AirportIcao,
    DateTime ObservationTime,
    string RawMetar,
    WeatherConditions Conditions,
    WindData Wind,
    VisibilityData Visibility,
    CloudData[] Clouds,
    TemperatureData Temperature,
    PressureData Pressure
);

public record WeatherConditions(
    string Description,
    string[] Phenomena,
    bool IsInstrumental
);

public record WindData(
    int Direction,
    int Speed,
    int? Gusts,
    string Unit
);

public record VisibilityData(
    double Distance,
    string Unit,
    string Condition
);

public record CloudData(
    string Coverage,
    int? Altitude,
    string Type
);

public record TemperatureData(
    int Temperature,
    int DewPoint,
    string Unit
);

public record PressureData(
    double Value,
    string Unit,
    double InHg
);

public class MetarService : IMetarService
{
    private readonly SimDbContext _context;
    private readonly ICachingService _cachingService;
    private readonly ILogger<MetarService> _logger;
    private readonly Random _random = new();

    // Australian airport weather patterns
    private readonly Dictionary<string, string[]> _airportWeatherPatterns = new()
    {
        ["YMML"] = new[] { "Clear", "Few clouds", "Scattered clouds", "Light rain", "Overcast" },
        ["YSSY"] = new[] { "Clear", "Scattered clouds", "Broken clouds", "Light rain", "Fog" },
        ["YBBN"] = new[] { "Clear", "Few clouds", "Thunderstorms", "Heavy rain", "Haze" },
        ["YPAD"] = new[] { "Clear", "Few clouds", "Scattered clouds", "Light winds", "Calm" },
        ["YMMB"] = new[] { "Clear", "Few clouds", "Overcast", "Light rain", "Gusty winds" }
    };

    public MetarService(
        SimDbContext context,
        ICachingService cachingService,
        ILogger<MetarService> logger)
    {
        _context = context;
        _cachingService = cachingService;
        _logger = logger;
    }

    public async Task<MetarData?> GetMetarAsync(string airportIcao, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"metar-{airportIcao}";
        
        // Check cache first (METAR data is cached for 30 minutes)
        var cachedMetar = await _cachingService.GetAsync<MetarData>(cacheKey, cancellationToken);
        if (cachedMetar != null)
        {
            return cachedMetar;
        }

        // Check if airport exists
        var airport = await _context.Airports
            .FirstOrDefaultAsync(a => a.Icao == airportIcao, cancellationToken);

        if (airport == null)
        {
            _logger.LogWarning("Airport {AirportIcao} not found", airportIcao);
            return null;
        }

        // Generate realistic METAR data (stub implementation)
        var metar = GenerateRealisticMetar(airportIcao);
        
        // Cache for 30 minutes (typical METAR update frequency)
        await _cachingService.SetAsync(cacheKey, metar, TimeSpan.FromMinutes(30), cancellationToken);

        _logger.LogDebug("Generated METAR for {AirportIcao}", airportIcao);
        
        return metar;
    }

    public async Task<List<MetarData>> GetRecentMetarsAsync(string airportIcao, int hours = 24, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"metar-history-{airportIcao}-{hours}h";
        
        var cachedHistory = await _cachingService.GetAsync<List<MetarData>>(cacheKey, cancellationToken);
        if (cachedHistory != null)
        {
            return cachedHistory;
        }

        // Generate historical METAR data
        var metars = new List<MetarData>();
        var now = DateTime.UtcNow;
        
        for (int i = 0; i < hours; i++)
        {
            var observationTime = now.AddHours(-i);
            var metar = GenerateRealisticMetar(airportIcao, observationTime);
            metars.Add(metar);
        }

        // Cache for 15 minutes
        await _cachingService.SetAsync(cacheKey, metars, TimeSpan.FromMinutes(15), cancellationToken);

        return metars;
    }

    private MetarData GenerateRealisticMetar(string airportIcao, DateTime? observationTime = null)
    {
        var obsTime = observationTime ?? DateTime.UtcNow;
        
        // Get weather patterns for this airport
        var patterns = _airportWeatherPatterns.GetValueOrDefault(airportIcao, _airportWeatherPatterns["YMML"]);
        var weatherDesc = patterns[_random.Next(patterns.Length)];

        // Generate realistic wind data
        var windDir = _random.Next(0, 360);
        var windSpeed = _random.Next(0, 25);
        var windGusts = windSpeed > 15 ? windSpeed + _random.Next(5, 15) : (int?)null;

        // Generate visibility
        var visibility = weatherDesc.Contains("Fog") ? _random.Next(1, 3) : 
                        weatherDesc.Contains("Rain") ? _random.Next(3, 8) : 
                        _random.Next(8, 15);

        // Generate clouds
        var clouds = GenerateClouds(weatherDesc);

        // Generate temperature and pressure
        var temp = _random.Next(5, 35);
        var dewPoint = temp - _random.Next(1, 15);
        var pressure = 1013.25 + _random.Next(-20, 21);

        // Generate raw METAR string
        var rawMetar = $"{airportIcao} {obsTime:ddHHmm}Z {windDir:000}{windSpeed:00}KT " +
                      $"{visibility:00}SM {GenerateCloudString(clouds)} " +
                      $"{temp:00}/{dewPoint:00} A{pressure * 0.02953:0000}";

        return new MetarData(
            airportIcao,
            obsTime,
            rawMetar,
            new WeatherConditions(
                weatherDesc,
                weatherDesc.Contains("Rain") ? new[] { "RA" } : 
                weatherDesc.Contains("Fog") ? new[] { "FG" } : 
                Array.Empty<string>(),
                visibility < 3 || weatherDesc.Contains("Overcast")
            ),
            new WindData(windDir, windSpeed, windGusts, "KT"),
            new VisibilityData(visibility, "SM", visibility < 3 ? "Poor" : visibility < 6 ? "Moderate" : "Good"),
            clouds,
            new TemperatureData(temp, dewPoint, "C"),
            new PressureData(pressure, "hPa", pressure * 0.02953)
        );
    }

    private CloudData[] GenerateClouds(string weatherDesc)
    {
        return weatherDesc switch
        {
            "Clear" => Array.Empty<CloudData>(),
            "Few clouds" => new[] { new CloudData("FEW", _random.Next(1500, 3000), "CU") },
            "Scattered clouds" => new[] 
            { 
                new CloudData("SCT", _random.Next(1000, 2000), "CU"),
                new CloudData("FEW", _random.Next(3000, 5000), "AC")
            },
            "Broken clouds" => new[] { new CloudData("BKN", _random.Next(800, 1500), "SC") },
            "Overcast" => new[] { new CloudData("OVC", _random.Next(500, 1000), "ST") },
            _ => new[] { new CloudData("SCT", _random.Next(1500, 2500), "CU") }
        };
    }

    private string GenerateCloudString(CloudData[] clouds)
    {
        if (!clouds.Any()) return "CLR";
        
        return string.Join(" ", clouds.Select(c => 
            c.Altitude.HasValue ? $"{c.Coverage}{c.Altitude.Value / 100:000}" : c.Coverage));
    }
}