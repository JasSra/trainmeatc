using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PilotSim.Data.Migrations
{
    /// <inheritdoc />
    public partial class _20250927_Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "airport",
                columns: table => new
                {
                    icao = table.Column<string>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    lat = table.Column<double>(type: "REAL", nullable: true),
                    lon = table.Column<double>(type: "REAL", nullable: true),
                    atis_freq = table.Column<string>(type: "TEXT", nullable: true),
                    tower_freq = table.Column<string>(type: "TEXT", nullable: true),
                    ground_freq = table.Column<string>(type: "TEXT", nullable: true),
                    app_freq = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airport", x => x.icao);
                });

            migrationBuilder.CreateTable(
                name: "runway",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    airport_icao = table.Column<string>(type: "TEXT", nullable: false),
                    ident = table.Column<string>(type: "TEXT", nullable: false),
                    magnetic_heading = table.Column<int>(type: "INTEGER", nullable: true),
                    length_m = table.Column<int>(type: "INTEGER", nullable: true),
                    ils = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_runway", x => x.id);
                    table.ForeignKey(
                        name: "FK_runway_airport_airport_icao",
                        column: x => x.airport_icao,
                        principalTable: "airport",
                        principalColumn: "icao",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scenario",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: true),
                    airport_icao = table.Column<string>(type: "TEXT", nullable: true),
                    kind = table.Column<string>(type: "TEXT", nullable: true),
                    difficulty = table.Column<string>(type: "TEXT", nullable: true),
                    seed = table.Column<int>(type: "INTEGER", nullable: true),
                    initial_state_json = table.Column<string>(type: "TEXT", nullable: true),
                    rubric_json = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scenario", x => x.id);
                    table.ForeignKey(
                        name: "FK_scenario_airport_airport_icao",
                        column: x => x.airport_icao,
                        principalTable: "airport",
                        principalColumn: "icao");
                });

            migrationBuilder.CreateTable(
                name: "session",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<int>(type: "INTEGER", nullable: true),
                    scenario_id = table.Column<int>(type: "INTEGER", nullable: true),
                    started_utc = table.Column<string>(type: "TEXT", nullable: true),
                    ended_utc = table.Column<string>(type: "TEXT", nullable: true),
                    difficulty = table.Column<string>(type: "TEXT", nullable: true),
                    parameters_json = table.Column<string>(type: "TEXT", nullable: true),
                    score_total = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    outcome = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_session", x => x.id);
                    table.ForeignKey(
                        name: "FK_session_scenario_scenario_id",
                        column: x => x.scenario_id,
                        principalTable: "scenario",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "metric",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    session_id = table.Column<int>(type: "INTEGER", nullable: true),
                    k = table.Column<string>(type: "TEXT", nullable: true),
                    v = table.Column<double>(type: "REAL", nullable: true),
                    t_utc = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metric", x => x.id);
                    table.ForeignKey(
                        name: "FK_metric_session_session_id",
                        column: x => x.session_id,
                        principalTable: "session",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "turn",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    session_id = table.Column<int>(type: "INTEGER", nullable: true),
                    idx = table.Column<int>(type: "INTEGER", nullable: true),
                    user_audio_path = table.Column<string>(type: "TEXT", nullable: true),
                    user_transcript = table.Column<string>(type: "TEXT", nullable: true),
                    instructor_json = table.Column<string>(type: "TEXT", nullable: true),
                    atc_json = table.Column<string>(type: "TEXT", nullable: true),
                    tts_audio_path = table.Column<string>(type: "TEXT", nullable: true),
                    verdict = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_turn", x => x.id);
                    table.ForeignKey(
                        name: "FK_turn_session_session_id",
                        column: x => x.session_id,
                        principalTable: "session",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_metric_session_id",
                table: "metric",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_runway_airport_icao",
                table: "runway",
                column: "airport_icao");

            migrationBuilder.CreateIndex(
                name: "IX_scenario_airport_icao",
                table: "scenario",
                column: "airport_icao");

            migrationBuilder.CreateIndex(
                name: "IX_session_scenario_id",
                table: "session",
                column: "scenario_id");

            migrationBuilder.CreateIndex(
                name: "IX_turn_session_id",
                table: "turn",
                column: "session_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "metric");

            migrationBuilder.DropTable(
                name: "runway");

            migrationBuilder.DropTable(
                name: "turn");

            migrationBuilder.DropTable(
                name: "session");

            migrationBuilder.DropTable(
                name: "scenario");

            migrationBuilder.DropTable(
                name: "airport");
        }
    }
}
