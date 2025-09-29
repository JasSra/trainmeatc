using Microsoft.EntityFrameworkCore;
using PilotSim.Data.Models;

namespace PilotSim.Data;

public class SimDbContext : DbContext
{
    public SimDbContext(DbContextOptions<SimDbContext> options) : base(options)
    {
    }

    public DbSet<Airport> Airports { get; set; }
    public DbSet<Runway> Runways { get; set; }
    public DbSet<Scenario> Scenarios { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<Turn> Turns { get; set; }
    public DbSet<Metric> Metrics { get; set; }
    public DbSet<Aircraft> Aircraft { get; set; }
    public DbSet<TrafficProfile> TrafficProfiles { get; set; }
    public DbSet<PilotProfile> PilotProfiles { get; set; }
    public DbSet<Airspace> Airspaces { get; set; }
    public DbSet<AirspaceNotice> AirspaceNotices { get; set; }
    public DbSet<VerdictDetail> VerdictDetails { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Airport configuration
        modelBuilder.Entity<Airport>(entity =>
        {
            entity.HasKey(e => e.Icao);
            entity.Property(e => e.Icao).HasColumnName("icao");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Lat).HasColumnName("lat");
            entity.Property(e => e.Lon).HasColumnName("lon");
            entity.Property(e => e.AtisFreq).HasColumnName("atis_freq");
            entity.Property(e => e.TowerFreq).HasColumnName("tower_freq");
            entity.Property(e => e.GroundFreq).HasColumnName("ground_freq");
            entity.Property(e => e.AppFreq).HasColumnName("app_freq");
            entity.Property(e => e.Category).HasColumnName("category").HasDefaultValue("Major");
            entity.Property(e => e.ElevationFt).HasColumnName("elevation_ft");
            entity.Property(e => e.OperatingHours).HasColumnName("operating_hours");
            entity.Property(e => e.HasFuel).HasColumnName("has_fuel").HasDefaultValue(true);
            entity.Property(e => e.HasMaintenance).HasColumnName("has_maintenance").HasDefaultValue(false);
            entity.Property(e => e.FuelTypes).HasColumnName("fuel_types");
            entity.ToTable("airport");
        });

        // Runway configuration
        modelBuilder.Entity<Runway>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AirportIcao).HasColumnName("airport_icao");
            entity.Property(e => e.Ident).HasColumnName("ident");
            entity.Property(e => e.MagneticHeading).HasColumnName("magnetic_heading");
            entity.Property(e => e.LengthM).HasColumnName("length_m");
            entity.Property(e => e.Ils).HasColumnName("ils");
            
            entity.HasOne(e => e.Airport)
                .WithMany(a => a.Runways)
                .HasForeignKey(e => e.AirportIcao)
                .HasPrincipalKey(a => a.Icao);
            
