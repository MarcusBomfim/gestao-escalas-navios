using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace PortManagement.Api.OpenApi;

internal static class OpenApiConfiguration
{
    public const string DocumentName = "v1";
    public const string BearerScheme = "Bearer";

    public static IServiceCollection AddPortManagementOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(DocumentName, options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "Port Management API",
                    Version = DocumentName,
                    Description =
                        "API REST para gestão demonstrativa de navios, escalas, " +
                        "planejamento de berços e execução portuária."
                };
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??=
                    new Dictionary<string, IOpenApiSecurityScheme>(StringComparer.Ordinal);
                document.Components.SecuritySchemes[BearerScheme] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Access token obtido em POST /api/v1/auth/login."
                };

                return Task.CompletedTask;
            });

            options.AddOperationTransformer((operation, context, _) =>
            {
                operation.Responses ??= new OpenApiResponses();
                var requiresAuthorization = context.Description.ActionDescriptor.EndpointMetadata
                    .OfType<IAuthorizeData>()
                    .Any();

                if (requiresAuthorization)
                {
                    operation.Security ??= [];
                    operation.Security.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference(BearerScheme, context.Document)] = []
                    });

                    operation.Responses.TryAdd(
                        "401",
                        new OpenApiResponse { Description = "Sessão ausente, inválida ou expirada." });
                    operation.Responses.TryAdd(
                        "403",
                        new OpenApiResponse { Description = "O perfil não possui a permissão exigida." });
                }

                operation.Responses.TryAdd(
                    "500",
                    new OpenApiResponse { Description = "Falha interna no processamento da requisição." });

                return Task.CompletedTask;
            });
        });

        return services;
    }

    public static IEndpointRouteBuilder MapPortManagementOpenApi(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapOpenApi();
        endpoints.MapScalarApiReference("/docs", options =>
        {
            options.WithTitle("Port Management API");
            options.AddPreferredSecuritySchemes(BearerScheme);
            options.DisableAgent();
            options.DisableDefaultFonts();
        });

        return endpoints;
    }
}
