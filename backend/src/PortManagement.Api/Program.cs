using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PortManagement.Api.Contracts;
using PortManagement.Api.Endpoints.Planning;
using PortManagement.Api.Endpoints.PortCalls;
using PortManagement.Api.Endpoints.ReferenceData;
using PortManagement.Api.Endpoints.Security;
using PortManagement.Api.Endpoints.Vessels;
using PortManagement.Api.Security;
using PortManagement.Application;
using PortManagement.Infrastructure;
using PortManagement.Infrastructure.Persistence;
using PortManagement.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var databaseConnection = builder.Configuration.GetConnectionString("Database")
    ?? throw new InvalidOperationException(
        "A conexão 'ConnectionStrings:Database' não foi configurada.");
var jwtOptions = new JwtOptions
{
    Issuer = builder.Configuration["Jwt:Issuer"] ?? string.Empty,
    Audience = builder.Configuration["Jwt:Audience"] ?? string.Empty,
    SigningKey = builder.Configuration["Jwt:SigningKey"] ?? string.Empty,
    AccessTokenMinutes = builder.Configuration.GetValue("Jwt:AccessTokenMinutes", 15),
    RefreshTokenDays = builder.Configuration.GetValue("Jwt:RefreshTokenDays", 7)
};
jwtOptions.Validate();
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .GetChildren()
    .Select(origin => origin.Value)
    .OfType<string>()
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services
    .AddApplication()
    .AddInfrastructure(databaseConnection, jwtOptions);
builder.Services.AddApiSecurity(jwtOptions, allowedOrigins);

var app = builder.Build();

if (args.Contains("--migrate", StringComparer.Ordinal))
{
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<PortManagementDbContext>();
    await database.Database.MigrateAsync();
    return;
}

if (args.Contains("--seed-demo", StringComparer.Ordinal))
{
    await using var scope = app.Services.CreateAsyncScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
    await seeder.SeedAsync();
    return;
}

app.UseExceptionHandler();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors(SecurityConfiguration.WebClientCorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet(
        "/api/v1",
        () => Results.Ok(
            new ApiInfoResponse(
                "Port Management API",
                "v1",
                "healthy",
                DateTimeOffset.UtcNow)))
    .WithName("GetApiInfo");

app.MapHealthChecks("/health");
app.MapVesselEndpoints();
app.MapPortCallEndpoints();
app.MapPlanningEndpoints();
app.MapReferenceDataEndpoints();
app.MapSecurityEndpoints();

app.MapGet(
        "/health/database",
        async (PortManagementDbContext database, CancellationToken cancellationToken) =>
            await database.Database.CanConnectAsync(cancellationToken)
                ? Results.Ok(new { status = "healthy" })
                : Results.Problem(
                    title: "Database unavailable",
                    detail: "A API não conseguiu acessar o banco de dados.",
                    statusCode: StatusCodes.Status503ServiceUnavailable))
    .WithName("GetDatabaseHealth");

app.Run();

public partial class Program;
