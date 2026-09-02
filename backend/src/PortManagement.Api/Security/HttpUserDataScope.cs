using System.Security.Claims;
using PortManagement.Application.Security;

namespace PortManagement.Api.Security;

/// <summary>
/// Escopo de dados derivado da requisição autenticada. Sem requisição não há
/// escopo: trabalhos em segundo plano que precisem ler todas as organizações
/// devem pedir isso explicitamente por <see cref="DataScopeContext"/>.
/// </summary>
internal sealed class HttpUserDataScope(IHttpContextAccessor accessor) : IUserDataScope
{
    private HttpContext? Context => accessor.HttpContext;

    public Guid? OrganizationId
    {
        get
        {
            var user = Context?.User;
            return user?.Identity?.IsAuthenticated == true
                && Guid.TryParse(
                    user.FindFirstValue(DataScopeClaims.OrganizationId),
                    out var organizationId)
                    ? organizationId
                    : null;
        }
    }

    public bool HasGlobalAccess
    {
        get
        {
            var context = Context;
            if (context is null)
            {
                return false;
            }

            var user = context.User;
            return user.Identity?.IsAuthenticated == true
                && (user.IsInRole(SecurityRoles.Administrator)
                    || user.HasClaim(DataScopeClaims.Scope, DataScopeClaims.Global));
        }
    }
}

/// <summary>
/// Escopo usado por processos internos que legitimamente leem todas as
/// organizações. Só é obtido quando um escopo de serviço é elevado de forma
/// explícita, nunca por ausência de contexto.
/// </summary>
internal sealed class SystemDataScope : IUserDataScope
{
    public static SystemDataScope Instance { get; } = new();

    public Guid? OrganizationId => null;

    public bool HasGlobalAccess => true;
}

/// <summary>
/// Marcador por escopo de serviço. O padrão é o escopo da requisição; a
/// elevação precisa ser pedida antes de resolver qualquer dependência que
/// consulte dados.
/// </summary>
internal sealed class DataScopeContext
{
    public bool IsSystem { get; private set; }

    public void ElevateToSystem() => IsSystem = true;
}
