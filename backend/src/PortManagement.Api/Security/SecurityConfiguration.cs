using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using PortManagement.Application.Security;
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
                policy => policy.RequireRole(SecurityRoles.Administrator, SecurityRoles.Planner));

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
                            .AllowCredentials();
                    }
                }));

        return services;
    }
}
