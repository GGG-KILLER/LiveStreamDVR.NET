using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace LiveStreamDVR.Api.OpenApi.Transformers;

public sealed class AddOauthSecuritySchemeTransformer()
    : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Name = "Authorization",
                In = ParameterLocation.Header,
                Scheme = "Bearer",
                BearerFormat = "Json Web Token"
            }
        };

        return Task.CompletedTask;
    }
}
