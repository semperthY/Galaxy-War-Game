using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Galaxy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanetResourcesAndCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Planets_Players_PlayerId",
                table: "Planets");

            migrationBuilder.DropIndex(
                name: "IX_Planets_StarSystemId",
                table: "Planets");

            migrationBuilder.AddColumn<int>(
                name: "GalaxyNumber",
                table: "StarSystems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SystemNumber",
                table: "StarSystems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "PlayerId",
                table: "Planets",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<long>(
                name: "Crystal",
                table: "Planets",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Deuterium",
                table: "Planets",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Metal",
                table: "Planets",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "Planets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_StarSystems_GalaxyNumber_SystemNumber",
                table: "StarSystems",
                columns: new[] { "GalaxyNumber", "SystemNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Planets_StarSystemId_Position",
                table: "Planets",
                columns: new[] { "StarSystemId", "Position" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Planets_Players_PlayerId",
                table: "Planets",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Planets_Players_PlayerId",
                table: "Planets");

            migrationBuilder.DropIndex(
                name: "IX_StarSystems_GalaxyNumber_SystemNumber",
                table: "StarSystems");

            migrationBuilder.DropIndex(
                name: "IX_Planets_StarSystemId_Position",
                table: "Planets");

            migrationBuilder.DropColumn(
                name: "GalaxyNumber",
                table: "StarSystems");

            migrationBuilder.DropColumn(
                name: "SystemNumber",
                table: "StarSystems");

            migrationBuilder.DropColumn(
                name: "Crystal",
                table: "Planets");

            migrationBuilder.DropColumn(
                name: "Deuterium",
                table: "Planets");

            migrationBuilder.DropColumn(
                name: "Metal",
                table: "Planets");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "Planets");

            migrationBuilder.AlterColumn<Guid>(
                name: "PlayerId",
                table: "Planets",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Planets_StarSystemId",
                table: "Planets",
                column: "StarSystemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Planets_Players_PlayerId",
                table: "Planets",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
