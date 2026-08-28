using PortManagement.Api.Common;
using PortManagement.Application.Administration;
using PortManagement.Application.Security;
using PortManagement.Domain.Organizations;
using PortManagement.Domain.Ports;
using PortManagement.Domain.Vessels;

namespace PortManagement.Api.Endpoints.Administration;

internal static class MasterDataEndpoints
{
    public static IEndpointRouteBuilder MapMasterDataEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/admin/master-data")
            .WithTags("Master Data Administration")
            .RequireAuthorization(AuthorizationPolicies.ManageMasterData);

        group.MapGet(
                "/organizations",
                async (
                    int? page,
                    int? pageSize,
                    string? search,
                    OrganizationType? type,
                    bool? isActive,
                    ListOrganizationsHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(
                        new ListOrganizationsQuery(
                            page ?? 1,
                            pageSize ?? 20,
                            search,
                            type,
                            isActive),
                        cancellationToken);

                    return result.ToHttpResult(Results.Ok);
                })
            .WithName("ListManagedOrganizations")
            .WithSummary("Lista organizações para administração");

        group.MapPost(
                "/organizations",
                async (
                    CreateOrganizationRequest request,
                    CreateOrganizationHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(
                        new CreateOrganizationCommand(
                            request.Name,
                            request.RegistrationNumber,
                            request.Type),
                        cancellationToken);

                    return result.ToHttpResult(organization => Results.Created(
                        $"/api/v1/admin/master-data/organizations/{organization.Id}",
                        organization));
                })
            .WithName("CreateManagedOrganization")
            .WithSummary("Cadastra uma organização");

        group.MapPut(
                "/organizations/{id:guid}",
                async (
                    Guid id,
                    UpdateOrganizationRequest request,
                    UpdateOrganizationHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(
                        new UpdateOrganizationCommand(
                            id,
                            request.Name,
                            request.RegistrationNumber,
                            request.Type,
                            request.IsActive,
                            request.ExpectedUpdatedAtUtc),
                        cancellationToken);

                    return result.ToHttpResult(Results.Ok);
                })
            .WithName("UpdateManagedOrganization")
            .WithSummary("Atualiza uma organização com controle de concorrência");

        group.MapGet(
                "/ports",
                async (
                    GetMasterPortStructureHandler handler,
                    CancellationToken cancellationToken) =>
                    Results.Ok(await handler.HandleAsync(cancellationToken)))
            .WithName("ListManagedPortStructure")
            .WithSummary("Lista portos, terminais e berços, inclusive inativos");

        group.MapPost(
                "/ports",
                async (
                    CreatePortRequest request,
                    CreatePortHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(
                        new CreatePortCommand(
                            request.Name,
                            request.UnLocode,
                            request.CountryCode,
                            request.TimeZoneId),
                        cancellationToken);

                    return result.ToHttpResult(port => Results.Created(
                        $"/api/v1/admin/master-data/ports/{port.Id}",
                        port));
                })
            .WithName("CreateManagedPort")
            .WithSummary("Cadastra um porto");

        group.MapPut(
                "/ports/{id:guid}",
                async (
                    Guid id,
                    UpdatePortRequest request,
                    UpdatePortHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(
                        new UpdatePortCommand(
                            id,
                            request.Name,
                            request.UnLocode,
                            request.CountryCode,
                            request.TimeZoneId,
                            request.IsActive,
                            request.ExpectedUpdatedAtUtc),
                        cancellationToken);

                    return result.ToHttpResult(Results.Ok);
                })
            .WithName("UpdateManagedPort")
            .WithSummary("Atualiza um porto com controle de concorrência");

