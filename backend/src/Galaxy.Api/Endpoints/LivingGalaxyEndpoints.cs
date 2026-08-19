using System.Text.Json;
using Galaxy.Api.Security;
using Galaxy.Application.Economy;
using Galaxy.Application.LivingGalaxy;
using Galaxy.Domain.Entities;
using Galaxy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Galaxy.Api.Endpoints;

public static class LivingGalaxyEndpoints
{
    public static void MapLivingGalaxyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/game/living-galaxy").RequireAuthorization();
        group.MapGet("/fleets", GetFleetsAsync);
        group.MapPost("/fleets", CreateFleetAsync);
        group.MapPut("/fleets/{fleetId:guid}/plan", ReplacePlanAsync);
        group.MapPost("/fleets/{fleetId:guid}/launch", LaunchAsync);
        group.MapPut("/fleets/{fleetId:guid}/next-command", ReplaceNextAsync);
        group.MapPost("/fleets/{fleetId:guid}/land", LandAsync);
        group.MapPost("/fleets/{fleetId:guid}/refuel", RefuelAsync);
        group.MapGet("/system", GetSystemAsync);
        group.MapGet("/battles", GetBattlesAsync);
        group.MapPost("/battles/{battleId:guid}/orders", SubmitOrderAsync);
        group.MapPost("/ships/{fleetShipId:guid}/service", StartServiceAsync);
    }

    private static async Task<IResult> GetFleetsAsync(HttpContext http, ApplicationDbContext db, CancellationToken token)
    {
        var playerId = await CurrentAccount.GetPlayerIdAsync(http.User, db, token);
        if (playerId is null) return Results.Unauthorized();
        await LivingGalaxyProcessor.ProcessAsync(db, DateTime.UtcNow, token);
        var fleets = await db.Fleets.AsNoTracking().Where(x => x.PlayerId == playerId)
            .Include(x => x.Ships).Include(x => x.Commands)
            .OrderBy(x => x.CreatedAt).ToListAsync(token);
        return Results.Ok(fleets.Select(ToFleetResponse));
    }

    private static async Task<IResult> CreateFleetAsync(CreateFleetRequest request, HttpContext http, ApplicationDbContext db, CancellationToken token)
    {
        var playerId = await CurrentAccount.GetPlayerIdAsync(http.User, db, token);
        if (playerId is null) return Results.Unauthorized();
        var planet = await db.Planets.Where(x => x.Id == request.PlanetId && x.PlayerId == playerId)
            .Include(x => x.StarSystem).Include(x => x.Ships).ThenInclude(x => x.FleetShip)
            .Include(x => x.Ships).ThenInclude(x => x.Blueprint).ThenInclude(x => x.Modules)
            .SingleOrDefaultAsync(token);
        if (planet is null) return Results.NotFound();
        try
        {
            var ids = request.ShipIds.Distinct().ToHashSet();
            var ships = planet.Ships.Where(x => ids.Contains(x.Id)).ToList();
            if (ships.Count != ids.Count) throw new InvalidOperationException("Часть выбранных кораблей недоступна.");
            var fleet = FleetFactory.Create(playerId.Value, planet, request.Name, ships, DateTime.UtcNow);
            db.Fleets.Add(fleet); await db.SaveChangesAsync(token);
            return Results.Created($"/api/game/living-galaxy/fleets/{fleet.Id}", ToFleetResponse(fleet));
        }
        catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
    }

    private static async Task<IResult> ReplacePlanAsync(Guid fleetId, PlanRequest request, HttpContext http, ApplicationDbContext db, CancellationToken token)
    {
        var fleet = await OwnedFleetAsync(fleetId, http, db, token);
        if (fleet is null) return Results.NotFound();
        try
        {
            var previousCommands = fleet.Commands.ToList();
            FlightRules.ReplacePlan(fleet, request.Commands.Select(ToCommand).ToList());
            db.FlightCommands.RemoveRange(previousCommands);
            db.FlightCommands.AddRange(fleet.Commands);
            await db.SaveChangesAsync(token); return Results.Ok(ToFleetResponse(fleet));
        }
        catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
    }

    private static async Task<IResult> LaunchAsync(Guid fleetId, HttpContext http, ApplicationDbContext db, CancellationToken token)
    {
        var fleet = await OwnedFleetAsync(fleetId, http, db, token);
        if (fleet is null) return Results.NotFound();
        try
        {
            var now = DateTime.UtcNow;
            FlightRules.Start(fleet, now);
            await LivingGalaxyProcessor.CreateAttackAlertAsync(db, fleet, now, token);
            await db.SaveChangesAsync(token);
            return Results.Ok(ToFleetResponse(fleet));
        }
        catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
    }

    private static async Task<IResult> ReplaceNextAsync(Guid fleetId, NextCommandRequest request, HttpContext http, ApplicationDbContext db, CancellationToken token)
    {
        var fleet = await OwnedFleetAsync(fleetId, http, db, token);
        if (fleet is null) return Results.NotFound();
        try
        {
            var sequence = fleet.CurrentCommandSequence + 1;
            var previousCommand = fleet.Commands.SingleOrDefault(x => x.Sequence == sequence);
            FlightRules.ReplaceNext(fleet, request.Command is null ? null : ToCommand(request.Command));
            if (previousCommand is not null) db.FlightCommands.Remove(previousCommand);
            var replacement = fleet.Commands.SingleOrDefault(x => x.Sequence == sequence);
            if (replacement is not null) db.FlightCommands.Add(replacement);
            if (fleet.Status == FleetStatus.Patrolling && request.Command is not null)
            {
                var current = fleet.Commands.Single(x => x.Sequence == fleet.CurrentCommandSequence);
                var now = DateTime.UtcNow;
                FlightRules.FinishAndAdvance(fleet, current, now, "Патруль завершён новым приказом.");
                await LivingGalaxyProcessor.CreateAttackAlertAsync(db, fleet, now, token);
            }
            await db.SaveChangesAsync(token); return Results.Ok(ToFleetResponse(fleet));
        }
        catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
    }

    private static async Task<IResult> LandAsync(Guid fleetId, HttpContext http, ApplicationDbContext db, CancellationToken token)
    {
        var fleet = await OwnedFleetAsync(fleetId, http, db, token);
        if (fleet is null) return Results.NotFound();
        if (fleet.Status != FleetStatus.Orbiting) return Results.BadRequest(new { message = "Посадка доступна только флоту на орбите." });
        var planet = await db.Planets.SingleOrDefaultAsync(x => x.Id == fleet.HomePlanetId && x.PlayerId == fleet.PlayerId, token);
        var homeCoordinates = planet is null
            ? null
            : await db.StarSystems.Where(x => x.Id == planet.StarSystemId)
                .Select(x => new { x.GalaxyNumber, x.SystemNumber }).SingleAsync(token);
        if (planet is null || homeCoordinates is null ||
            fleet.GalaxyNumber != homeCoordinates.GalaxyNumber ||
            fleet.SystemNumber != homeCoordinates.SystemNumber ||
            fleet.Position != planet.Position)
            return Results.BadRequest(new { message = "Флот должен находиться на орбите своей планеты." });
        fleet.Status = FleetStatus.Landed; fleet.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(token); return Results.Ok(ToFleetResponse(fleet));
    }

    private static async Task<IResult> RefuelAsync(
        Guid fleetId,
        RefuelRequest request,
        HttpContext http,
        ApplicationDbContext db,
        CancellationToken token)
    {
        var fleet = await OwnedFleetAsync(fleetId, http, db, token);
        if (fleet is null) return Results.NotFound();

        var planet = await db.Planets.SingleOrDefaultAsync(
            x => x.Id == fleet.HomePlanetId && x.PlayerId == fleet.PlayerId,
            token);
        if (planet is null) return Results.NotFound();

        ResourceProductionCalculator.Update(planet, DateTime.UtcNow);

        try
        {
            var transferred = FleetRefueling.Transfer(fleet, planet, request.Amount);
            await db.SaveChangesAsync(token);
            return Results.Ok(new
            {
                amount = transferred,
                fuelReserve = fleet.FuelReserve,
                planetDeuterium = planet.Deuterium
            });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> GetSystemAsync(int galaxy, int system, HttpContext http, ApplicationDbContext db, CancellationToken token)
    {
        var playerId = await CurrentAccount.GetPlayerIdAsync(http.User, db, token);
        if (playerId is null) return Results.Unauthorized();
        await LivingGalaxyProcessor.ProcessAsync(db, DateTime.UtcNow, token);
        var starSystem = await db.StarSystems.AsNoTracking().SingleOrDefaultAsync(x => x.GalaxyNumber == galaxy && x.SystemNumber == system, token);
        if (starSystem is null) return Results.NotFound();
        var fields = await db.ResourceFields.AsNoTracking().Where(x => x.StarSystemId == starSystem.Id).OrderBy(x => x.Position).ToListAsync(token);
        var debris = await db.DebrisFields.AsNoTracking().Where(x => x.GalaxyNumber == galaxy && x.SystemNumber == system && x.ExpiresAt > DateTime.UtcNow).ToListAsync(token);
        var fleets = await db.Fleets.AsNoTracking().Where(x => x.GalaxyNumber == galaxy && x.SystemNumber == system && x.Status != FleetStatus.Landed)
            .Include(x => x.Ships).ToListAsync(token);
        return Results.Ok(new
        {
            starSystem.Id, starSystem.Name, galaxy, system,
            fields = fields.Select(x => new { x.Id, x.Name, x.Position, x.Type, x.Materials, x.Deuterium, x.MaxMaterials, x.MaxDeuterium, x.ThroughputPerHour, x.Threat }),
            debris = debris.Select(x => new { x.Id, x.Position, x.Materials, x.Deuterium, x.ExclusivePlayerId, x.ExclusiveUntil, x.ExpiresAt }),
            fleets = fleets.Select(x => new { x.Id, x.Name, x.Position, x.Status, x.IsPirate, isOwn = x.PlayerId == playerId, shipCount = x.Ships.Count, canAttack = x.PlayerId != playerId })
        });
    }

    private static async Task<IResult> GetBattlesAsync(HttpContext http, ApplicationDbContext db, CancellationToken token)
    {
        var playerId = await CurrentAccount.GetPlayerIdAsync(http.User, db, token);
        if (playerId is null) return Results.Unauthorized();
        await LivingGalaxyProcessor.ProcessAsync(db, DateTime.UtcNow, token);
        var fleetIds = await db.Fleets.Where(x => x.PlayerId == playerId).Select(x => x.Id).ToListAsync(token);
        var battles = await db.Battles.AsNoTracking().Where(x => fleetIds.Contains(x.AttackerFleetId) || fleetIds.Contains(x.DefenderFleetId))
            .OrderByDescending(x => x.CreatedAt).Take(40).ToListAsync(token);
        return Results.Ok(battles.Select(x => new { x.Id, x.AttackerFleetId, x.DefenderFleetId, x.Status, x.Round, x.OrderDeadline, x.ResolveAt, x.WinnerFleetId, report = JsonSerializer.Deserialize<object>(x.ReportJson), x.CreatedAt, x.CompletedAt }));
    }

    private static async Task<IResult> SubmitOrderAsync(Guid battleId, BattleOrderRequest request, HttpContext http, ApplicationDbContext db, CancellationToken token)
    {
        var playerId = await CurrentAccount.GetPlayerIdAsync(http.User, db, token);
        if (playerId is null) return Results.Unauthorized();
        var battle = await db.Battles.SingleOrDefaultAsync(x => x.Id == battleId && x.Status != BattleStatus.Completed, token);
        if (battle is null) return Results.NotFound();
        if (DateTime.UtcNow > battle.OrderDeadline)
            return Results.BadRequest(new { message = "Время приказов истекло; идёт расчёт раунда." });
        var fleet = await db.Fleets.SingleOrDefaultAsync(x => x.Id == request.FleetId && x.PlayerId == playerId && (x.Id == battle.AttackerFleetId || x.Id == battle.DefenderFleetId), token);
        if (fleet is null) return Results.Forbid();
        var order = await db.BattleOrders.SingleOrDefaultAsync(x => x.BattleId == battleId && x.FleetId == fleet.Id && x.Round == battle.Round, token);
        if (order is null) db.BattleOrders.Add(new BattleOrder { Id = Guid.NewGuid(), BattleId = battleId, FleetId = fleet.Id, Round = battle.Round, TargetPriority = request.TargetPriority ?? "Weakest", Retreat = request.Retreat, SubmittedAt = DateTime.UtcNow });
        else { order.TargetPriority = request.TargetPriority ?? "Weakest"; order.Retreat = request.Retreat; order.SubmittedAt = DateTime.UtcNow; }
        await db.SaveChangesAsync(token); return Results.Ok();
    }

    private static async Task<IResult> StartServiceAsync(Guid fleetShipId, ServiceRequest request, HttpContext http, ApplicationDbContext db, CancellationToken token)
    {
        var playerId = await CurrentAccount.GetPlayerIdAsync(http.User, db, token);
        if (playerId is null) return Results.Unauthorized();
        var ship = await db.FleetShips.Include(x => x.Fleet).SingleOrDefaultAsync(x => x.Id == fleetShipId && x.Fleet.PlayerId == playerId, token);
        if (ship is null) return Results.NotFound();
        if (ship.Fleet.Status != FleetStatus.Landed) return Results.BadRequest(new { message = "Сервис доступен только после посадки." });
        var planet = await db.Planets.SingleAsync(x => x.Id == ship.Fleet.HomePlanetId, token);
        if (request.Type == ShipServiceType.HullRepair && planet.ShipyardLevel < 1) return Results.BadRequest(new { message = "Для ремонта корпуса нужна верфь." });
        if (await db.ShipServiceOrders.AnyAsync(x => x.FleetShipId == ship.Id, token)) return Results.BadRequest(new { message = "Корабль уже обслуживается." });
        var missing = request.Type == ShipServiceType.ShieldRecharge ? ship.MaxShield - ship.Shield : ship.MaxHull - ship.Hull;
        if (missing <= 0) return Results.BadRequest(new { message = "Обслуживание не требуется." });
        var ratio = request.Type == ShipServiceType.ShieldRecharge ? 0m : missing / Math.Max(1m, ship.MaxHull);
        var materials = decimal.Ceiling(ship.ComponentMaterials * ratio * .25m);
        var deuterium = decimal.Ceiling(ship.ComponentDeuterium * ratio * .25m);
        ResourceProductionCalculator.Update(planet, DateTime.UtcNow);
        if (planet.Materials < materials || planet.Deuterium < deuterium) return Results.BadRequest(new { message = "Недостаточно ресурсов для ремонта." });
        planet.Materials -= materials; planet.Deuterium -= deuterium;
        var seconds = request.Type == ShipServiceType.ShieldRecharge ? 300 : Math.Max(30, (int)(ratio * 1200 / Math.Max(1, planet.ShipyardLevel)));
        db.ShipServiceOrders.Add(new ShipServiceOrder { Id = Guid.NewGuid(), FleetShipId = ship.Id, PlanetId = planet.Id, Type = request.Type, MaterialsCost = materials, DeuteriumCost = deuterium, StartedAt = DateTime.UtcNow, CompletesAt = DateTime.UtcNow.AddSeconds(seconds) });
        await db.SaveChangesAsync(token); return Results.Ok(new { completesAt = DateTime.UtcNow.AddSeconds(seconds), materials, deuterium });
    }

    private static async Task<Fleet?> OwnedFleetAsync(Guid id, HttpContext http, ApplicationDbContext db, CancellationToken token)
    {
        var playerId = await CurrentAccount.GetPlayerIdAsync(http.User, db, token);
        if (playerId is null) return null;
        return await db.Fleets.Where(x => x.Id == id && x.PlayerId == playerId).Include(x => x.Ships).Include(x => x.Commands).SingleOrDefaultAsync(token);
    }

    private static FlightCommand ToCommand(CommandRequest x) => new()
    {
        Type = x.Type, SpeedMode = x.SpeedMode, TargetGalaxy = x.TargetGalaxy,
        TargetSystem = x.TargetSystem, TargetPosition = x.TargetPosition,
        TargetFleetId = x.TargetFleetId, TargetObjectId = x.TargetObjectId,
        DurationMinutes = x.DurationMinutes, ManifestMaterials = Math.Max(0, x.ManifestMaterials),
        ManifestDeuterium = Math.Max(0, x.ManifestDeuterium)
    };

    private static object ToFleetResponse(Fleet x) => new
    {
        x.Id, x.Name, x.HomePlanetId, x.HomeGalaxyNumber, x.HomeSystemNumber, x.HomePosition, x.Status, x.LocationType, x.GalaxyNumber, x.SystemNumber, x.Position,
        x.MaterialsCargo, x.DeuteriumCargo, x.FuelReserve, x.CurrentCommandSequence,
        cargoCapacity = x.Ships.Sum(s => s.CargoCapacity), editableSequence = FlightRules.EditableSequence(x),
        ships = x.Ships.Select(s => new { s.Id, s.ShipId, s.Name, s.BlueprintName, s.Hull, s.MaxHull, s.Shield, s.MaxShield, s.CargoCapacity, s.MiningRatePerMinute, s.ScanRange }),
        commands = x.Commands.OrderBy(c => c.Sequence).Select(c => new { c.Id, c.Sequence, c.Type, c.Status, c.SpeedMode, c.TargetGalaxy, c.TargetSystem, c.TargetPosition, c.TargetFleetId, c.TargetObjectId, c.DurationMinutes, c.ManifestMaterials, c.ManifestDeuterium, c.StartedAt, c.CompletesAt, c.CompletedAt, c.Outcome })
    };
}

public sealed record CreateFleetRequest(Guid PlanetId, string Name, List<Guid> ShipIds);
public sealed record PlanRequest(List<CommandRequest> Commands);
public sealed record NextCommandRequest(CommandRequest? Command);
public sealed record RefuelRequest(decimal Amount);
public sealed record CommandRequest(FlightCommandType Type, FlightSpeedMode SpeedMode, int? TargetGalaxy, int? TargetSystem, int? TargetPosition, Guid? TargetFleetId, Guid? TargetObjectId, int DurationMinutes, decimal ManifestMaterials, decimal ManifestDeuterium);
public sealed record BattleOrderRequest(Guid FleetId, string? TargetPriority, bool Retreat);
public sealed record ServiceRequest(ShipServiceType Type);
