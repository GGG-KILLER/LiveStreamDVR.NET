using LiveStreamDVR.Api.Configuration;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

namespace LiveStreamDVR.Api.OpenApi.Transformers;

public sealed class AddPrefixToPathsTransformer(IOptionsMonitor<BasicOptions> options)
    : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var pathPrefix = options.CurrentValue.PublicUri.AbsolutePath;
        if (pathPrefix != "/" && document.Paths != null && document.Paths.Any())
        {
            var newPaths = new OpenApiPaths();
            foreach (var path in document.Paths)
            {
                newPaths.Add($"{pathPrefix}{path.Key.TrimStart('/')}", path.Value);
            }
            document.Paths = newPaths;
        }

        return Task.CompletedTask;
    }
}
