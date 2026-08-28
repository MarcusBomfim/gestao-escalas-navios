using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PortManagement.Api.Auditing;
using PortManagement.Api.Contracts;
using PortManagement.Api.Endpoints.Administration;
using PortManagement.Api.Endpoints.Auditing;
using PortManagement.Api.Endpoints.ControlTower;
using PortManagement.Api.Endpoints.Notifications;
using PortManagement.Api.Endpoints.Observability;
using PortManagement.Api.Endpoints.Operations;
using PortManagement.Api.Endpoints.Planning;
using PortManagement.Api.Endpoints.PortCalls;
using PortManagement.Api.Endpoints.ReferenceData;
using PortManagement.Api.Endpoints.Security;
using PortManagement.Api.Endpoints.Vessels;
using PortManagement.Api.Observability;
using PortManagement.Api.OpenApi;
using PortManagement.Api.Realtime;
using PortManagement.Api.Resilience;
using PortManagement.Api.Security;
using PortManagement.Application;
using PortManagement.Application.Auditing;
using PortManagement.Application.Security;
using PortManagement.Infrastructure;
using PortManagement.Infrastructure.Persistence;
using PortManagement.Infrastructure.Resilience;
using PortManagement.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.UseUtcTimestamp = true;
    options.TimestampFormat = "O";
});

builder.Services.AddProblemDetails();
builder.Services.AddSingleton<ApiTelemetry>();
builder.Services
    .AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(),
        tags: ["live"])
    .AddCheck<DatabaseReadinessHealthCheck>(
        "postgresql",
        tags: ["ready"]);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddPortManagementOpenApi();

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
var apiResilienceOptions = new ApiResilienceOptions
{
    RequestTimeoutSeconds = builder.Configuration.GetValue(
        "Resilience:RequestTimeoutSeconds",
        30),
    ShutdownTimeoutSeconds = builder.Configuration.GetValue(
        "Resilience:ShutdownTimeoutSeconds",
        30)
};
apiResilienceOptions.Validate();
var databaseResilienceOptions = new DatabaseResilienceOptions
{
    CommandTimeoutSeconds = builder.Configuration.GetValue(
        "Resilience:Database:CommandTimeoutSeconds",
        30),
    MaxRetryCount = builder.Configuration.GetValue(
        "Resilience:Database:MaxRetryCount",
        3),
    MaxRetryDelaySeconds = builder.Configuration.GetValue(
        "Resilience:Database:MaxRetryDelaySeconds",
        5)
};
databaseResilienceOptions.Validate();
var passwordRecoveryOptions = new PasswordRecoveryOptions
{
    SmtpHost = builder.Configuration["PasswordRecovery:SmtpHost"] ?? "localhost",
    SmtpPort = builder.Configuration.GetValue("PasswordRecovery:SmtpPort", 1025),
    EnableSsl = builder.Configuration.GetValue("PasswordRecovery:EnableSsl", false),
    FromAddress = builder.Configuration["PasswordRecovery:FromAddress"]
        ?? "no-reply@portmanagement.local",
    FromName = builder.Configuration["PasswordRecovery:FromName"] ?? "Port Management",
    Username = builder.Configuration["PasswordRecovery:Username"],
    Password = builder.Configuration["PasswordRecovery:Password"],
    PublicWebUrl = builder.Configuration["PasswordRecovery:PublicWebUrl"]
        ?? "http://localhost:5173",
    TokenLifetimeMinutes = builder.Configuration.GetValue(
        "PasswordRecovery:TokenLifetimeMinutes",
        30)
};
passwordRecoveryOptions.Validate();
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
    .AddInfrastructure(
        databaseConnection,
        jwtOptions,
        databaseResilienceOptions,
        passwordRecoveryOptions);
var dataProtection = builder.Services
    .AddDataProtection()
    .SetApplicationName("PortManagement");
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
}
builder.Services.AddRequestTimeouts(options =>
{
    options.DefaultPolicy = new RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromSeconds(apiResilienceOptions.RequestTimeoutSeconds),
        TimeoutStatusCode = StatusCodes.Status504GatewayTimeout
    };
});
builder.Services.Configure<HostOptions>(options =>
    options.ShutdownTimeout = TimeSpan.FromSeconds(apiResilienceOptions.ShutdownTimeoutSeconds));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditRequestContext, HttpAuditRequestContext>();
builder.Services.AddScoped<IUserDataScope, HttpUserDataScope>();
builder.Services.AddApiSecurity(jwtOptions, allowedOrigins);
builder.Services.AddSignalR();
builder.Services.AddHostedService<ControlTowerBroadcastService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapPortManagementOpenApi();
}

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

app.UseMiddleware<CorrelationAndMetricsMiddleware>();
app.UseExceptionHandler();
app.UseRequestTimeouts();
if (!app.Environment.IsDevelopment()
    && builder.Configuration.GetValue("Security:EnforceHttps", false))
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

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("live"),
        ResponseWriter = HealthResponseWriter.WriteAsync
    });
app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("ready"),
        ResponseWriter = HealthResponseWriter.WriteAsync
    });
app.MapHealthChecks(
    "/health",
    new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("ready"),
        ResponseWriter = HealthResponseWriter.WriteAsync
    });
app.MapVesselEndpoints();
app.MapPortCallEndpoints();
app.MapPlanningEndpoints();
app.MapOperationalExecutionEndpoints();
app.MapControlTowerEndpoints();
app.MapNotificationEndpoints();
app.MapAuditEndpoints();
app.MapObservabilityEndpoints();
app.MapHub<ControlTowerHub>("/hubs/control-tower")
    .DisableRequestTimeout();
app.MapReferenceDataEndpoints();
app.MapSecurityEndpoints();
app.MapMasterDataEndpoints();

app.Run();

public partial class Program;
