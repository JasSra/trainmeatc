using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PilotSim.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGAAirportsAndAirspace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "airport",
                type: "TEXT",
                nullable: false,
                defaultValue: "Major");

            migrationBuilder.AddColumn<int>(
                name: "elevation_ft",
                table: "airport",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fuel_types",
                table: "airport",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "has_fuel",
                table: "airport",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "has_maintenance",
                table: "airport",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "operating_hours",
                table: "airport",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "airspace",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    type = table.Column<string>(type: "TEXT", nullable: false),
                    @class = table.Column<string>(name: "class", type: "TEXT", nullable: false),
                    lower_altitude = table.Column<int>(type: "INTEGER", nullable: true),
                    upper_altitude = table.Column<int>(type: "INTEGER", nullable: true),
                    frequency = table.Column<string>(type: "TEXT", nullable: true),
                    operating_hours = table.Column<string>(type: "TEXT", nullable: true),
                    restrictions = table.Column<string>(type: "TEXT", nullable: true),
                    boundary_json = table.Column<string>(type: "TEXT", nullable: true),
                    center_lat = table.Column<double>(type: "REAL", nullable: true),
                    center_lon = table.Column<double>(type: "REAL", nullable: true),
                    radius_nm = table.Column<double>(type: "REAL", nullable: true),
                    associated_airport = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airspace", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "airspace_notice",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    airspace_id = table.Column<int>(type: "INTEGER", nullable: false),
                    type = table.Column<string>(type: "TEXT", nullable: false),
                    title = table.Column<string>(type: "TEXT", nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: false),
                    effective_from = table.Column<DateTime>(type: "TEXT", nullable: true),
                    effective_to = table.Column<DateTime>(type: "TEXT", nullable: true),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airspace_notice", x => x.id);
                    table.ForeignKey(
                        name: "FK_airspace_notice_airspace_airspace_id",
                        column: x => x.airspace_id,
                        principalTable: "airspace",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_airspace_notice_airspace_id",
                table: "airspace_notice",
                column: "airspace_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "airspace_notice");

            migrationBuilder.DropTable(
                name: "airspace");

            migrationBuilder.DropColumn(
                name: "category",
                table: "airport");

            migrationBuilder.DropColumn(
                name: "elevation_ft",
                table: "airport");

            migrationBuilder.DropColumn(
                name: "fuel_types",
                table: "airport");

            migrationBuilder.DropColumn(
                name: "has_fuel",
                table: "airport");

            migrationBuilder.DropColumn(
                name: "has_maintenance",
                table: "airport");

            migrationBuilder.DropColumn(
                name: "operating_hours",
                table: "airport");
        }
    }
}
