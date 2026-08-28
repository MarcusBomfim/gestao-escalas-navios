using PortManagement.Application.Common;
using PortManagement.Domain.Common;
using PortManagement.Domain.Organizations;
using PortManagement.Domain.Ports;

namespace PortManagement.Application.Administration;

public sealed class ListOrganizationsHandler(IMasterDataRepository masterData)
{
    public async Task<Result<PagedResult<OrganizationAdminResponse>>> HandleAsync(
        ListOrganizationsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Page < 1 || query.PageSize is < 1 or > 100)
        {
            return Result.Failure<PagedResult<OrganizationAdminResponse>>(
                ApplicationErrors.Validation(
                    "master_data.invalid_pagination",
                    "A página deve ser positiva e o tamanho deve estar entre 1 e 100."));
        }

        if (query.Search?.Trim().Length > 160)
        {
            return Result.Failure<PagedResult<OrganizationAdminResponse>>(
                ApplicationErrors.Validation(
                    "master_data.invalid_search",
                    "A busca deve possuir no máximo 160 caracteres."));
        }

        return Result.Success(await masterData.ListOrganizationsAsync(query, cancellationToken));
    }
}

public sealed class CreateOrganizationHandler(
    IMasterDataRepository masterData,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Result<OrganizationAdminResponse>> HandleAsync(
        CreateOrganizationCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var normalizedRegistration = NormalizeIdentifier(command.RegistrationNumber);
            if (await masterData.OrganizationRegistrationExistsAsync(
                    normalizedRegistration,
                    null,
                    cancellationToken))
            {
                return MasterDataErrors.DuplicateOrganization();
            }

            var organization = new Organization(
                Guid.NewGuid(),
                command.Name,
                normalizedRegistration,
                command.Type,
                timeProvider.GetUtcNow());
            await masterData.AddOrganizationAsync(organization, cancellationToken);

            var error = await MasterDataSave.TryAsync(
                unitOfWork,
                "ix_organizations_registration_number",
                "master_data.organization_registration_exists",
                "Já existe uma organização com o registro informado.",
                cancellationToken);

            return error is null
                ? Result.Success(organization.ToAdminResponse())
                : Result.Failure<OrganizationAdminResponse>(error);
        }
        catch (DomainException exception)
        {
            return MasterDataErrors.Invalid<OrganizationAdminResponse>(exception.Message);
        }
    }

    private static string NormalizeIdentifier(string value) => value.Trim().ToUpperInvariant();
}

public sealed class UpdateOrganizationHandler(
    IMasterDataRepository masterData,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Result<OrganizationAdminResponse>> HandleAsync(
        UpdateOrganizationCommand command,
        CancellationToken cancellationToken)
    {
        var organization = await masterData.FindOrganizationAsync(command.Id, cancellationToken);
        if (organization is null)
        {
            return MasterDataErrors.NotFound<OrganizationAdminResponse>("organização");
        }

        if (!MasterDataSave.MatchesVersion(organization, command.ExpectedUpdatedAtUtc))
        {
            return MasterDataErrors.Concurrency<OrganizationAdminResponse>();
        }

        if (organization.IsActive && !command.IsActive
            && await masterData.OrganizationHasActiveUsersAsync(organization.Id, cancellationToken))
        {
            return Result.Failure<OrganizationAdminResponse>(ApplicationErrors.Conflict(
                "master_data.organization_has_active_users",
                "Bloqueie ou transfira os usuários ativos antes de desativar a organização."));
        }

        try
        {
            var normalizedRegistration = command.RegistrationNumber.Trim().ToUpperInvariant();
            if (await masterData.OrganizationRegistrationExistsAsync(
                    normalizedRegistration,
                    organization.Id,
                    cancellationToken))
            {
                return MasterDataErrors.DuplicateOrganization();
            }

            masterData.UseExpectedUpdatedAt(organization, command.ExpectedUpdatedAtUtc);
            organization.Update(
                command.Name,
                normalizedRegistration,
                command.Type,
                command.IsActive,
                timeProvider.GetUtcNow());

            var error = await MasterDataSave.TryAsync(
                unitOfWork,
                "ix_organizations_registration_number",
                "master_data.organization_registration_exists",
                "Já existe uma organização com o registro informado.",
                cancellationToken);

            return error is null
                ? Result.Success(organization.ToAdminResponse())
                : Result.Failure<OrganizationAdminResponse>(error);
        }
        catch (DomainException exception)
        {
            return MasterDataErrors.Invalid<OrganizationAdminResponse>(exception.Message);
        }
    }
}

