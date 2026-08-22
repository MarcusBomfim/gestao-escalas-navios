using PortManagement.Application.Common;
using PortManagement.Domain.Common;
using PortManagement.Domain.Operations;
using PortManagement.Domain.PortCalls;

namespace PortManagement.Application.Operations;

public sealed class GetOperationalExecutionHandler(IOperationalExecutionRepository repository)
{
    public async Task<Result<OperationalExecutionResponse>> HandleAsync(
        string publicCode,
        CancellationToken cancellationToken)
    {
        var execution = await repository.GetAsync(Normalize(publicCode), cancellationToken);
        return execution is null
            ? Result.Failure<OperationalExecutionResponse>(NotFound())
            : Result.Success(execution);
    }

    internal static string Normalize(string publicCode) => publicCode.Trim().ToUpperInvariant();

    internal static ApplicationError NotFound() => ApplicationErrors.NotFound(
        "operations.port_call_not_found",
        "A escala solicitada não foi encontrada.");
}

public sealed class RecordOperationalMilestoneHandler(
    IOperationalExecutionRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Result<OperationalExecutionResponse>> HandleAsync(
        RecordOperationalMilestoneCommand command,
        CancellationToken cancellationToken)
    {
        var code = GetOperationalExecutionHandler.Normalize(command.PublicCode);
        var portCall = await repository.FindPortCallTrackedAsync(code, cancellationToken);
        if (portCall is null)
        {
            return Result.Failure<OperationalExecutionResponse>(GetOperationalExecutionHandler.NotFound());
        }

        if (portCall.Version != command.ExpectedPortCallVersion)
        {
            return Result.Failure<OperationalExecutionResponse>(VersionConflict());
        }

        var rule = OperationalMilestoneRules.Get(command.Milestone);
        if (portCall.Status != rule.CurrentStatus)
        {
            return Result.Failure<OperationalExecutionResponse>(ApplicationErrors.Validation(
                "operations.invalid_sequence",
                $"O marco {command.Milestone} não é permitido enquanto a escala está em {portCall.Status}."));
        }

        var now = timeProvider.GetUtcNow();
        var occursAt = command.OccursAtUtc.ToUniversalTime();
        if (occursAt > now.AddMinutes(5))
        {
            return Result.Failure<OperationalExecutionResponse>(ApplicationErrors.Validation(
                "operations.future_event",
                "Um evento realizado não pode ser registrado no futuro."));
        }

        var latestEventAt = await repository.GetLatestActualEventAtAsync(portCall.Id, cancellationToken);
        if (latestEventAt.HasValue && occursAt < latestEventAt.Value)
        {
            return Result.Failure<OperationalExecutionResponse>(ApplicationErrors.Validation(
                "operations.non_chronological_event",
                "O horário do evento não pode ser anterior ao último evento operacional."));
        }

        if (command.Milestone == OperationalMilestone.CargoOperationStarted
            && !await repository.HasCargoOperationsAsync(portCall.Id, cancellationToken))
        {
            return Result.Failure<OperationalExecutionResponse>(ApplicationErrors.Validation(
                "operations.cargo_required",
                "Cadastre ao menos uma operação de carga antes de iniciar a operação."));
        }

        if (command.Milestone == OperationalMilestone.CargoOperationCompleted
            && !await repository.AreAllCargoOperationsCompletedAsync(portCall.Id, cancellationToken))
        {
            return Result.Failure<OperationalExecutionResponse>(ApplicationErrors.Validation(
                "operations.cargo_incomplete",
                "Conclua todas as operações de carga antes de encerrar a etapa operacional."));
        }

        try
        {
            var portCallEvent = new PortCallEvent(
                Guid.NewGuid(),
                portCall.Id,
                rule.Phase,
                rule.Action,
                TemporalClassifier.Actual,
                occursAt,
                command.Source,
                command.RecordedBy,
                now);
            await repository.AddEventAsync(portCallEvent, cancellationToken);
            portCall.TransitionTo(rule.TargetStatus, command.RecordedBy, occursAt, $"Marco operacional: {command.Milestone}.");
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success((await repository.GetAsync(code, cancellationToken))!);
        }
        catch (OptimisticConcurrencyException)
        {
            return Result.Failure<OperationalExecutionResponse>(VersionConflict());
        }
        catch (DomainException exception)
        {
            return Result.Failure<OperationalExecutionResponse>(ApplicationErrors.Validation(
                "operations.invalid_event",
                exception.Message));
        }
    }

    private static ApplicationError VersionConflict() => ApplicationErrors.Conflict(
        "operations.port_call_version_conflict",
        "A escala foi alterada por outra operação. Atualize os dados e tente novamente.");
}

