using PortManagement.Application.Common;
using PortManagement.Domain.Common;
using PortManagement.Domain.PortCalls;

namespace PortManagement.Application.PortCalls;

public sealed class CreatePortCallHandler(
    IPortCallRepository portCalls,
    IUnitOfWork unitOfWork)
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

        var existing = await portCalls.FindByIdempotencyKeyAsync(normalizedKey, cancellationToken);
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
            var portCall = new PortCall(
                Guid.NewGuid(),
                command.VesselId,
                command.PortId,
                command.Purpose,
                normalizedKey,
                DateTimeOffset.UtcNow,
                command.VoyageNumber,
                command.PreviousPortUnLocode,
                command.NextPortUnLocode);

            await portCalls.AddAsync(portCall, cancellationToken);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (UniqueConstraintException exception)
                when (exception.ConstraintName == "ix_port_calls_idempotency_key")
            {
                var concurrent = await portCalls.FindByIdempotencyKeyAsync(normalizedKey, cancellationToken);
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

    private async Task<Result<CreatePortCallResponse>> ExistingResponseAsync(
        string publicCode,
        CancellationToken cancellationToken)
    {
        var response = await portCalls.GetDetailsByPublicCodeAsync(publicCode, cancellationToken)
            ?? throw new InvalidOperationException("A escala idempotente não pôde ser consultada.");

        return Result.Success(new CreatePortCallResponse(response, false));
    }
}
