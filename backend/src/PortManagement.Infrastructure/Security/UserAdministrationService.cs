using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PortManagement.Application.Auditing;
using PortManagement.Application.Common;
using PortManagement.Application.Security;
using PortManagement.Domain.Auditing;
using PortManagement.Infrastructure.Identity;
using PortManagement.Infrastructure.Persistence;

namespace PortManagement.Infrastructure.Security;

internal sealed class UserAdministrationService(
    UserManager<ApplicationUser> userManager,
    PortManagementDbContext database,
    IAuditRequestContext auditContext,
    TimeProvider timeProvider) : IUserAdministrationService
{
    private const string NormalizedAdministratorRole = "ADMINISTRATOR";

    public async Task<Result<AuthenticatedUserResponse>> CreateUserAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.DisplayName) ||
            command.DisplayName.Trim().Length > 160 ||
            string.IsNullOrWhiteSpace(command.Email) ||
            command.Email.Length > 320 ||
            string.IsNullOrEmpty(command.Password) ||
            command.Password.Length > 256)
        {
            return Result.Failure<AuthenticatedUserResponse>(new ApplicationError(
                "security.invalid_user",
                "Nome, e-mail ou senha possuem formato inválido.",
                ApplicationErrorType.Validation));
        }

        if (!SecurityRoles.All.Contains(command.Role))
        {
            return Result.Failure<AuthenticatedUserResponse>(new ApplicationError(
                "security.invalid_role",
                "O papel informado não é reconhecido.",
                ApplicationErrorType.Validation));
        }

        var organizationError = await ValidateOrganizationAssignmentAsync(
            command.Role,
            command.OrganizationId,
            cancellationToken);
        if (organizationError is not null)
        {
            return Result.Failure<AuthenticatedUserResponse>(organizationError);
        }

        var email = command.Email.Trim();
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return Result.Failure<AuthenticatedUserResponse>(new ApplicationError(
                "security.email_already_registered",
                "Já existe um usuário com esse e-mail.",
                ApplicationErrorType.Conflict));
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = command.DisplayName.Trim(),
            OrganizationId = command.OrganizationId,
            CreatedAtUtc = timeProvider.GetUtcNow(),
            IsActive = true
        };

        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var creation = await userManager.CreateAsync(user, command.Password);
        if (!creation.Succeeded)
        {
            return Result.Failure<AuthenticatedUserResponse>(IdentityValidationFailure(creation));
        }

        var roleAssignment = await userManager.AddToRoleAsync(user, command.Role);
        if (!roleAssignment.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure<AuthenticatedUserResponse>(IdentityValidationFailure(roleAssignment));
        }

        AddAuditRecord(
            user.Id,
            AuditAction.Created,
            ["DisplayName", "Email", "OrganizationId", "IsActive", "Role"]);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success(await ToAuthenticatedResponseAsync(user));
    }

    public async Task<Result<PagedResult<ManagedUserResponse>>> ListUsersAsync(
        ListUsersQuery query,
        CancellationToken cancellationToken)
    {
        var users = database.Users.AsNoTracking();
        var search = query.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            users = users.Where(user =>
                EF.Functions.ILike(user.DisplayName, $"%{search}%") ||
                (user.Email != null && EF.Functions.ILike(user.Email, $"%{search}%")));
        }

        if (query.IsActive is bool isActive)
        {
            users = users.Where(user => user.IsActive == isActive);
        }

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var normalizedRole = query.Role.ToUpperInvariant();
            users = users.Where(user => database.UserRoles.Any(userRole =>
                userRole.UserId == user.Id &&
                database.Roles.Any(role =>
                    role.Id == userRole.RoleId && role.NormalizedName == normalizedRole)));
        }

        var totalItems = await users.CountAsync(cancellationToken);
        var pageUsers = await users
            .OrderByDescending(user => user.IsActive)
            .ThenBy(user => user.DisplayName)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(user => new
            {
                User = user,
                OrganizationName = database.Organizations
                    .Where(organization => organization.Id == user.OrganizationId)
                    .Select(organization => organization.Name)
                    .SingleOrDefault()
            })
            .ToListAsync(cancellationToken);

        var userIds = pageUsers.Select(item => item.User.Id).ToArray();
        var roleAssignments = await (
            from userRole in database.UserRoles.AsNoTracking()
            join role in database.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where userIds.Contains(userRole.UserId)
            select new { userRole.UserId, Role = role.Name ?? string.Empty })
            .ToListAsync(cancellationToken);

        var responses = pageUsers
            .Select(item => ToManagedResponse(
                item.User,
                item.OrganizationName,
                roleAssignments
                    .Where(assignment => assignment.UserId == item.User.Id)
                    .Select(assignment => assignment.Role)
                    .Order(StringComparer.Ordinal)
                    .ToArray()))
            .ToArray();

        return Result.Success(new PagedResult<ManagedUserResponse>(
            responses,
            query.Page,
            query.PageSize,
            totalItems));
    }

    public async Task<Result<UserManagementOptionsResponse>> GetUserManagementOptionsAsync(
        CancellationToken cancellationToken)
    {
        var organizations = await database.Organizations
            .AsNoTracking()
            .Where(organization => organization.IsActive)
            .OrderBy(organization => organization.Name)
            .Select(organization => new OrganizationOptionResponse(
                organization.Id,
                organization.Name,
                organization.Type.ToString()))
            .ToArrayAsync(cancellationToken);

        return Result.Success(new UserManagementOptionsResponse(
            SecurityRoles.All.Order(StringComparer.Ordinal).ToArray(),
            organizations));
    }

    public async Task<Result<ManagedUserResponse>> UpdateUserAsync(
        UpdateUserCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.DisplayName) ||
            command.DisplayName.Trim().Length > 160 ||
            !SecurityRoles.All.Contains(command.Role) ||
            string.IsNullOrWhiteSpace(command.ExpectedVersion) ||
            command.ExpectedVersion.Length > 128)
        {
            return Result.Failure<ManagedUserResponse>(new ApplicationError(
                "security.invalid_user_update",
                "Os dados informados para o usuário são inválidos.",
                ApplicationErrorType.Validation));
        }

        var user = await userManager.FindByIdAsync(command.UserId.ToString());
        if (user is null)
        {
            return Result.Failure<ManagedUserResponse>(new ApplicationError(
                "security.user_not_found",
                "Usuário não encontrado.",
                ApplicationErrorType.NotFound));
        }

        if (!string.Equals(
                user.ConcurrencyStamp,
                command.ExpectedVersion,
                StringComparison.Ordinal))
        {
            return Result.Failure<ManagedUserResponse>(new ApplicationError(
                "security.user_concurrency_conflict",
                "O usuário foi alterado por outra operação. Atualize a página e tente novamente.",
                ApplicationErrorType.Conflict));
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var currentClaims = await userManager.GetClaimsAsync(user);
        var keepsControlledGlobalViewer = command.Role == SecurityRoles.Viewer &&
            command.OrganizationId is null &&
            currentClaims.Any(claim =>
                claim.Type == DataScopeClaims.Scope &&
                claim.Value == DataScopeClaims.Global);
        var organizationError = await ValidateOrganizationAssignmentAsync(
            command.Role,
            command.OrganizationId,
            cancellationToken,
            keepsControlledGlobalViewer);
        if (organizationError is not null)
        {
            return Result.Failure<ManagedUserResponse>(organizationError);
        }

        var removesAdministrator = currentRoles.Contains(SecurityRoles.Administrator) &&
            (!command.IsActive || command.Role != SecurityRoles.Administrator);
        if (command.UserId == command.ActingUserId && removesAdministrator)
        {
            return Result.Failure<ManagedUserResponse>(new ApplicationError(
                "security.self_admin_lockout",
                "Você não pode bloquear sua própria conta ou remover seu acesso administrativo.",
                ApplicationErrorType.Forbidden));
        }

        if (user.IsActive && removesAdministrator &&
            await CountActiveAdministratorsAsync(cancellationToken) <= 1)
        {
            return Result.Failure<ManagedUserResponse>(new ApplicationError(
                "security.last_administrator",
                "O último administrador ativo não pode ser bloqueado ou rebaixado.",
                ApplicationErrorType.Conflict));
        }

        var accessChanged = user.OrganizationId != command.OrganizationId ||
            user.IsActive != command.IsActive ||
            currentRoles.Count != 1 ||
            !currentRoles.Contains(command.Role);
        var changedFields = new List<string>();
        if (!string.Equals(user.DisplayName, command.DisplayName.Trim(), StringComparison.Ordinal))
        {
            changedFields.Add("DisplayName");
        }
        if (user.OrganizationId != command.OrganizationId)
        {
            changedFields.Add("OrganizationId");
        }
        if (user.IsActive != command.IsActive)
        {
            changedFields.Add("IsActive");
        }
        if (currentRoles.Count != 1 || !currentRoles.Contains(command.Role))
        {
            changedFields.Add("Role");
        }

        if (changedFields.Count == 0)
        {
            return Result.Success(ToManagedResponse(
                user,
                await ResolveOrganizationNameAsync(command.OrganizationId, cancellationToken),
                currentRoles.ToArray()));
        }

        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        user.DisplayName = command.DisplayName.Trim();
        user.OrganizationId = command.OrganizationId;
        user.IsActive = command.IsActive;

        var update = await userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            return Result.Failure<ManagedUserResponse>(IdentityValidationFailure(update));
        }

        var rolesToRemove = currentRoles
            .Where(role => role != command.Role)
            .ToArray();
        if (rolesToRemove.Length > 0)
        {
            var removal = await userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removal.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Failure<ManagedUserResponse>(IdentityValidationFailure(removal));
            }
        }

        if (!currentRoles.Contains(command.Role))
        {
            var assignment = await userManager.AddToRoleAsync(user, command.Role);
            if (!assignment.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Failure<ManagedUserResponse>(IdentityValidationFailure(assignment));
            }
        }

        var globalScopeClaim = currentClaims.SingleOrDefault(claim =>
            claim.Type == DataScopeClaims.Scope &&
            claim.Value == DataScopeClaims.Global);
        if (globalScopeClaim is not null && !keepsControlledGlobalViewer)
        {
            var claimRemoval = await userManager.RemoveClaimAsync(user, globalScopeClaim);
            if (!claimRemoval.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Failure<ManagedUserResponse>(IdentityValidationFailure(claimRemoval));
            }
        }

        if (accessChanged)
        {
            var stampUpdate = await userManager.UpdateSecurityStampAsync(user);
            if (!stampUpdate.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Failure<ManagedUserResponse>(IdentityValidationFailure(stampUpdate));
            }

            await RevokeAllActiveSessionsAsync(
                user.Id,
                timeProvider.GetUtcNow(),
                command.ClientIp,
                cancellationToken);
        }
        AddAuditRecord(user.Id, AuditAction.Updated, changedFields);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success(ToManagedResponse(
            user,
            await ResolveOrganizationNameAsync(command.OrganizationId, cancellationToken),
            [command.Role]));
    }

    private async Task<ApplicationError?> ValidateOrganizationAssignmentAsync(
        string role,
        Guid? organizationId,
        CancellationToken cancellationToken,
        bool allowControlledGlobalViewer = false)
    {
        if (role == SecurityRoles.Administrator && organizationId is not null)
        {
            return new ApplicationError(
                "security.administrator_global_scope",
                "Administradores devem utilizar o escopo global, sem organização vinculada.",
                ApplicationErrorType.Validation);
        }

        if (role != SecurityRoles.Administrator &&
            organizationId is null &&
            !allowControlledGlobalViewer)
        {
            return new ApplicationError(
                "security.organization_required",
                "Selecione uma organização para usuários não administradores.",
                ApplicationErrorType.Validation);
        }

        if (organizationId is Guid value &&
            !await database.Organizations.AnyAsync(
                organization => organization.Id == value && organization.IsActive,
                cancellationToken))
        {
            return new ApplicationError(
                "security.organization_not_found",
                "A organização informada não existe ou está inativa.",
                ApplicationErrorType.Validation);
        }

        return null;
    }

    private Task<int> CountActiveAdministratorsAsync(CancellationToken cancellationToken) =>
        (from user in database.Users
         join userRole in database.UserRoles on user.Id equals userRole.UserId
         join role in database.Roles on userRole.RoleId equals role.Id
         where user.IsActive && role.NormalizedName == NormalizedAdministratorRole
         select user.Id)
        .Distinct()
        .CountAsync(cancellationToken);

    private static ManagedUserResponse ToManagedResponse(
        ApplicationUser user,
        string? organizationName,
        IReadOnlyCollection<string> roles) => new(
        user.Id,
        user.DisplayName,
        user.Email ?? string.Empty,
        user.OrganizationId,
        organizationName,
        user.IsActive,
        user.CreatedAtUtc,
        user.ConcurrencyStamp ?? string.Empty,
        roles);

    private async Task<AuthenticatedUserResponse> ToAuthenticatedResponseAsync(
        ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new AuthenticatedUserResponse(
            user.Id,
            user.DisplayName,
            user.Email ?? string.Empty,
            user.OrganizationId,
            roles.ToArray());
    }

    private async Task<string?> ResolveOrganizationNameAsync(
        Guid? organizationId,
        CancellationToken cancellationToken) =>
        organizationId is Guid value
            ? await database.Organizations
                .AsNoTracking()
                .Where(organization => organization.Id == value)
                .Select(organization => organization.Name)
                .SingleAsync(cancellationToken)
            : null;

    private void AddAuditRecord(
        Guid userId,
        AuditAction action,
        IReadOnlyCollection<string> changedFields)
    {
        if (auditContext.UserId is not Guid actorId)
        {
            return;
        }

        database.AuditRecords.Add(AuditRecord.Capture(
            actorId,
            auditContext.UserDisplayName,
            action,
            nameof(ApplicationUser),
            userId.ToString(),
            changedFields,
            auditContext.HttpMethod,
            auditContext.RequestPath,
            auditContext.CorrelationId,
            timeProvider.GetUtcNow()));
    }

    private Task<int> RevokeAllActiveSessionsAsync(
        Guid userId,
        DateTimeOffset now,
        string clientIp,
        CancellationToken cancellationToken) =>
        database.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAtUtc, now)
                    .SetProperty(token => token.RevokedByIp, clientIp),
                cancellationToken);

    private static ApplicationError IdentityValidationFailure(IdentityResult result) => new(
        "security.identity_validation_failed",
        string.Join(" ", result.Errors.Select(error => error.Description)),
        ApplicationErrorType.Validation);
}
