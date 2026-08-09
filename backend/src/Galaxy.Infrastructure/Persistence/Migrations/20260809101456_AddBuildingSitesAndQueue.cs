using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Galaxy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBuildingSitesAndQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BuildingCompletesAt",
                table: "Planets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BuildingSiteCapacity",
                table: "Planets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QueuedBuilding",
                table: "Planets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QueuedBuildingLevel",
                table: "Planets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarehouseLevel",
                table: "Planets",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuildingCompletesAt",
                table: "Planets");

            migrationBuilder.DropColumn(
                name: "BuildingSiteCapacity",
                table: "Planets");

            migrationBuilder.DropColumn(
                name: "QueuedBuilding",
                table: "Planets");

            migrationBuilder.DropColumn(
                name: "QueuedBuildingLevel",
                table: "Planets");

            migrationBuilder.DropColumn(
                name: "WarehouseLevel",
                table: "Planets");
        }
    }
}

