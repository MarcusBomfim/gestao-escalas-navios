using PortManagement.Application.Common;
using PortManagement.Domain.Common;

namespace PortManagement.Application.Planning;

public sealed class ReprogramBerthWindowHandler(
    IBerthWindowRepository windows,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<BerthWindowResponse>> HandleAsync(
        ReprogramBerthWindowCommand command,
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

        var window = await windows.FindActiveTrackedByPortCallAsync(portCall.Id, cancellationToken);
        if (window is null)
        {
            return Result.Failure<BerthWindowResponse>(ApplicationErrors.NotFound(
                "planning.window_not_found",
                "A escala não possui uma janela de berço ativa."));
        }

        if (window.Version != command.ExpectedWindowVersion)
        {
            return Result.Failure<BerthWindowResponse>(BerthWindowRules.VersionConflict());
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
                window.Id,
                cancellationToken))
        {
            return Result.Failure<BerthWindowResponse>(BerthWindowRules.OverlapConflict());
        }

        try
        {
            var now = DateTimeOffset.UtcNow;
            window.ReprogramAt(
                berth.Id,
                command.StartsAtUtc,
                command.EndsAtUtc,
                command.ChangedBy,
                command.Reason,
                now);
            portCall.PlanAt(berth.TerminalId, berth.Id, now);

            var saveError = await SaveAsync(unitOfWork, cancellationToken);
            if (saveError is not null)
            {
                return Result.Failure<BerthWindowResponse>(saveError);
            }

            var response = await windows.GetDetailsByIdAsync(window.Id, cancellationToken)
                ?? throw new InvalidOperationException("A janela reprogramada não pôde ser consultada.");

            return Result.Success(response);
        }
        catch (DomainException exception)
        {
            return Result.Failure<BerthWindowResponse>(ApplicationErrors.Validation(
                "planning.invalid_window",
                exception.Message));
        }
    }

    private static async Task<ApplicationError?> SaveAsync(
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (OptimisticConcurrencyException)
        {
            return BerthWindowRules.VersionConflict();
        }
        catch (ExclusionConstraintException)
        {
            return BerthWindowRules.OverlapConflict();
        }
    }
}