            entity.ToTable("runway");
        });

        // Scenario configuration
        modelBuilder.Entity<Scenario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.AirportIcao).HasColumnName("airport_icao");
            entity.Property(e => e.Kind).HasColumnName("kind");
            entity.Property(e => e.Difficulty).HasColumnName("difficulty");
            entity.Property(e => e.Seed).HasColumnName("seed");
            entity.Property(e => e.InitialStateJson).HasColumnName("initial_state_json");
            entity.Property(e => e.RubricJson).HasColumnName("rubric_json");
            
            entity.HasOne(e => e.Airport)
                .WithMany(a => a.Scenarios)
                .HasForeignKey(e => e.AirportIcao)
                .HasPrincipalKey(a => a.Icao);
            
            entity.ToTable("scenario");
        });

        // Session configuration
        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ScenarioId).HasColumnName("scenario_id");
            entity.Property(e => e.StartedUtc).HasColumnName("started_utc");
            entity.Property(e => e.EndedUtc).HasColumnName("ended_utc");
            entity.Property(e => e.Difficulty).HasColumnName("difficulty");
            entity.Property(e => e.ParametersJson).HasColumnName("parameters_json");
            entity.Property(e => e.ScoreTotal).HasColumnName("score_total").HasDefaultValue(0);
            entity.Property(e => e.Outcome).HasColumnName("outcome");
            
            entity.HasOne(e => e.Scenario)
                .WithMany(s => s.Sessions)
                .HasForeignKey(e => e.ScenarioId);
            
            entity.ToTable("session");
        });

        // Turn configuration
        modelBuilder.Entity<Turn>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.Idx).HasColumnName("idx");
            entity.Property(e => e.UserAudioPath).HasColumnName("user_audio_path");
            entity.Property(e => e.UserTranscript).HasColumnName("user_transcript");
            entity.Property(e => e.InstructorJson).HasColumnName("instructor_json");
            entity.Property(e => e.AtcJson).HasColumnName("atc_json");
            entity.Property(e => e.TtsAudioPath).HasColumnName("tts_audio_path");
            entity.Property(e => e.Verdict).HasColumnName("verdict");
            entity.Property(e => e.StartedUtc).HasColumnName("started_utc");
            entity.Property(e => e.SttMs).HasColumnName("stt_ms");
            entity.Property(e => e.InstructorMs).HasColumnName("instructor_ms");
            entity.Property(e => e.AtcMs).HasColumnName("atc_ms");
            entity.Property(e => e.TtsMs).HasColumnName("tts_ms");
            entity.Property(e => e.TotalMs).HasColumnName("total_ms");
            entity.Property(e => e.ScoreDelta).HasColumnName("score_delta");
            entity.Property(e => e.Blocked).HasColumnName("blocked").HasDefaultValue(false);
            
            entity.HasOne(e => e.Session)
                .WithMany(s => s.Turns)
                .HasForeignKey(e => e.SessionId);

            entity.HasMany(e => e.VerdictDetails)
                .WithOne(d => d.Turn)
                .HasForeignKey(d => d.TurnId);
            
            entity.ToTable("turn");
        });

        // VerdictDetail configuration
        modelBuilder.Entity<VerdictDetail>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TurnId).HasColumnName("turn_id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.Category).HasColumnName("category");
            entity.Property(e => e.Severity).HasColumnName("severity");
            entity.Property(e => e.Weight).HasColumnName("weight");
            entity.Property(e => e.Score).HasColumnName("score");
            entity.Property(e => e.Delta).HasColumnName("delta");
            entity.Property(e => e.Detail).HasColumnName("detail");
            entity.Property(e => e.RubricVersion).HasColumnName("rubric_version");
            entity.ToTable("verdict_detail");
        });

        // Metric configuration
        modelBuilder.Entity<Metric>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.K).HasColumnName("k");
            entity.Property(e => e.V).HasColumnName("v");
            entity.Property(e => e.TUtc).HasColumnName("t_utc");
            
            entity.HasOne(e => e.Session)
                .WithMany(s => s.Metrics)
                .HasForeignKey(e => e.SessionId);
            
            entity.ToTable("metric");
        });

        // Aircraft configuration
        modelBuilder.Entity<Aircraft>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Type).HasColumnName("type").IsRequired();
            entity.Property(e => e.Category).HasColumnName("category").IsRequired();
            entity.Property(e => e.Manufacturer).HasColumnName("manufacturer").IsRequired();
            entity.Property(e => e.CallsignPrefix).HasColumnName("callsign_prefix").IsRequired();
            entity.Property(e => e.CruiseSpeed).HasColumnName("cruise_speed");
            entity.Property(e => e.ServiceCeiling).HasColumnName("service_ceiling");
            entity.Property(e => e.WakeCategory).HasColumnName("wake_category");
            entity.Property(e => e.EngineType).HasColumnName("engine_type");
            entity.Property(e => e.SeatCapacity).HasColumnName("seat_capacity");
            
            // MSFS-specific properties
            entity.Property(e => e.MsfsTitle).HasColumnName("msfs_title");
            entity.Property(e => e.MsfsModelMatchCode).HasColumnName("msfs_model_match_code");
            entity.Property(e => e.SupportsSimConnect).HasColumnName("supports_simconnect").HasDefaultValue(false);
            
            entity.ToTable("aircraft");
        });

        // TrafficProfile configuration
        modelBuilder.Entity<TrafficProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AircraftId).HasColumnName("aircraft_id");
            entity.Property(e => e.AirportIcao).HasColumnName("airport_icao");
            entity.Property(e => e.Callsign).HasColumnName("callsign").IsRequired();
            entity.Property(e => e.FlightType).HasColumnName("flight_type");
            entity.Property(e => e.Route).HasColumnName("route");
            entity.Property(e => e.FrequencyWeight).HasColumnName("frequency_weight").HasDefaultValue(1.0);
            
            entity.HasOne(e => e.Aircraft)
                .WithMany(a => a.TrafficProfiles)
                .HasForeignKey(e => e.AircraftId);
                
            entity.HasOne(e => e.Airport)
                .WithMany()
                .HasForeignKey(e => e.AirportIcao)
                .HasPrincipalKey(a => a.Icao);
            
            entity.ToTable("traffic_profile");
        });

        // Airspace configuration
        modelBuilder.Entity<Airspace>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.Type).HasColumnName("type").IsRequired();
            entity.Property(e => e.Class).HasColumnName("class").IsRequired();
            entity.Property(e => e.LowerAltitude).HasColumnName("lower_altitude");
            entity.Property(e => e.UpperAltitude).HasColumnName("upper_altitude");
            entity.Property(e => e.Frequency).HasColumnName("frequency");
            entity.Property(e => e.OperatingHours).HasColumnName("operating_hours");
            entity.Property(e => e.Restrictions).HasColumnName("restrictions");
            entity.Property(e => e.BoundaryJson).HasColumnName("boundary_json");
            entity.Property(e => e.CenterLat).HasColumnName("center_lat");
            entity.Property(e => e.CenterLon).HasColumnName("center_lon");
            entity.Property(e => e.RadiusNm).HasColumnName("radius_nm");
            entity.Property(e => e.AssociatedAirport).HasColumnName("associated_airport");
            
            entity.ToTable("airspace");
        });

        // AirspaceNotice configuration
        modelBuilder.Entity<AirspaceNotice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AirspaceId).HasColumnName("airspace_id");
            entity.Property(e => e.Type).HasColumnName("type").IsRequired();
            entity.Property(e => e.Title).HasColumnName("title").IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").IsRequired();
            entity.Property(e => e.EffectiveFrom).HasColumnName("effective_from");
            entity.Property(e => e.EffectiveTo).HasColumnName("effective_to");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            
            entity.HasOne(e => e.Airspace)
                .WithMany(a => a.Notices)
                .HasForeignKey(e => e.AirspaceId);
            
            entity.ToTable("airspace_notice");
        });

        // PilotProfile configuration
        modelBuilder.Entity<PilotProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Callsign).HasColumnName("callsign").IsRequired();
            entity.Property(e => e.AircraftId).HasColumnName("aircraft_id");
            entity.Property(e => e.PilotName).HasColumnName("pilot_name");
            entity.Property(e => e.ExperienceLevel).HasColumnName("experience_level");
            entity.Property(e => e.PreferredAirports).HasColumnName("preferred_airports");
            entity.Property(e => e.CertificatesRatings).HasColumnName("certificates_ratings");
            
            // MSFS Live Integration properties
            entity.Property(e => e.IsLive).HasColumnName("is_live").HasDefaultValue(false);
            entity.Property(e => e.CurrentLatitude).HasColumnName("current_latitude");
            entity.Property(e => e.CurrentLongitude).HasColumnName("current_longitude");
            entity.Property(e => e.CurrentAltitude).HasColumnName("current_altitude");
            entity.Property(e => e.CurrentHeading).HasColumnName("current_heading");
            entity.Property(e => e.CurrentSpeed).HasColumnName("current_speed");
            entity.Property(e => e.CurrentPhase).HasColumnName("current_phase");
            entity.Property(e => e.AssignedFrequency).HasColumnName("assigned_frequency");
            entity.Property(e => e.FlightPlan).HasColumnName("flight_plan");
            entity.Property(e => e.LastUpdate).HasColumnName("last_update");
            entity.Property(e => e.SimConnectStatus).HasColumnName("simconnect_status");
            
            entity.HasOne(e => e.Aircraft)
                .WithMany(a => a.PilotProfiles)
                .HasForeignKey(e => e.AircraftId);
            
            entity.ToTable("pilot_profile");
        });
    }
}