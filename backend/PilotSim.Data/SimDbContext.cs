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
            
            entity.HasOne(e => e.Session)
                .WithMany(s => s.Turns)
                .HasForeignKey(e => e.SessionId);
            
            entity.ToTable("turn");
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
    }
}