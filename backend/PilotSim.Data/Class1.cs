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
