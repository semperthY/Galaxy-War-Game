using System.Security.Cryptography;
using System.Text;

namespace Galaxy.Api.Hosting;

public static class TestAccessExtensions
{
    public static void UseTestAccessProtection(
        this WebApplication app)
    {
        var password = app.Configuration["TestAccess:Password"];

        if (string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var username = app.Configuration["TestAccess:Username"]
            ?? "tester";

        var expectedCredentials = Encoding.UTF8.GetBytes(
            $"{username}:{password}");

        app.Use(async (context, next) =>
        {
            if (context.Request.Path.Equals(
                    new PathString("/health")))
            {
                await next(context);
                return;
            }

            if (HasValidCredentials(
                    context,
                    expectedCredentials))
            {
                await next(context);
                return;
            }

            context.Response.Headers.WWWAuthenticate =
                "Basic realm=\"Galaxy War Game Test\", charset=\"UTF-8\"";
            context.Response.StatusCode =
                StatusCodes.Status401Unauthorized;
        });
    }

    private static bool HasValidCredentials(
        HttpContext context,
        byte[] expectedCredentials)
    {
        var authorization = context.Request.Headers.Authorization
            .ToString();

        const string prefix = "Basic ";

        if (!authorization.StartsWith(
                prefix,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            var actualCredentials = Convert.FromBase64String(
                authorization[prefix.Length..].Trim());

            return actualCredentials.Length ==
                    expectedCredentials.Length &&
                CryptographicOperations.FixedTimeEquals(
                    actualCredentials,
                    expectedCredentials);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
