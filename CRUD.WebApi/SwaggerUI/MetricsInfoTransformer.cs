using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace CRUD.WebApi.SwaggerUI;

/// <summary>
/// Добавляет "/metrics" в Swagger UI.
/// </summary>
public class MetricsInfoTransformer : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Paths.Add("/metrics", new OpenApiPathItem()
        {
            Operations =
                new Dictionary<OperationType, OpenApiOperation>()
                {
                    {
                        OperationType.Get,
                        new OpenApiOperation()
                        { 
                            Tags = [new OpenApiTag() { Name = EndpointTags.AllEndpointsForBusiness }, new OpenApiTag() { Name = EndpointTags.Metrics } ],
                            Summary = "Метрики приложения.",
                            Description = "Используется для Prometheus.",
                            Responses =
                            {
                                ["200"] = new OpenApiResponse() { Description = "OK" }
                            }
                        }
                    }
                },
        });

        await Task.CompletedTask;
    }
}