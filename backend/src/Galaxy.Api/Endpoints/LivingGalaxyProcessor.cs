using System.Text.Json;
using Galaxy.Application.LivingGalaxy;
using Galaxy.Domain.Entities;
using Galaxy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Galaxy.Api.Endpoints;

internal static class LivingGalaxyProcessor
{
    private static readonly SemaphoreSlim ProcessGate = new(1, 1);
    private static readonly JsonSerializerOptions EventJsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task EnsureWorldAsync(ApplicationDbContext db, DateTime now, CancellationToken token)
    {
        var systems = await db.StarSystems.ToListAsync(token);
        var existingFieldSystems = await db.ResourceFields.Select(x => x.StarSystemId).Distinct().ToListAsync(token);
        var existingPirateSystems = await db.PirateCells.Select(x => x.StarSystemId).ToListAsync(token);
        foreach (var system in systems)
        {
            if (!existingFieldSystems.Contains(system.Id))
            {
                var count = 4 + system.SystemNumber % 3;
                for (var i = 0; i < count; i++) db.ResourceFields.Add(new ResourceField
                {
                    Id = Guid.NewGuid(), StarSystemId = system.Id, Name = $"{(i % 2 == 0 ? "Пояс" : "Облако")} {system.SystemNumber}-{i + 1}",
                    Position = 101 + i, Type = i % 3 == 0 ? ResourceFieldType.IceCloud : i % 3 == 1 ? ResourceFieldType.AsteroidBelt : ResourceFieldType.MixedCluster,
                    Materials = 12000 + i * 2500, Deuterium = 5000 + i * 1500, MaxMaterials = 24000 + i * 5000,
                    MaxDeuterium = 10000 + i * 3000, RegenPerHour = 180 + i * 20, ThroughputPerHour = 900 + i * 100,
                    Threat = 1 + (system.SystemNumber + i) % 5, UpdatedAt = now
                });
            }
            if (!existingPirateSystems.Contains(system.Id))
            {
                var cell = new PirateCell { Id = Guid.NewGuid(), StarSystemId = system.Id, State = PirateCellState.Scouting, Threat = 1 + system.SystemNumber % 4, Materials = 2000, Deuterium = 600, LastActedAt = now };
                var fleet = new Fleet { Id = Guid.NewGuid(), PirateCellId = cell.Id, Name = $"Корсары {system.Name}", IsPirate = true, Status = FleetStatus.Patrolling, LocationType = FleetLocationType.DeepSpace, GalaxyNumber = system.GalaxyNumber, SystemNumber = system.SystemNumber, Position = 90 + system.SystemNumber % 8, FuelReserve = 1000, CreatedAt = now, UpdatedAt = now };
                fleet.Ships.Add(new FleetShip { Id = Guid.NewGuid(), Fleet = fleet, FleetId = fleet.Id, Name = "Пиратский рейдер", BlueprintName = "Рейдер", LocalSpeed = 90, InterSystemSpeed = 55, CargoCapacity = 80, ScanRange = 25, MaxHull = 120, Hull = 120, MaxShield = 30, Shield = 30, LaserShieldDamage = 12, LaserHullDamage = 5, MissileShieldDamage = 6, MissileHullDamage = 16, ComponentMaterials = 900, ComponentDeuterium = 220 });
                db.PirateCells.Add(cell); db.Fleets.Add(fleet);
            }
        }
        if (db.ChangeTracker.HasChanges()) await db.SaveChangesAsync(token);
    }

    public static async Task ProcessAsync(ApplicationDbContext db, DateTime now, CancellationToken token)
    {
        await ProcessGate.WaitAsync(token);
        try { await ProcessCoreAsync(db, now, token); }
        finally { ProcessGate.Release(); }
    }

