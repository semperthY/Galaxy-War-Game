using Galaxy.Application.Assembly;
using Galaxy.Domain.Entities;
using Galaxy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Galaxy.Api.Endpoints;

public static class ShipAssemblyEndpoints
{
    public static void MapShipAssemblyEndpoints(
        this WebApplication app)
    {
        var group = app.MapGroup("/api/game/assembly");

        group.MapGet("/", GetStatusAsync);
        group.MapPost("/orders", EnqueueAsync);
    }

    private static async Task<IResult> GetStatusAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var state = await LoadStateAsync(
            dbContext,
            cancellationToken);

        if (state is null)
        {
            return Results.NotFound();
        }

        ShipAssemblyService.Process(
            state.Planet,
            DateTime.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(CreateStatus(state.Planet));
    }

    private static async Task<IResult> EnqueueAsync(
        CreateShipAssemblyOrderRequest request,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var state = await LoadStateAsync(
            dbContext,
            cancellationToken);

        if (state is null)
        {
            return Results.NotFound();
        }

        var blueprint = state.Player.Blueprints
            .SingleOrDefault(x => x.Id == request.BlueprintId);

        if (blueprint is null)
        {
            return Results.BadRequest(new
            {
                error = "Blueprint does not exist."
            });
        }

        try
        {
            ShipAssemblyService.Enqueue(
                state.Player,
                state.Planet,
                blueprint,
                request.Quantity,
                DateTime.UtcNow);
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(new
            {
                error = exception.Message
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(CreateStatus(state.Planet));
    }

    private static async Task<AssemblyState?> LoadStateAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var player = await dbContext.Players
            .Include(x => x.Blueprints)
            .ThenInclude(x => x.Modules)
            .SingleOrDefaultAsync(cancellationToken);

        if (player is null)
        {
            return null;
        }

        var planet = await dbContext.Planets
            .AsSplitQuery()
            .Include(x => x.ComponentInventory)
            .Include(x => x.AssemblyOrders)
            .ThenInclude(x => x.Blueprint)
            .ThenInclude(x => x.Modules)
            .Include(x => x.Ships)
            .ThenInclude(x => x.Blueprint)
            .SingleOrDefaultAsync(
                x => x.PlayerId == player.Id,
                cancellationToken);

        return planet is null
            ? null
            : new AssemblyState(player, planet);
    }

    private static ShipAssemblyStatusResponse CreateStatus(
        Planet planet)
    {
        var inventory = planet.ComponentInventory
            .OrderBy(x => x.ComponentCode)
            .Select(x => new AssemblyInventoryResponse(
                x.ComponentCode,
                x.Quantity))
            .ToList();

        var orders = planet.AssemblyOrders
            .OrderBy(x => x.QueuePosition)
            .Select(x => new ShipAssemblyOrderResponse(
                x.Id,
                x.QueuePosition,
                x.ShipBlueprintId,
                x.Blueprint.Name,
                x.Blueprint.Version,
                x.Quantity,
                x.StartedAt,
                x.CompletesAt))
            .ToList();

        var reserve = planet.Ships
            .OrderBy(x => x.CreatedAt)
            .Select(x => new ReserveShipResponse(
                x.Id,
                x.Name,
                x.ShipBlueprintId,
                x.Blueprint.Name,
                x.Blueprint.Version,
                x.CreatedAt))
            .ToList();

        return new ShipAssemblyStatusResponse(
            planet.AssemblyComplexLevel,
            inventory,
            orders,
            reserve);
    }

    private sealed record AssemblyState(
        Player Player,
        Planet Planet);
}

public sealed record CreateShipAssemblyOrderRequest(
    Guid BlueprintId,
    int Quantity);

public sealed record ShipAssemblyStatusResponse(
    int AssemblyComplexLevel,
    List<AssemblyInventoryResponse> Inventory,
    List<ShipAssemblyOrderResponse> Orders,
    List<ReserveShipResponse> Reserve);

public sealed record AssemblyInventoryResponse(
    string ComponentCode,
    int Quantity);

public sealed record ShipAssemblyOrderResponse(
    Guid Id,
    int QueuePosition,
    Guid BlueprintId,
    string BlueprintName,
    int BlueprintVersion,
    int Quantity,
    DateTime? StartedAt,
    DateTime? CompletesAt);

public sealed record ReserveShipResponse(
    Guid Id,
    string Name,
    Guid BlueprintId,
    string BlueprintName,
    int BlueprintVersion,
    DateTime CreatedAt);

