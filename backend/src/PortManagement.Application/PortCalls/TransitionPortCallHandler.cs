using PortManagement.Application.Common;
using PortManagement.Domain.Common;
using PortManagement.Domain.PortCalls;

namespace PortManagement.Application.PortCalls;

public sealed class TransitionPortCallHandler(
    IPortCallRepository portCalls,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<PortCallResponse>> HandleAsync(
        TransitionPortCallCommand command,
        CancellationToken cancellationToken)
    {
        var publicCode = command.PublicCode.Trim().ToUpperInvariant();
        var portCall = await portCalls.FindTrackedByPublicCodeAsync(publicCode, cancellationToken);
        if (portCall is null)
        {
            return Result.Failure<PortCallResponse>(ApplicationErrors.NotFound(
                "port_calls.not_found",
                "A escala solicitada não foi encontrada."));
        }

        if (portCall.Version != command.ExpectedVersion)
        {
            return Result.Failure<PortCallResponse>(ConcurrencyConflict());
        }

        if (command.NewStatus is PortCallStatus.AtAnchorage
            or PortCallStatus.ClearedForBerthing
            or PortCallStatus.Berthed
            or PortCallStatus.InOperation
            or PortCallStatus.OperationCompleted
            or PortCallStatus.Unberthed
            or PortCallStatus.Closed)
        {
            return Result.Failure<PortCallResponse>(ApplicationErrors.Validation(
                "port_calls.operational_transition_requires_event",
                "Use o registro de marcos operacionais para avançar esta etapa da escala."));
        }

        try
        {
            portCall.TransitionTo(
                command.NewStatus,
                command.ChangedBy,
                DateTimeOffset.UtcNow,
                command.Reason);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (OptimisticConcurrencyException)
            {
                return Result.Failure<PortCallResponse>(ConcurrencyConflict());
            }

            var response = await portCalls.GetDetailsByPublicCodeAsync(publicCode, cancellationToken)
                ?? throw new InvalidOperationException("A escala atualizada não pôde ser consultada.");

            return Result.Success(response);
        }
        catch (DomainException exception)
        {
            return Result.Failure<PortCallResponse>(ApplicationErrors.Validation(
                "port_calls.invalid_transition",
                exception.Message));
        }
    }

    private static ApplicationError ConcurrencyConflict() => ApplicationErrors.Conflict(
        "port_calls.version_conflict",
        "A escala foi alterada por outra operação. Atualize os dados antes de tentar novamente.");
}
