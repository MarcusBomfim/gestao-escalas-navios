using PortManagement.Application.Common;
using PortManagement.Domain.Common;
using PortManagement.Domain.PortCalls;

namespace PortManagement.Application.Planning;

public sealed class ConfirmBerthWindowHandler(
    IBerthWindowRepository windows,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<BerthWindowResponse>> HandleAsync(
        ChangeBerthWindowStatusCommand command,
        CancellationToken cancellationToken)
    {
        var context = await GetContextAsync(windows, command, cancellationToken);
        if (!context.IsSuccess)
        {
            return Result.Failure<BerthWindowResponse>(context.Error!);
        }

        var (portCall, window) = context.Value!;
        if (await windows.ConfirmedOverlapExistsAsync(
                window.BerthId,
                window.StartsAtUtc,
                window.EndsAtUtc,
                window.Id,
                cancellationToken))
        {
            return Result.Failure<BerthWindowResponse>(BerthWindowRules.OverlapConflict());
        }

        try
        {
            var now = DateTimeOffset.UtcNow;
            window.Confirm(command.ChangedBy, now);
            if (portCall.Status == PortCallStatus.UnderReview)
            {
                portCall.TransitionTo(
                    PortCallStatus.Planned,
                    command.ChangedBy,
                    now,
                    "Janela de berço confirmada.");
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (OptimisticConcurrencyException)
            {
                return Result.Failure<BerthWindowResponse>(BerthWindowRules.VersionConflict());
            }
            catch (ExclusionConstraintException)
            {
                return Result.Failure<BerthWindowResponse>(BerthWindowRules.OverlapConflict());
            }

            return Result.Success(await GetSavedAsync(windows, window.Id, cancellationToken));
        }
        catch (DomainException exception)
        {
            return Result.Failure<BerthWindowResponse>(ApplicationErrors.Validation(
                "planning.invalid_status_change",
                exception.Message));
        }
    }

    private static async Task<Result<(PortCall PortCall, Domain.Planning.BerthWindow Window)>> GetContextAsync(
        IBerthWindowRepository windows,
        ChangeBerthWindowStatusCommand command,
        CancellationToken cancellationToken)
    {
        var portCallReference = await windows.FindPortCallForPlanningAsync(
            command.PortCallPublicCode.Trim().ToUpperInvariant(),
            cancellationToken);
        if (portCallReference is null)
        {
            return Result.Failure<(PortCall, Domain.Planning.BerthWindow)>(ApplicationErrors.NotFound(
                "port_calls.not_found",
                "A escala solicitada não foi encontrada."));
        }

        var portCall = portCallReference.PortCall;

        var window = await windows.FindActiveTrackedByPortCallAsync(portCall.Id, cancellationToken);
        if (window is null)
        {
            return Result.Failure<(PortCall, Domain.Planning.BerthWindow)>(ApplicationErrors.NotFound(
                "planning.window_not_found",
                "A escala não possui uma janela de berço ativa."));
        }

        return window.Version != command.ExpectedWindowVersion
            ? Result.Failure<(PortCall, Domain.Planning.BerthWindow)>(BerthWindowRules.VersionConflict())
            : Result.Success((portCall, window));
    }

    private static async Task<BerthWindowResponse> GetSavedAsync(
        IBerthWindowRepository windows,
        Guid id,
        CancellationToken cancellationToken) =>
        await windows.GetDetailsByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("A janela atualizada não pôde ser consultada.");
}

public sealed class CancelBerthWindowHandler(
    IBerthWindowRepository windows,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<BerthWindowResponse>> HandleAsync(
        ChangeBerthWindowStatusCommand command,
        CancellationToken cancellationToken)
    {
        var portCallReference = await windows.FindPortCallForPlanningAsync(
            command.PortCallPublicCode.Trim().ToUpperInvariant(),
            cancellationToken);
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

        try
        {
            var now = DateTimeOffset.UtcNow;
            window.Cancel(command.ChangedBy, command.Reason ?? string.Empty, now);
            portCall.ClearPlan(now);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (OptimisticConcurrencyException)
            {
                return Result.Failure<BerthWindowResponse>(BerthWindowRules.VersionConflict());
            }

            var response = await windows.GetDetailsByIdAsync(window.Id, cancellationToken)
                ?? throw new InvalidOperationException("A janela cancelada não pôde ser consultada.");
            return Result.Success(response);
        }
        catch (DomainException exception)
        {
            return Result.Failure<BerthWindowResponse>(ApplicationErrors.Validation(
                "planning.invalid_status_change",
                exception.Message));
        }
    }
}
