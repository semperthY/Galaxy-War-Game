using System;
using Galaxy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Galaxy.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260812120000_AddColonizationOperations")]
    public partial class AddColonizationOperations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Race",
                table: "Players",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1);

            migrationBuilder.CreateTable(
                name: "ColonizationOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourcePlanetId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetPlanetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumedShipId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BlueprintName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BlueprintVersion = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletesAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ColonizationOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ColonizationOperations_Planets_TargetPlanetId",
                        column: x => x.TargetPlanetId,
                        principalTable: "Planets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ColonizationOperations_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ColonizationOperations_ConsumedShipId",
                table: "ColonizationOperations",
                column: "ConsumedShipId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ColonizationOperations_PlayerId",
                table: "ColonizationOperations",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ColonizationOperations_TargetPlanetId",
                table: "ColonizationOperations",
                column: "TargetPlanetId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ColonizationOperations");

            migrationBuilder.AlterColumn<int>(
                name: "Race",
                table: "Players",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