public sealed class GetMasterPortStructureHandler(IMasterDataRepository masterData)
{
    public Task<IReadOnlyCollection<PortAdminResponse>> HandleAsync(
        CancellationToken cancellationToken) =>
        masterData.ListPortStructureAsync(cancellationToken);
}

public sealed class CreatePortHandler(
    IMasterDataRepository masterData,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Result<PortAdminResponse>> HandleAsync(
        CreatePortCommand command,
        CancellationToken cancellationToken)
    {
        if (!MasterDataSave.IsValidTimeZone(command.TimeZoneId))
        {
            return MasterDataErrors.Invalid<PortAdminResponse>("O fuso horário informado não é válido.");
        }

        try
        {
            var normalizedUnLocode = NormalizeCode(command.UnLocode);
            if (await masterData.PortUnLocodeExistsAsync(
                    normalizedUnLocode,
                    null,
                    cancellationToken))
            {
                return MasterDataErrors.DuplicatePort();
            }

            var port = new Port(
                Guid.NewGuid(),
                command.Name,
                normalizedUnLocode,
                command.CountryCode,
                command.TimeZoneId,
                timeProvider.GetUtcNow());
            await masterData.AddPortAsync(port, cancellationToken);

            var error = await MasterDataSave.TryAsync(
                unitOfWork,
                "ix_ports_un_locode",
                "master_data.port_unlocode_exists",
                "Já existe um porto com o UN/LOCODE informado.",
                cancellationToken);

            return error is null
                ? Result.Success(port.ToAdminResponse([]))
                : Result.Failure<PortAdminResponse>(error);
        }
        catch (DomainException exception)
        {
            return MasterDataErrors.Invalid<PortAdminResponse>(exception.Message);
        }
    }

    private static string NormalizeCode(string value) =>
        value.Trim().Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
}

public sealed class UpdatePortHandler(
    IMasterDataRepository masterData,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Result<PortAdminResponse>> HandleAsync(
        UpdatePortCommand command,
        CancellationToken cancellationToken)
    {
        var port = await masterData.FindPortAsync(command.Id, cancellationToken);
        if (port is null)
        {
            return MasterDataErrors.NotFound<PortAdminResponse>("porto");
        }

        if (!MasterDataSave.MatchesVersion(port, command.ExpectedUpdatedAtUtc))
        {
            return MasterDataErrors.Concurrency<PortAdminResponse>();
        }

        if (!MasterDataSave.IsValidTimeZone(command.TimeZoneId))
        {
            return MasterDataErrors.Invalid<PortAdminResponse>("O fuso horário informado não é válido.");
        }

        if (port.IsActive && !command.IsActive
            && await masterData.PortHasActiveTerminalsAsync(port.Id, cancellationToken))
        {
            return Result.Failure<PortAdminResponse>(ApplicationErrors.Conflict(
                "master_data.port_has_active_terminals",
                "Desative os terminais ativos antes de desativar o porto."));
        }

        try
        {
            var normalizedUnLocode = command.UnLocode
                .Trim()
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .ToUpperInvariant();
            if (await masterData.PortUnLocodeExistsAsync(
                    normalizedUnLocode,
                    port.Id,
                    cancellationToken))
            {
                return MasterDataErrors.DuplicatePort();
            }

            masterData.UseExpectedUpdatedAt(port, command.ExpectedUpdatedAtUtc);
            port.Update(
                command.Name,
                normalizedUnLocode,
                command.CountryCode,
                command.TimeZoneId,
                command.IsActive,
                timeProvider.GetUtcNow());

            var error = await MasterDataSave.TryAsync(
                unitOfWork,
                "ix_ports_un_locode",
                "master_data.port_unlocode_exists",
                "Já existe um porto com o UN/LOCODE informado.",
                cancellationToken);

            return error is null
                ? Result.Success(port.ToAdminResponse([]))
                : Result.Failure<PortAdminResponse>(error);
        }
        catch (DomainException exception)
        {
            return MasterDataErrors.Invalid<PortAdminResponse>(exception.Message);
        }
    }
}

