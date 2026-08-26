using System.Security.Cryptography;
using System.Text;
using PortManagement.Application.Common;
using PortManagement.Application.Security;
using PortManagement.Domain.Common;
using PortManagement.Domain.Organizations;
using PortManagement.Domain.PortCalls;

namespace PortManagement.Application.PortCalls;

public sealed class CreatePortCallHandler(
    IPortCallRepository portCalls,
    IUnitOfWork unitOfWork,
    IUserDataScope dataScope)
{
    public async Task<Result<CreatePortCallResponse>> HandleAsync(
        CreatePortCallCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            return Result.Failure<CreatePortCallResponse>(ApplicationErrors.Validation(
                "port_calls.idempotency_key_required",
                "O cabeçalho Idempotency-Key é obrigatório."));
        }

        var normalizedKey = command.IdempotencyKey.Trim();
        if (normalizedKey.Length > 100)
        {
            return Result.Failure<CreatePortCallResponse>(ApplicationErrors.Validation(
                "port_calls.idempotency_key_too_long",
                "O cabeçalho Idempotency-Key deve possuir no máximo 100 caracteres."));
        }

        var assignmentResult = await ResolveOrganizationAssignmentAsync(cancellationToken);
        if (!assignmentResult.IsSuccess)
        {
            return Result.Failure<CreatePortCallResponse>(assignmentResult.Error!);
        }

        var assignment = assignmentResult.Value!;
        var storedIdempotencyKey = ScopeIdempotencyKey(normalizedKey, assignment.OrganizationId);
        var existing = await portCalls.FindByIdempotencyKeyAsync(storedIdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return await ExistingResponseAsync(existing.PublicCode, cancellationToken);
        }

        if (!await portCalls.ActiveVesselExistsAsync(command.VesselId, cancellationToken))
        {
            return Result.Failure<CreatePortCallResponse>(ApplicationErrors.NotFound(
                "port_calls.vessel_not_found",
                "O navio ativo informado não foi encontrado."));
        }

        if (!await portCalls.ActivePortExistsAsync(command.PortId, cancellationToken))
        {
            return Result.Failure<CreatePortCallResponse>(ApplicationErrors.NotFound(
                "port_calls.port_not_found",
                "O porto ativo informado não foi encontrado."));
        }

        try
        {
            var now = DateTimeOffset.UtcNow;
            var portCall = new PortCall(
                Guid.NewGuid(),
                command.VesselId,
                command.PortId,
                command.Purpose,
                storedIdempotencyKey,
                now,
                command.VoyageNumber,
                command.PreviousPortUnLocode,
                command.NextPortUnLocode);
            portCall.AssignOrganizations(
                assignment.AgentOrganizationId,
                assignment.ShippingLineOrganizationId,
                now);

            await portCalls.AddAsync(portCall, cancellationToken);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (UniqueConstraintException exception)
                when (exception.ConstraintName == "ix_port_calls_idempotency_key")
            {
                var concurrent = await portCalls.FindByIdempotencyKeyAsync(
                    storedIdempotencyKey,
                    cancellationToken);
                if (concurrent is not null)
                {
                    return await ExistingResponseAsync(concurrent.PublicCode, cancellationToken);
                }

                throw;
            }

            var response = await portCalls.GetDetailsByPublicCodeAsync(portCall.PublicCode, cancellationToken)
                ?? throw new InvalidOperationException("A escala criada não pôde ser consultada.");

            return Result.Success(new CreatePortCallResponse(response, true));
        }
        catch (DomainException exception)
        {
            return Result.Failure<CreatePortCallResponse>(ApplicationErrors.Validation(
                "port_calls.invalid_data",
                exception.Message));
        }
    }

    private async Task<Result<OrganizationAssignment>> ResolveOrganizationAssignmentAsync(
        CancellationToken cancellationToken)
    {
        if (dataScope.HasGlobalAccess)
        {
            return Result.Success(new OrganizationAssignment(null, null, null));
        }

        if (dataScope.OrganizationId is not Guid organizationId)
        {
            return Result.Failure<OrganizationAssignment>(ApplicationErrors.Forbidden(
                "port_calls.organization_scope_required",
                "A conta precisa estar vinculada a uma organização ativa para criar escalas."));
        }

        var organizationType = await portCalls.GetActiveOrganizationTypeAsync(
            organizationId,
            cancellationToken);

        return organizationType switch
        {
            OrganizationType.ShippingAgency => Result.Success(
                new OrganizationAssignment(organizationId, organizationId, null)),
            OrganizationType.ShippingLine => Result.Success(
                new OrganizationAssignment(organizationId, null, organizationId)),
            _ => Result.Failure<OrganizationAssignment>(ApplicationErrors.Forbidden(
                "port_calls.organization_not_allowed",
                "A organização vinculada não pode originar uma escala."))
        };
    }

    private static string ScopeIdempotencyKey(string key, Guid? organizationId)
    {
        if (!organizationId.HasValue)
        {
            return key;
        }

        var scopedValue = $"{organizationId.Value:N}:{key}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(scopedValue)));
    }

    private async Task<Result<CreatePortCallResponse>> ExistingResponseAsync(
        string publicCode,
        CancellationToken cancellationToken)
    {
        var response = await portCalls.GetDetailsByPublicCodeAsync(publicCode, cancellationToken)
            ?? throw new InvalidOperationException("A escala idempotente não pôde ser consultada.");

        return Result.Success(new CreatePortCallResponse(response, false));
    }

    private sealed record OrganizationAssignment(
        Guid? OrganizationId,
        Guid? AgentOrganizationId,
        Guid? ShippingLineOrganizationId);
}
