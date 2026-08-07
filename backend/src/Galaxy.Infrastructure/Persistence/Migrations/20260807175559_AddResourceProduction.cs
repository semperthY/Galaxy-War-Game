using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Galaxy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceProduction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Metal",
                table: "Planets",
                type: "numeric(20,4)",
                precision: 20,
                scale: 4,
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<decimal>(
                name: "Deuterium",
                table: "Planets",
                type: "numeric(20,4)",
                precision: 20,
                scale: 4,
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<decimal>(
                name: "Crystal",
                table: "Planets",
                type: "numeric(20,4)",
                precision: 20,
                scale: 4,
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<int>(
                name: "CrystalMineLevel",
                table: "Planets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DeuteriumMineLevel",
                table: "Planets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MetalMineLevel",
                table: "Planets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResourcesUpdatedAt",
                table: "Planets",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CrystalMineLevel",
                table: "Planets");

            migrationBuilder.DropColumn(
                name: "DeuteriumMineLevel",
                table: "Planets");

            migrationBuilder.DropColumn(
                name: "MetalMineLevel",
                table: "Planets");

            migrationBuilder.DropColumn(
                name: "ResourcesUpdatedAt",
                table: "Planets");

            migrationBuilder.AlterColumn<long>(
                name: "Metal",
                table: "Planets",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,4)",
                oldPrecision: 20,
                oldScale: 4);

            migrationBuilder.AlterColumn<long>(
                name: "Deuterium",
                table: "Planets",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,4)",
                oldPrecision: 20,
                oldScale: 4);

            migrationBuilder.AlterColumn<long>(
                name: "Crystal",
                table: "Planets",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,4)",
                oldPrecision: 20,
                oldScale: 4);
        }
    }
}
