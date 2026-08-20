using Microsoft.EntityFrameworkCore;
using PortManagement.Api.Contracts;
using PortManagement.Infrastructure;
using PortManagement.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

var databaseConnection = builder.Configuration.GetConnectionString("Database")
    ?? throw new InvalidOperationException(
        "A conexão 'ConnectionStrings:Database' não foi configurada.");

builder.Services.AddInfrastructure(databaseConnection);

var app = builder.Build();

if (args.Contains("--migrate", StringComparer.Ordinal))
{
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<PortManagementDbContext>();
    await database.Database.MigrateAsync();
    return;
}

app.UseExceptionHandler();
app.UseHttpsRedirection();

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
