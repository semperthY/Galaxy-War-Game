using System.Text.Json;
using Galaxy.Application.Components;
using Galaxy.Application.ShipDesign;
using Galaxy.Domain.Entities;
using Galaxy.Domain.ShipDesign;

namespace Galaxy.Application.LivingGalaxy;

public static class FleetFactory
{
    public static Fleet Create(
        Guid playerId,
        Planet homePlanet,
        string name,
        IReadOnlyCollection<Ship> ships,
        DateTime utcNow)
    {
        if (ships.Count == 0) throw new InvalidOperationException("Select at least one reserve ship.");
        if (ships.Any(x => x.PlayerId != playerId || x.PlanetId != homePlanet.Id || x.FleetShip is not null))
            throw new InvalidOperationException("Every selected ship must be available in this planet reserve.");

        var fleet = new Fleet
        {
            Id = Guid.NewGuid(), PlayerId = playerId, HomePlanetId = homePlanet.Id,
            Name = string.IsNullOrWhiteSpace(name) ? "Новая группа" : name.Trim(),
            Status = FleetStatus.Landed, LocationType = FleetLocationType.Planet,
            GalaxyNumber = homePlanet.StarSystem.GalaxyNumber,
            SystemNumber = homePlanet.StarSystem.SystemNumber,
            Position = homePlanet.Position,
            HomeGalaxyNumber = homePlanet.StarSystem.GalaxyNumber,
            HomeSystemNumber = homePlanet.StarSystem.SystemNumber,
            HomePosition = homePlanet.Position,
            CreatedAt = utcNow, UpdatedAt = utcNow
        };

        foreach (var ship in ships)
        {
            var modules = ship.Blueprint.Modules.Select(x => new ModuleSelection(x.ComponentCode, x.Quantity)).ToList();
            var design = ShipDesignCalculator.Calculate(ship.Blueprint.HullCode, modules);
            var definitions = modules.SelectMany(x => Enumerable.Repeat(StarterComponentCatalog.Find(x.ComponentCode)!, x.Quantity)).ToList();
            var required = design.RequiredComponents.SelectMany(x => Enumerable.Repeat(StarterComponentCatalog.Find(x.ComponentCode)!, x.Quantity)).ToList();
            var snapshot = new FleetShip
            {
                Id = Guid.NewGuid(), Fleet = fleet, FleetId = fleet.Id, Ship = ship, ShipId = ship.Id,
                Name = ship.Name, BlueprintName = ship.Blueprint.Name,
                LocalSpeed = design.InSystemSpeed, InterSystemSpeed = design.InterSystemSpeed,
                CargoCapacity = design.CargoCapacity, MiningRatePerMinute = design.MiningRatePerMinute,
                ScanRange = design.ScanRange, MaxHull = design.StructuralIntegrity, Hull = design.StructuralIntegrity,
                MaxShield = design.ShieldCapacity, Shield = design.ShieldCapacity,
                LaserShieldDamage = definitions.OfType<LaserWeaponDefinition>().Sum(x => x.ShieldDamage),
                LaserHullDamage = definitions.OfType<LaserWeaponDefinition>().Sum(x => x.HullDamage),
                MissileShieldDamage = definitions.OfType<MissileWeaponDefinition>().Sum(x => x.ShieldDamage),
                MissileHullDamage = definitions.OfType<MissileWeaponDefinition>().Sum(x => x.HullDamage),
                ComponentMaterials = required.Sum(x => x.Cost.Materials),
                ComponentDeuterium = required.Sum(x => x.Cost.Deuterium),
                ComponentCodesJson = JsonSerializer.Serialize(required.Select(x => x.Code))
            };
            ship.FleetShip = snapshot;
            fleet.Ships.Add(snapshot);
        }

        fleet.FuelReserve = Math.Max(100m, fleet.Ships.Sum(x => x.CargoCapacity) * .25m);
        return fleet;
    }
}

public static class FlightRules
{
    public static bool RequiresTargetCoordinates(FlightCommandType type) =>
        type is FlightCommandType.Flight or FlightCommandType.Recon or FlightCommandType.Attack or FlightCommandType.Mine;

