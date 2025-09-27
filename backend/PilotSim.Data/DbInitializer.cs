using PilotSim.Data.Models;
using Microsoft.EntityFrameworkCore;

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

        // Add sample airport data
        var airport = new Airport
        {
            Icao = "YMML",
            Name = "Melbourne Airport",
            Lat = -37.6733,
            Lon = 144.8430,
            TowerFreq = "120.5",
            GroundFreq = "121.7",
            AppFreq = "132.0",
            AtisFreq = "126.25"
        };

        context.Airports.Add(airport);

        // Add sample runway
        var runway = new Runway
        {
            AirportIcao = "YMML",
            Ident = "16/34",
            MagneticHeading = 160,
            LengthM = 3658,
            Ils = true
        };

        context.Runways.Add(runway);

        // Add sample scenario
        var scenario = new Scenario
        {
            Name = "Basic Taxi Request",
            AirportIcao = "YMML",
            Kind = "taxi",
            Difficulty = "Basic",
            Seed = 12345,
            InitialStateJson = "{\"position\":\"gate\",\"aircraft\":\"VH-ABC\"}",
            RubricJson = "{\"taxi_clearance\":10,\"readback_accuracy\":5}"
        };

        context.Scenarios.Add(scenario);

        await context.SaveChangesAsync();
    }
}