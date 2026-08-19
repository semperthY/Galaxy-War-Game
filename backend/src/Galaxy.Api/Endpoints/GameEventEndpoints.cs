using System.Text.Json;
using Galaxy.Api.Security;
using Galaxy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Galaxy.Api.Endpoints;

public static class GameEventEndpoints
{
    public static void MapGameEventEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/game/events").RequireAuthorization();
        group.MapGet("/", GetEventsAsync);
        group.MapPost("/{eventId:guid}/read", MarkReadAsync);
        group.MapPost("/read-all", MarkAllReadAsync);
    }

    private static async Task<IResult> GetEventsAsync(
        int? limit,
        HttpContext http,
        ApplicationDbContext db,
        CancellationToken token)
    {
        var playerId = await CurrentAccount.GetPlayerIdAsync(http.User, db, token);
        if (playerId is null) return Results.Unauthorized();

        await LivingGalaxyProcessor.ProcessAsync(db, DateTime.UtcNow, token);
        var take = Math.Clamp(limit ?? 50, 0, 100);
        var unreadCount = await db.GameEvents.CountAsync(
            x => x.PlayerId == playerId && x.ReadAt == null,
            token);
        var events = await db.GameEvents.AsNoTracking()
            .Where(x => x.PlayerId == playerId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .ToListAsync(token);

        return Results.Ok(new
        {
            unreadCount,
            events = events.Select(x => new
            {
                x.Id,
                type = x.Type.ToString(),
                x.Title,
                x.Body,
                data = JsonSerializer.Deserialize<JsonElement>(x.DataJson),
                x.CreatedAt,
                x.ReadAt
            })
        });
    }

    private static async Task<IResult> MarkReadAsync(
        Guid eventId,
        HttpContext http,
        ApplicationDbContext db,
        CancellationToken token)
    {
        var playerId = await CurrentAccount.GetPlayerIdAsync(http.User, db, token);
        if (playerId is null) return Results.Unauthorized();
        var gameEvent = await db.GameEvents.SingleOrDefaultAsync(
            x => x.Id == eventId && x.PlayerId == playerId,
            token);
        if (gameEvent is null) return Results.NotFound();
        gameEvent.ReadAt ??= DateTime.UtcNow;
        await db.SaveChangesAsync(token);
        return Results.Ok(new { read = true });
    }

    private static async Task<IResult> MarkAllReadAsync(
        HttpContext http,
        ApplicationDbContext db,
        CancellationToken token)
    {
        var playerId = await CurrentAccount.GetPlayerIdAsync(http.User, db, token);
        if (playerId is null) return Results.Unauthorized();
        var now = DateTime.UtcNow;
        await db.GameEvents
            .Where(x => x.PlayerId == playerId && x.ReadAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ReadAt, now), token);
        return Results.Ok(new { read = true });
    }
}