    public static bool HasValidTargetCoordinates(FlightCommand command) =>
        !RequiresTargetCoordinates(command.Type) ||
        command.TargetGalaxy is >= 1 &&
        command.TargetSystem is >= 1 &&
        command.TargetPosition is >= 1;

    public static int EditableSequence(Fleet fleet) =>
        fleet.Status == FleetStatus.Landed || fleet.Status == FleetStatus.Orbiting
            ? Math.Max(1, fleet.Commands.Where(x => x.Status == FlightCommandStatus.Planned).Select(x => x.Sequence).DefaultIfEmpty(1).Min())
            : fleet.CurrentCommandSequence + 1;

    public static void ReplacePlan(Fleet fleet, IReadOnlyCollection<FlightCommand> commands)
    {
        if (fleet.Status is not (FleetStatus.Landed or FleetStatus.Orbiting))
            throw new InvalidOperationException("После старта можно менять только следующую команду.");
        fleet.Commands.Clear();
        var sequence = 1;
        foreach (var command in commands)
        {
            ValidateCommand(fleet, command);
            command.Id = Guid.NewGuid(); command.Fleet = fleet; command.FleetId = fleet.Id;
            command.Sequence = sequence++; command.Status = FlightCommandStatus.Planned;
            fleet.Commands.Add(command);
        }
        if (fleet.Commands.Count == 0) throw new InvalidOperationException("Полётный лист не может быть пустым.");
        EnsureFuel(fleet);
    }

    public static void ReplaceNext(Fleet fleet, FlightCommand? replacement)
    {
        if (fleet.Status is FleetStatus.Landed or FleetStatus.Orbiting)
            throw new InvalidOperationException("До старта редактируйте весь полётный лист.");
        var sequence = fleet.CurrentCommandSequence + 1;
        var existing = fleet.Commands.SingleOrDefault(x => x.Sequence == sequence);
        if (existing is not null) fleet.Commands.Remove(existing);
        if (replacement is not null)
        {
            ValidateCommand(fleet, replacement);
            replacement.Id = Guid.NewGuid(); replacement.Fleet = fleet; replacement.FleetId = fleet.Id;
            replacement.Sequence = sequence; replacement.Status = FlightCommandStatus.Planned;
            fleet.Commands.Add(replacement);
        }
        EnsureFuel(fleet);
    }

    public static TimeSpan TravelTime(Fleet fleet, FlightCommand command)
    {
        var galaxyDelta = Math.Abs((command.TargetGalaxy ?? fleet.GalaxyNumber) - fleet.GalaxyNumber);
        var systemDelta = Math.Abs((command.TargetSystem ?? fleet.SystemNumber) - fleet.SystemNumber);
        var positionDelta = Math.Abs((command.TargetPosition ?? fleet.Position) - fleet.Position);
        var distance = Math.Max(1m, galaxyDelta * 10000m + systemDelta * 100m + positionDelta * 4m);
        var inter = galaxyDelta > 0 || systemDelta > 0;
        var speed = fleet.Ships.Select(x => inter ? x.InterSystemSpeed : x.LocalSpeed).DefaultIfEmpty(1m).Min();
        var factor = command.SpeedMode switch { FlightSpeedMode.Economy => .5m, FlightSpeedMode.Cruise => .75m, _ => 1m };
        return TimeSpan.FromSeconds((double)Math.Max(5m, decimal.Ceiling(distance * 20m / Math.Max(1m, speed * factor))));
    }

    public static decimal FuelCost(Fleet fleet, FlightCommand command)
    {
        var distance = Math.Max(1m,
            Math.Abs((command.TargetGalaxy ?? fleet.GalaxyNumber) - fleet.GalaxyNumber) * 10000m +
            Math.Abs((command.TargetSystem ?? fleet.SystemNumber) - fleet.SystemNumber) * 100m +
            Math.Abs((command.TargetPosition ?? fleet.Position) - fleet.Position) * 4m);
        var throttle = command.SpeedMode switch { FlightSpeedMode.Economy => .5m, FlightSpeedMode.Cruise => .75m, _ => 1m };
        return decimal.Ceiling(distance * Math.Max(1, fleet.Ships.Count) * throttle * throttle / 5m);
    }

