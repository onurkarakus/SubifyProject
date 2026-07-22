using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Subify.Api.Common.OpenApi;

/// <summary>
/// Adds JWT Bearer security scheme to the OpenAPI document so Scalar/Swagger can authorize.
/// </summary>
internal sealed class BearerSecuritySchemeTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    private const string SchemeId = "Bearer";

    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var schemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (!schemes.Any(scheme =>
                string.Equals(scheme.Name, JwtBearerDefaults.AuthenticationScheme, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes[SchemeId] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description =
                "JWT Authorization header using the Bearer scheme. " +
                "Example: \"Authorization: Bearer {accessToken}\" — paste the access token from POST /api/auth/login."
        };

        // Optional global requirement reference; Scalar still shows Authorize when scheme is present.
        // Per-operation auth is inferred from endpoint metadata where possible.
        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SchemeId, document)] = []
        });
    }
}