        group.MapPost(
                "/ports/{portId:guid}/terminals",
                async (
                    Guid portId,
                    CreateTerminalRequest request,
                    CreateTerminalHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(
                        new CreateTerminalCommand(
                            portId,
                            request.Code,
                            request.Name,
                            request.TimeZoneId),
                        cancellationToken);

                    return result.ToHttpResult(terminal => Results.Created(
                        $"/api/v1/admin/master-data/terminals/{terminal.Id}",
                        terminal));
                })
            .WithName("CreateManagedTerminal")
            .WithSummary("Cadastra um terminal em um porto ativo");

        group.MapPut(
                "/terminals/{id:guid}",
                async (
                    Guid id,
                    UpdateTerminalRequest request,
                    UpdateTerminalHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(
                        new UpdateTerminalCommand(
                            id,
                            request.Code,
                            request.Name,
                            request.TimeZoneId,
                            request.IsActive,
                            request.ExpectedUpdatedAtUtc),
                        cancellationToken);

                    return result.ToHttpResult(Results.Ok);
                })
            .WithName("UpdateManagedTerminal")
            .WithSummary("Atualiza um terminal com controle de concorrência");

        group.MapPost(
                "/terminals/{terminalId:guid}/berths",
                async (
                    Guid terminalId,
                    CreateBerthRequest request,
                    CreateBerthHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(
                        new CreateBerthCommand(
                            terminalId,
                            request.Code,
                            request.Name,
                            request.UsefulLengthMeters,
                            request.MaximumBeamMeters,
                            request.MaximumDraftMeters,
                            request.SupportedVesselTypes),
                        cancellationToken);

                    return result.ToHttpResult(berth => Results.Created(
                        $"/api/v1/admin/master-data/berths/{berth.Id}",
                        berth));
                })
            .WithName("CreateManagedBerth")
            .WithSummary("Cadastra um berço em um terminal ativo");

        group.MapPut(
                "/berths/{id:guid}",
                async (
                    Guid id,
                    UpdateBerthRequest request,
                    UpdateBerthHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(
                        new UpdateBerthCommand(
                            id,
                            request.Code,
                            request.Name,
                            request.UsefulLengthMeters,
                            request.MaximumBeamMeters,
                            request.MaximumDraftMeters,
                            request.SupportedVesselTypes,
                            request.Status,
                            request.ExpectedUpdatedAtUtc),
                        cancellationToken);

                    return result.ToHttpResult(Results.Ok);
                })
            .WithName("UpdateManagedBerth")
            .WithSummary("Atualiza capacidade e situação de um berço");

        return endpoints;
    }
}

internal sealed record CreateOrganizationRequest(
    string Name,
    string RegistrationNumber,
    OrganizationType Type);

internal sealed record UpdateOrganizationRequest(
    string Name,
    string RegistrationNumber,
    OrganizationType Type,
    bool IsActive,
    DateTimeOffset ExpectedUpdatedAtUtc);

internal sealed record CreatePortRequest(
    string Name,
    string UnLocode,
    string CountryCode,
    string TimeZoneId);

internal sealed record UpdatePortRequest(
    string Name,
    string UnLocode,
    string CountryCode,
    string TimeZoneId,
    bool IsActive,
    DateTimeOffset ExpectedUpdatedAtUtc);

internal sealed record CreateTerminalRequest(
    string Code,
    string Name,
    string TimeZoneId);

internal sealed record UpdateTerminalRequest(
    string Code,
    string Name,
    string TimeZoneId,
    bool IsActive,
    DateTimeOffset ExpectedUpdatedAtUtc);

internal sealed record CreateBerthRequest(
    string Code,
    string Name,
    decimal UsefulLengthMeters,
    decimal MaximumBeamMeters,
    decimal MaximumDraftMeters,
    VesselType[] SupportedVesselTypes);

internal sealed record UpdateBerthRequest(
    string Code,
    string Name,
    decimal UsefulLengthMeters,
    decimal MaximumBeamMeters,
    decimal MaximumDraftMeters,
    VesselType[] SupportedVesselTypes,
    BerthStatus Status,
    DateTimeOffset ExpectedUpdatedAtUtc);