    public static void Start(Fleet fleet, DateTime utcNow)
    {
        if (fleet.Status is not (FleetStatus.Landed or FleetStatus.Orbiting))
            throw new InvalidOperationException("Флот уже выполняет полётный лист.");
        var first = fleet.Commands.OrderBy(x => x.Sequence).FirstOrDefault(x => x.Status == FlightCommandStatus.Planned)
            ?? throw new InvalidOperationException("Добавьте команды в полётный лист.");
        fleet.Status = FleetStatus.Executing;
        Activate(fleet, first, utcNow);
    }

    public static void Activate(Fleet fleet, FlightCommand command, DateTime utcNow)
    {
        fleet.CurrentCommandSequence = command.Sequence;
        command.Status = FlightCommandStatus.Active;
        command.StartedAt = utcNow;
        command.CompletesAt = command.Type switch
        {
            FlightCommandType.Patrol => null,
            FlightCommandType.Mine => utcNow.Add(TravelTime(fleet, command)).AddMinutes(command.DurationMinutes <= 0
                ? (double)Math.Max(1m, decimal.Ceiling(
                    Math.Max(0m, fleet.Ships.Sum(x => x.CargoCapacity) - fleet.MaterialsCargo - fleet.DeuteriumCargo) /
                    Math.Max(1m, fleet.Ships.Sum(x => x.MiningRatePerMinute))))
                : command.DurationMinutes),
            FlightCommandType.LoadUnload => utcNow,
            _ => utcNow.Add(TravelTime(fleet, command))
        };
        fleet.Status = command.Type switch
        {
            FlightCommandType.Patrol => FleetStatus.Patrolling,
            FlightCommandType.Mine => FleetStatus.Mining,
            _ => FleetStatus.Executing
        };
        if (command.Type is FlightCommandType.Flight or FlightCommandType.Attack or FlightCommandType.Return or FlightCommandType.Recon or FlightCommandType.Mine)
            fleet.FuelReserve -= FuelCost(fleet, command);
        fleet.UpdatedAt = utcNow;
    }

    public static void FinishAndAdvance(Fleet fleet, FlightCommand command, DateTime utcNow, string outcome)
    {
        command.Status = FlightCommandStatus.Completed; command.CompletedAt = utcNow; command.Outcome = outcome;
        var next = fleet.Commands.Where(x => x.Sequence > command.Sequence && x.Status == FlightCommandStatus.Planned)
            .OrderBy(x => x.Sequence).FirstOrDefault();
        if (next is null)
        {
            fleet.Status = FleetStatus.Orbiting;
            if (command.Type == FlightCommandType.Return)
                fleet.LocationType = FleetLocationType.Planet;
            fleet.UpdatedAt = utcNow; return;
        }
        Activate(fleet, next, utcNow);
    }

    private static void EnsureFuel(Fleet fleet)
    {
        var galaxy = fleet.GalaxyNumber;
        var system = fleet.SystemNumber;
        var position = fleet.Position;
        var required = 0m;
        foreach (var command in fleet.Commands.Where(x => x.Status == FlightCommandStatus.Planned).OrderBy(x => x.Sequence))
        {
            if (command.Type == FlightCommandType.Return)
            {
                command.TargetGalaxy = fleet.HomeGalaxyNumber;
                command.TargetSystem = fleet.HomeSystemNumber;
                command.TargetPosition = fleet.HomePosition;
            }
            if (command.Type is FlightCommandType.Flight or FlightCommandType.Attack or FlightCommandType.Return or FlightCommandType.Recon or FlightCommandType.Mine)
            {
                required += FuelBetween(fleet, galaxy, system, position, command.TargetGalaxy ?? galaxy, command.TargetSystem ?? system, command.TargetPosition ?? position, command.SpeedMode);
                galaxy = command.TargetGalaxy ?? galaxy;
                system = command.TargetSystem ?? system;
                position = command.TargetPosition ?? position;
            }
        }
        required += FuelBetween(fleet, galaxy, system, position, fleet.HomeGalaxyNumber, fleet.HomeSystemNumber, fleet.HomePosition, FlightSpeedMode.Economy);
        if (required > fleet.FuelReserve)
            throw new InvalidOperationException($"Недостаточно топлива: требуется {required:0}, доступно {fleet.FuelReserve:0}.");
    }

