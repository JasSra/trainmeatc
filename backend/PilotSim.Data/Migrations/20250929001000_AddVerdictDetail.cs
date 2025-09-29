using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PilotSim.Data.Migrations;

public partial class AddVerdictDetail : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "verdict_detail",
            columns: table => new
            {
                id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                turn_id = table.Column<int>(nullable: false),
                code = table.Column<string>(nullable: false),
                category = table.Column<string>(nullable: false),
                severity = table.Column<string>(nullable: false),
                weight = table.Column<double>(nullable: true),
                score = table.Column<double>(nullable: true),
                delta = table.Column<double>(nullable: true),
                detail = table.Column<string>(nullable: true),
                rubric_version = table.Column<string>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_verdict_detail", x => x.id);
                table.ForeignKey(
                    name: "FK_verdict_detail_turn_turn_id",
                    column: x => x.turn_id,
                    principalTable: "turn",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_verdict_detail_turn_id",
            table: "verdict_detail",
            column: "turn_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "verdict_detail");
    }
}
