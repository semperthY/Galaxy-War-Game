using Galaxy.Domain.Entities;

namespace Galaxy.Api.Endpoints;

internal static class OwnedPlanetQuery
{
    public static IQueryable<Planet> SelectOwnedPlanet(
        this IQueryable<Planet> query,
        Guid playerId,
        Guid? planetId)
    {
        query = query.Where(x => x.PlayerId == playerId);

        if (planetId is not null)
        {
            return query.Where(x => x.Id == planetId.Value);
        }

        return query
            .OrderBy(x => x.StarSystem.SystemNumber)
            .ThenBy(x => x.Position);
    }
}