public sealed class CreateTerminalHandler(
    IMasterDataRepository masterData,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Result<TerminalAdminResponse>> HandleAsync(
        CreateTerminalCommand command,
        CancellationToken cancellationToken)
    {
        var port = await masterData.FindPortAsync(command.PortId, cancellationToken);
        if (port is null)
        {
            return MasterDataErrors.NotFound<TerminalAdminResponse>("porto");
        }

        if (!port.IsActive)
        {
            return MasterDataErrors.InactiveParent<TerminalAdminResponse>("porto");
        }

        if (!MasterDataSave.IsValidTimeZone(command.TimeZoneId))
        {
            return MasterDataErrors.Invalid<TerminalAdminResponse>("O fuso horário informado não é válido.");
        }

        try
        {
            var normalizedCode = command.Code.Trim().ToUpperInvariant();
            if (await masterData.TerminalCodeExistsAsync(
                    port.Id,
                    normalizedCode,
                    null,
                    cancellationToken))
            {
                return MasterDataErrors.DuplicateTerminal();
            }

            var terminal = new Terminal(
                Guid.NewGuid(),
                port.Id,
                normalizedCode,
                command.Name,
                command.TimeZoneId,
                timeProvider.GetUtcNow());
            await masterData.AddTerminalAsync(terminal, cancellationToken);

            var error = await MasterDataSave.TryAsync(
                unitOfWork,
                "ix_terminals_port_id_code",
                "master_data.terminal_code_exists",
                "Já existe um terminal com esse código no porto selecionado.",
                cancellationToken);

            return error is null
                ? Result.Success(terminal.ToAdminResponse([]))
                : Result.Failure<TerminalAdminResponse>(error);
        }
        catch (DomainException exception)
        {
            return MasterDataErrors.Invalid<TerminalAdminResponse>(exception.Message);
        }
    }
}

public sealed class UpdateTerminalHandler(
    IMasterDataRepository masterData,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Result<TerminalAdminResponse>> HandleAsync(
        UpdateTerminalCommand command,
        CancellationToken cancellationToken)
    {
        var terminal = await masterData.FindTerminalAsync(command.Id, cancellationToken);
        if (terminal is null)
        {
            return MasterDataErrors.NotFound<TerminalAdminResponse>("terminal");
        }

        if (!MasterDataSave.MatchesVersion(terminal, command.ExpectedUpdatedAtUtc))
        {
            return MasterDataErrors.Concurrency<TerminalAdminResponse>();
        }

        if (!MasterDataSave.IsValidTimeZone(command.TimeZoneId))
        {
            return MasterDataErrors.Invalid<TerminalAdminResponse>("O fuso horário informado não é válido.");
        }

        if (command.IsActive)
        {
            var port = await masterData.FindPortAsync(terminal.PortId, cancellationToken);
            if (port is null || !port.IsActive)
            {
                return MasterDataErrors.InactiveParent<TerminalAdminResponse>("porto");
            }
        }

        if (terminal.IsActive && !command.IsActive
            && await masterData.TerminalHasAvailableBerthsAsync(terminal.Id, cancellationToken))
        {
            return Result.Failure<TerminalAdminResponse>(ApplicationErrors.Conflict(
                "master_data.terminal_has_available_berths",
                "Coloque os berços disponíveis em manutenção ou indisponíveis antes de desativar o terminal."));
        }

        try
        {
            var normalizedCode = command.Code.Trim().ToUpperInvariant();
            if (await masterData.TerminalCodeExistsAsync(
                    terminal.PortId,
                    normalizedCode,
                    terminal.Id,
                    cancellationToken))
            {
                return MasterDataErrors.DuplicateTerminal();
            }

            masterData.UseExpectedUpdatedAt(terminal, command.ExpectedUpdatedAtUtc);
            terminal.Update(
                normalizedCode,
                command.Name,
                command.TimeZoneId,
                command.IsActive,
                timeProvider.GetUtcNow());

            var error = await MasterDataSave.TryAsync(
                unitOfWork,
                "ix_terminals_port_id_code",
                "master_data.terminal_code_exists",
                "Já existe um terminal com esse código no porto selecionado.",
                cancellationToken);

            return error is null
                ? Result.Success(terminal.ToAdminResponse([]))
                : Result.Failure<TerminalAdminResponse>(error);
        }
        catch (DomainException exception)
        {
            return MasterDataErrors.Invalid<TerminalAdminResponse>(exception.Message);
        }
    }
}