public sealed class CreateCargoOperationHandler(
    IOperationalExecutionRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private static readonly PortCallStatus[] AllowedStatuses =
    [
        PortCallStatus.Planned,
        PortCallStatus.AtAnchorage,
        PortCallStatus.ClearedForBerthing,
        PortCallStatus.Berthed,
        PortCallStatus.InOperation
    ];

    public async Task<Result<CargoOperationResponse>> HandleAsync(
        CreateCargoOperationCommand command,
        CancellationToken cancellationToken)
    {
        var code = GetOperationalExecutionHandler.Normalize(command.PublicCode);
        var portCall = await repository.FindPortCallTrackedAsync(code, cancellationToken);
        if (portCall is null)
        {
            return Result.Failure<CargoOperationResponse>(GetOperationalExecutionHandler.NotFound());
        }

        if (portCall.Version != command.ExpectedPortCallVersion)
        {
            return Result.Failure<CargoOperationResponse>(ApplicationErrors.Conflict(
                "operations.port_call_version_conflict",
                "A escala foi alterada por outra operação. Atualize os dados e tente novamente."));
        }

        if (!AllowedStatuses.Contains(portCall.Status))
        {
            return Result.Failure<CargoOperationResponse>(ApplicationErrors.Validation(
                "operations.cargo_registration_closed",
                "Não é possível cadastrar carga na situação atual da escala."));
        }

        try
        {
            var now = timeProvider.GetUtcNow();
            var cargo = new CargoOperation(
                Guid.NewGuid(),
                portCall.Id,
                command.Direction,
                command.CargoType,
                command.PlannedQuantity,
                command.QuantityUnit,
                command.IsDangerousCargo,
                now,
                command.DangerousCargoClassification);
            cargo.Schedule(command.PlannedStartAtUtc, command.PlannedEndAtUtc, now);
            await repository.AddCargoOperationAsync(cargo, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(ToResponse(cargo));
        }
        catch (DomainException exception)
        {
            return Result.Failure<CargoOperationResponse>(ApplicationErrors.Validation(
                "operations.invalid_cargo",
                exception.Message));
        }
    }

    public static CargoOperationResponse ToResponse(CargoOperation cargo) => new(
        cargo.Id,
        cargo.Direction,
        cargo.CargoType,
        cargo.PlannedQuantity,
        cargo.ActualQuantity,
        cargo.QuantityUnit,
        cargo.IsDangerousCargo,
        cargo.DangerousCargoClassification,
        cargo.PlannedStartAtUtc,
        cargo.PlannedEndAtUtc,
        cargo.ActualStartAtUtc,
        cargo.ActualEndAtUtc,
        cargo.Version,
        cargo.ActualEndAtUtc.HasValue ? "Completed" : cargo.ActualStartAtUtc.HasValue ? "InProgress" : "Planned");
}

public sealed class StartCargoOperationHandler(
    IOperationalExecutionRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Result<CargoOperationResponse>> HandleAsync(
        StartCargoOperationCommand command,
        CancellationToken cancellationToken)
    {
        var code = GetOperationalExecutionHandler.Normalize(command.PublicCode);
        var portCall = await repository.FindPortCallTrackedAsync(code, cancellationToken);
        if (portCall is null)
        {
            return Result.Failure<CargoOperationResponse>(GetOperationalExecutionHandler.NotFound());
        }

        var cargo = await repository.FindCargoOperationTrackedAsync(code, command.CargoOperationId, cancellationToken);
        if (cargo is null)
        {
            return Result.Failure<CargoOperationResponse>(CargoNotFound());
        }

        if (portCall.Status != PortCallStatus.InOperation)
        {
            return Result.Failure<CargoOperationResponse>(ApplicationErrors.Validation(
                "operations.port_call_not_in_operation",
                "Registre o início da operação da escala antes de iniciar a carga."));
        }

        return await ChangeAsync(cargo, command.ExpectedVersion, () =>
            cargo.Start(command.StartedAtUtc, timeProvider.GetUtcNow()), cancellationToken);
    }

    private async Task<Result<CargoOperationResponse>> ChangeAsync(
        CargoOperation cargo,
        long expectedVersion,
        Action change,
        CancellationToken cancellationToken)
    {
        if (cargo.Version != expectedVersion)
        {
            return Result.Failure<CargoOperationResponse>(CargoVersionConflict());
        }

        try
        {
            change();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(CreateCargoOperationHandler.ToResponse(cargo));
        }
        catch (OptimisticConcurrencyException)
        {
            return Result.Failure<CargoOperationResponse>(CargoVersionConflict());
        }
        catch (DomainException exception)
        {
            return Result.Failure<CargoOperationResponse>(ApplicationErrors.Validation(
                "operations.invalid_cargo_progress",
                exception.Message));
        }
    }

    internal static ApplicationError CargoNotFound() => ApplicationErrors.NotFound(
        "operations.cargo_not_found",
        "A operação de carga não foi encontrada para esta escala.");

    internal static ApplicationError CargoVersionConflict() => ApplicationErrors.Conflict(
        "operations.cargo_version_conflict",
        "A operação de carga foi alterada por outro usuário. Atualize os dados e tente novamente.");
}

public sealed class CompleteCargoOperationHandler(
    IOperationalExecutionRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Result<CargoOperationResponse>> HandleAsync(
        CompleteCargoOperationCommand command,
        CancellationToken cancellationToken)
    {
        var code = GetOperationalExecutionHandler.Normalize(command.PublicCode);
        var cargo = await repository.FindCargoOperationTrackedAsync(code, command.CargoOperationId, cancellationToken);
        if (cargo is null)
        {
            return Result.Failure<CargoOperationResponse>(StartCargoOperationHandler.CargoNotFound());
        }

        if (cargo.PortCall.Status != PortCallStatus.InOperation)
        {
            return Result.Failure<CargoOperationResponse>(ApplicationErrors.Validation(
                "operations.port_call_not_in_operation",
                "A carga só pode ser concluída enquanto a escala está em operação."));
        }

        if (cargo.Version != command.ExpectedVersion)
        {
            return Result.Failure<CargoOperationResponse>(StartCargoOperationHandler.CargoVersionConflict());
        }

        try
        {
            cargo.Complete(command.ActualQuantity, command.CompletedAtUtc, timeProvider.GetUtcNow());
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(CreateCargoOperationHandler.ToResponse(cargo));
        }
        catch (OptimisticConcurrencyException)
        {
            return Result.Failure<CargoOperationResponse>(StartCargoOperationHandler.CargoVersionConflict());
        }
        catch (DomainException exception)
        {
            return Result.Failure<CargoOperationResponse>(ApplicationErrors.Validation(
                "operations.invalid_cargo_progress",
                exception.Message));
        }
    }
}
