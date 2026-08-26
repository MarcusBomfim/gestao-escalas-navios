using System.Security.Claims;
using PortManagement.Application.Security;

namespace PortManagement.Api.Security;

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
                return true;
            }

            var user = context.User;
            return user.Identity?.IsAuthenticated == true
                && (user.IsInRole(SecurityRoles.Administrator)
                    || user.HasClaim(DataScopeClaims.Scope, DataScopeClaims.Global));
        }
    }
}
