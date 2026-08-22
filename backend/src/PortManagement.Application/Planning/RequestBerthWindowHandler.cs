using PortManagement.Application.Common;
using PortManagement.Domain.Common;
using PortManagement.Domain.Planning;

namespace PortManagement.Application.Planning;

public sealed class RequestBerthWindowHandler(
    IBerthWindowRepository windows,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<BerthWindowResponse>> HandleAsync(
        RequestBerthWindowCommand command,
        CancellationToken cancellationToken)
    {
        var publicCode = command.PortCallPublicCode.Trim().ToUpperInvariant();
        var portCallReference = await windows.FindPortCallForPlanningAsync(publicCode, cancellationToken);
        if (portCallReference is null)
        {
            return Result.Failure<BerthWindowResponse>(ApplicationErrors.NotFound(
                "port_calls.not_found",
                "A escala solicitada não foi encontrada."));
        }

        var portCall = portCallReference.PortCall;

        if (portCall.Version != command.ExpectedPortCallVersion)
        {
            return Result.Failure<BerthWindowResponse>(BerthWindowRules.VersionConflict());
        }

        if (await windows.FindActiveTrackedByPortCallAsync(portCall.Id, cancellationToken) is not null)
        {
            return Result.Failure<BerthWindowResponse>(BerthWindowRules.ActiveWindowConflict());
        }

        var berthReference = await windows.FindBerthForPlanningAsync(command.BerthId, cancellationToken);
        if (berthReference is null)
        {
            return Result.Failure<BerthWindowResponse>(ApplicationErrors.NotFound(
                "planning.berth_not_found",
                "O berço selecionado não foi encontrado."));
        }

        var berth = berthReference.Berth;
        var contextError = BerthWindowRules.ValidatePlanningContext(
            portCall,
            berthReference,
            portCallReference.Vessel);
        if (contextError is not null)
        {
            return Result.Failure<BerthWindowResponse>(contextError);
        }

        if (await windows.ConfirmedOverlapExistsAsync(
                berth.Id,
                command.StartsAtUtc,
                command.EndsAtUtc,
                null,
                cancellationToken))
        {
            return Result.Failure<BerthWindowResponse>(BerthWindowRules.OverlapConflict());
        }

        try
        {
            var now = DateTimeOffset.UtcNow;
            var window = new BerthWindow(
                Guid.NewGuid(),
                portCall.Id,
                berth.Id,
                command.StartsAtUtc,
                command.EndsAtUtc,
                command.RequestedBy,
                now);

            portCall.PlanAt(berth.TerminalId, berth.Id, now);
            await windows.AddAsync(window, cancellationToken);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (UniqueConstraintException)
            {
                return Result.Failure<BerthWindowResponse>(BerthWindowRules.ActiveWindowConflict());
            }
            catch (OptimisticConcurrencyException)
            {
                return Result.Failure<BerthWindowResponse>(BerthWindowRules.VersionConflict());
            }

            var response = await windows.GetDetailsByIdAsync(window.Id, cancellationToken)
                ?? throw new InvalidOperationException("A janela criada não pôde ser consultada.");

            return Result.Success(response);
        }
        catch (DomainException exception)
        {
            return Result.Failure<BerthWindowResponse>(ApplicationErrors.Validation(
                "planning.invalid_window",
                exception.Message));
        }
    }
}
