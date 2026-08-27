using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using PortManagement.Api.Observability;
using PortManagement.Application.Security;
using PortManagement.Infrastructure.Identity;
using PortManagement.Infrastructure.Security;

namespace PortManagement.Api.Security;

internal static class SecurityConfiguration
{
    public const string AuthenticationRateLimit = "authentication";
    public const string WebClientCorsPolicy = "web-client";

    public static IServiceCollection AddApiSecurity(
        this IServiceCollection services,
        JwtOptions jwtOptions,
        IReadOnlyCollection<string> allowedOrigins)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrWhiteSpace(accessToken)
                            && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                        var tokenStamp = context.Principal?.FindFirstValue(
                            DataScopeClaims.SecurityStamp);
                        if (!Guid.TryParse(userId, out var parsedUserId) ||
                            string.IsNullOrWhiteSpace(tokenStamp))
                        {
                            context.Fail("Sessão inválida.");
                            return;
                        }

                        var userManager = context.HttpContext.RequestServices
                            .GetRequiredService<UserManager<ApplicationUser>>();
                        var user = await userManager.FindByIdAsync(parsedUserId.ToString());
                        if (user is null ||
                            !user.IsActive ||
                            !string.Equals(
                                user.SecurityStamp,
                                tokenStamp,
                                StringComparison.Ordinal))
                        {
                            context.Fail("Sessão inválida.");
                        }
                    }
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(
                AuthorizationPolicies.ManageUsers,
                policy => policy.RequireRole(SecurityRoles.Administrator))
            .AddPolicy(
                AuthorizationPolicies.ManageVessels,
                policy => policy.RequireRole(SecurityRoles.Administrator, SecurityRoles.Planner))
            .AddPolicy(
                AuthorizationPolicies.CreatePortCalls,
                policy => policy.RequireRole(SecurityRoles.Administrator, SecurityRoles.Planner))
            .AddPolicy(
                AuthorizationPolicies.TransitionPortCalls,
                policy => policy.RequireRole(
                    SecurityRoles.Administrator,
                    SecurityRoles.Planner,
                    SecurityRoles.Operator))
            .AddPolicy(
                AuthorizationPolicies.ManageBerthPlanning,
                policy => policy.RequireRole(SecurityRoles.Administrator, SecurityRoles.Planner))
            .AddPolicy(
                AuthorizationPolicies.ManageOperationalExecution,
                policy => policy.RequireRole(SecurityRoles.Administrator, SecurityRoles.Operator))
            .AddPolicy(
                AuthorizationPolicies.ViewAuditReports,
                policy => policy.RequireRole(SecurityRoles.Administrator))
            .AddPolicy(
                AuthorizationPolicies.ViewObservability,
                policy => policy.RequireRole(SecurityRoles.Administrator));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(
                AuthenticationRateLimit,
                context => RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
        });

        services.AddCors(options =>
            options.AddPolicy(
                WebClientCorsPolicy,
                policy =>
                {
                    if (allowedOrigins.Count > 0)
                    {
                        policy
                            .WithOrigins([.. allowedOrigins])
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .WithExposedHeaders(CorrelationAndMetricsMiddleware.CorrelationHeader)
                            .AllowCredentials();
                    }
                }));

        return services;
    }
}
