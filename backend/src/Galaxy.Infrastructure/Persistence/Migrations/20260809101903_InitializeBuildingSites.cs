using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Galaxy.Infrastructure.Persistence.Migrations;

public partial class InitializeBuildingSites : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "Planets"
            SET
                "BuildingSiteCapacity" =
                    CASE
                        WHEN "PlayerId" IS NOT NULL THEN 20
                        ELSE 15
                    END,
                "WarehouseLevel" =
                    CASE
                        WHEN "PlayerId" IS NOT NULL THEN 1
                        ELSE 0
                    END
            WHERE "BuildingSiteCapacity" = 0;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
