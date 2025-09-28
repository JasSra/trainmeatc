using PilotSim.Data.Models;

namespace PilotSim.Server.Services;

public interface IAirserviceChartsService
{
    string? GetVtcChartUrl(string icaoCode);
    string? GetTacChartUrl(string icaoCode);
    string? GetErrcChartUrl(string icaoCode);
    string? GetAipUrl(string icaoCode);
    void PopulateChartUrls(Airport airport);
    List<ChartInfo> GetAvailableChartsForAirport(string icaoCode);
}

public class ChartInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconClass { get; set; } = string.Empty;
}

public class AirserviceChartsService : IAirserviceChartsService
{
    // Base URLs for Airservices Australia charts
    private const string AipBaseUrl = "https://www.airservicesaustralia.com/aip/current/aip/";
    private const string ChartsBaseUrl = "https://www.airservicesaustralia.com/aip/current/dap/";
    
    // Common Australian airports with their chart availability
    private readonly Dictionary<string, AirportChartConfig> _airportChartConfigs = new()
    {
        // Major airports
        ["YSSY"] = new AirportChartConfig("Sydney", HasVtc: true, HasTac: true, HasAip: true),
        ["YMML"] = new AirportChartConfig("Melbourne", HasVtc: true, HasTac: true, HasAip: true),
        ["YBBN"] = new AirportChartConfig("Brisbane", HasVtc: true, HasTac: true, HasAip: true),
        ["YBCG"] = new AirportChartConfig("Gold Coast", HasVtc: true, HasTac: true, HasAip: true),
        ["YSCB"] = new AirportChartConfig("Canberra", HasVtc: true, HasTac: true, HasAip: true),
        ["YPAD"] = new AirportChartConfig("Adelaide", HasVtc: true, HasTac: true, HasAip: true),
        ["YPPH"] = new AirportChartConfig("Perth", HasVtc: true, HasTac: true, HasAip: true),
        ["YPDN"] = new AirportChartConfig("Darwin", HasVtc: true, HasTac: true, HasAip: true),
        ["YMHB"] = new AirportChartConfig("Hobart", HasVtc: true, HasTac: true, HasAip: true),
        ["YPJT"] = new AirportChartConfig("Jandakot", HasVtc: true, HasTac: false, HasAip: true),
        
        // Regional and GA airports
        ["YSBK"] = new AirportChartConfig("Bankstown", HasVtc: true, HasTac: false, HasAip: true),
        ["YMMB"] = new AirportChartConfig("Moorabbin", HasVtc: true, HasTac: false, HasAip: true),
        ["YSCN"] = new AirportChartConfig("Scone", HasVtc: false, HasTac: false, HasAip: true),
        ["YWOL"] = new AirportChartConfig("Wollongong", HasVtc: false, HasTac: false, HasAip: true),
        ["YMDG"] = new AirportChartConfig("Mudgee", HasVtc: false, HasTac: false, HasAip: true),
        ["YPPF"] = new AirportChartConfig("Para Fields", HasVtc: false, HasTac: false, HasAip: true),
        ["YBAF"] = new AirportChartConfig("Archerfield", HasVtc: true, HasTac: false, HasAip: true),
    };
    
    public string? GetVtcChartUrl(string icaoCode)
    {
        if (!_airportChartConfigs.TryGetValue(icaoCode, out var config) || !config.HasVtc)
            return null;
            
        // VTC charts typically follow this pattern for Airservices Australia
        return $"{ChartsBaseUrl}VTC{icaoCode}.PDF";
    }
    
    public string? GetTacChartUrl(string icaoCode)
    {
        if (!_airportChartConfigs.TryGetValue(icaoCode, out var config) || !config.HasTac)
            return null;
            
        // TAC charts typically follow this pattern
        return $"{ChartsBaseUrl}TAC{icaoCode}.PDF";
    }
    
    public string? GetErrcChartUrl(string icaoCode)
    {
        // ERRC charts cover larger areas and are not airport-specific
        // They are typically organized by regions
        return icaoCode switch
        {
            var code when code.StartsWith("YS") => $"{ChartsBaseUrl}ERRC1.PDF", // NSW/ACT
            var code when code.StartsWith("YM") => $"{ChartsBaseUrl}ERRC2.PDF", // VIC/TAS
            var code when code.StartsWith("YB") => $"{ChartsBaseUrl}ERRC3.PDF", // QLD
            var code when code.StartsWith("YP") => $"{ChartsBaseUrl}ERRC4.PDF", // SA/WA/NT
            _ => null
        };
    }
    
    public string? GetAipUrl(string icaoCode)
    {
        if (!_airportChartConfigs.TryGetValue(icaoCode, out var config) || !config.HasAip)
            return null;
            
        // AIP URLs typically follow this pattern
        return $"{AipBaseUrl}ad/ad2/{icaoCode}/";
    }
    
    public void PopulateChartUrls(Airport airport)
    {
        airport.VtcChartUrl = GetVtcChartUrl(airport.Icao);
        airport.TacChartUrl = GetTacChartUrl(airport.Icao);
        airport.ErrcChartUrl = GetErrcChartUrl(airport.Icao);
        airport.AipUrl = GetAipUrl(airport.Icao);
    }
    
    public List<ChartInfo> GetAvailableChartsForAirport(string icaoCode)
    {
        var charts = new List<ChartInfo>();
        
        if (!_airportChartConfigs.TryGetValue(icaoCode, out var config))
            return charts;
        
        if (config.HasVtc)
        {
            charts.Add(new ChartInfo
            {
                Name = "Visual Terminal Chart (VTC)",
                Type = "VTC",
                Url = GetVtcChartUrl(icaoCode) ?? "",
                Description = "Visual reference chart showing airport layout, taxiways, and terminal procedures",
                IconClass = "bi-eye"
            });
        }
        
        if (config.HasTac)
        {
            charts.Add(new ChartInfo
            {
                Name = "Terminal Area Chart (TAC)",
                Type = "TAC", 
                Url = GetTacChartUrl(icaoCode) ?? "",
                Description = "Terminal area procedures and approach/departure routes",
                IconClass = "bi-compass"
            });
        }
        
        var errcUrl = GetErrcChartUrl(icaoCode);
        if (!string.IsNullOrEmpty(errcUrl))
        {
            charts.Add(new ChartInfo
            {
                Name = "En Route Chart (ERC)",
                Type = "ERC",
                Url = errcUrl,
                Description = "High-level navigation chart for en route operations",
                IconClass = "bi-map"
            });
        }
        
        if (config.HasAip)
        {
            charts.Add(new ChartInfo
            {
                Name = "AIP Airport Data",
                Type = "AIP",
                Url = GetAipUrl(icaoCode) ?? "",
                Description = "Official aeronautical information publication for this airport",
                IconClass = "bi-file-text"
            });
        }
        
        return charts;
    }
    
    private record AirportChartConfig(string Name, bool HasVtc = false, bool HasTac = false, bool HasAip = false);
}