    public static async Task CreateAttackAlertAsync(
        ApplicationDbContext db,
        Fleet attacker,
        DateTime now,
        CancellationToken token)
    {
        var command = attacker.Commands.SingleOrDefault(x =>
            x.Status == FlightCommandStatus.Active &&
            x.Type == FlightCommandType.Attack &&
            x.TargetFleetId != null);
        if (command is null ||
            await db.GameEvents.AnyAsync(x => x.SourceCommandId == command.Id, token)) return;

        var target = await db.Fleets.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == command.TargetFleetId && x.PlayerId != null,
            token);
        if (target?.PlayerId is not Guid defenderId || defenderId == attacker.PlayerId) return;

        db.GameEvents.Add(new GameEvent
        {
            Id = Guid.NewGuid(),
            PlayerId = defenderId,
            Type = GameEventType.IncomingAttack,
            Title = "Тревога: обнаружена атака",
            Body = $"Флот «{attacker.Name}» направляется к флоту «{target.Name}».",
            SourceCommandId = command.Id,
            CreatedAt = now,
            DataJson = JsonSerializer.Serialize(new
            {
                attackerFleetId = attacker.Id,
                attackerName = attacker.Name,
                targetFleetId = target.Id,
                targetName = target.Name,
                galaxy = command.TargetGalaxy,
                system = command.TargetSystem,
                position = command.TargetPosition,
                arrivesAt = command.CompletesAt
            }, EventJsonOptions)
        });
    }

    private static async Task ProcessCoreAsync(ApplicationDbContext db, DateTime now, CancellationToken token)
    {
        await EnsureWorldAsync(db, now, token);
        await EnsureAttackAlertsAsync(db, now, token);
        var services = await db.ShipServiceOrders.Where(x => x.CompletesAt <= now).ToListAsync(token);
        if (services.Count > 0)
        {
            var shipIds = services.Select(x => x.FleetShipId).ToList();
            var ships = await db.FleetShips.Where(x => shipIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, token);
            foreach (var order in services) if (ships.TryGetValue(order.FleetShipId, out var ship))
            {
                if (order.Type == ShipServiceType.ShieldRecharge) ship.Shield = ship.MaxShield; else ship.Hull = ship.MaxHull;
                db.ShipServiceOrders.Remove(order);
            }
        }

        var dueFleets = await db.Fleets.Where(x => !x.IsPirate).Include(x => x.Ships)
            .Include(x => x.Commands).Where(x => x.Commands.Any(c =>
                c.Status == FlightCommandStatus.Active &&
                ((c.CompletesAt != null && c.CompletesAt <= now) ||
                 ((c.Type == FlightCommandType.Flight || c.Type == FlightCommandType.Recon ||
                   c.Type == FlightCommandType.Attack || c.Type == FlightCommandType.Mine) &&
                  (c.TargetGalaxy == null || c.TargetGalaxy < 1 ||
                   c.TargetSystem == null || c.TargetSystem < 1 ||
                   c.TargetPosition == null || c.TargetPosition < 1))))).ToListAsync(token);
        foreach (var fleet in dueFleets)
        {
            var command = fleet.Commands.Single(x => x.Status == FlightCommandStatus.Active);
            if (!FlightRules.HasValidTargetCoordinates(command))
            {
                fleet.FuelReserve += FlightRules.FuelCost(fleet, command);
                Fail(fleet, command, now, "Некорректная команда отменена: координаты цели должны быть не меньше 1. Топливо возвращено.");
                await CreateAttackAlertAsync(db, fleet, now, token);
                continue;
            }
            await CompleteCommandAsync(db, fleet, command, now, token);
            await CreateAttackAlertAsync(db, fleet, now, token);
        }

        var battles = await db.Battles.Where(x => x.Status != BattleStatus.Completed && x.ResolveAt <= now).ToListAsync(token);
        foreach (var battle in battles) await ResolveBattleAsync(db, battle, now, token);

        await ProcessPiratesAsync(db, now, token);

        var fields = await db.ResourceFields.Where(x => x.UpdatedAt < now.AddMinutes(-5)).ToListAsync(token);
        foreach (var field in fields)
        {
            var hours = (decimal)(now - field.UpdatedAt).TotalHours;
            var regen = field.RegenPerHour * hours;
            field.Materials = Math.Min(field.MaxMaterials, field.Materials + regen * .7m);
            field.Deuterium = Math.Min(field.MaxDeuterium, field.Deuterium + regen * .3m);
            field.UpdatedAt = now;
        }
        var debris = await db.DebrisFields.ToListAsync(token);
        foreach (var field in debris) { DebrisRules.ApplyDecay(field, now); if (field.ExpiresAt <= now || field.Materials + field.Deuterium < 1) db.DebrisFields.Remove(field); }
        if (db.ChangeTracker.HasChanges()) await db.SaveChangesAsync(token);
    }

    private static async Task CompleteCommandAsync(ApplicationDbContext db, Fleet fleet, FlightCommand command, DateTime now, CancellationToken token)
    {
        switch (command.Type)
        {
            case FlightCommandType.Flight:
                Move(fleet, command); FlightRules.FinishAndAdvance(fleet, command, now, "Флот прибыл в точку назначения."); break;
            case FlightCommandType.Recon:
                Move(fleet, command);
                var scanRange = fleet.Ships.Select(x => x.ScanRange).DefaultIfEmpty(0).Max();
                var scanPositions = (int)decimal.Floor(scanRange);
                var contacts = await db.Fleets.AsNoTracking()
                    .Where(x => x.Id != fleet.Id && x.Status != FleetStatus.Landed &&
                        x.GalaxyNumber == fleet.GalaxyNumber && x.SystemNumber == fleet.SystemNumber &&
                        x.Position >= fleet.Position - scanPositions && x.Position <= fleet.Position + scanPositions)
                    .Select(x => new
                    {
                        fleetId = x.Id,
                        x.Name,
                        x.IsPirate,
                        shipCount = x.Ships.Count,
                        x.Position,
                        canAttack = x.PlayerId != fleet.PlayerId
                    })
                    .OrderBy(x => x.Position).ToListAsync(token);
                if (fleet.PlayerId is Guid reconPlayerId)
                {
                    db.GameEvents.Add(new GameEvent
                    {
                        Id = Guid.NewGuid(),
                        PlayerId = reconPlayerId,
                        Type = GameEventType.ReconReport,
                        Title = $"Разведка {fleet.GalaxyNumber}:{fleet.SystemNumber}:{fleet.Position}",
                        Body = contacts.Count == 0
                            ? "Сканирование завершено. Флоты не обнаружены."
                            : $"Сканирование завершено. Обнаружено флотов: {contacts.Count}.",
                        SourceCommandId = command.Id,
                        CreatedAt = now,
                        DataJson = JsonSerializer.Serialize(new
                        {
                            galaxy = fleet.GalaxyNumber,
                            system = fleet.SystemNumber,
                            position = fleet.Position,
                            scanRange,
                            contacts
                        }, EventJsonOptions)
                    });
                }
                FlightRules.FinishAndAdvance(fleet, command, now, $"Разведка завершена: обнаружено контактов — {contacts.Count}."); break;
            case FlightCommandType.Return:
                var home = await db.Planets.Include(x => x.StarSystem).SingleOrDefaultAsync(x => x.Id == fleet.HomePlanetId, token);
                if (home is null) { Fail(fleet, command, now, "Домашняя планета недоступна."); break; }
                fleet.GalaxyNumber = home.StarSystem.GalaxyNumber; fleet.SystemNumber = home.StarSystem.SystemNumber; fleet.Position = home.Position; fleet.LocationType = FleetLocationType.Planet;
                FlightRules.FinishAndAdvance(fleet, command, now, "Флот на орбите домашней планеты. Для защиты выполните посадку."); break;
            case FlightCommandType.LoadUnload:
                await TransferCargoAsync(db, fleet, command, now, token); break;
            case FlightCommandType.Mine:
                await MineAsync(db, fleet, command, now, token); break;
            case FlightCommandType.Attack:
                Move(fleet, command);
                var target = await db.Fleets.Include(x => x.Ships).SingleOrDefaultAsync(x => x.Id == command.TargetFleetId && x.Status != FleetStatus.Landed && x.GalaxyNumber == fleet.GalaxyNumber && x.SystemNumber == fleet.SystemNumber && x.Position == fleet.Position, token);
                if (target is null) { FlightRules.FinishAndAdvance(fleet, command, now, "Цель покинула координаты или совершила посадку. Боя не было."); break; }
                if (target.PlayerId == fleet.PlayerId) { FlightRules.FinishAndAdvance(fleet, command, now, "Атака собственного флота отменена."); break; }
                var protectedPlayer = target.PlayerId is Guid targetPlayer && await db.Players.AnyAsync(x => x.Id == targetPlayer && x.CreatedAt > now.AddDays(-7), token);
                if (protectedPlayer) { FlightRules.FinishAndAdvance(fleet, command, now, "Цель находится под защитой нового игрока."); break; }
                var battle = new Battle { Id = Guid.NewGuid(), AttackerFleetId = fleet.Id, DefenderFleetId = target.Id, Status = BattleStatus.AwaitingOrders, Round = 1, OrderDeadline = now.AddSeconds(60), ResolveAt = now.AddSeconds(90), CreatedAt = now };
                fleet.Status = FleetStatus.InBattle; target.Status = FleetStatus.InBattle; db.Battles.Add(battle); break;
        }
    }

    private static async Task TransferCargoAsync(ApplicationDbContext db, Fleet fleet, FlightCommand command, DateTime now, CancellationToken token)
    {
        var planet = await db.Planets.Include(x => x.StarSystem).SingleOrDefaultAsync(x => x.Position == fleet.Position && x.StarSystem.GalaxyNumber == fleet.GalaxyNumber && x.StarSystem.SystemNumber == fleet.SystemNumber, token);
        if (planet is null) { Fail(fleet, command, now, "В координатах нет планеты для погрузки или выгрузки."); return; }
        if (fleet.MaterialsCargo + fleet.DeuteriumCargo > 0)
        {
            planet.Materials += fleet.MaterialsCargo; planet.Deuterium += fleet.DeuteriumCargo;
            fleet.MaterialsCargo = 0; fleet.DeuteriumCargo = 0; fleet.FuelReserve = Math.Max(fleet.FuelReserve, 100m);
            var outcome = planet.PlayerId == fleet.PlayerId
                ? "Трюмы автоматически выгружены; топливо пополнено."
                : $"Торговая доставка на планету «{planet.Name}» завершена.";
            FlightRules.FinishAndAdvance(fleet, command, now, outcome); return;
        }
        if (planet.PlayerId != fleet.PlayerId) { Fail(fleet, command, now, "Погрузка с чужой планеты запрещена без доставки её владельцем."); return; }
        var capacity = fleet.Ships.Sum(x => x.CargoCapacity);
        var materials = Math.Min(Math.Min(command.ManifestMaterials, planet.Materials), capacity);
        var deuterium = Math.Min(Math.Min(command.ManifestDeuterium, planet.Deuterium), capacity - materials);
        planet.Materials -= materials; planet.Deuterium -= deuterium; fleet.MaterialsCargo = materials; fleet.DeuteriumCargo = deuterium; fleet.FuelReserve = Math.Max(fleet.FuelReserve, 100m);
        FlightRules.FinishAndAdvance(fleet, command, now, $"Погружено: {materials:0} материалов, {deuterium:0} дейтерия.");
    }

    private static async Task MineAsync(ApplicationDbContext db, Fleet fleet, FlightCommand command, DateTime now, CancellationToken token)
    {
        var capacity = fleet.Ships.Sum(x => x.CargoCapacity) - fleet.MaterialsCargo - fleet.DeuteriumCargo;
        var durationMinutes = command.DurationMinutes > 0
            ? command.DurationMinutes
            : Math.Max(1m, (decimal)(now - (command.StartedAt ?? now)).TotalMinutes);
        var rate = fleet.Ships.Sum(x => x.MiningRatePerMinute) * durationMinutes;
        if (capacity <= 0 || rate <= 0) { Fail(fleet, command, now, "Нет свободного трюма или добывающих модулей."); return; }
        if (command.TargetObjectId is Guid objectId)
        {
            var field = await db.ResourceFields.SingleOrDefaultAsync(x => x.Id == objectId, token);
            if (field is not null)
            {
                var fieldSystem = await db.StarSystems.Where(x => x.Id == field.StarSystemId)
                    .Select(x => new { x.GalaxyNumber, x.SystemNumber }).SingleAsync(token);
                if (fieldSystem.GalaxyNumber != command.TargetGalaxy || fieldSystem.SystemNumber != command.TargetSystem || field.Position != command.TargetPosition)
                { Fail(fleet, command, now, "Координаты поля не совпадают с полётным листом."); return; }
                fleet.GalaxyNumber = fieldSystem.GalaxyNumber; fleet.SystemNumber = fieldSystem.SystemNumber;
                fleet.Position = field.Position; fleet.LocationType = FleetLocationType.ResourceField;
                var miners = await db.FlightCommands.CountAsync(x => x.TargetObjectId == field.Id && x.Type == FlightCommandType.Mine && x.Status == FlightCommandStatus.Active, token);
                var sharedThroughput = field.ThroughputPerHour * durationMinutes / 60m / Math.Max(1, miners);
                var total = Math.Min(capacity, Math.Min(rate, sharedThroughput));
                var materialShare = field.Materials + field.Deuterium == 0 ? 0 : field.Materials / (field.Materials + field.Deuterium);
                var materials = Math.Min(field.Materials, total * materialShare); var deuterium = Math.Min(field.Deuterium, total - materials);
                field.Materials -= materials; field.Deuterium -= deuterium; fleet.MaterialsCargo += materials; fleet.DeuteriumCargo += deuterium;
                FlightRules.FinishAndAdvance(fleet, command, now, $"Добыто: {materials:0} материалов, {deuterium:0} дейтерия."); return;
            }
            var debris = await db.DebrisFields.SingleOrDefaultAsync(x => x.Id == objectId, token);
            if (debris is not null && (debris.ExclusivePlayerId == fleet.PlayerId || debris.ExclusiveUntil <= now))
            {
                if (debris.GalaxyNumber != command.TargetGalaxy || debris.SystemNumber != command.TargetSystem || debris.Position != command.TargetPosition)
                { Fail(fleet, command, now, "Координаты обломков не совпадают с полётным листом."); return; }
                fleet.GalaxyNumber = debris.GalaxyNumber; fleet.SystemNumber = debris.SystemNumber;
                DebrisRules.ApplyDecay(debris, now); fleet.Position = debris.Position; fleet.LocationType = FleetLocationType.DebrisField;
                var total = Math.Min(capacity, rate * 1.25m); var materialShare = debris.Materials + debris.Deuterium == 0 ? 0 : debris.Materials / (debris.Materials + debris.Deuterium);
                var materials = Math.Min(debris.Materials, total * materialShare); var deuterium = Math.Min(debris.Deuterium, total - materials);
                debris.Materials -= materials; debris.Deuterium -= deuterium; fleet.MaterialsCargo += materials; fleet.DeuteriumCargo += deuterium;
                FlightRules.FinishAndAdvance(fleet, command, now, $"Собрано из обломков: {materials:0} / {deuterium:0}."); return;
            }
        }
        Fail(fleet, command, now, "Поле недоступно или окно исключительного сбора ещё не окончено.");
    }

    private static async Task ResolveBattleAsync(ApplicationDbContext db, Battle battle, DateTime now, CancellationToken token)
    {
        var fleets = await db.Fleets.Include(x => x.Ships).Include(x => x.Commands).Where(x => x.Id == battle.AttackerFleetId || x.Id == battle.DefenderFleetId).ToListAsync(token);
        var attacker = fleets.SingleOrDefault(x => x.Id == battle.AttackerFleetId); var defender = fleets.SingleOrDefault(x => x.Id == battle.DefenderFleetId);
        if (attacker is null || defender is null) { battle.Status = BattleStatus.Completed; battle.CompletedAt = now; return; }
        var orders = await db.BattleOrders.Where(x => x.BattleId == battle.Id && x.Round == battle.Round).ToListAsync(token);
        var attackerOrder = orders.SingleOrDefault(x => x.FleetId == attacker.Id);
        var defenderOrder = orders.SingleOrDefault(x => x.FleetId == defender.Id);
        var result = CombatRules.ResolveRound(
            attacker,
            defender,
            attackerOrder?.Retreat == true,
            defenderOrder?.Retreat == true,
            attackerOrder?.TargetPriority ?? "Weakest",
            defenderOrder?.TargetPriority ?? "Weakest");
        var reports = JsonSerializer.Deserialize<List<string>>(battle.ReportJson) ?? []; reports.Add($"Раунд {battle.Round}: {result.Summary}"); battle.ReportJson = JsonSerializer.Serialize(reports);
        var destroyed = attacker.Ships.Concat(defender.Ships).Where(x => x.Hull <= 0).ToList(); var debrisYield = CombatRules.CalculateDebris(destroyed);
        foreach (var ship in destroyed) { if (ship.ShipId is Guid shipId) { var entity = await db.Ships.FindAsync(new object?[] { shipId }, token); if (entity is not null) db.Ships.Remove(entity); } else db.FleetShips.Remove(ship); }
        if (debrisYield.Materials + debrisYield.Deuterium > 0) db.DebrisFields.Add(new DebrisField { Id = Guid.NewGuid(), GalaxyNumber = attacker.GalaxyNumber, SystemNumber = attacker.SystemNumber, Position = attacker.Position, Materials = debrisYield.Materials, Deuterium = debrisYield.Deuterium, ExclusivePlayerId = result.WinnerFleetId == attacker.Id ? attacker.PlayerId : defender.PlayerId, ExclusiveUntil = now.AddMinutes(10), CreatedAt = now, UpdatedAt = now, ExpiresAt = now.AddHours(48), ComponentsJson = DebrisRules.SalvageComponents(destroyed) });
        if (result.WinnerFleetId is not null || battle.Round >= 15)
        {
            battle.Status = BattleStatus.Completed; battle.WinnerFleetId = result.WinnerFleetId; battle.CompletedAt = now;
            foreach (var fleet in fleets) { fleet.Status = FleetStatus.Orbiting; var command = fleet.Commands.SingleOrDefault(x => x.Status == FlightCommandStatus.Active); if (command is not null) FlightRules.FinishAndAdvance(fleet, command, now, "Бой завершён."); }
            foreach (var defeatedPirate in fleets.Where(x => x.IsPirate && x.Ships.All(s => s.Hull <= 0)))
            {
                var cell = await db.PirateCells.SingleOrDefaultAsync(x => x.Id == defeatedPirate.PirateCellId, token);
                if (cell is not null) { cell.State = PirateCellState.Weakened; cell.Threat = Math.Max(1, cell.Threat - 1); }
                db.Fleets.Remove(defeatedPirate);
            }
        }
        else { battle.Round++; battle.Status = BattleStatus.AwaitingOrders; battle.OrderDeadline = now.AddSeconds(60); battle.ResolveAt = now.AddSeconds(90); }
    }

    private static void Move(Fleet fleet, FlightCommand command)
    {
        fleet.GalaxyNumber = command.TargetGalaxy ?? fleet.GalaxyNumber; fleet.SystemNumber = command.TargetSystem ?? fleet.SystemNumber; fleet.Position = command.TargetPosition ?? fleet.Position; fleet.LocationType = FleetLocationType.DeepSpace;
    }

    private static async Task ProcessPiratesAsync(ApplicationDbContext db, DateTime now, CancellationToken token)
    {
        var cells = await db.PirateCells.Where(x => x.LastActedAt <= now.AddMinutes(-10)).ToListAsync(token);
        foreach (var cell in cells.OrderBy(x => x.Id))
        {
            var system = await db.StarSystems.SingleAsync(x => x.Id == cell.StarSystemId, token);
            cell.Materials += 120m * cell.Threat; cell.Deuterium += 35m * cell.Threat;
            var pirate = await db.Fleets.Include(x => x.Ships).SingleOrDefaultAsync(x => x.PirateCellId == cell.Id, token);
            if (pirate is null && cell.Materials >= 900 && cell.Deuterium >= 220)
            {
                pirate = new Fleet { Id = Guid.NewGuid(), PirateCellId = cell.Id, Name = $"Рейдеры {system.Name}", IsPirate = true, Status = FleetStatus.Patrolling, LocationType = FleetLocationType.DeepSpace, GalaxyNumber = system.GalaxyNumber, SystemNumber = system.SystemNumber, Position = 101 + system.SystemNumber % 6, FuelReserve = 1000, CreatedAt = now, UpdatedAt = now };
                pirate.Ships.Add(new FleetShip { Id = Guid.NewGuid(), Fleet = pirate, FleetId = pirate.Id, Name = "Пиратский рейдер", BlueprintName = "Рейдер", LocalSpeed = 90, InterSystemSpeed = 55, CargoCapacity = 80, ScanRange = 25, MaxHull = 120, Hull = 120, MaxShield = 30, Shield = 30, LaserShieldDamage = 12, LaserHullDamage = 5, MissileShieldDamage = 6, MissileHullDamage = 16, ComponentMaterials = 900, ComponentDeuterium = 220 });
                cell.Materials -= 900; cell.Deuterium -= 220; db.Fleets.Add(pirate);
            }
            if (pirate is null || pirate.Status == FleetStatus.InBattle)
            { cell.LastActedAt = now; continue; }
            cell.State = cell.Threat >= 4 ? PirateCellState.Entrenched : cell.Threat >= 2 ? PirateCellState.Raiding : PirateCellState.Scouting;
            var candidates = await db.Fleets.Where(x => x.PlayerId != null && x.Status != FleetStatus.Landed && x.Status != FleetStatus.InBattle && x.GalaxyNumber == system.GalaxyNumber && x.SystemNumber == system.SystemNumber)
                .OrderBy(x => x.CreatedAt).ToListAsync(token);
            Fleet? target = null;
            foreach (var candidate in candidates)
            {
                var oldEnough = await db.Players.AnyAsync(x => x.Id == candidate.PlayerId && x.CreatedAt <= now.AddHours(-72), token);
                if (oldEnough && Math.Abs(candidate.Position - pirate.Position) <= 25) { target = candidate; break; }
            }
            if (target is not null)
            {
                pirate.Position = target.Position; pirate.UpdatedAt = now; pirate.Status = FleetStatus.InBattle; target.Status = FleetStatus.InBattle;
                db.Battles.Add(new Battle { Id = Guid.NewGuid(), AttackerFleetId = pirate.Id, DefenderFleetId = target.Id, Status = BattleStatus.AwaitingOrders, Round = 1, OrderDeadline = now.AddSeconds(60), ResolveAt = now.AddSeconds(90), CreatedAt = now });
            }
            else { pirate.Position = 101 + (pirate.Position + cell.Threat) % 6; pirate.Status = FleetStatus.Patrolling; pirate.UpdatedAt = now; }
            cell.LastActedAt = now;
        }
    }

    private static void Fail(Fleet fleet, FlightCommand command, DateTime now, string reason)
    {
        command.Status = FlightCommandStatus.Failed; command.CompletedAt = now; command.Outcome = reason;
        var next = fleet.Commands.Where(x => x.Sequence > command.Sequence && x.Status == FlightCommandStatus.Planned).OrderBy(x => x.Sequence).FirstOrDefault();
        if (next is null) fleet.Status = FleetStatus.Orbiting; else FlightRules.Activate(fleet, next, now);
    }

    private static async Task EnsureAttackAlertsAsync(
        ApplicationDbContext db,
        DateTime now,
        CancellationToken token)
    {
        var attackers = await db.Fleets
            .Include(x => x.Commands)
            .Where(x => !x.IsPirate && x.Commands.Any(c =>
                c.Status == FlightCommandStatus.Active &&
                c.Type == FlightCommandType.Attack &&
                c.TargetFleetId != null))
            .ToListAsync(token);
        foreach (var attacker in attackers)
            await CreateAttackAlertAsync(db, attacker, now, token);
    }
}
