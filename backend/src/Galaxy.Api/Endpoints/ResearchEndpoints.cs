using Galaxy.Application.Economy;
using Galaxy.Api.Security;
using Galaxy.Application.Research;
using Galaxy.Domain.Entities;
using Galaxy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Galaxy.Api.Endpoints;

public static class ResearchEndpoints
{
    public static void MapResearchEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/game/research")
            .RequireAuthorization();

        group.MapGet("/", GetStatusAsync);
        group.MapPost("/{technology}/start", StartAsync);
    }

    private static async Task<IResult> GetStatusAsync(
        Guid? planetId,
        HttpContext httpContext,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var state = await LoadStateAsync(
            planetId,
            httpContext,
            dbContext,
            cancellationToken);

        if (state is null)
        {
            return Results.NotFound();
        }

        var utcNow = DateTime.UtcNow;

        var completed = ResearchService.Complete(
            state.Player,
            utcNow);
        dbContext.ResearchOrders.RemoveRange(completed);

        ResourceProductionCalculator.Update(
            state.Planet,
            utcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(CreateStatus(
            state.Player,
            state.Planet));
    }

    private static async Task<IResult> StartAsync(
        Guid? planetId,
        TechnologyType technology,
        HttpContext httpContext,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var state = await LoadStateAsync(
            planetId,
            httpContext,
            dbContext,
            cancellationToken);

        if (state is null)
        {
            return Results.NotFound();
        }

        var utcNow = DateTime.UtcNow;

        var completed = ResearchService.Complete(
            state.Player,
            utcNow);
        dbContext.ResearchOrders.RemoveRange(completed);

        try
        {
            ResearchService.Start(
                state.Player,
                state.Planet,
                technology,
                utcNow);
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(new
            {
                error = exception.Message
            });
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return Results.BadRequest(new { error = exception.Message });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(CreateStatus(
            state.Player,
            state.Planet));
    }

    private static async Task<ResearchState?> LoadStateAsync(
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
            return null;
        }

        var player = await dbContext.Players
            .Include(x => x.Technologies)
            .Include(x => x.ResearchOrders)
            .ThenInclude(x => x.Planet)
            .SingleOrDefaultAsync(x => x.Id == playerId, cancellationToken);

        if (player is null)
        {
            return null;
        }

        var planet = await dbContext.Planets
            .Include(x => x.ResearchOrders)
            .SelectOwnedPlanet(player.Id, planetId)
            .FirstOrDefaultAsync(cancellationToken);

        return planet is null
            ? null
            : new ResearchState(player, planet);
    }

    private static ResearchStatusResponse CreateStatus(
        Player player,
        Planet planet)
    {
        var technologies = TechnologyCatalog.GetAll()
            .Select(definition =>
            {
                var currentLevel = ResearchService.GetLevel(
                    player,
                    definition.Technology);
                var targetLevel = currentLevel + 1;
                var hasNextLevel = definition.Levels.TryGetValue(
                    targetLevel,
                    out var nextLevel);

                return new TechnologyOptionResponse(
                    definition.Technology,
                    definition.Code,
                    definition.Name,
                    definition.Category,
                    currentLevel,
                    definition.MaxLevel,
                    hasNextLevel ? nextLevel!.Cost : null,
                    hasNextLevel ? (int)nextLevel!.Duration.TotalSeconds : null,
                    hasNextLevel ? nextLevel!.RequiredLaboratoryLevel : null,
                    hasNextLevel
                        ? nextLevel!.Requirements.Select(x =>
                            new TechnologyRequirementResponse(
                                x.Technology,
                                TechnologyCatalog.Get(x.Technology).Name,
                                x.Level,
                                ResearchService.GetLevel(player, x.Technology) >= x.Level))
                            .ToList()
                        : new List<TechnologyRequirementResponse>(),
                    player.ResearchOrders.Any(x =>
                        x.Technology == definition.Technology));
            })
            .ToList();

        var activeResearch = planet.ResearchOrders
            .OrderBy(x => x.StreamNumber)
            .Select(x => new ActiveResearchResponse(
                x.Id,
                x.PlanetId,
                x.Planet.Name,
                x.StreamNumber,
                x.Technology,
                TechnologyCatalog.Get(x.Technology).Name,
                x.TargetLevel,
                x.StartedAt,
                x.CompletesAt))
            .ToList();

        var empireActiveResearch = player.ResearchOrders
            .OrderBy(x => x.CompletesAt)
            .Select(x => new ActiveResearchResponse(
                x.Id,
                x.PlanetId,
                x.Planet.Name,
                x.StreamNumber,
                x.Technology,
                TechnologyCatalog.Get(x.Technology).Name,
                x.TargetLevel,
                x.StartedAt,
                x.CompletesAt))
            .ToList();

        return new ResearchStatusResponse(
            planet.Materials,
            planet.Deuterium,
            planet.ResearchLaboratoryLevel,
            ResearchService.GetAvailableStreamCount(player, planet),
            activeResearch,
            empireActiveResearch,
            technologies);
    }

    private sealed record ResearchState(
        Player Player,
        Planet Planet);
}

public sealed record ResearchStatusResponse(
    decimal Materials,
    decimal Deuterium,
    int ResearchLaboratoryLevel,
    int AvailableStreams,
    List<ActiveResearchResponse> ActiveResearch,
    List<ActiveResearchResponse> EmpireActiveResearch,
    List<TechnologyOptionResponse> Technologies);

public sealed record TechnologyOptionResponse(
    TechnologyType Technology,
    string Code,
    string Name,
    string Category,
    int CurrentLevel,
    int MaxLevel,
    ResearchCost? NextLevelCost,
    int? DurationSeconds,
    int? RequiredLaboratoryLevel,
    List<TechnologyRequirementResponse> Requirements,
    bool IsBeingResearched);

public sealed record TechnologyRequirementResponse(
    TechnologyType Technology,
    string Name,
    int Level,
    bool Met);

public sealed record ActiveResearchResponse(
    Guid Id,
    Guid PlanetId,
    string PlanetName,
    int StreamNumber,
    TechnologyType Technology,
    string Name,
    int TargetLevel,
    DateTime StartedAt,
    DateTime CompletesAt);


