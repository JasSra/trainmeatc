using PilotSim.Core;

namespace PilotSim.Data.Models;

public class Airport
{
    public string Icao { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double? Lat { get; set; }
    public double? Lon { get; set; }
    public string? AtisFreq { get; set; }
    public string? TowerFreq { get; set; }
    public string? GroundFreq { get; set; }
    public string? AppFreq { get; set; }
    public string Category { get; set; } = "Major"; // "Major", "Regional", "GA"
    public int? ElevationFt { get; set; }
    public string? OperatingHours { get; set; }
    public bool HasFuel { get; set; } = true;
    public bool HasMaintenance { get; set; } = false;
    public string? FuelTypes { get; set; } // "100LL, JetA1, Diesel"
    
    // Chart and documentation links
    public string? VtcChartUrl { get; set; } // Visual Terminal Chart URL
    public string? TacChartUrl { get; set; } // Terminal Area Chart URL
    public string? ErrcChartUrl { get; set; } // En Route Chart URL
    public string? PcaChartUrl { get; set; } // Planned Chart Area URL
    public string? AipUrl { get; set; } // Aeronautical Information Publication URL
    
    public ICollection<Runway> Runways { get; set; } = new List<Runway>();
    public ICollection<Scenario> Scenarios { get; set; } = new List<Scenario>();
}

public class Runway
{
    public int Id { get; set; }
    public string AirportIcao { get; set; } = string.Empty;
    public string Ident { get; set; } = string.Empty;
    public int? MagneticHeading { get; set; }
    public int? LengthM { get; set; }
    public bool Ils { get; set; }
    
    public Airport Airport { get; set; } = null!;
}

public class Scenario
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? AirportIcao { get; set; }
    public string? Kind { get; set; }
    public string? Difficulty { get; set; }
    public int? Seed { get; set; }
    public string? InitialStateJson { get; set; }
    public string? RubricJson { get; set; }
    
    // Enhanced scenario categorization
    public string FlightRules { get; set; } = "VFR"; // "VFR", "IFR", "Mixed"
    public string PilotType { get; set; } = "Private"; // "Private", "Commercial", "Cargo", "Airline", "Training"
    public string OperationType { get; set; } = "Training"; // "Training", "Cross-Country", "Pattern Work", "Emergency", "Instrument Approach"
    public string? WeatherConditions { get; set; } // "VMC", "IMC", "Marginal", "Severe"
    public string? TrafficDensity { get; set; } // "Light", "Moderate", "Heavy"
    public string? PrimaryFrequency { get; set; } // Which frequency this scenario primarily uses
    public int EstimatedDurationMinutes { get; set; } = 15;
    public string? Prerequisites { get; set; } // JSON array of required skills/completions
    public string? LearningObjectives { get; set; } // JSON array of learning goals
    
    public Airport? Airport { get; set; }
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
}

public class Session
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public int? ScenarioId { get; set; }
    public string? StartedUtc { get; set; }
    public string? EndedUtc { get; set; }
    public string? Difficulty { get; set; }
    public string? ParametersJson { get; set; }
    public int ScoreTotal { get; set; } = 0;
    public string? Outcome { get; set; }
    
    public Scenario? Scenario { get; set; }
    public ICollection<Turn> Turns { get; set; } = new List<Turn>();
    public ICollection<Metric> Metrics { get; set; } = new List<Metric>();
}

public class Turn
{
    public int Id { get; set; }
    public int? SessionId { get; set; }
    public int? Idx { get; set; }
    public string? UserAudioPath { get; set; }
    public string? UserTranscript { get; set; }
    public string? InstructorJson { get; set; }
    public string? AtcJson { get; set; }
    public string? TtsAudioPath { get; set; }
    public string? Verdict { get; set; }
    
    public Session? Session { get; set; }
}

public class Metric
{
    public int Id { get; set; }
    public int? SessionId { get; set; }
    public string? K { get; set; }
    public double? V { get; set; }
    public string? TUtc { get; set; }
    
    public Session? Session { get; set; }
}

public class Aircraft
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty; // e.g., "C172", "B737", "A380"
    public string Category { get; set; } = string.Empty; // "GA", "Medium", "Heavy"
    public string Manufacturer { get; set; } = string.Empty;
    public string CallsignPrefix { get; set; } = string.Empty; // e.g., "VH-" for Australia
    public int? CruiseSpeed { get; set; } // Knots
    public int? ServiceCeiling { get; set; } // Feet
    public string? WakeCategory { get; set; } // "Light", "Medium", "Heavy", "Super"
    public string? EngineType { get; set; } // "Piston", "Turboprop", "Jet"
    public int? SeatCapacity { get; set; }
    
    public ICollection<TrafficProfile> TrafficProfiles { get; set; } = new List<TrafficProfile>();
}

public class TrafficProfile
{
    public int Id { get; set; }
    public int AircraftId { get; set; }
    public string AirportIcao { get; set; } = string.Empty;
    public string Callsign { get; set; } = string.Empty; // e.g., "VH-ABC", "QFA123"
    public string? FlightType { get; set; } // "Training", "Charter", "Commercial", "Private"
    public string? Route { get; set; } // Typical route for this aircraft
    public double FrequencyWeight { get; set; } = 1.0; // How often this appears in scenarios
    
    public Aircraft Aircraft { get; set; } = null!;
    public Airport Airport { get; set; } = null!;
}

public class Airspace
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "CTA", "CTR", "TMA", "Restricted", "Prohibited", "Danger"
    public string Class { get; set; } = string.Empty; // "A", "B", "C", "D", "E", "G"
    public int? LowerAltitude { get; set; } // Feet
    public int? UpperAltitude { get; set; } // Feet
    public string? Frequency { get; set; }
    public string? OperatingHours { get; set; }
    public string? Restrictions { get; set; }
    public string? BoundaryJson { get; set; } // JSON coordinates for boundary polygon
    public double? CenterLat { get; set; }
    public double? CenterLon { get; set; }
    public double? RadiusNm { get; set; } // For circular airspace
    public string? AssociatedAirport { get; set; } // ICAO code if associated with an airport
    
    public ICollection<AirspaceNotice> Notices { get; set; } = new List<AirspaceNotice>();
}

public class AirspaceNotice
{
    public int Id { get; set; }
    public int AirspaceId { get; set; }
    public string Type { get; set; } = string.Empty; // "NOTAM", "Warning", "Restriction"
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    
    public Airspace Airspace { get; set; } = null!;
}