public sealed class CreateBerthHandler(
    IMasterDataRepository masterData,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Result<BerthAdminResponse>> HandleAsync(
        CreateBerthCommand command,
        CancellationToken cancellationToken)
    {
        var terminal = await masterData.FindTerminalAsync(command.TerminalId, cancellationToken);
        if (terminal is null)
        {
            return MasterDataErrors.NotFound<BerthAdminResponse>("terminal");
        }

        if (!terminal.IsActive)
        {
            return MasterDataErrors.InactiveParent<BerthAdminResponse>("terminal");
        }

        try
        {
            var normalizedCode = command.Code.Trim().ToUpperInvariant();
            if (await masterData.BerthCodeExistsAsync(
                    terminal.Id,
                    normalizedCode,
                    null,
                    cancellationToken))
            {
                return MasterDataErrors.DuplicateBerth();
            }

            var berth = new Berth(
                Guid.NewGuid(),
                terminal.Id,
                normalizedCode,
                command.Name,
                command.UsefulLengthMeters,
                command.MaximumBeamMeters,
                command.MaximumDraftMeters,
                command.SupportedVesselTypes,
                timeProvider.GetUtcNow());
            await masterData.AddBerthAsync(berth, cancellationToken);

            var error = await MasterDataSave.TryAsync(
                unitOfWork,
                "ix_berths_terminal_id_code",
                "master_data.berth_code_exists",
                "Já existe um berço com esse código no terminal selecionado.",
                cancellationToken);

            return error is null
                ? Result.Success(berth.ToAdminResponse())
                : Result.Failure<BerthAdminResponse>(error);
        }
        catch (DomainException exception)
        {
            return MasterDataErrors.Invalid<BerthAdminResponse>(exception.Message);
        }
    }
}

public sealed class UpdateBerthHandler(
    IMasterDataRepository masterData,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Result<BerthAdminResponse>> HandleAsync(
        UpdateBerthCommand command,
        CancellationToken cancellationToken)
    {
        var berth = await masterData.FindBerthAsync(command.Id, cancellationToken);
        if (berth is null)
        {
            return MasterDataErrors.NotFound<BerthAdminResponse>("berço");
        }

        if (!MasterDataSave.MatchesVersion(berth, command.ExpectedUpdatedAtUtc))
        {
            return MasterDataErrors.Concurrency<BerthAdminResponse>();
        }

        if (command.Status == BerthStatus.Available)
        {
            var terminal = await masterData.FindTerminalAsync(
                berth.TerminalId,
                cancellationToken);
            if (terminal is null || !terminal.IsActive)
            {
                return MasterDataErrors.InactiveParent<BerthAdminResponse>("terminal");
            }
        }

        var supportedVesselTypes = command.SupportedVesselTypes ?? [];
        var changesCapacity = berth.UsefulLengthMeters != command.UsefulLengthMeters
            || berth.MaximumBeamMeters != command.MaximumBeamMeters
            || berth.MaximumDraftMeters != command.MaximumDraftMeters
            || berth.Status != command.Status
            || !berth.SupportedVesselTypes.Order().SequenceEqual(
                supportedVesselTypes.Distinct().Order());
        if (changesCapacity
            && await masterData.BerthHasOpenWindowsAsync(
                berth.Id,
                timeProvider.GetUtcNow(),
                cancellationToken))
        {
            return Result.Failure<BerthAdminResponse>(ApplicationErrors.Conflict(
                "master_data.berth_has_open_windows",
                "O berço possui janelas solicitadas ou confirmadas. Reprograme-as antes de alterar sua capacidade ou situação."));
        }

        try
        {
            var normalizedCode = command.Code.Trim().ToUpperInvariant();
            if (await masterData.BerthCodeExistsAsync(
                    berth.TerminalId,
                    normalizedCode,
                    berth.Id,
                    cancellationToken))
            {
                return MasterDataErrors.DuplicateBerth();
            }

            masterData.UseExpectedUpdatedAt(berth, command.ExpectedUpdatedAtUtc);
            berth.Update(
                normalizedCode,
                command.Name,
                command.UsefulLengthMeters,
                command.MaximumBeamMeters,
                command.MaximumDraftMeters,
                supportedVesselTypes,
                command.Status,
                timeProvider.GetUtcNow());

            var error = await MasterDataSave.TryAsync(
                unitOfWork,
                "ix_berths_terminal_id_code",
                "master_data.berth_code_exists",
                "Já existe um berço com esse código no terminal selecionado.",
                cancellationToken);

            return error is null
                ? Result.Success(berth.ToAdminResponse())
                : Result.Failure<BerthAdminResponse>(error);
        }
        catch (DomainException exception)
        {
            return MasterDataErrors.Invalid<BerthAdminResponse>(exception.Message);
        }
    }
}

