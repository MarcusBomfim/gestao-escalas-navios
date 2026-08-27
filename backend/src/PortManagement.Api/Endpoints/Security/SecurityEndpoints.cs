using System.Security.Claims;
using PortManagement.Api.Common;
using PortManagement.Api.Security;
using PortManagement.Application.Security;
using PortManagement.Infrastructure.Security;

namespace PortManagement.Api.Endpoints.Security;

internal static class SecurityEndpoints
{
    private const string RefreshTokenCookie = "port_management_refresh";

    public static IEndpointRouteBuilder MapSecurityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints
            .MapGroup("/api/v1/auth")
            .WithTags("Authentication");

        auth.MapPost(
                "/login",
                async (
                    LoginRequest request,
                    HttpContext context,
                    LoginHandler handler,
                    JwtOptions jwtOptions,
                    IWebHostEnvironment environment,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(
                        new LoginCommand(request.Email, request.Password),
                        GetClientIp(context),
                        cancellationToken);
                    return result.ToHttpResult(session =>
                    {
                        WriteRefreshTokenCookie(
                            context,
                            session.RefreshToken,
                            jwtOptions,
                            environment);
                        return Results.Ok(ToResponse(session));
                    });
                })
            .RequireRateLimiting(SecurityConfiguration.AuthenticationRateLimit)
            .WithName("Login")
            .WithSummary("Autentica um usuário e inicia uma sessão");

        auth.MapPost(
                "/refresh",
                async (
                    HttpContext context,
                    RefreshSessionHandler handler,
                    JwtOptions jwtOptions,
                    IWebHostEnvironment environment,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(
                        new RefreshSessionCommand(GetRefreshToken(context)),
                        GetClientIp(context),
                        cancellationToken);
                    return result.ToHttpResult(session =>
                    {
                        WriteRefreshTokenCookie(
                            context,
                            session.RefreshToken,
                            jwtOptions,
                            environment);
                        return Results.Ok(ToResponse(session));
                    });
                })
            .RequireRateLimiting(SecurityConfiguration.AuthenticationRateLimit)
            .WithName("RefreshSession")
            .WithSummary("Rotaciona o refresh token e renova a sessão");

        auth.MapPost(
                "/logout",
                async (
                    HttpContext context,
                    RevokeSessionHandler handler,
                    IWebHostEnvironment environment,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(
                        new RevokeSessionCommand(GetRefreshToken(context)),
                        GetClientIp(context),
                        cancellationToken);
                    return result.ToHttpResult(_ =>
                    {
                        DeleteRefreshTokenCookie(context, environment);
                        return Results.NoContent();
                    });
                })
            .RequireRateLimiting(SecurityConfiguration.AuthenticationRateLimit)
            .WithName("Logout")
            .WithSummary("Revoga a sessão de forma idempotente");

        auth.MapPost(
                "/forgot-password",
                async (
                    RequestPasswordResetRequest request,
                    RequestPasswordResetHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(
                        new RequestPasswordResetCommand(request.Email),
                        cancellationToken);
                    return result.ToHttpResult(_ => Results.Accepted(
                        value: new MessageResponse(
                            "Se o e-mail estiver cadastrado, as instruções serão enviadas.")));
                })
            .RequireRateLimiting(SecurityConfiguration.AuthenticationRateLimit)
            .WithName("RequestPasswordReset")
            .WithSummary("Solicita uma redefinição sem revelar se a conta existe");

        auth.MapPost(
                "/reset-password",
                async (
                    ResetPasswordRequest request,
                    HttpContext context,
                    ResetPasswordHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(
                        new ResetPasswordCommand(
                            request.UserId,
                            request.Token,
                            request.NewPassword),
                        GetClientIp(context),
                        cancellationToken);
                    return result.ToHttpResult(_ => Results.NoContent());
                })
            .RequireRateLimiting(SecurityConfiguration.AuthenticationRateLimit)
            .WithName("ResetPassword")
            .WithSummary("Redefine a senha com um token temporário");

