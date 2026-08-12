using System.Security.Claims;
using Galaxy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Galaxy.Api.Security;

public static class CurrentAccount
{
    public static bool TryGetId(ClaimsPrincipal principal, out Guid accountId) =>
        Guid.TryParse(
            principal.FindFirstValue(ClaimTypes.NameIdentifier),
            out accountId);

    public static async Task<Guid?> GetPlayerIdAsync(
        ClaimsPrincipal principal,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!TryGetId(principal, out var accountId))
        {
            return null;
        }

        return await dbContext.UserAccounts
            .Where(x => x.Id == accountId)
            .Select(x => x.PlayerId)
            .SingleOrDefaultAsync(cancellationToken);
    }
}
