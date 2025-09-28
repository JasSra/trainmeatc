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
                AtisFreq = "126.25",
                Category = "Major",
                ElevationFt = 434,
                OperatingHours = "H24",
                HasFuel = true,
                HasMaintenance = true,
                FuelTypes = "100LL, JetA1"
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
                AtisFreq = "126.25",
                Category = "Major",
                ElevationFt = 21,
                OperatingHours = "H24",
                HasFuel = true,
                HasMaintenance = true,
                FuelTypes = "100LL, JetA1"
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
                AtisFreq = "118.95",
                Category = "Major",
                ElevationFt = 13,
                OperatingHours = "H24",
                HasFuel = true,
                HasMaintenance = true,
                FuelTypes = "100LL, JetA1"
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
                AtisFreq = "128.25",
                Category = "Major",
                ElevationFt = 67,
                OperatingHours = "H24",
                HasFuel = true,
                HasMaintenance = true,
                FuelTypes = "100LL, JetA1"
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
                AtisFreq = "127.85",
                Category = "Major",
                ElevationFt = 20,
                OperatingHours = "H24",
                HasFuel = true,
                HasMaintenance = true,
                FuelTypes = "100LL, JetA1"
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
                AtisFreq = "119.85",
                Category = "Regional",
                ElevationFt = 21,
                OperatingHours = "H24",
                HasFuel = true,
                HasMaintenance = true,
                FuelTypes = "100LL, JetA1"
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
                AtisFreq = "134.1",
                Category = "Regional",
                ElevationFt = 1886,
                OperatingHours = "H24",
                HasFuel = true,
                HasMaintenance = true,
                FuelTypes = "100LL, JetA1"
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
                AtisFreq = "127.4",
                Category = "Regional",
                ElevationFt = 10,
                OperatingHours = "H24",
                HasFuel = true,
                HasMaintenance = true,
                FuelTypes = "100LL, JetA1"
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
                AtisFreq = "132.3",
                Category = "Regional",
                ElevationFt = 103,
                OperatingHours = "H24",
                HasFuel = true,
                HasMaintenance = true,
                FuelTypes = "100LL, JetA1"
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
                AtisFreq = "134.5",
                Category = "Regional",
                ElevationFt = 13,
                OperatingHours = "H24",
                HasFuel = true,
                HasMaintenance = true,
                FuelTypes = "100LL, JetA1"
            },

            // GA Airports
            // Moorabbin Airport - Major GA hub in Melbourne
            new Airport
            {
                Icao = "YMMB",
                Name = "Moorabbin Airport",
                Lat = -37.9750,
                Lon = 145.1022,
                TowerFreq = "118.1",
                GroundFreq = "121.9",
                AppFreq = "125.2",
                AtisFreq = "134.7",
                Category = "GA",
                ElevationFt = 50,
                OperatingHours = "0530-2200 LT",
                HasFuel = true,
                HasMaintenance = true,
                FuelTypes = "100LL, JetA1"
            },
            // Bankstown Airport - Major GA hub in Sydney
            new Airport
            {
                Icao = "YSBK",
                Name = "Bankstown Airport",
                Lat = -33.9239,
                Lon = 150.9881,
                TowerFreq = "132.35",
                GroundFreq = "121.9",
                AppFreq = "125.2",
                AtisFreq = "127.45",
                Category = "GA",
                ElevationFt = 73,
                OperatingHours = "0630-2200 LT",
                HasFuel = true,
                HasMaintenance = true,
                FuelTypes = "100LL, JetA1"
            },
            // Archerfield Airport - GA hub in Brisbane
            new Airport
            {
                Icao = "YBAF",
                Name = "Archerfield Airport",
                Lat = -27.5703,
                Lon = 153.0078,
                TowerFreq = "118.3",
                GroundFreq = "121.9",
                AppFreq = "125.6",
                AtisFreq = "127.25",
                Category = "GA",
                ElevationFt = 63,
                OperatingHours = "0530-2200 LT",
                HasFuel = true,
                HasMaintenance = true,
                FuelTypes = "100LL, JetA1"
            },
            // Jandakot Airport - GA hub in Perth
            new Airport
            {
                Icao = "YPJT",
                Name = "Jandakot Airport",
                Lat = -32.0975,
                Lon = 115.8811,
                TowerFreq = "120.1",
                GroundFreq = "121.9",
                AppFreq = "123.6",
                AtisFreq = "119.7",
                Category = "GA",
                ElevationFt = 99,
                OperatingHours = "0530-2200 LT",
                HasFuel = true,
                HasMaintenance = true,
                FuelTypes = "100LL, JetA1"
            },
            // Camden Airport - Historic GA field near Sydney
            new Airport
            {
                Icao = "YSCN",
                Name = "Camden Airport",
                Lat = -34.0406,
                Lon = 150.6878,
                TowerFreq = "126.7",
                GroundFreq = "121.9",
                AppFreq = "125.2",
                Category = "GA",
                ElevationFt = 230,
                OperatingHours = "0600-2100 LT",
                HasFuel = true,
                HasMaintenance = false,
                FuelTypes = "100LL"
            },
            // Parafield Airport - GA hub in Adelaide
            new Airport
            {
                Icao = "YPPF",
                Name = "Parafield Airport",
                Lat = -34.7939,
                Lon = 138.6331,
                TowerFreq = "126.1",
                GroundFreq = "121.9",
                AppFreq = "126.1",
                Category = "GA",
                ElevationFt = 57,
                OperatingHours = "0600-2200 LT",
                HasFuel = true,
                HasMaintenance = true,
                FuelTypes = "100LL, JetA1"
            },
            // Mudgee Airport - Regional uncontrolled
            new Airport
            {
                Icao = "YMDG",
                Name = "Mudgee Airport",
                Lat = -32.5625,
                Lon = 149.6092,
                TowerFreq = "CTAF 126.7",
                GroundFreq = "CTAF 126.7",
                AppFreq = "CTAF 126.7",
                Category = "Regional",
                ElevationFt = 1549,
                OperatingHours = "H24",
                HasFuel = true,
                HasMaintenance = false,
                FuelTypes = "100LL"
            },
            // Wollongong Airport - Regional
            new Airport
            {
                Icao = "YWOL",
                Name = "Wollongong Airport",
                Lat = -34.5606,
                Lon = 150.7894,
                TowerFreq = "CTAF 119.6",
                GroundFreq = "CTAF 119.6", 
                AppFreq = "CTAF 119.6",
                Category = "Regional",
                ElevationFt = 31,
                OperatingHours = "0600-2200 LT",
                HasFuel = true,
                HasMaintenance = true,
                FuelTypes = "100LL, JetA1"
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
            new Runway { AirportIcao = "YMHB", Ident = "12/30", MagneticHeading = 120, LengthM = 2270, Ils = true },

            // GA Airports
            // YMMB - Moorabbin
            new Runway { AirportIcao = "YMMB", Ident = "13L/31R", MagneticHeading = 130, LengthM = 1546, Ils = false },
            new Runway { AirportIcao = "YMMB", Ident = "13R/31L", MagneticHeading = 130, LengthM = 1070, Ils = false },
            new Runway { AirportIcao = "YMMB", Ident = "17L/35R", MagneticHeading = 170, LengthM = 1070, Ils = false },
            new Runway { AirportIcao = "YMMB", Ident = "17R/35L", MagneticHeading = 170, LengthM = 820, Ils = false },

            // YSBK - Bankstown
            new Runway { AirportIcao = "YSBK", Ident = "11C/29C", MagneticHeading = 110, LengthM = 1372, Ils = false },
            new Runway { AirportIcao = "YSBK", Ident = "11L/29R", MagneticHeading = 110, LengthM = 1580, Ils = false },
            new Runway { AirportIcao = "YSBK", Ident = "11R/29L", MagneticHeading = 110, LengthM = 792, Ils = false },

            // YBAF - Archerfield
            new Runway { AirportIcao = "YBAF", Ident = "10L/28R", MagneticHeading = 100, LengthM = 1833, Ils = false },
            new Runway { AirportIcao = "YBAF", Ident = "10R/28L", MagneticHeading = 100, LengthM = 1170, Ils = false },
            new Runway { AirportIcao = "YBAF", Ident = "04/22", MagneticHeading = 40, LengthM = 914, Ils = false },

            // YPJT - Jandakot
            new Runway { AirportIcao = "YPJT", Ident = "06L/24R", MagneticHeading = 60, LengthM = 1400, Ils = false },
            new Runway { AirportIcao = "YPJT", Ident = "06R/24L", MagneticHeading = 60, LengthM = 1199, Ils = false },
            new Runway { AirportIcao = "YPJT", Ident = "12/30", MagneticHeading = 120, LengthM = 1070, Ils = false },

            // YSCN - Camden
            new Runway { AirportIcao = "YSCN", Ident = "06/24", MagneticHeading = 60, LengthM = 1387, Ils = false },
            new Runway { AirportIcao = "YSCN", Ident = "10/28", MagneticHeading = 100, LengthM = 838, Ils = false },

            // YPPF - Parafield
            new Runway { AirportIcao = "YPPF", Ident = "03/21", MagneticHeading = 30, LengthM = 1342, Ils = false },
            new Runway { AirportIcao = "YPPF", Ident = "08/26", MagneticHeading = 80, LengthM = 1070, Ils = false },
            
            // YMDG - Mudgee
            new Runway { AirportIcao = "YMDG", Ident = "10/28", MagneticHeading = 100, LengthM = 1200, Ils = false },
            
            // YWOL - Wollongong
            new Runway { AirportIcao = "YWOL", Ident = "16/34", MagneticHeading = 160, LengthM = 1560, Ils = false }
        };

        context.Runways.AddRange(runways);

        // Add aircraft profiles for GA, Medium, and Heavy aircraft
        var aircraft = new List<Aircraft>
        {
            // General Aviation Aircraft
            new Aircraft 
            { 
                Type = "C172", Manufacturer = "Cessna", Category = "GA", CallsignPrefix = "VH-",
                CruiseSpeed = 122, ServiceCeiling = 14000, WakeCategory = "Light", EngineType = "Piston", SeatCapacity = 4,
                MsfsTitle = "Cessna 172 Skyhawk", MsfsModelMatchCode = "C172", SupportsSimConnect = true
            },
            new Aircraft 
            { 
                Type = "C182", Manufacturer = "Cessna", Category = "GA", CallsignPrefix = "VH-",
                CruiseSpeed = 145, ServiceCeiling = 18000, WakeCategory = "Light", EngineType = "Piston", SeatCapacity = 4,
                MsfsTitle = "Cessna 182 Skylane", MsfsModelMatchCode = "C182", SupportsSimConnect = true
            },
            new Aircraft 
            { 
                Type = "C210", Manufacturer = "Cessna", Category = "GA", CallsignPrefix = "VH-",
                CruiseSpeed = 174, ServiceCeiling = 27000, WakeCategory = "Light", EngineType = "Piston", SeatCapacity = 6,
                MsfsTitle = "Cessna 210 Centurion", MsfsModelMatchCode = "C210", SupportsSimConnect = false
            },
            new Aircraft 
            { 
                Type = "PA28", Manufacturer = "Piper", Category = "GA", CallsignPrefix = "VH-",
                CruiseSpeed = 115, ServiceCeiling = 14000, WakeCategory = "Light", EngineType = "Piston", SeatCapacity = 4,
                MsfsTitle = "Piper PA-28 Cherokee", MsfsModelMatchCode = "PA28", SupportsSimConnect = true
            },
            new Aircraft 
            { 
                Type = "DA40", Manufacturer = "Diamond", Category = "GA", CallsignPrefix = "VH-",
                CruiseSpeed = 142, ServiceCeiling = 16400, WakeCategory = "Light", EngineType = "Piston", SeatCapacity = 4,
                MsfsTitle = "Diamond DA40 Diamond Star", MsfsModelMatchCode = "DA40", SupportsSimConnect = true
            },

            // Medium Aircraft
            new Aircraft 
            { 
                Type = "DHC8", Manufacturer = "De Havilland Canada", Category = "Medium", CallsignPrefix = "VH-",
                CruiseSpeed = 360, ServiceCeiling = 25000, WakeCategory = "Medium", EngineType = "Turboprop", SeatCapacity = 78,
                MsfsTitle = "DHC-8 Dash 8", MsfsModelMatchCode = "DHC8", SupportsSimConnect = false
            },
            new Aircraft 
            { 
                Type = "AT72", Manufacturer = "ATR", Category = "Medium", CallsignPrefix = "VH-",
                CruiseSpeed = 276, ServiceCeiling = 25000, WakeCategory = "Medium", EngineType = "Turboprop", SeatCapacity = 72,
                MsfsTitle = "ATR 72-600", MsfsModelMatchCode = "AT72", SupportsSimConnect = false
            },
            new Aircraft 
            { 
                Type = "E190", Manufacturer = "Embraer", Category = "Medium", CallsignPrefix = "VH-",
                CruiseSpeed = 470, ServiceCeiling = 41000, WakeCategory = "Medium", EngineType = "Jet", SeatCapacity = 114,
                MsfsTitle = "Embraer E-Jet E190", MsfsModelMatchCode = "E190", SupportsSimConnect = false
            },
            new Aircraft 
            { 
                Type = "B737", Manufacturer = "Boeing", Category = "Medium", CallsignPrefix = "VH-",
                CruiseSpeed = 453, ServiceCeiling = 41000, WakeCategory = "Medium", EngineType = "Jet", SeatCapacity = 189,
                MsfsTitle = "Boeing 737-800", MsfsModelMatchCode = "B738", SupportsSimConnect = true
            },
            new Aircraft 
            { 
                Type = "A320", Manufacturer = "Airbus", Category = "Medium", CallsignPrefix = "VH-",
                CruiseSpeed = 447, ServiceCeiling = 39000, WakeCategory = "Medium", EngineType = "Jet", SeatCapacity = 180,
                MsfsTitle = "Airbus A320neo", MsfsModelMatchCode = "A320", SupportsSimConnect = true
            },

            // Heavy Aircraft
            new Aircraft 
            { 
                Type = "B777", Manufacturer = "Boeing", Category = "Heavy", CallsignPrefix = "VH-",
                CruiseSpeed = 490, ServiceCeiling = 43100, WakeCategory = "Heavy", EngineType = "Jet", SeatCapacity = 396,
                MsfsTitle = "Boeing 777-300ER", MsfsModelMatchCode = "B77W", SupportsSimConnect = true
            },
            new Aircraft 
            { 
                Type = "B787", Manufacturer = "Boeing", Category = "Heavy", CallsignPrefix = "VH-",
                CruiseSpeed = 488, ServiceCeiling = 43000, WakeCategory = "Heavy", EngineType = "Jet", SeatCapacity = 330,
                MsfsTitle = "Boeing 787-9 Dreamliner", MsfsModelMatchCode = "B789", SupportsSimConnect = true
            },
            new Aircraft 
            { 
                Type = "A330", Manufacturer = "Airbus", Category = "Heavy", CallsignPrefix = "VH-",
                CruiseSpeed = 473, ServiceCeiling = 41000, WakeCategory = "Heavy", EngineType = "Jet", SeatCapacity = 335,
                MsfsTitle = "Airbus A330-300", MsfsModelMatchCode = "A333", SupportsSimConnect = false
            },
            new Aircraft 
            { 
                Type = "A380", Manufacturer = "Airbus", Category = "Heavy", CallsignPrefix = "VH-",
                CruiseSpeed = 488, ServiceCeiling = 43000, WakeCategory = "Super", EngineType = "Jet", SeatCapacity = 853,
                MsfsTitle = "Airbus A380-800", MsfsModelMatchCode = "A388", SupportsSimConnect = false
            },
            new Aircraft 
            { 
                Type = "B747", Manufacturer = "Boeing", Category = "Heavy", CallsignPrefix = "VH-",
                CruiseSpeed = 490, ServiceCeiling = 43100, WakeCategory = "Heavy", EngineType = "Jet", SeatCapacity = 660,
                MsfsTitle = "Boeing 747-8F", MsfsModelMatchCode = "B748", SupportsSimConnect = false
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

        // Add sample pilot profiles for MSFS integration testing
        var pilotProfiles = new List<PilotProfile>();
        var msfsAircraft = aircraftInDb.Where(a => a.SupportsSimConnect).ToList();
        
        if (msfsAircraft.Any())
        {
            // GA Pilot profiles
            var c172 = msfsAircraft.FirstOrDefault(a => a.Type == "C172");
            if (c172 != null)
            {
                pilotProfiles.Add(new PilotProfile
                {
                    Callsign = "VH-ABC",
                    AircraftId = c172.Id,
                    PilotName = "John Smith",
                    ExperienceLevel = "Private",
                    CertificatesRatings = "PPL",
                    IsLive = false,
                    SimConnectStatus = "Disconnected"
                });
            }

            var pa28 = msfsAircraft.FirstOrDefault(a => a.Type == "PA28");
            if (pa28 != null)
            {
                pilotProfiles.Add(new PilotProfile
                {
                    Callsign = "VH-XYZ",
                    AircraftId = pa28.Id,
                    PilotName = "Sarah Wilson",
                    ExperienceLevel = "Student",
                    CertificatesRatings = "Student Pilot",
                    IsLive = false,
                    SimConnectStatus = "Disconnected"
                });
            }

            // Commercial pilot profiles
            var b737 = msfsAircraft.FirstOrDefault(a => a.Type == "B737");
            if (b737 != null)
            {
                pilotProfiles.Add(new PilotProfile
                {
                    Callsign = "QFA456",
                    AircraftId = b737.Id,
                    PilotName = "Captain Mike Johnson",
                    ExperienceLevel = "ATP",
                    CertificatesRatings = "ATPL, B737 Type Rating, IR",
                    IsLive = false,
                    SimConnectStatus = "Disconnected"
                });
            }

            var a320 = msfsAircraft.FirstOrDefault(a => a.Type == "A320");
            if (a320 != null)
            {
                pilotProfiles.Add(new PilotProfile
                {
                    Callsign = "JST789",
                    AircraftId = a320.Id,
                    PilotName = "Captain Emma Davis",
                    ExperienceLevel = "ATP",
                    CertificatesRatings = "ATPL, A320 Type Rating, IR",
                    IsLive = false,
                    SimConnectStatus = "Disconnected"
                });
            }
        }

        context.PilotProfiles.AddRange(pilotProfiles);

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
            },

            // GA Scenarios at Moorabbin
            new Scenario
            {
                Name = "First Solo at Moorabbin",
                AirportIcao = "YMMB",
                Kind = "pattern",
                Difficulty = "Basic",
                Seed = 52345,
                InitialStateJson = "{\"position\":\"apron\",\"aircraft\":\"VH-ABC\",\"type\":\"C172\",\"instructor\":false}",
                RubricJson = "{\"radio_discipline\":15,\"pattern_work\":20,\"emergency_procedures\":10}"
            },
            new Scenario
            {
                Name = "Controlled Airspace Entry",
                AirportIcao = "YMMB",
                Kind = "transit",
                Difficulty = "Intermediate",
                Seed = 52346,
                InitialStateJson = "{\"position\":\"outside_cta\",\"aircraft\":\"VH-DEF\",\"type\":\"C182\",\"destination\":\"YSSY\"}",
                RubricJson = "{\"clearance_requests\":20,\"cta_compliance\":25,\"navigation\":15}"
            },

            // GA Scenarios at Bankstown
            new Scenario
            {
                Name = "Busy GA Airport Operations",
                AirportIcao = "YSBK",
                Kind = "taxi",
                Difficulty = "Intermediate",
                Seed = 62345,
                InitialStateJson = "{\"position\":\"gate\",\"aircraft\":\"VH-GHI\",\"type\":\"PA28\",\"busy_traffic\":true}",
                RubricJson = "{\"traffic_awareness\":20,\"taxi_compliance\":15,\"radio_congestion_management\":20}"
            },
            new Scenario
            {
                Name = "Navigation Training Exercise",
                AirportIcao = "YSBK",
                Kind = "departure",
                Difficulty = "Basic",
                Seed = 62346,
                InitialStateJson = "{\"position\":\"runup\",\"aircraft\":\"VH-JKL\",\"type\":\"C172\",\"nav_exercise\":true}",
                RubricJson = "{\"departure_procedures\":15,\"nav_awareness\":20,\"frequency_changes\":10}"
            },

            // GA Scenarios at Archerfield
            new Scenario
            {
                Name = "Formation Flight Operations",
                AirportIcao = "YBAF",
                Kind = "formation",
                Difficulty = "Advanced",
                Seed = 72345,
                InitialStateJson = "{\"position\":\"holding_point\",\"aircraft\":\"VH-MNO\",\"type\":\"C210\",\"formation\":2}",
                RubricJson = "{\"formation_procedures\":25,\"separation_awareness\":20,\"emergency_breakup\":20}"
            },
            new Scenario
            {
                Name = "Night Circuit Training",
                AirportIcao = "YBAF",
                Kind = "pattern",
                Difficulty = "Intermediate",
                Seed = 72346,
                InitialStateJson = "{\"position\":\"downwind\",\"aircraft\":\"VH-PQR\",\"type\":\"C182\",\"night\":true}",
                RubricJson = "{\"night_procedures\":20,\"lighting_compliance\":15,\"safety_awareness\":25}"
            },

            // GA Scenarios at Jandakot
            new Scenario
            {
                Name = "Multi-Engine Training",
                AirportIcao = "YPJT",
                Kind = "training",
                Difficulty = "Intermediate",
                Seed = 82345,
                InitialStateJson = "{\"position\":\"apron\",\"aircraft\":\"VH-STU\",\"type\":\"PA44\",\"multi_engine\":true}",
                RubricJson = "{\"engine_procedures\":20,\"emergency_handling\":25,\"multi_engine_ops\":20}"
            },
            new Scenario
            {
                Name = "Crosswind Landing Practice",
                AirportIcao = "YPJT",
                Kind = "approach",
                Difficulty = "Intermediate",
                Seed = 82346,
                InitialStateJson = "{\"position\":\"final\",\"aircraft\":\"VH-VWX\",\"type\":\"C172\",\"wind\":\"25015G25KT\"}",
                RubricJson = "{\"crosswind_technique\":25,\"go_around_decision\":20,\"weather_assessment\":15}"
            },

            // GA Scenarios at Camden
            new Scenario
            {
                Name = "Historic Airfield Operations",
                AirportIcao = "YSCN",
                Kind = "heritage",
                Difficulty = "Basic",
                Seed = 92345,
                InitialStateJson = "{\"position\":\"grass_strip\",\"aircraft\":\"VH-YZA\",\"type\":\"TIGR\",\"tailwheel\":true}",
                RubricJson = "{\"tailwheel_ops\":20,\"grass_strip_awareness\":15,\"heritage_procedures\":10}"
            },

            // GA Scenarios at Parafield
            new Scenario
            {
                Name = "Flight Training Circuit Work",
                AirportIcao = "YPPF",
                Kind = "pattern",
                Difficulty = "Basic",
                Seed = 102345,
                InitialStateJson = "{\"position\":\"upwind\",\"aircraft\":\"VH-BCD\",\"type\":\"C152\",\"student\":true}",
                RubricJson = "{\"circuit_procedures\":15,\"student_guidance\":20,\"safety_oversight\":15}"
            },

            // TAF Interpretation Training Scenarios
            new Scenario
            {
                Name = "TAF Interpretation - Melbourne Weather",
                AirportIcao = "YMML",
                Kind = "weather_interpretation",
                Difficulty = "Intermediate",
                Seed = 110001,
                InitialStateJson = "{\"position\":\"briefing\",\"aircraft\":\"VH-TAF\",\"type\":\"C172\",\"weather_brief\":true}",
                RubricJson = "{\"taf_reading\":25,\"weather_decision\":20,\"alternate_planning\":15,\"fuel_considerations\":10}"
            },
            new Scenario
            {
                Name = "TAF Interpretation - Sydney Coastal Weather",
                AirportIcao = "YSSY",
                Kind = "weather_interpretation",
                Difficulty = "Advanced",
                Seed = 110002,
                InitialStateJson = "{\"position\":\"briefing\",\"aircraft\":\"VH-WTH\",\"type\":\"B737\",\"coastal_weather\":true}",
                RubricJson = "{\"complex_taf\":30,\"trend_analysis\":25,\"decision_making\":20,\"risk_assessment\":15}"
            },
            new Scenario
            {
                Name = "TAF Interpretation - Brisbane Tropical Conditions",
                AirportIcao = "YBBN",
                Kind = "weather_interpretation",
                Difficulty = "Advanced",
                Seed = 110003,
                InitialStateJson = "{\"position\":\"briefing\",\"aircraft\":\"VH-TRP\",\"type\":\"DHC8\",\"tropical_weather\":true}",
                RubricJson = "{\"tropical_taf\":30,\"thunderstorm_timing\":25,\"safety_margins\":20,\"alternate_requirements\":15}"
            },

            // Aviation Alphabet & Phraseology Training
            new Scenario
            {
                Name = "Phonetic Alphabet Mastery",
                AirportIcao = "YMMB",
                Kind = "phraseology_training",
                Difficulty = "Basic",
                Seed = 120001,
                InitialStateJson = "{\"position\":\"ground\",\"aircraft\":\"VH-PHO\",\"type\":\"C172\",\"alphabet_drill\":true}",
                RubricJson = "{\"phonetic_accuracy\":25,\"clarity\":20,\"speed\":15,\"consistency\":10}"
            },
            new Scenario
            {
                Name = "Standard Phraseology Practice",
                AirportIcao = "YSBK",
                Kind = "phraseology_training",
                Difficulty = "Intermediate",
                Seed = 120002,
                InitialStateJson = "{\"position\":\"various\",\"aircraft\":\"VH-STD\",\"type\":\"PA28\",\"phraseology_drill\":true}",
                RubricJson = "{\"standard_phrases\":25,\"appropriate_usage\":20,\"conciseness\":15,\"professional_delivery\":15}"
            },
            new Scenario
            {
                Name = "International Phraseology Standards",
                AirportIcao = "YSSY",
                Kind = "phraseology_training",
                Difficulty = "Advanced",
                Seed = 120003,
                InitialStateJson = "{\"position\":\"international_gate\",\"aircraft\":\"VH-INT\",\"type\":\"A330\",\"international\":true}",
                RubricJson = "{\"icao_phraseology\":30,\"international_procedures\":25,\"emergency_phrases\":20,\"non_native_clarity\":15}"
            },

            // Controlled Aerodrome Operations
            new Scenario
            {
                Name = "Class C Tower Operations - Melbourne",
                AirportIcao = "YMML",
                Kind = "controlled_ops",
                Difficulty = "Intermediate",
                Seed = 130001,
                InitialStateJson = "{\"position\":\"holding_point\",\"aircraft\":\"VH-CTL\",\"type\":\"C182\",\"controlled\":true}",
                RubricJson = "{\"tower_communication\":25,\"clearance_compliance\":20,\"separation_awareness\":15,\"emergency_procedures\":15}"
            },
            new Scenario
            {
                Name = "Class C Complex Operations - Sydney",
                AirportIcao = "YSSY",
                Kind = "controlled_ops",
                Difficulty = "Advanced",
                Seed = 130002,
                InitialStateJson = "{\"position\":\"approach\",\"aircraft\":\"VH-CMP\",\"type\":\"A320\",\"complex_airspace\":true}",
                RubricJson = "{\"complex_clearances\":30,\"traffic_integration\":25,\"priority_handling\":20,\"stress_management\":15}"
            },
            new Scenario
            {
                Name = "Class D Tower Operations - Gold Coast",
                AirportIcao = "YBCG",
                Kind = "controlled_ops",
                Difficulty = "Intermediate",
                Seed = 130003,
                InitialStateJson = "{\"position\":\"circuit\",\"aircraft\":\"VH-GLD\",\"type\":\"C172\",\"class_d\":true}",
                RubricJson = "{\"class_d_procedures\":20,\"mixed_traffic\":20,\"position_reporting\":15,\"runway_management\":15}"
            },
            new Scenario
            {
                Name = "Controlled Airspace Transition",
                AirportIcao = "YSCB",
                Kind = "controlled_ops",
                Difficulty = "Advanced",
                Seed = 130004,
                InitialStateJson = "{\"position\":\"cta_boundary\",\"aircraft\":\"VH-CTA\",\"type\":\"PA44\",\"transition\":true}",
                RubricJson = "{\"clearance_requests\":25,\"altitude_management\":20,\"frequency_changes\":15,\"navigation_accuracy\":20}"
            },

            // Uncontrolled Aerodrome Operations
            new Scenario
            {
                Name = "CTAF Procedures - Mudgee",
                AirportIcao = "YMDG",  
                Kind = "uncontrolled_ops",
                Difficulty = "Basic",
                Seed = 140001,
                InitialStateJson = "{\"position\":\"10nm_out\",\"aircraft\":\"VH-CTF\",\"type\":\"C172\",\"ctaf\":true}",
                RubricJson = "{\"ctaf_procedures\":25,\"position_reports\":20,\"traffic_awareness\":20,\"self_separation\":15}"
            },
            new Scenario
            {
                Name = "Non-Towered Airport Operations",
                AirportIcao = "YWOL",
                Kind = "uncontrolled_ops", 
                Difficulty = "Intermediate",
                Seed = 140002,
                InitialStateJson = "{\"position\":\"overhead\",\"aircraft\":\"VH-NTW\",\"type\":\"PA28\",\"non_towered\":true}",
                RubricJson = "{\"standard_procedures\":20,\"traffic_pattern\":20,\"radio_discipline\":15,\"safety_awareness\":25}"
            },
            new Scenario
            {
                Name = "Grass Strip Operations",
                AirportIcao = "YSCN",
                Kind = "uncontrolled_ops",
                Difficulty = "Advanced", 
                Seed = 140003,
                InitialStateJson = "{\"position\":\"approach\",\"aircraft\":\"VH-GRS\",\"type\":\"C180\",\"grass_strip\":true}",
                RubricJson = "{\"grass_procedures\":25,\"wind_assessment\":20,\"surface_conditions\":20,\"precautionary_approach\":15}"
            },
            new Scenario
            {
                Name = "Remote Aerodrome Operations",
                AirportIcao = "YPDN",
                Kind = "uncontrolled_ops",
                Difficulty = "Advanced",
                Seed = 140004, 
                InitialStateJson = "{\"position\":\"remote_approach\",\"aircraft\":\"VH-RMT\",\"type\":\"C210\",\"remote\":true}",
                RubricJson = "{\"remote_procedures\":25,\"fuel_planning\":20,\"weather_assessment\":20,\"emergency_preparedness\":20}"
            },

            // Mixed Training Scenarios
            new Scenario
            {
                Name = "Controlled to Uncontrolled Transition",
                AirportIcao = "YMML",
                Kind = "mixed_ops",
                Difficulty = "Advanced",
                Seed = 150001,
                InitialStateJson = "{\"position\":\"departure\",\"aircraft\":\"VH-MIX\",\"type\":\"C182\",\"transition\":true}",
                RubricJson = "{\"procedure_changes\":25,\"frequency_management\":20,\"navigation_transition\":20,\"communication_adaptation\":15}"
            },
            new Scenario
            {
                Name = "Emergency Communications Training",
                AirportIcao = "YSSY",
                Kind = "emergency_training",
                Difficulty = "Advanced",
                Seed = 150002,
                InitialStateJson = "{\"position\":\"emergency\",\"aircraft\":\"VH-EMG\",\"type\":\"B737\",\"emergency\":true}",
                RubricJson = "{\"emergency_phraseology\":30,\"urgency_communication\":25,\"priority_handling\":20,\"stress_management\":20}"
            }
        };

        context.Scenarios.AddRange(scenarios);

        // Add Australian airspace data
        var airspaces = new List<Airspace>
        {
            // Melbourne Terminal Area
            new Airspace
            {
                Name = "Melbourne Terminal Area",
                Type = "TMA",
                Class = "C",
                LowerAltitude = 1500,
                UpperAltitude = 8500,
                Frequency = "132.0",
                OperatingHours = "H24",
                CenterLat = -37.6733,
                CenterLon = 144.8430,
                RadiusNm = 25,
                AssociatedAirport = "YMML",
                Restrictions = "All aircraft require clearance before entry"
            },
            // Sydney Terminal Area
            new Airspace
            {
                Name = "Sydney Terminal Area",
                Type = "TMA",
                Class = "C",
                LowerAltitude = 1500,
                UpperAltitude = 8500,
                Frequency = "124.4",
                OperatingHours = "H24",
                CenterLat = -33.9399,
                CenterLon = 151.1753,
                RadiusNm = 25,
                AssociatedAirport = "YSSY",
                Restrictions = "All aircraft require clearance before entry"
            },
            // Brisbane Terminal Area
            new Airspace
            {
                Name = "Brisbane Terminal Area",
                Type = "TMA",
                Class = "C",
                LowerAltitude = 1500,
                UpperAltitude = 8500,
                Frequency = "125.6",
                OperatingHours = "H24",
                CenterLat = -27.3842,
                CenterLon = 153.1175,
                RadiusNm = 25,
                AssociatedAirport = "YBBN",
                Restrictions = "All aircraft require clearance before entry"
            },
            // Moorabbin Control Zone
            new Airspace
            {
                Name = "Moorabbin Control Zone",
                Type = "CTR",
                Class = "D",
                LowerAltitude = 0,
                UpperAltitude = 1500,
                Frequency = "118.1",
                OperatingHours = "0530-2200 LT",
                CenterLat = -37.9750,
                CenterLon = 145.1022,
                RadiusNm = 5,
                AssociatedAirport = "YMMB",
                Restrictions = "Two-way radio contact required during operating hours"
            },
            // Bankstown Control Zone
            new Airspace
            {
                Name = "Bankstown Control Zone",
                Type = "CTR",
                Class = "D",
                LowerAltitude = 0,
                UpperAltitude = 1500,
                Frequency = "132.35",
                OperatingHours = "0630-2200 LT",
                CenterLat = -33.9239,
                CenterLon = 150.9881,
                RadiusNm = 5,
                AssociatedAirport = "YSBK",
                Restrictions = "Two-way radio contact required during operating hours"
            },
            // Restricted Area - Richmond RAAF Base
            new Airspace
            {
                Name = "Richmond Restricted Area",
                Type = "Restricted",
                Class = "R",
                LowerAltitude = 0,
                UpperAltitude = 3000,
                OperatingHours = "H24",
                CenterLat = -33.6006,
                CenterLon = 150.7811,
                RadiusNm = 3,
                Restrictions = "Military operations - civilian aircraft prohibited without prior approval"
            },
            // Prohibited Area - Parliament House Canberra
            new Airspace
            {
                Name = "Parliament House Prohibited Area",
                Type = "Prohibited",
                Class = "P",
                LowerAltitude = 0,
                UpperAltitude = 3000,
                OperatingHours = "H24",
                CenterLat = -35.3081,
                CenterLon = 149.1244,
                RadiusNm = 2,
                Restrictions = "Flight prohibited at all times for security reasons"
            },
            // Danger Area - Puckapunyal Military Range
            new Airspace
            {
                Name = "Puckapunyal Danger Area",
                Type = "Danger",
                Class = "D",
                LowerAltitude = 0,
                UpperAltitude = 4500,
                OperatingHours = "0800-1700 MON-FRI",
                CenterLat = -37.1500,
                CenterLon = 145.0333,
                RadiusNm = 5,
                Restrictions = "Military training activities - extreme danger to aircraft"
            }
        };

        context.Airspaces.AddRange(airspaces);

        // Add airspace notices (NOTAMs and restrictions)
        var notices = new List<AirspaceNotice>
        {
            new AirspaceNotice
            {
                AirspaceId = 1, // Melbourne TMA
                Type = "NOTAM",
                Title = "Temporary Flight Restrictions",
                Description = "Special event operations may result in temporary airspace restrictions. Monitor ATIS for updates.",
                EffectiveFrom = DateTime.Now.AddDays(-30),
                EffectiveTo = DateTime.Now.AddDays(30),
                IsActive = true
            },
            new AirspaceNotice
            {
                AirspaceId = 4, // Moorabbin CTR
                Type = "Warning",
                Title = "High Training Activity",
                Description = "Multiple training aircraft operating in the circuit. Maintain extra vigilance and monitor frequency continuously.",
                EffectiveFrom = DateTime.Now.AddDays(-7),
                EffectiveTo = DateTime.Now.AddDays(7),
                IsActive = true
            },
            new AirspaceNotice
            {
                AirspaceId = 8, // Puckapunyal Danger Area
                Type = "Restriction",
                Title = "Live Firing Exercises",
                Description = "Artillery range active during published hours. Aircraft penetration strictly prohibited.",
                EffectiveFrom = DateTime.Now.AddDays(-1),
                EffectiveTo = DateTime.Now.AddDays(14),
                IsActive = true
            }
        };

        context.AirspaceNotices.AddRange(notices);
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