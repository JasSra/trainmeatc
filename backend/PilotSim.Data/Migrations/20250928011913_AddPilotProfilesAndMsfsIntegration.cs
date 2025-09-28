using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PilotSim.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPilotProfilesAndMsfsIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "msfs_model_match_code",
                table: "aircraft",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "msfs_title",
                table: "aircraft",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "supports_simconnect",
                table: "aircraft",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "pilot_profile",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    callsign = table.Column<string>(type: "TEXT", nullable: false),
                    aircraft_id = table.Column<int>(type: "INTEGER", nullable: false),
                    pilot_name = table.Column<string>(type: "TEXT", nullable: true),
                    experience_level = table.Column<string>(type: "TEXT", nullable: true),
                    preferred_airports = table.Column<string>(type: "TEXT", nullable: true),
                    certificates_ratings = table.Column<string>(type: "TEXT", nullable: true),
                    is_live = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    current_latitude = table.Column<double>(type: "REAL", nullable: true),
                    current_longitude = table.Column<double>(type: "REAL", nullable: true),
                    current_altitude = table.Column<double>(type: "REAL", nullable: true),
                    current_heading = table.Column<double>(type: "REAL", nullable: true),
                    current_speed = table.Column<double>(type: "REAL", nullable: true),
                    current_phase = table.Column<string>(type: "TEXT", nullable: true),
                    assigned_frequency = table.Column<string>(type: "TEXT", nullable: true),
                    flight_plan = table.Column<string>(type: "TEXT", nullable: true),
                    last_update = table.Column<DateTime>(type: "TEXT", nullable: true),
                    simconnect_status = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pilot_profile", x => x.id);
                    table.ForeignKey(
                        name: "FK_pilot_profile_aircraft_aircraft_id",
                        column: x => x.aircraft_id,
                        principalTable: "aircraft",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pilot_profile_aircraft_id",
                table: "pilot_profile",
                column: "aircraft_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pilot_profile");

            migrationBuilder.DropColumn(
                name: "msfs_model_match_code",
                table: "aircraft");

            migrationBuilder.DropColumn(
                name: "msfs_title",
                table: "aircraft");

            migrationBuilder.DropColumn(
                name: "supports_simconnect",
                table: "aircraft");
        }
    }
}