    private static decimal FuelBetween(Fleet fleet, int fromGalaxy, int fromSystem, int fromPosition, int toGalaxy, int toSystem, int toPosition, FlightSpeedMode speedMode)
    {
        var distance = Math.Max(1m, Math.Abs(toGalaxy - fromGalaxy) * 10000m + Math.Abs(toSystem - fromSystem) * 100m + Math.Abs(toPosition - fromPosition) * 4m);
        var throttle = speedMode switch { FlightSpeedMode.Economy => .5m, FlightSpeedMode.Cruise => .75m, _ => 1m };
        return decimal.Ceiling(distance * Math.Max(1, fleet.Ships.Count) * throttle * throttle / 5m);
    }

    private static void ValidateCommand(Fleet fleet, FlightCommand command)
    {
        if (!HasValidTargetCoordinates(command))
            throw new InvalidOperationException("Координаты цели должны быть целыми числами не меньше 1.");
        if (command.Type == FlightCommandType.Attack && command.TargetFleetId is null)
            throw new InvalidOperationException("Для атаки выберите обнаруженный чужой флот.");
        if (command.Type == FlightCommandType.Recon && fleet.Ships.All(x => x.ScanRange <= 0))
            throw new InvalidOperationException("Для разведки нужен корабль со сканером.");
        if (command.Type == FlightCommandType.Mine && fleet.Ships.All(x => x.MiningRatePerMinute <= 0))
            throw new InvalidOperationException("Для добычи нужен добывающий модуль.");
        if (command.Type == FlightCommandType.Mine && command.TargetObjectId is null)
            throw new InvalidOperationException("Для добычи выберите поле или обломки на карте операций.");
    }
}

public static class FleetRefueling
{
    public static decimal Transfer(Fleet fleet, Planet planet, decimal requestedAmount)
    {
        if (fleet.Status != FleetStatus.Landed)
            throw new InvalidOperationException("Заправка доступна только после посадки флота.");
        if (fleet.HomePlanetId != planet.Id || fleet.PlayerId != planet.PlayerId)
            throw new InvalidOperationException("Заправка доступна только на домашней планете флота.");

        var amount = decimal.Floor(requestedAmount);
        if (amount <= 0)
            throw new InvalidOperationException("Укажите количество топлива не меньше 1.");
        if (planet.Deuterium < amount)
            throw new InvalidOperationException(
                $"Недостаточно дейтерия на планете: требуется {amount:0}, доступно {planet.Deuterium:0}.");

        planet.Deuterium -= amount;
        fleet.FuelReserve += amount;
        fleet.UpdatedAt = DateTime.UtcNow;
        return amount;
    }
}

public static class CombatRules
{
    public static CombatRoundResult ResolveRound(
        Fleet attacker,
        Fleet defender,
        bool attackerRetreat,
        bool defenderRetreat,
        string attackerPriority = "Weakest",
        string defenderPriority = "Weakest")
    {
        if (attackerRetreat || defenderRetreat)
            return new CombatRoundResult(attackerRetreat ? defender.Id : attacker.Id, 0, 0, "Флот вышел из боя.");

        var attackers = OrderTargets(attacker.Ships.Where(x => x.Hull > 0), defenderPriority).ToList();
        var defenders = OrderTargets(defender.Ships.Where(x => x.Hull > 0), attackerPriority).ToList();
        var attackerShots = attackers.Select(x => (x.LaserShieldDamage, x.LaserHullDamage, x.MissileShieldDamage, x.MissileHullDamage)).ToList();
        var defenderShots = defenders.Select(x => (x.LaserShieldDamage, x.LaserHullDamage, x.MissileShieldDamage, x.MissileHullDamage)).ToList();
        ApplyShots(attackerShots, defenders);
        ApplyShots(defenderShots, attackers);
        var attackerLosses = attackers.Count(x => x.Hull <= 0);
        var defenderLosses = defenders.Count(x => x.Hull <= 0);
        Guid? winner = defenders.All(x => x.Hull <= 0) ? attacker.Id : attackers.All(x => x.Hull <= 0) ? defender.Id : null;
        return new CombatRoundResult(winner, attackerLosses, defenderLosses,
            $"Потери: атакующий {attackerLosses}, защитник {defenderLosses}.");
    }

