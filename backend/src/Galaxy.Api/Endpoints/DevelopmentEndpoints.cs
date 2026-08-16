using Galaxy.Application.Components;
using Galaxy.Application.Colonization;
using Galaxy.Api.Security;
using Galaxy.Domain.Entities;
using Galaxy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Galaxy.Api.Endpoints;

public static class DevelopmentEndpoints
{
    private const decimal MaterialsGrant = 100_000m;
    private const decimal DeuteriumGrant = 50_000m;
    private const int ComponentQuantityGrant = 100;

    public static void MapDevelopmentEndpoints(
        this WebApplication app)
    {
        var group = app.MapGroup("/api/dev")
            .RequireAuthorization();

        group.MapGet("/status", () => Results.Ok(new
        {
            enabled = true
        }));

        group.MapPost("/supply", GrantSupplyAsync);
        group.MapPost(
            "/colonization/{operationId:guid}/complete",
            CompleteColonizationAsync);
    }

    private static async Task<IResult> GrantSupplyAsync(
        Guid? planetId,
        HttpContext httpContext,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var playerId = await CurrentAccount.GetPlayerIdAsync(
            httpContext.User,
            dbContext,
            cancellationToken);
        if (playerId is null)
        {
            return Results.NotFound();
        }

        var planet = await dbContext.Planets
            .Include(x => x.ComponentInventory)
            .SelectOwnedPlanet(playerId.Value, planetId)
            .FirstOrDefaultAsync(cancellationToken);

        if (planet is null)
        {
            return Results.NotFound();
        }

        planet.Materials += MaterialsGrant;
        planet.Deuterium += DeuteriumGrant;

        foreach (var component in StarterComponentCatalog.GetAll())
        {
            var inventoryItem = planet.ComponentInventory
                .SingleOrDefault(item =>
                    string.Equals(
                        item.ComponentCode,
                        component.Code,
                        StringComparison.OrdinalIgnoreCase));

            if (inventoryItem is null)
            {
                planet.ComponentInventory.Add(
                    new ComponentInventoryItem
                    {
                        PlanetId = planet.Id,
                        Planet = planet,
                        ComponentCode = component.Code,
                        Quantity = ComponentQuantityGrant
                    });

                continue;
            }

            inventoryItem.Quantity += ComponentQuantityGrant;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new DevelopmentSupplyResponse(
            planet.Id,
            planet.Materials,
            planet.Deuterium,
            planet.ComponentInventory.Count,
            MaterialsGrant,
            DeuteriumGrant,
            ComponentQuantityGrant));
    }

    private static async Task<IResult> CompleteColonizationAsync(
        Guid operationId,
        HttpContext httpContext,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var playerId = await CurrentAccount.GetPlayerIdAsync(
            httpContext.User,
            dbContext,
            cancellationToken);
        if (playerId is null)
        {
            return Results.NotFound();
        }

        var operation = await dbContext.ColonizationOperations
            .Include(x => x.TargetPlanet)
            .ThenInclude(x => x.StarSystem)
            .SingleOrDefaultAsync(
                x => x.Id == operationId && x.PlayerId == playerId,
                cancellationToken);

        if (operation is null)
        {
            return Results.NotFound();
        }

        var completedAt = DateTime.UtcNow;
        operation.CompletesAt = completedAt;
        var planet = ColonizationService.Complete(
            operation,
            completedAt);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new
        {
            OperationId = operation.Id,
            PlanetId = planet.Id,
            operation.CompletedAt
        });
    }
}

public sealed record DevelopmentSupplyResponse(
    Guid PlanetId,
    decimal Materials,
    decimal Deuterium,
    int ComponentTypes,
    decimal MaterialsGranted,
    decimal DeuteriumGranted,
    int ComponentQuantityGranted);
