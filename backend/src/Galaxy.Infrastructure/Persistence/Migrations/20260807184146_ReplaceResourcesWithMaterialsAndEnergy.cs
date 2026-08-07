using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Galaxy.Infrastructure.Persistence.Migrations;

public partial class ReplaceResourcesWithMaterialsAndEnergy : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Crystal",
            table: "Planets");

        migrationBuilder.RenameColumn(
            name: "Metal",
            table: "Planets",
            newName: "Materials");

        migrationBuilder.RenameColumn(
            name: "MetalMineLevel",
            table: "Planets",
            newName: "MaterialsExtractorLevel");

        migrationBuilder.RenameColumn(
            name: "DeuteriumMineLevel",
            table: "Planets",
            newName: "DeuteriumExtractorLevel");

        migrationBuilder.RenameColumn(
            name: "CrystalMineLevel",
            table: "Planets",
            newName: "PowerPlantLevel");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "Materials",
            table: "Planets",
            newName: "Metal");

        migrationBuilder.RenameColumn(
            name: "MaterialsExtractorLevel",
            table: "Planets",
            newName: "MetalMineLevel");

        migrationBuilder.RenameColumn(
            name: "DeuteriumExtractorLevel",
            table: "Planets",
            newName: "DeuteriumMineLevel");

        migrationBuilder.RenameColumn(
            name: "PowerPlantLevel",
            table: "Planets",
            newName: "CrystalMineLevel");

        migrationBuilder.AddColumn<decimal>(
            name: "Crystal",
            table: "Planets",
            type: "numeric(20,4)",
            precision: 20,
            scale: 4,
            nullable: false,
            defaultValue: 0m);
    }
}
