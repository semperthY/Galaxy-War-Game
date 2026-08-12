using Galaxy.Application.Components;
using Galaxy.Application.Colonization;
using Galaxy.Domain.Entities;
using Galaxy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Galaxy.Api.Endpoints;

public static class DevelopmentEndpoints
{
    private const decimal MinimumMaterials = 100_000m;
    private const decimal MinimumDeuterium = 50_000m;
    private const int MinimumComponentQuantity = 100;

    public static void MapDevelopmentEndpoints(
        this WebApplication app)
    {
        var group = app.MapGroup("/api/dev");

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
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var planet = await dbContext.Planets
            .Include(x => x.ComponentInventory)
            .SelectOwnedPlanet(planetId)
            .FirstOrDefaultAsync(cancellationToken);

        if (planet is null)
        {
            return Results.NotFound();
        }

        planet.Materials = decimal.Max(
            planet.Materials,
            MinimumMaterials);

        planet.Deuterium = decimal.Max(
            planet.Deuterium,
            MinimumDeuterium);

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
                        Quantity = MinimumComponentQuantity
                    });

                continue;
            }

            inventoryItem.Quantity = int.Max(
                inventoryItem.Quantity,
                MinimumComponentQuantity);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new DevelopmentSupplyResponse(
            planet.Id,
            planet.Materials,
            planet.Deuterium,
            planet.ComponentInventory.Count,
            MinimumComponentQuantity));
    }

    private static async Task<IResult> CompleteColonizationAsync(
        Guid operationId,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var operation = await dbContext.ColonizationOperations
            .Include(x => x.TargetPlanet)
            .ThenInclude(x => x.StarSystem)
            .SingleOrDefaultAsync(
                x => x.Id == operationId,
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
    int MinimumQuantityPerComponent);
