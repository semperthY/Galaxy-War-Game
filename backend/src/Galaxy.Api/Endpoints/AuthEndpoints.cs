using System.Net.Mail;
using System.Security.Claims;
using Galaxy.Api.Security;
using Galaxy.Application.Games;
using Galaxy.Domain.Entities;
using Galaxy.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace Galaxy.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapGet("/me", GetCurrentAsync);
        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);
        group.MapPost("/logout", LogoutAsync)
            .RequireAuthorization();
        group.MapPost("/race", SelectRaceAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> GetCurrentAsync(
        HttpContext httpContext,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!CurrentAccount.TryGetId(httpContext.User, out var accountId))
        {
            return Results.Ok(new { authenticated = false });
        }

        var account = await dbContext.UserAccounts
            .AsNoTracking()
            .Include(x => x.Player)
            .SingleOrDefaultAsync(x => x.Id == accountId, cancellationToken);

        return account is null
            ? Results.Ok(new { authenticated = false })
            : Results.Ok(CreateSessionResponse(account));
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        HttpContext httpContext,
        ApplicationDbContext dbContext,
        PasswordHashingService passwordHashing,
        CancellationToken cancellationToken)
    {
        var commanderName = request.CommanderName?.Trim() ?? string.Empty;
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var password = request.Password ?? string.Empty;
        var confirmation = request.ConfirmPassword ?? string.Empty;

        var validationError = ValidateRegistration(
            commanderName,
            email,
            password,
            confirmation);
        if (validationError is not null)
        {
            return Results.BadRequest(new { error = validationError });
        }

        if (await dbContext.UserAccounts.AnyAsync(
                x => x.Email == email,
                cancellationToken))
        {
            return Results.Conflict(new
            {
                error = "An account with this email already exists."
            });
        }

        if (await dbContext.UserAccounts.AnyAsync(
                x => x.CommanderName == commanderName,
                cancellationToken) ||
            await dbContext.Players.AnyAsync(
                x => x.Username == commanderName,
                cancellationToken))
        {
            return Results.Conflict(new
            {
                error = "This commander name is already taken."
            });
        }

        var account = new UserAccount
        {
            Id = Guid.NewGuid(),
            Email = email,
            CommanderName = commanderName,
            PasswordHash = passwordHashing.Hash(password),
            CreatedAt = DateTime.UtcNow
        };

        dbContext.UserAccounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);
        await SignInAsync(httpContext, account);

        return Results.Created("/api/auth/me", CreateSessionResponse(account));
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        HttpContext httpContext,
        ApplicationDbContext dbContext,
        PasswordHashingService passwordHashing,
        CancellationToken cancellationToken)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var password = request.Password ?? string.Empty;
        var account = await dbContext.UserAccounts
            .Include(x => x.Player)
            .SingleOrDefaultAsync(x => x.Email == email, cancellationToken);

        if (account is null ||
            !passwordHashing.Verify(password, account.PasswordHash))
        {
            return Results.Json(
                new { error = "Invalid email or password." },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        await SignInAsync(httpContext, account);
        return Results.Ok(CreateSessionResponse(account));
    }

    private static async Task<IResult> LogoutAsync(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.NoContent();
    }

    private static async Task<IResult> SelectRaceAsync(
        SelectRaceRequest request,
        HttpContext httpContext,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Race))
        {
            return Results.BadRequest(new { error = "Race must be selected." });
        }

        if (!CurrentAccount.TryGetId(httpContext.User, out var accountId))
        {
            return Results.Unauthorized();
        }

        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync<IResult>(async () =>
        {
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(
                    System.Data.IsolationLevel.ReadCommitted,
                    cancellationToken);
            await dbContext.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_xact_lock(20260812213000)",
                cancellationToken);

            var account = await dbContext.UserAccounts
                .Include(x => x.Player)
                .SingleOrDefaultAsync(x => x.Id == accountId, cancellationToken);

            if (account is null)
            {
                return Results.Unauthorized();
            }

            if (account.Player is not null)
            {
                return Results.Conflict(new
                {
                    error = "Race has already been selected and cannot be changed."
                });
            }

            Player player;
            if (!await dbContext.StarSystems.AnyAsync(cancellationToken))
            {
                var game = NewGameFactory.Create(
                    account.CommanderName,
                    request.Race);
                player = game.Player;
                dbContext.StarSystems.AddRange(game.StarSystems);
            }
            else
            {
                var homeworld = await dbContext.Planets
                    .Where(x => x.PlayerId == null)
                    .OrderBy(x => x.StarSystem.GalaxyNumber)
                    .ThenBy(x => x.StarSystem.SystemNumber)
                    .ThenBy(x => x.Position)
                    .FirstOrDefaultAsync(cancellationToken);

                if (homeworld is null)
                {
                    return Results.Conflict(new
                    {
                        error = "The galaxy has no free planet for a new commander."
                    });
                }

                player = NewGameFactory.ClaimHomeworld(
                    account.CommanderName,
                    request.Race,
                    homeworld,
                    DateTime.UtcNow);
            }

            dbContext.Players.Add(player);
            account.Player = player;
            account.PlayerId = player.Id;

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Results.Ok(CreateSessionResponse(account));
        });
    }

    private static async Task SignInAsync(
        HttpContext httpContext,
        UserAccount account)
    {
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
                new Claim(ClaimTypes.Name, account.CommanderName)
            },
            CookieAuthenticationDefaults.AuthenticationScheme);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
            });
    }

    private static object CreateSessionResponse(UserAccount account) => new
    {
        authenticated = true,
        commanderName = account.CommanderName,
        email = account.Email,
        requiresRaceSelection = account.Player is null,
        race = account.Player?.Race.ToString()
    };

    private static string? ValidateRegistration(
        string commanderName,
        string email,
        string password,
        string confirmation)
    {
        if (commanderName.Length is < 3 or > 32)
        {
            return "Commander name must contain from 3 to 32 characters.";
        }

        try
        {
            var parsed = new MailAddress(email);
            if (!string.Equals(parsed.Address, email, StringComparison.OrdinalIgnoreCase))
            {
                return "Enter a valid email address.";
            }
        }
        catch (FormatException)
        {
            return "Enter a valid email address.";
        }
        catch (ArgumentException)
        {
            return "Enter a valid email address.";
        }

        if (password.Length < 10 ||
            !password.Any(char.IsLetter) ||
            !password.Any(char.IsDigit))
        {
            return "Password must contain at least 10 characters, a letter and a digit.";
        }

        return password == confirmation
            ? null
            : "Password confirmation does not match.";
    }
}

public sealed record RegisterRequest(
    string? CommanderName,
    string? Email,
    string? Password,
    string? ConfirmPassword);

public sealed record LoginRequest(string? Email, string? Password);

public sealed record SelectRaceRequest(RaceType Race);
