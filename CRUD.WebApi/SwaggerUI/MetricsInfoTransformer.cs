using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CRUD.WebApi.SwaggerUI;

/// <summary>
/// Добавляет "/metrics" в Swagger UI.
/// </summary>
public sealed class MetricsInfoTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Paths.Add("/metrics", new OpenApiPathItem()
        {
            Operations =
                new Dictionary<HttpMethod, OpenApiOperation>()
                {
                    {
                        HttpMethod.Get,
                        new OpenApiOperation()
                        {
                            Tags = new HashSet<OpenApiTagReference> { new(EndpointTags.AllEndpointsForBusiness), new(EndpointTags.Metrics) },
                            Summary = "Метрики приложения.",
                            Description = "Используется для Prometheus.",
                            Responses = new OpenApiResponses()
                            {
                                ["200"] = new OpenApiResponse() { Description = "OK" }
                            }
                        }
                    }
                },
        });

        return Task.CompletedTask;
    }
}