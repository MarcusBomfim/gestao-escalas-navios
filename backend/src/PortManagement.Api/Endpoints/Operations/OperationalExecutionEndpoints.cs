using System.Security.Claims;
using PortManagement.Api.Common;
using PortManagement.Application.Operations;
using PortManagement.Application.Security;
using PortManagement.Domain.Operations;

namespace PortManagement.Api.Endpoints.Operations;

internal static class OperationalExecutionEndpoints
{
    public static IEndpointRouteBuilder MapOperationalExecutionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/operations/port-calls/{publicCode}")
            .WithTags("Operational Execution")
            .RequireAuthorization();

        group.MapGet(
                "/",
                async (
                    string publicCode,
                    GetOperationalExecutionHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(publicCode, cancellationToken);
                    return result.ToHttpResult(Results.Ok);
                })
            .WithName("GetOperationalExecution")
            .WithSummary("Consulta linha do tempo, cargas e indicadores da execução");

        group.MapPost(
                "/milestones",
                async (
                    string publicCode,
                    RecordOperationalMilestoneRequest request,
                    ClaimsPrincipal principal,
                    RecordOperationalMilestoneHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(
                        new RecordOperationalMilestoneCommand(
                            publicCode,
                            request.Milestone,
                            request.OccursAtUtc,
                            request.ExpectedPortCallVersion,
                            "Portal operacional",
                            GetActor(principal)),
                        cancellationToken);
                    return result.ToHttpResult(Results.Ok);
                })
            .WithName("RecordOperationalMilestone")
            .WithSummary("Registra um marco real e avança a escala atomicamente")
            .RequireAuthorization(AuthorizationPolicies.ManageOperationalExecution);

        group.MapPost(
                "/cargo-operations",
                async (
                    string publicCode,
                    CreateCargoOperationRequest request,
                    CreateCargoOperationHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(
                        new CreateCargoOperationCommand(
                            publicCode,
                            request.Direction,
                            request.CargoType,
                            request.PlannedQuantity,
                            request.QuantityUnit,
                            request.IsDangerousCargo,
                            request.DangerousCargoClassification,
                            request.PlannedStartAtUtc,
                            request.PlannedEndAtUtc,
                            request.ExpectedPortCallVersion),
                        cancellationToken);
                    return result.ToHttpResult(operation => Results.Created(
                        $"/api/v1/operations/port-calls/{publicCode}/cargo-operations/{operation.Id}",
                        operation));
                })
            .WithName("CreateCargoOperation")
            .WithSummary("Cadastra uma operação de carga planejada")
            .RequireAuthorization(AuthorizationPolicies.ManageOperationalExecution);

        group.MapPost(
                "/cargo-operations/{cargoOperationId:guid}/start",
                async (
                    string publicCode,
                    Guid cargoOperationId,
                    StartCargoOperationRequest request,
                    StartCargoOperationHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(
                        new StartCargoOperationCommand(
                            publicCode,
                            cargoOperationId,
                            request.StartedAtUtc,
                            request.ExpectedVersion),
                        cancellationToken);
                    return result.ToHttpResult(Results.Ok);
                })
            .WithName("StartCargoOperation")
            .WithSummary("Registra o início real de uma carga")
            .RequireAuthorization(AuthorizationPolicies.ManageOperationalExecution);

        group.MapPost(
                "/cargo-operations/{cargoOperationId:guid}/complete",
                async (
                    string publicCode,
                    Guid cargoOperationId,
                    CompleteCargoOperationRequest request,
                    CompleteCargoOperationHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(
                        new CompleteCargoOperationCommand(
                            publicCode,
                            cargoOperationId,
                            request.ActualQuantity,
                            request.CompletedAtUtc,
                            request.ExpectedVersion),
                        cancellationToken);
                    return result.ToHttpResult(Results.Ok);
                })
            .WithName("CompleteCargoOperation")
            .WithSummary("Conclui uma carga com quantidade realizada")
            .RequireAuthorization(AuthorizationPolicies.ManageOperationalExecution);

        return endpoints;
    }

    private static string GetActor(ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
}

internal sealed record RecordOperationalMilestoneRequest(
    OperationalMilestone Milestone,
    DateTimeOffset OccursAtUtc,
    long ExpectedPortCallVersion);

internal sealed record CreateCargoOperationRequest(
    CargoOperationDirection Direction,
    string CargoType,
    decimal PlannedQuantity,
    CargoQuantityUnit QuantityUnit,
    bool IsDangerousCargo,
    string? DangerousCargoClassification,
    DateTimeOffset PlannedStartAtUtc,
    DateTimeOffset PlannedEndAtUtc,
    long ExpectedPortCallVersion);

internal sealed record StartCargoOperationRequest(DateTimeOffset StartedAtUtc, long ExpectedVersion);

internal sealed record CompleteCargoOperationRequest(
    decimal ActualQuantity,
    DateTimeOffset CompletedAtUtc,
    long ExpectedVersion);
