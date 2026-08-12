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

        ResearchService.Complete(
            state.Player,
            utcNow);

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

        ResearchService.Complete(
            state.Player,
            utcNow);

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
            .SingleOrDefaultAsync(x => x.Id == playerId, cancellationToken);

        if (player is null)
        {
            return null;
        }

        var planet = await dbContext.Planets
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
        var technologies = Enum.GetValues<TechnologyType>()
            .Select(technology =>
            {
                var currentLevel = ResearchService.GetLevel(
                    player,
                    technology);

                return new TechnologyOptionResponse(
                    technology,
                    currentLevel,
                    ResearchService.CalculateCost(
                        technology,
                        currentLevel + 1));
            })
            .ToList();

        return new ResearchStatusResponse(
            planet.Materials,
            planet.Deuterium,
            planet.ResearchLaboratoryLevel,
            player.QueuedTechnology,
            player.QueuedTechnologyLevel,
            player.ResearchCompletesAt,
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
    TechnologyType? QueuedTechnology,
    int? QueuedTechnologyLevel,
    DateTime? ResearchCompletesAt,
    List<TechnologyOptionResponse> Technologies);

public sealed record TechnologyOptionResponse(
    TechnologyType Technology,
    int CurrentLevel,
    ResearchCost NextLevelCost);