internal static class MasterDataSave
{
    public static bool MatchesVersion(AuditableEntity entity, DateTimeOffset expectedUpdatedAtUtc) =>
        entity.UpdatedAtUtc == expectedUpdatedAtUtc.ToUniversalTime();

    public static bool IsValidTimeZone(string timeZoneId)
    {
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId?.Trim() ?? string.Empty);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }

    public static async Task<ApplicationError?> TryAsync(
        IUnitOfWork unitOfWork,
        string uniqueConstraint,
        string duplicateCode,
        string duplicateDescription,
        CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (UniqueConstraintException exception)
            when (exception.ConstraintName == uniqueConstraint)
        {
            return ApplicationErrors.Conflict(duplicateCode, duplicateDescription);
        }
        catch (OptimisticConcurrencyException)
        {
            return ApplicationErrors.Conflict(
                "master_data.concurrent_update",
                "O registro foi alterado por outro usuário. Atualize a página antes de tentar novamente.");
        }
    }
}

internal static class MasterDataErrors
{
    public static Result<T> Invalid<T>(string message) =>
        Result.Failure<T>(ApplicationErrors.Validation("master_data.invalid_data", message));

    public static Result<T> NotFound<T>(string entityName) =>
        Result.Failure<T>(ApplicationErrors.NotFound(
            "master_data.not_found",
            $"O {entityName} solicitado não foi encontrado."));

    public static Result<T> InactiveParent<T>(string parentName) =>
        Result.Failure<T>(ApplicationErrors.Conflict(
            "master_data.inactive_parent",
            $"O {parentName} selecionado está inativo."));

    public static Result<T> Concurrency<T>() =>
        Result.Failure<T>(ApplicationErrors.Conflict(
            "master_data.concurrent_update",
            "O registro foi alterado por outro usuário. Atualize a página antes de tentar novamente."));

    public static Result<OrganizationAdminResponse> DuplicateOrganization() =>
        Result.Failure<OrganizationAdminResponse>(ApplicationErrors.Conflict(
            "master_data.organization_registration_exists",
            "Já existe uma organização com o registro informado."));

    public static Result<PortAdminResponse> DuplicatePort() =>
        Result.Failure<PortAdminResponse>(ApplicationErrors.Conflict(
            "master_data.port_unlocode_exists",
            "Já existe um porto com o UN/LOCODE informado."));

    public static Result<TerminalAdminResponse> DuplicateTerminal() =>
        Result.Failure<TerminalAdminResponse>(ApplicationErrors.Conflict(
            "master_data.terminal_code_exists",
            "Já existe um terminal com esse código no porto selecionado."));

    public static Result<BerthAdminResponse> DuplicateBerth() =>
        Result.Failure<BerthAdminResponse>(ApplicationErrors.Conflict(
            "master_data.berth_code_exists",
            "Já existe um berço com esse código no terminal selecionado."));
}
