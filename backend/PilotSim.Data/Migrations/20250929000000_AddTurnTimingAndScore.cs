using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PilotSim.Data.Migrations
{
    public partial class AddTurnTimingAndScore : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "started_utc",
                table: "turn",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "stt_ms",
                table: "turn",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "instructor_ms",
                table: "turn",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "atc_ms",
                table: "turn",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tts_ms",
                table: "turn",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "total_ms",
                table: "turn",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "score_delta",
                table: "turn",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "blocked",
                table: "turn",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "started_utc", table: "turn");
            migrationBuilder.DropColumn(name: "stt_ms", table: "turn");
            migrationBuilder.DropColumn(name: "instructor_ms", table: "turn");
            migrationBuilder.DropColumn(name: "atc_ms", table: "turn");
            migrationBuilder.DropColumn(name: "tts_ms", table: "turn");
            migrationBuilder.DropColumn(name: "total_ms", table: "turn");
            migrationBuilder.DropColumn(name: "score_delta", table: "turn");
            migrationBuilder.DropColumn(name: "blocked", table: "turn");
        }
    }
}
