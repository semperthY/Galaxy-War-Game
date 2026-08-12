using Galaxy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Galaxy.Api.Endpoints;

public static class GalaxyEndpoints
{
    public static void MapGalaxyEndpoints(this WebApplication app)
    {
        app.MapGet("/api/galaxy", GetGalaxyAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> GetGalaxyAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var systems = await dbContext.StarSystems
            .AsNoTracking()
            .OrderBy(x => x.GalaxyNumber)
            .ThenBy(x => x.SystemNumber)
            .Select(system => new GalaxySystemResponse(
                system.Id,
                system.GalaxyNumber,
                system.SystemNumber,
                system.Name,
                system.Planets
                    .OrderBy(planet => planet.Position)
                    .Select(planet => new GalaxyPlanetResponse(
                        planet.Id,
                        planet.Name,
                        planet.Position,
                        planet.PlayerId,
                        planet.Player != null
                            ? planet.Player.Username
                            : null))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return Results.Ok(systems);
    }
}

public sealed record GalaxySystemResponse(
    Guid Id,
    int Galaxy,
    int System,
    string Name,
    List<GalaxyPlanetResponse> Planets);

public sealed record GalaxyPlanetResponse(
    Guid Id,
    string Name,
    int Position,
    Guid? PlayerId,
    string? PlayerName);
