using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Galaxy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShipAssembly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssemblyComplexLevel",
                table: "Planets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ShipAssemblyOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipBlueprintId = table.Column<Guid>(type: "uuid", nullable: false),
                    QueuePosition = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletesAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipAssemblyOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipAssemblyOrders_Planets_PlanetId",
                        column: x => x.PlanetId,
                        principalTable: "Planets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShipAssemblyOrders_ShipBlueprints_ShipBlueprintId",
                        column: x => x.ShipBlueprintId,
                        principalTable: "ShipBlueprints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Ships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipBlueprintId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ships_Planets_PlanetId",
                        column: x => x.PlanetId,
                        principalTable: "Planets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Ships_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Ships_ShipBlueprints_ShipBlueprintId",
                        column: x => x.ShipBlueprintId,
                        principalTable: "ShipBlueprints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShipAssemblyOrders_PlanetId_QueuePosition",
                table: "ShipAssemblyOrders",
                columns: new[] { "PlanetId", "QueuePosition" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShipAssemblyOrders_ShipBlueprintId",
                table: "ShipAssemblyOrders",
                column: "ShipBlueprintId");

            migrationBuilder.CreateIndex(
                name: "IX_Ships_PlanetId",
                table: "Ships",
                column: "PlanetId");

            migrationBuilder.CreateIndex(
                name: "IX_Ships_PlayerId",
                table: "Ships",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Ships_ShipBlueprintId",
                table: "Ships",
                column: "ShipBlueprintId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShipAssemblyOrders");

            migrationBuilder.DropTable(
                name: "Ships");

            migrationBuilder.DropColumn(
                name: "AssemblyComplexLevel",
                table: "Planets");
        }
    }
}
