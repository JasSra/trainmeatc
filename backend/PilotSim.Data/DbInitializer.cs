using PilotSim.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace PilotSim.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(SimDbContext context)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Skip if data already exists
        if (await context.Airports.AnyAsync())
            return;

        // Add Australian airports with comprehensive data
        var airports = new List<Airport>
        {
            // Melbourne Airport
            new Airport
            {
                Icao = "YMML",
                Name = "Melbourne Airport",
                Lat = -37.6733,
                Lon = 144.8430,
                TowerFreq = "120.5",
                GroundFreq = "121.7",
                AppFreq = "132.0",
                AtisFreq = "126.25"
            },
            // Sydney Kingsford Smith Airport
            new Airport
            {
                Icao = "YSSY",
                Name = "Sydney Kingsford Smith Airport",
                Lat = -33.9399,
                Lon = 151.1753,
                TowerFreq = "120.5",
                GroundFreq = "121.7",
                AppFreq = "124.4",
                AtisFreq = "126.25"
            },
            // Brisbane Airport
            new Airport
            {
                Icao = "YBBN",
                Name = "Brisbane Airport",
                Lat = -27.3842,
                Lon = 153.1175,
                TowerFreq = "120.9",
                GroundFreq = "121.9",
                AppFreq = "125.6",
                AtisFreq = "118.95"
            },
            // Perth Airport
            new Airport
            {
                Icao = "YPPH",
                Name = "Perth Airport",
                Lat = -31.9403,
                Lon = 115.9669,
                TowerFreq = "120.5",
                GroundFreq = "121.7",
                AppFreq = "123.6",
                AtisFreq = "128.25"
            },
            // Adelaide Airport
            new Airport
            {
                Icao = "YPAD",
                Name = "Adelaide Airport",
                Lat = -34.9462,
                Lon = 138.5306,
                TowerFreq = "120.1",
                GroundFreq = "121.9",
                AppFreq = "126.1",
                AtisFreq = "127.85"
            },
            // Gold Coast Airport
            new Airport
            {
                Icao = "YBCG",
                Name = "Gold Coast Airport",
                Lat = -28.1644,
                Lon = 153.5053,
                TowerFreq = "118.7",
                GroundFreq = "121.7",
                AppFreq = "125.6",
                AtisFreq = "119.85"
            },
            // Canberra Airport
            new Airport
            {
                Icao = "YSCB",
                Name = "Canberra Airport",
                Lat = -35.3069,
                Lon = 149.1950,
                TowerFreq = "120.3",
                GroundFreq = "121.8",
                AppFreq = "125.2",
                AtisFreq = "134.1"
            },
            // Cairns Airport
            new Airport
            {
                Icao = "YBCS",
                Name = "Cairns Airport",
                Lat = -16.8858,
                Lon = 145.7781,
                TowerFreq = "118.2",
                GroundFreq = "121.9",
                AppFreq = "125.5",
                AtisFreq = "127.4"
            },
            // Darwin Airport
            new Airport
            {
                Icao = "YPDN",
                Name = "Darwin Airport",
                Lat = -12.4147,
                Lon = 130.8767,
                TowerFreq = "118.1",
                GroundFreq = "121.9",
                AppFreq = "125.8",
                AtisFreq = "132.3"
            },
            // Hobart Airport
            new Airport
            {
                Icao = "YMHB",
                Name = "Hobart Airport",
                Lat = -42.8361,
                Lon = 147.5103,
                TowerFreq = "118.7",
                GroundFreq = "121.9",
                AppFreq = "125.35",
                AtisFreq = "134.5"
            }
        };

        context.Airports.AddRange(airports);

        // Add runways for each airport
        var runways = new List<Runway>
        {
            // YMML - Melbourne
            new Runway { AirportIcao = "YMML", Ident = "16/34", MagneticHeading = 160, LengthM = 3658, Ils = true },
            new Runway { AirportIcao = "YMML", Ident = "09/27", MagneticHeading = 90, LengthM = 2286, Ils = true },
            
            // YSSY - Sydney
            new Runway { AirportIcao = "YSSY", Ident = "16L/34R", MagneticHeading = 160, LengthM = 4400, Ils = true },
            new Runway { AirportIcao = "YSSY", Ident = "16R/34L", MagneticHeading = 160, LengthM = 2530, Ils = true },
            new Runway { AirportIcao = "YSSY", Ident = "07/25", MagneticHeading = 70, LengthM = 2438, Ils = false },
            
            // YBBN - Brisbane
            new Runway { AirportIcao = "YBBN", Ident = "01/19", MagneticHeading = 10, LengthM = 3560, Ils = true },
            new Runway { AirportIcao = "YBBN", Ident = "14/32", MagneticHeading = 140, LengthM = 1700, Ils = false },
            
            // YPPH - Perth
            new Runway { AirportIcao = "YPPH", Ident = "03/21", MagneticHeading = 30, LengthM = 3444, Ils = true },
            new Runway { AirportIcao = "YPPH", Ident = "06/24", MagneticHeading = 60, LengthM = 2164, Ils = true },
            
            // YPAD - Adelaide
            new Runway { AirportIcao = "YPAD", Ident = "05/23", MagneticHeading = 50, LengthM = 3142, Ils = true },
            new Runway { AirportIcao = "YPAD", Ident = "12/30", MagneticHeading = 120, LengthM = 1533, Ils = false },
            
            // YBCG - Gold Coast
            new Runway { AirportIcao = "YBCG", Ident = "14/32", MagneticHeading = 140, LengthM = 2500, Ils = true },
            
            // YSCB - Canberra
            new Runway { AirportIcao = "YSCB", Ident = "17/35", MagneticHeading = 170, LengthM = 3023, Ils = true },
            
            // YBCS - Cairns
            new Runway { AirportIcao = "YBCS", Ident = "15/33", MagneticHeading = 150, LengthM = 3192, Ils = true },
            
            // YPDN - Darwin
            new Runway { AirportIcao = "YPDN", Ident = "11/29", MagneticHeading = 110, LengthM = 3354, Ils = true },
            new Runway { AirportIcao = "YPDN", Ident = "18/36", MagneticHeading = 180, LengthM = 1829, Ils = false },
            
            // YMHB - Hobart
            new Runway { AirportIcao = "YMHB", Ident = "12/30", MagneticHeading = 120, LengthM = 2270, Ils = true }
        };

        context.Runways.AddRange(runways);

        // Add aircraft profiles for GA, Medium, and Heavy aircraft
        var aircraft = new List<Aircraft>
        {
            // General Aviation Aircraft
            new Aircraft 
            { 
                Type = "C172", Manufacturer = "Cessna", Category = "GA", CallsignPrefix = "VH-",
                CruiseSpeed = 122, ServiceCeiling = 14000, WakeCategory = "Light", EngineType = "Piston", SeatCapacity = 4
            },
            new Aircraft 
            { 
                Type = "C182", Manufacturer = "Cessna", Category = "GA", CallsignPrefix = "VH-",
                CruiseSpeed = 145, ServiceCeiling = 18000, WakeCategory = "Light", EngineType = "Piston", SeatCapacity = 4
            },
            new Aircraft 
            { 
                Type = "C210", Manufacturer = "Cessna", Category = "GA", CallsignPrefix = "VH-",
                CruiseSpeed = 174, ServiceCeiling = 27000, WakeCategory = "Light", EngineType = "Piston", SeatCapacity = 6
            },
            new Aircraft 
            { 
                Type = "PA28", Manufacturer = "Piper", Category = "GA", CallsignPrefix = "VH-",
                CruiseSpeed = 115, ServiceCeiling = 14000, WakeCategory = "Light", EngineType = "Piston", SeatCapacity = 4
            },
            new Aircraft 
            { 
                Type = "DA40", Manufacturer = "Diamond", Category = "GA", CallsignPrefix = "VH-",
                CruiseSpeed = 142, ServiceCeiling = 16400, WakeCategory = "Light", EngineType = "Piston", SeatCapacity = 4
            },

            // Medium Aircraft
            new Aircraft 
            { 
                Type = "DHC8", Manufacturer = "De Havilland Canada", Category = "Medium", CallsignPrefix = "VH-",
                CruiseSpeed = 360, ServiceCeiling = 25000, WakeCategory = "Medium", EngineType = "Turboprop", SeatCapacity = 78
            },
            new Aircraft 
            { 
                Type = "AT72", Manufacturer = "ATR", Category = "Medium", CallsignPrefix = "VH-",
                CruiseSpeed = 276, ServiceCeiling = 25000, WakeCategory = "Medium", EngineType = "Turboprop", SeatCapacity = 72
            },
            new Aircraft 
            { 
                Type = "E190", Manufacturer = "Embraer", Category = "Medium", CallsignPrefix = "VH-",
                CruiseSpeed = 470, ServiceCeiling = 41000, WakeCategory = "Medium", EngineType = "Jet", SeatCapacity = 114
            },
            new Aircraft 
            { 
                Type = "B737", Manufacturer = "Boeing", Category = "Medium", CallsignPrefix = "VH-",
                CruiseSpeed = 453, ServiceCeiling = 41000, WakeCategory = "Medium", EngineType = "Jet", SeatCapacity = 189
            },
            new Aircraft 
            { 
                Type = "A320", Manufacturer = "Airbus", Category = "Medium", CallsignPrefix = "VH-",
                CruiseSpeed = 447, ServiceCeiling = 39000, WakeCategory = "Medium", EngineType = "Jet", SeatCapacity = 180
            },

            // Heavy Aircraft
            new Aircraft 
            { 
                Type = "B777", Manufacturer = "Boeing", Category = "Heavy", CallsignPrefix = "VH-",
                CruiseSpeed = 490, ServiceCeiling = 43100, WakeCategory = "Heavy", EngineType = "Jet", SeatCapacity = 396
            },
            new Aircraft 
            { 
                Type = "B787", Manufacturer = "Boeing", Category = "Heavy", CallsignPrefix = "VH-",
                CruiseSpeed = 488, ServiceCeiling = 43000, WakeCategory = "Heavy", EngineType = "Jet", SeatCapacity = 330
            },
            new Aircraft 
            { 
                Type = "A330", Manufacturer = "Airbus", Category = "Heavy", CallsignPrefix = "VH-",
                CruiseSpeed = 473, ServiceCeiling = 41000, WakeCategory = "Heavy", EngineType = "Jet", SeatCapacity = 335
            },
            new Aircraft 
            { 
                Type = "A380", Manufacturer = "Airbus", Category = "Heavy", CallsignPrefix = "VH-",
                CruiseSpeed = 488, ServiceCeiling = 43000, WakeCategory = "Super", EngineType = "Jet", SeatCapacity = 853
            },
            new Aircraft 
            { 
                Type = "B747", Manufacturer = "Boeing", Category = "Heavy", CallsignPrefix = "VH-",
                CruiseSpeed = 490, ServiceCeiling = 43100, WakeCategory = "Heavy", EngineType = "Jet", SeatCapacity = 660
            }
        };

        context.Aircraft.AddRange(aircraft);
        await context.SaveChangesAsync();

        // Add traffic profiles for realistic scenarios
        var trafficProfiles = new List<TrafficProfile>();
        var aircraftInDb = await context.Aircraft.ToListAsync();
        var airportsInDb = await context.Airports.ToListAsync();

        // Add traffic profiles for each major airport
        foreach (var airport in airportsInDb)
        {
            switch (airport.Icao)
            {
                case "YMML": // Melbourne
                    AddMelbourneTraffic(trafficProfiles, aircraftInDb, airport.Icao);
                    break;
                case "YSSY": // Sydney
                    AddSydneyTraffic(trafficProfiles, aircraftInDb, airport.Icao);
                    break;
                case "YBBN": // Brisbane
                    AddBrisbaneTraffic(trafficProfiles, aircraftInDb, airport.Icao);
                    break;
                case "YPPH": // Perth
                    AddPerthTraffic(trafficProfiles, aircraftInDb, airport.Icao);
                    break;
                default:
                    AddGenericTraffic(trafficProfiles, aircraftInDb, airport.Icao);
                    break;
            }
        }

        context.TrafficProfiles.AddRange(trafficProfiles);

        // Add comprehensive scenarios for different airports and aircraft types
        var scenarios = new List<Scenario>
        {
            // Melbourne scenarios
            new Scenario
            {
                Name = "Basic Taxi Request",
                AirportIcao = "YMML",
                Kind = "taxi",
                Difficulty = "Basic",
                Seed = 12345,
                InitialStateJson = "{\"position\":\"gate\",\"aircraft\":\"VH-ABC\",\"type\":\"C172\"}",
                RubricJson = "{\"taxi_clearance\":10,\"readback_accuracy\":5}"
            },
            new Scenario
            {
                Name = "IFR Departure Melbourne",
                AirportIcao = "YMML",
                Kind = "departure",
                Difficulty = "Intermediate",
                Seed = 12346,
                InitialStateJson = "{\"position\":\"gate\",\"aircraft\":\"VH-JQA\",\"type\":\"A320\",\"destination\":\"YSSY\"}",
                RubricJson = "{\"clearance_readback\":15,\"taxi_compliance\":10,\"departure_execution\":20}"
            },
            new Scenario
            {
                Name = "Heavy Aircraft Arrival",
                AirportIcao = "YMML",
                Kind = "arrival",
                Difficulty = "Advanced",
                Seed = 12347,
                InitialStateJson = "{\"position\":\"final\",\"aircraft\":\"VH-OQA\",\"type\":\"A380\",\"origin\":\"EGLL\"}",
                RubricJson = "{\"approach_compliance\":20,\"landing_clearance\":15,\"taxi_after_landing\":10}"
            },

            // Sydney scenarios
            new Scenario
            {
                Name = "Parallel Runway Operations",
                AirportIcao = "YSSY",
                Kind = "arrival",
                Difficulty = "Advanced",
                Seed = 22345,
                InitialStateJson = "{\"position\":\"approach\",\"aircraft\":\"VH-BBA\",\"type\":\"B737\",\"runway\":\"16L\"}",
                RubricJson = "{\"runway_assignment\":15,\"spacing_compliance\":20,\"go_around_procedures\":25}"
            },

            // Brisbane scenarios
            new Scenario
            {
                Name = "Tropical Weather Operations",
                AirportIcao = "YBBN",
                Kind = "departure",
                Difficulty = "Intermediate",
                Seed = 32345,
                InitialStateJson = "{\"position\":\"gate\",\"aircraft\":\"VH-YQS\",\"type\":\"DHC8\",\"weather\":\"thunderstorms\"}",
                RubricJson = "{\"weather_assessment\":20,\"alternate_procedures\":15,\"safety_considerations\":25}"
            },

            // Training scenarios for GA aircraft
            new Scenario
            {
                Name = "Student Solo Flight",
                AirportIcao = "YPAD",
                Kind = "pattern",
                Difficulty = "Basic",
                Seed = 42345,
                InitialStateJson = "{\"position\":\"downwind\",\"aircraft\":\"VH-CTF\",\"type\":\"C172\",\"training\":true}",
                RubricJson = "{\"pattern_compliance\":15,\"radio_discipline\":10,\"safety_awareness\":20}"
            }
        };

        context.Scenarios.AddRange(scenarios);
        await context.SaveChangesAsync();
    }

    private static void AddMelbourneTraffic(List<TrafficProfile> profiles, List<Aircraft> aircraft, string icao)
    {
        // Jetstar callsigns
        var jetstarAircraft = aircraft.Where(a => a.Type == "A320" || a.Type == "B787").ToList();
        foreach (var ac in jetstarAircraft)
        {
            profiles.Add(new TrafficProfile 
            { 
                AircraftId = ac.Id, AirportIcao = icao, Callsign = "JST123", 
                FlightType = "Commercial", Route = "MEL-SYD", FrequencyWeight = 2.0 
            });
        }
        
        // Virgin Australia
        var virginAircraft = aircraft.Where(a => a.Type == "B737" || a.Type == "B777").ToList();
        foreach (var ac in virginAircraft)
        {
            profiles.Add(new TrafficProfile 
            { 
                AircraftId = ac.Id, AirportIcao = icao, Callsign = "VOZ456", 
                FlightType = "Commercial", Route = "MEL-PER", FrequencyWeight = 2.0 
            });
        }

        // GA traffic
        var gaAircraft = aircraft.Where(a => a.Category == "GA").ToList();
        foreach (var ac in gaAircraft)
        {
            profiles.Add(new TrafficProfile 
            { 
                AircraftId = ac.Id, AirportIcao = icao, Callsign = $"VH-{GenerateRandomCallsign()}", 
                FlightType = "Training", Route = "Local", FrequencyWeight = 1.0 
            });
        }
    }

    private static void AddSydneyTraffic(List<TrafficProfile> profiles, List<Aircraft> aircraft, string icao)
    {
        // Qantas mainline
        var qantasHeavy = aircraft.Where(a => a.Type == "A380" || a.Type == "B747").ToList();
        foreach (var ac in qantasHeavy)
        {
            profiles.Add(new TrafficProfile 
            { 
                AircraftId = ac.Id, AirportIcao = icao, Callsign = "QFA1", 
                FlightType = "Commercial", Route = "SYD-LAX", FrequencyWeight = 1.5 
            });
        }

        // Rex Airlines (Regional Express)
        var rexAircraft = aircraft.FirstOrDefault(a => a.Type == "DHC8");
        if (rexAircraft != null)
        {
            profiles.Add(new TrafficProfile 
            { 
                AircraftId = rexAircraft.Id, AirportIcao = icao, Callsign = "RXA234", 
                FlightType = "Commercial", Route = "SYD-ABX", FrequencyWeight = 1.0 
            });
        }
    }

    private static void AddBrisbaneTraffic(List<TrafficProfile> profiles, List<Aircraft> aircraft, string icao)
    {
        // Alliance Airlines
        var allianceAircraft = aircraft.FirstOrDefault(a => a.Type == "E190");
        if (allianceAircraft != null)
        {
            profiles.Add(new TrafficProfile 
            { 
                AircraftId = allianceAircraft.Id, AirportIcao = icao, Callsign = "UTY567", 
                FlightType = "Charter", Route = "BNE-Mining Sites", FrequencyWeight = 1.2 
            });
        }
    }

    private static void AddPerthTraffic(List<TrafficProfile> profiles, List<Aircraft> aircraft, string icao)
    {
        // International traffic
        var internationalAircraft = aircraft.Where(a => a.Type == "B787" || a.Type == "A330").ToList();
        foreach (var ac in internationalAircraft)
        {
            profiles.Add(new TrafficProfile 
            { 
                AircraftId = ac.Id, AirportIcao = icao, Callsign = "QFA9", 
                FlightType = "Commercial", Route = "PER-LHR", FrequencyWeight = 0.8 
            });
        }
    }

    private static void AddGenericTraffic(List<TrafficProfile> profiles, List<Aircraft> aircraft, string icao)
    {
        // Add basic GA and regional traffic for smaller airports
        var gaAircraft = aircraft.Where(a => a.Category == "GA").Take(3).ToList();
        foreach (var ac in gaAircraft)
        {
            profiles.Add(new TrafficProfile 
            { 
                AircraftId = ac.Id, AirportIcao = icao, Callsign = $"VH-{GenerateRandomCallsign()}", 
                FlightType = "Private", Route = "Local", FrequencyWeight = 1.0 
            });
        }

        var regionalAircraft = aircraft.FirstOrDefault(a => a.Type == "DHC8" || a.Type == "AT72");
        if (regionalAircraft != null)
        {
            profiles.Add(new TrafficProfile 
            { 
                AircraftId = regionalAircraft.Id, AirportIcao = icao, Callsign = "REX789", 
                FlightType = "Commercial", Route = "Regional", FrequencyWeight = 0.7 
            });
        }
    }

    private static string GenerateRandomCallsign()
    {
        var letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var random = new Random();
        return new string(Enumerable.Repeat(letters, 3)
            .Select(s => s[random.Next(letters.Length)]).ToArray());
    }
}