        auth.MapGet(
                "/me",
                async (
                    ClaimsPrincipal principal,
                    GetCurrentUserHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(GetUserId(principal), cancellationToken);
                    return result.ToHttpResult(Results.Ok);
                })
            .RequireAuthorization()
            .WithName("GetCurrentUser")
            .WithSummary("Retorna a identidade da sessão atual");

        var users = endpoints
            .MapGroup("/api/v1/users")
            .WithTags("Users")
            .RequireAuthorization(AuthorizationPolicies.ManageUsers);

        users.MapGet(
                "/",
                async (
                    int? page,
                    int? pageSize,
                    string? search,
                    string? role,
                    bool? isActive,
                    ListUsersHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(
                        new ListUsersQuery(
                            page ?? 1,
                            pageSize ?? 20,
                            search,
                            role,
                            isActive),
                        cancellationToken);
                    return result.ToHttpResult(Results.Ok);
                })
            .WithName("ListUsers")
            .WithSummary("Lista usuários com paginação, busca e filtros");

        users.MapGet(
                "/options",
                async (
                    GetUserManagementOptionsHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(cancellationToken);
                    return result.ToHttpResult(Results.Ok);
                })
            .WithName("GetUserManagementOptions")
            .WithSummary("Lista papéis e organizações disponíveis para usuários");

        users.MapPost(
                "/",
                async (
                    CreateUserRequest request,
                    CreateUserHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(
                        new CreateUserCommand(
                            request.DisplayName,
                            request.Email,
                            request.Password,
                            request.Role,
                            request.OrganizationId),
                        cancellationToken);
                    return result.ToHttpResult(user => Results.Created((string?)null, user));
                })
            .WithName("CreateUser")
            .WithSummary("Cria um usuário e atribui um papel");

        users.MapPut(
                "/{id:guid}",
                async (
                    Guid id,
                    UpdateUserRequest request,
                    ClaimsPrincipal principal,
                    HttpContext context,
                    UpdateUserHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(
                        new UpdateUserCommand(
                            id,
                            GetUserId(principal),
                            request.DisplayName,
                            request.Role,
                            request.OrganizationId,
                            request.IsActive,
                            request.ExpectedVersion,
                            GetClientIp(context)),
                        cancellationToken);
                    return result.ToHttpResult(Results.Ok);
                })
            .WithName("UpdateUser")
            .WithSummary("Atualiza perfil, escopo e situação de um usuário");

        return endpoints;
    }

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : Guid.Empty;
    }

    private static string GetClientIp(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static string GetRefreshToken(HttpContext context) =>
        context.Request.Cookies[RefreshTokenCookie] ?? string.Empty;

    private static void WriteRefreshTokenCookie(
        HttpContext context,
        string refreshToken,
        JwtOptions jwtOptions,
        IWebHostEnvironment environment) =>
        context.Response.Cookies.Append(
            RefreshTokenCookie,
            refreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = !environment.IsDevelopment() || context.Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Path = "/api/v1/auth",
                MaxAge = TimeSpan.FromDays(jwtOptions.RefreshTokenDays)
            });

    private static void DeleteRefreshTokenCookie(
        HttpContext context,
        IWebHostEnvironment environment) =>
        context.Response.Cookies.Delete(
            RefreshTokenCookie,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = !environment.IsDevelopment() || context.Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Path = "/api/v1/auth"
            });

    private static SessionResponse ToResponse(AuthTokenResponse session) => new(
        session.AccessToken,
        session.AccessTokenExpiresAtUtc,
        session.User);
}

internal sealed record LoginRequest(string Email, string Password);

internal sealed record RequestPasswordResetRequest(string Email);

internal sealed record ResetPasswordRequest(
    string UserId,
    string Token,
    string NewPassword);

internal sealed record MessageResponse(string Message);

internal sealed record CreateUserRequest(
    string DisplayName,
    string Email,
    string Password,
    string Role,
    Guid? OrganizationId);

internal sealed record UpdateUserRequest(
    string DisplayName,
    string Role,
    Guid? OrganizationId,
    bool IsActive,
    string ExpectedVersion);

internal sealed record SessionResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    AuthenticatedUserResponse User);