    public static DebrisYield CalculateDebris(IEnumerable<FleetShip> destroyed)
    {
        var ships = destroyed.ToList();
        return new DebrisYield(
            decimal.Floor(ships.Sum(x => x.ComponentMaterials) * .35m),
            decimal.Floor(ships.Sum(x => x.ComponentDeuterium) * .25m));
    }

    private static void ApplyShots(
        IEnumerable<(decimal laserShield, decimal laserHull, decimal missileShield, decimal missileHull)> shots,
        IReadOnlyList<FleetShip> targets)
    {
        if (targets.Count == 0) return;
        var index = 0;
        foreach (var shot in shots)
        {
            var target = targets[index++ % targets.Count];
            ApplyWeapon(target, shot.laserShield, shot.laserHull);
            ApplyWeapon(target, shot.missileShield, shot.missileHull);
        }
    }

    private static void ApplyWeapon(FleetShip target, decimal shieldDamage, decimal hullDamage)
    {
        if (target.Shield > 0)
        {
            target.Shield = Math.Max(0, target.Shield - shieldDamage);
            return;
        }
        target.Hull = Math.Max(0, target.Hull - hullDamage);
    }

    private static IOrderedEnumerable<FleetShip> OrderTargets(IEnumerable<FleetShip> ships, string priority) =>
        priority switch
        {
            "Shields" => ships.OrderByDescending(x => x.MaxShield),
            "Firepower" => ships.OrderByDescending(x => x.LaserHullDamage + x.MissileHullDamage),
            _ => ships.OrderBy(x => x.Hull + x.Shield)
        };
}

public sealed record CombatRoundResult(Guid? WinnerFleetId, int AttackerLosses, int DefenderLosses, string Summary);
public sealed record DebrisYield(decimal Materials, decimal Deuterium);

public static class DebrisRules
{
    public static void ApplyDecay(DebrisField field, DateTime utcNow)
    {
        var decayStarts = field.CreatedAt.AddHours(6);
        if (utcNow <= decayStarts || field.Materials + field.Deuterium <= 0) return;
        var reference = decayStarts > field.UpdatedAt ? decayStarts : field.UpdatedAt;
        var hours = (int)Math.Floor((utcNow - reference).TotalHours);
        if (hours <= 0) return;
        var factor = (decimal)Math.Pow(.98, hours);
        field.Materials *= factor; field.Deuterium *= factor; field.UpdatedAt = utcNow;
    }

    public static string AddComponents(string json, IEnumerable<string> componentCodes)
    {
        var values = JsonSerializer.Deserialize<List<string>>(json) ?? [];
        values.AddRange(componentCodes);
        return JsonSerializer.Serialize(values);
    }

    public static string SalvageComponents(IEnumerable<FleetShip> destroyed)
    {
        var recovered = new List<string>();
        foreach (var ship in destroyed.OrderBy(x => x.Id))
        {
            var codes = JsonSerializer.Deserialize<List<string>>(ship.ComponentCodesJson) ?? [];
            for (var index = 0; index < codes.Count; index++)
                if ((index + ship.Id.ToByteArray()[0]) % 20 == 0) recovered.Add(codes[index]);
        }
        return JsonSerializer.Serialize(recovered);
    }
}
