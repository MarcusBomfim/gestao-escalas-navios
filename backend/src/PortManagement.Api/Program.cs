using PortManagement.Api.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

var app = builder.Build();

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

app.Run();

public partial class Program;

