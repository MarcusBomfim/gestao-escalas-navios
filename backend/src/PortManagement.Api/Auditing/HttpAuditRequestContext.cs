using System.Security.Claims;
using PortManagement.Application.Auditing;

namespace PortManagement.Api.Auditing;

internal sealed class HttpAuditRequestContext(IHttpContextAccessor accessor) : IAuditRequestContext
{
    private HttpContext? Context => accessor.HttpContext;

    public Guid? UserId => Guid.TryParse(
        Context?.User.FindFirstValue(ClaimTypes.NameIdentifier),
        out var userId)
        ? userId
        : null;

    public string UserDisplayName =>
        Context?.User.FindFirstValue(ClaimTypes.Name) ?? "Usuário autenticado";

    public string HttpMethod => Context?.Request.Method ?? "SYSTEM";

    public string RequestPath => Context?.Request.Path.Value ?? "/system";

    public string CorrelationId => Context?.TraceIdentifier ?? Guid.NewGuid().ToString("N");
}
