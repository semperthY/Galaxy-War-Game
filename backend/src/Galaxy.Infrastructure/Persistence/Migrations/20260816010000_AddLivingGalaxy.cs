using Galaxy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Galaxy.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260816010000_AddLivingGalaxy")]
public sealed class AddLivingGalaxy : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "Planets" ADD COLUMN "ShipyardLevel" integer NOT NULL DEFAULT 0;

            CREATE TABLE "ResourceFields" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "StarSystemId" uuid NOT NULL REFERENCES "StarSystems" ("Id") ON DELETE CASCADE,
                "Name" character varying(100) NOT NULL,
                "Position" integer NOT NULL,
                "Type" integer NOT NULL,
                "Materials" numeric(20,4) NOT NULL,
                "Deuterium" numeric(20,4) NOT NULL,
                "MaxMaterials" numeric(20,4) NOT NULL,
                "MaxDeuterium" numeric(20,4) NOT NULL,
                "RegenPerHour" numeric(20,4) NOT NULL,
                "ThroughputPerHour" numeric(20,4) NOT NULL,
                "Threat" integer NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL
            );
            CREATE UNIQUE INDEX "IX_ResourceFields_StarSystemId_Position" ON "ResourceFields" ("StarSystemId", "Position");

            CREATE TABLE "PirateCells" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "StarSystemId" uuid NOT NULL REFERENCES "StarSystems" ("Id") ON DELETE CASCADE,
                "State" integer NOT NULL,
                "Threat" integer NOT NULL,
                "Materials" numeric(20,4) NOT NULL,
                "Deuterium" numeric(20,4) NOT NULL,
                "LastActedAt" timestamp with time zone NOT NULL
            );
            CREATE UNIQUE INDEX "IX_PirateCells_StarSystemId" ON "PirateCells" ("StarSystemId");

            CREATE TABLE "Fleets" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "PlayerId" uuid NULL REFERENCES "Players" ("Id") ON DELETE CASCADE,
                "HomePlanetId" uuid NULL REFERENCES "Planets" ("Id") ON DELETE SET NULL,
                "PirateCellId" uuid NULL REFERENCES "PirateCells" ("Id") ON DELETE CASCADE,
                "HomeGalaxyNumber" integer NOT NULL,
                "HomeSystemNumber" integer NOT NULL,
                "HomePosition" integer NOT NULL,
                "Name" character varying(100) NOT NULL,
                "IsPirate" boolean NOT NULL,
                "Status" integer NOT NULL,
                "LocationType" integer NOT NULL,
                "GalaxyNumber" integer NOT NULL,
                "SystemNumber" integer NOT NULL,
                "Position" integer NOT NULL,
                "MaterialsCargo" numeric(20,4) NOT NULL,
                "DeuteriumCargo" numeric(20,4) NOT NULL,
                "FuelReserve" numeric(20,4) NOT NULL,
                "CurrentCommandSequence" integer NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL
            );
            CREATE INDEX "IX_Fleets_PlayerId" ON "Fleets" ("PlayerId");
            CREATE INDEX "IX_Fleets_HomePlanetId" ON "Fleets" ("HomePlanetId");
            CREATE INDEX "IX_Fleets_PirateCellId" ON "Fleets" ("PirateCellId");
            CREATE INDEX "IX_Fleets_GalaxyNumber_SystemNumber_Position" ON "Fleets" ("GalaxyNumber", "SystemNumber", "Position");

            CREATE TABLE "FleetShips" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "FleetId" uuid NOT NULL REFERENCES "Fleets" ("Id") ON DELETE CASCADE,
                "ShipId" uuid NULL REFERENCES "Ships" ("Id") ON DELETE CASCADE,
                "Name" character varying(100) NOT NULL,
                "BlueprintName" character varying(100) NOT NULL,
                "LocalSpeed" numeric(20,4) NOT NULL,
                "InterSystemSpeed" numeric(20,4) NOT NULL,
                "CargoCapacity" numeric(20,4) NOT NULL,
                "MiningRatePerMinute" numeric(20,4) NOT NULL,
                "ScanRange" numeric(20,4) NOT NULL,
                "MaxHull" numeric(20,4) NOT NULL,
                "Hull" numeric(20,4) NOT NULL,
                "MaxShield" numeric(20,4) NOT NULL,
                "Shield" numeric(20,4) NOT NULL,
                "LaserShieldDamage" numeric(20,4) NOT NULL,
                "LaserHullDamage" numeric(20,4) NOT NULL,
                "MissileShieldDamage" numeric(20,4) NOT NULL,
                "MissileHullDamage" numeric(20,4) NOT NULL,
                "ComponentMaterials" numeric(20,4) NOT NULL,
                "ComponentDeuterium" numeric(20,4) NOT NULL,
                "ComponentCodesJson" jsonb NOT NULL DEFAULT '[]'::jsonb
            );
            CREATE INDEX "IX_FleetShips_FleetId" ON "FleetShips" ("FleetId");
            CREATE UNIQUE INDEX "IX_FleetShips_ShipId" ON "FleetShips" ("ShipId");

            CREATE TABLE "FlightCommands" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "FleetId" uuid NOT NULL REFERENCES "Fleets" ("Id") ON DELETE CASCADE,
                "Sequence" integer NOT NULL,
                "Type" integer NOT NULL,
                "Status" integer NOT NULL,
                "SpeedMode" integer NOT NULL,
                "TargetGalaxy" integer NULL,
                "TargetSystem" integer NULL,
                "TargetPosition" integer NULL,
                "TargetFleetId" uuid NULL,
                "TargetObjectId" uuid NULL,
                "DurationMinutes" integer NOT NULL,
                "ManifestMaterials" numeric(20,4) NOT NULL,
                "ManifestDeuterium" numeric(20,4) NOT NULL,
                "StartedAt" timestamp with time zone NULL,
                "CompletesAt" timestamp with time zone NULL,
                "CompletedAt" timestamp with time zone NULL,
                "Outcome" character varying(400) NULL
            );
            CREATE UNIQUE INDEX "IX_FlightCommands_FleetId_Sequence" ON "FlightCommands" ("FleetId", "Sequence");

            CREATE TABLE "DebrisFields" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "GalaxyNumber" integer NOT NULL,
                "SystemNumber" integer NOT NULL,
                "Position" integer NOT NULL,
                "Materials" numeric(20,4) NOT NULL,
                "Deuterium" numeric(20,4) NOT NULL,
                "ExclusivePlayerId" uuid NULL,
                "ExclusiveUntil" timestamp with time zone NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL,
                "ExpiresAt" timestamp with time zone NOT NULL,
                "ComponentsJson" jsonb NOT NULL DEFAULT '[]'::jsonb
            );
            CREATE INDEX "IX_DebrisFields_GalaxyNumber_SystemNumber_Position" ON "DebrisFields" ("GalaxyNumber", "SystemNumber", "Position");

            CREATE TABLE "Battles" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "AttackerFleetId" uuid NOT NULL,
                "DefenderFleetId" uuid NOT NULL,
                "Status" integer NOT NULL,
                "Round" integer NOT NULL,
                "OrderDeadline" timestamp with time zone NOT NULL,
                "ResolveAt" timestamp with time zone NOT NULL,
                "WinnerFleetId" uuid NULL,
                "ReportJson" jsonb NOT NULL DEFAULT '[]'::jsonb,
                "CreatedAt" timestamp with time zone NOT NULL,
                "CompletedAt" timestamp with time zone NULL
            );
            CREATE INDEX "IX_Battles_AttackerFleetId" ON "Battles" ("AttackerFleetId");
            CREATE INDEX "IX_Battles_DefenderFleetId" ON "Battles" ("DefenderFleetId");

            CREATE TABLE "BattleOrders" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "BattleId" uuid NOT NULL REFERENCES "Battles" ("Id") ON DELETE CASCADE,
                "FleetId" uuid NOT NULL,
                "Round" integer NOT NULL,
                "TargetPriority" character varying(32) NOT NULL,
                "Retreat" boolean NOT NULL,
                "SubmittedAt" timestamp with time zone NOT NULL
            );
            CREATE UNIQUE INDEX "IX_BattleOrders_BattleId_FleetId_Round" ON "BattleOrders" ("BattleId", "FleetId", "Round");

            CREATE TABLE "ShipServiceOrders" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "FleetShipId" uuid NOT NULL REFERENCES "FleetShips" ("Id") ON DELETE CASCADE,
                "PlanetId" uuid NOT NULL REFERENCES "Planets" ("Id") ON DELETE CASCADE,
                "Type" integer NOT NULL,
                "MaterialsCost" numeric(20,4) NOT NULL,
                "DeuteriumCost" numeric(20,4) NOT NULL,
                "StartedAt" timestamp with time zone NOT NULL,
                "CompletesAt" timestamp with time zone NOT NULL
            );
            CREATE UNIQUE INDEX "IX_ShipServiceOrders_FleetShipId" ON "ShipServiceOrders" ("FleetShipId");
            CREATE INDEX "IX_ShipServiceOrders_PlanetId" ON "ShipServiceOrders" ("PlanetId");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS "ShipServiceOrders";
            DROP TABLE IF EXISTS "BattleOrders";
            DROP TABLE IF EXISTS "Battles";
            DROP TABLE IF EXISTS "DebrisFields";
            DROP TABLE IF EXISTS "FlightCommands";
            DROP TABLE IF EXISTS "FleetShips";
            DROP TABLE IF EXISTS "Fleets";
            DROP TABLE IF EXISTS "PirateCells";
            DROP TABLE IF EXISTS "ResourceFields";
            ALTER TABLE "Planets" DROP COLUMN IF EXISTS "ShipyardLevel";
            """);
    }
}
