using Galaxy.Application.LivingGalaxy;
using Galaxy.Domain.Entities;

namespace Galaxy.Tests;

public class LivingGalaxyRulesTests
{
    [Fact]
    public void ReplaceNext_ChangesOnlyImmediatelyFollowingCommand()
    {
        var fleet = CreateFleet();
        fleet.Status = FleetStatus.Executing;
        fleet.CurrentCommandSequence = 1;
        fleet.Commands.Add(new FlightCommand { Id = Guid.NewGuid(), Fleet = fleet, FleetId = fleet.Id, Sequence = 1, Type = FlightCommandType.Flight, Status = FlightCommandStatus.Active });
        fleet.Commands.Add(new FlightCommand { Id = Guid.NewGuid(), Fleet = fleet, FleetId = fleet.Id, Sequence = 2, Type = FlightCommandType.Patrol, Status = FlightCommandStatus.Planned });
        fleet.Commands.Add(new FlightCommand { Id = Guid.NewGuid(), Fleet = fleet, FleetId = fleet.Id, Sequence = 3, Type = FlightCommandType.Return, Status = FlightCommandStatus.Planned });

        FlightRules.ReplaceNext(fleet, new FlightCommand { Type = FlightCommandType.Return, SpeedMode = FlightSpeedMode.Economy });

        Assert.Equal(FlightCommandType.Return, fleet.Commands.Single(x => x.Sequence == 2).Type);
        Assert.Equal(FlightCommandType.Return, fleet.Commands.Single(x => x.Sequence == 3).Type);
        Assert.Equal(1, fleet.CurrentCommandSequence);
    }

    [Fact]
    public void FinishWithoutNextCommand_LeavesFleetVulnerableInOrbit()
    {
        var fleet = CreateFleet();
        fleet.Status = FleetStatus.Executing;
        var command = new FlightCommand { Id = Guid.NewGuid(), Fleet = fleet, FleetId = fleet.Id, Sequence = 1, Type = FlightCommandType.Return, Status = FlightCommandStatus.Active };
        fleet.Commands.Add(command);

        FlightRules.FinishAndAdvance(fleet, command, DateTime.UtcNow, "done");

        Assert.Equal(FleetStatus.Orbiting, fleet.Status);
        Assert.Equal(FleetLocationType.Planet, fleet.LocationType);
    }

    [Fact]
    public void Combat_IsSimultaneousAndDoesNotOverflowSingleShotThroughShield()
    {
        var attacker = CreateFleet();
        var defender = CreateFleet();
        attacker.Ships.Single().LaserShieldDamage = 100;
        attacker.Ships.Single().LaserHullDamage = 100;
        defender.Ships.Single().LaserShieldDamage = 100;
        defender.Ships.Single().LaserHullDamage = 100;
        attacker.Ships.Single().Shield = defender.Ships.Single().Shield = 10;
        attacker.Ships.Single().Hull = defender.Ships.Single().Hull = 50;

        var result = CombatRules.ResolveRound(attacker, defender, false, false);

        Assert.Null(result.WinnerFleetId);
        Assert.Equal(0m, attacker.Ships.Single().Shield);
        Assert.Equal(50m, attacker.Ships.Single().Hull);
        Assert.Equal(0m, defender.Ships.Single().Shield);
        Assert.Equal(50m, defender.Ships.Single().Hull);
    }

    [Fact]
    public void Debris_DoesNotDecayForFirstSixHours()
    {
        var created = new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc);
        var field = new DebrisField { CreatedAt = created, UpdatedAt = created, Materials = 1000, Deuterium = 100 };
        DebrisRules.ApplyDecay(field, created.AddHours(5));
        Assert.Equal(1000m, field.Materials);
        DebrisRules.ApplyDecay(field, created.AddHours(8));
        Assert.InRange(field.Materials, 960.39m, 960.41m);
    }

    [Fact]
    public void Shipyard_IsARealBuildingWithOwnCostAndSite()
    {
        var planet = new Planet { Materials = 1000, Deuterium = 1000, BuildingSiteCapacity = 1, ResourcesUpdatedAt = DateTime.UtcNow };
        var result = Galaxy.Application.Economy.BuildingService.Start(planet, BuildingType.Shipyard, DateTime.UtcNow);
        Assert.Equal(450m, result.Cost.Materials);
        Assert.Equal(120m, result.Cost.Deuterium);
    }

    [Fact]
    public void Refueling_TransfersPlanetDeuteriumToLandedFleet()
    {
        var playerId = Guid.NewGuid();
        var planet = new Planet { Id = Guid.NewGuid(), PlayerId = playerId, Deuterium = 2500m };
        var fleet = CreateFleet();
        fleet.PlayerId = playerId;
        fleet.HomePlanetId = planet.Id;
        fleet.FuelReserve = 450m;

        var transferred = FleetRefueling.Transfer(fleet, planet, 1000m);

        Assert.Equal(1000m, transferred);
        Assert.Equal(1450m, fleet.FuelReserve);
        Assert.Equal(1500m, planet.Deuterium);
    }

    [Fact]
    public void Refueling_RejectsFleetThatIsNotLanded()
    {
        var playerId = Guid.NewGuid();
        var planet = new Planet { Id = Guid.NewGuid(), PlayerId = playerId, Deuterium = 2500m };
        var fleet = CreateFleet();
        fleet.PlayerId = playerId;
        fleet.HomePlanetId = planet.Id;
        fleet.Status = FleetStatus.Orbiting;

        var error = Assert.Throws<InvalidOperationException>(() =>
            FleetRefueling.Transfer(fleet, planet, 1000m));

        Assert.Equal("Заправка доступна только после посадки флота.", error.Message);
        Assert.Equal(2500m, planet.Deuterium);
    }

    private static Fleet CreateFleet()
    {
        var fleet = new Fleet { Id = Guid.NewGuid(), PlayerId = Guid.NewGuid(), Name = "Test", Status = FleetStatus.Landed, LocationType = FleetLocationType.Planet, FuelReserve = 1000 };
        fleet.Ships.Add(new FleetShip { Id = Guid.NewGuid(), Fleet = fleet, FleetId = fleet.Id, Name = "Ship", BlueprintName = "Test", LocalSpeed = 100, InterSystemSpeed = 80, MaxHull = 50, Hull = 50, MaxShield = 10, Shield = 10 });
        return fleet;
    }
}
