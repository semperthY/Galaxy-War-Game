using Galaxy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Galaxy.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260819070000_AddGameEvents")]
public sealed class AddGameEvents : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE "GameEvents" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "PlayerId" uuid NOT NULL REFERENCES "Players" ("Id") ON DELETE CASCADE,
                "Type" integer NOT NULL,
                "Title" character varying(140) NOT NULL,
                "Body" character varying(600) NOT NULL,
                "DataJson" jsonb NOT NULL,
                "SourceCommandId" uuid NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "ReadAt" timestamp with time zone NULL
            );
            CREATE INDEX "IX_GameEvents_PlayerId_CreatedAt" ON "GameEvents" ("PlayerId", "CreatedAt");
            CREATE UNIQUE INDEX "IX_GameEvents_SourceCommandId" ON "GameEvents" ("SourceCommandId");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "GameEvents");
    }
}
