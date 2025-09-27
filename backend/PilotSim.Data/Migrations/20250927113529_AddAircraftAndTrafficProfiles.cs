using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PilotSim.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAircraftAndTrafficProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "aircraft",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    type = table.Column<string>(type: "TEXT", nullable: false),
                    category = table.Column<string>(type: "TEXT", nullable: false),
                    manufacturer = table.Column<string>(type: "TEXT", nullable: false),
                    callsign_prefix = table.Column<string>(type: "TEXT", nullable: false),
                    cruise_speed = table.Column<int>(type: "INTEGER", nullable: true),
                    service_ceiling = table.Column<int>(type: "INTEGER", nullable: true),
                    wake_category = table.Column<string>(type: "TEXT", nullable: true),
                    engine_type = table.Column<string>(type: "TEXT", nullable: true),
                    seat_capacity = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aircraft", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "traffic_profile",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    aircraft_id = table.Column<int>(type: "INTEGER", nullable: false),
                    airport_icao = table.Column<string>(type: "TEXT", nullable: false),
                    callsign = table.Column<string>(type: "TEXT", nullable: false),
                    flight_type = table.Column<string>(type: "TEXT", nullable: true),
                    route = table.Column<string>(type: "TEXT", nullable: true),
                    frequency_weight = table.Column<double>(type: "REAL", nullable: false, defaultValue: 1.0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_traffic_profile", x => x.id);
                    table.ForeignKey(
                        name: "FK_traffic_profile_aircraft_aircraft_id",
                        column: x => x.aircraft_id,
                        principalTable: "aircraft",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_traffic_profile_airport_airport_icao",
                        column: x => x.airport_icao,
                        principalTable: "airport",
                        principalColumn: "icao",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_traffic_profile_aircraft_id",
                table: "traffic_profile",
                column: "aircraft_id");

            migrationBuilder.CreateIndex(
                name: "IX_traffic_profile_airport_icao",
                table: "traffic_profile",
                column: "airport_icao");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "traffic_profile");

            migrationBuilder.DropTable(
                name: "aircraft");
        }
    }
}
