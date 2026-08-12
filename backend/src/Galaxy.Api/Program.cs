using Galaxy.Api.Endpoints;
using Galaxy.Api.Hosting;
using Galaxy.Api.Security;
using Galaxy.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Database")
    ?? throw new InvalidOperationException(
        "Connection string 'Database' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsqlOptions =>
            npgsqlOptions.EnableRetryOnFailure()));

builder.Services.AddOpenApi();
builder.Services.AddSingleton<PasswordHashingService>();
builder.Services.AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "galaxy_session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter());
});

var app = builder.Build();

await app.MigrateDatabaseAsync();

app.UseTestAccessProtection();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (app.Environment.IsDevelopment() ||
    app.Configuration.GetValue<bool>(
        "DevelopmentTools:Enabled"))
{
    app.MapDevelopmentEndpoints();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

var defaultFiles = new DefaultFilesOptions();
defaultFiles.DefaultFileNames.Clear();
defaultFiles.DefaultFileNames.Add("start.html");
app.UseDefaultFiles(defaultFiles);
app.UseStaticFiles();

app.MapGet("/game/{page?}", async context =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(
        Path.Combine(app.Environment.WebRootPath, "index.html"));
});

app.MapAuthEndpoints();
app.MapGameEndpoints();
app.MapPlanetEndpoints();
app.MapGalaxyEndpoints();
app.MapBuildingEndpoints();
app.MapResearchEndpoints();
app.MapProductionEndpoints();
app.MapComponentCatalogEndpoints();
app.MapShipBlueprintEndpoints();
app.MapShipAssemblyEndpoints();
app.MapColonizationEndpoints();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy"
}));

app.Run();
