using System;
using Galaxy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Galaxy.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260812230000_AddParallelResearchStreams")]
    public partial class AddParallelResearchStreams : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "QueuedTechnology", table: "Players");
            migrationBuilder.DropColumn(name: "QueuedTechnologyLevel", table: "Players");
            migrationBuilder.DropColumn(name: "ResearchCompletesAt", table: "Players");

            migrationBuilder.CreateTable(
                name: "ResearchOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanetId = table.Column<Guid>(type: "uuid", nullable: false),
                    StreamNumber = table.Column<int>(type: "integer", nullable: false),
                    Technology = table.Column<int>(type: "integer", nullable: false),
                    TargetLevel = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletesAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResearchOrders_Planets_PlanetId",
                        column: x => x.PlanetId,
                        principalTable: "Planets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResearchOrders_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResearchOrders_PlanetId_StreamNumber",
                table: "ResearchOrders",
                columns: new[] { "PlanetId", "StreamNumber" },
                unique: true);
            migrationBuilder.CreateIndex(
                name: "IX_ResearchOrders_PlayerId_Technology",
                table: "ResearchOrders",
                columns: new[] { "PlayerId", "Technology" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ResearchOrders");

            migrationBuilder.AddColumn<int>(
                name: "QueuedTechnology",
                table: "Players",
                type: "integer",
                nullable: true);
            migrationBuilder.AddColumn<int>(
                name: "QueuedTechnologyLevel",
                table: "Players",
                type: "integer",
                nullable: true);
            migrationBuilder.AddColumn<DateTime>(
                name: "ResearchCompletesAt",
                table: "Players",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
