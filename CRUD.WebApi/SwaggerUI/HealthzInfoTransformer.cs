using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CRUD.WebApi.SwaggerUI;

/// <summary>
/// Добавляет "/healthz" в Swagger UI.
/// </summary>
public sealed class HealthzInfoTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Paths.Add("/healthz", new OpenApiPathItem()
        {
            Operations =
                new Dictionary<HttpMethod, OpenApiOperation>()
                {
                    {
                        HttpMethod.Get,
                        new OpenApiOperation()
                        { 
                            Tags = new HashSet<OpenApiTagReference> { new(EndpointTags.AllEndpointsForBusiness), new(EndpointTags.Healthz) },
                            Summary = "Проверяет состояние работоспособности сервера и его зависимостей.",
                            Description = "Требуется авторизация.",
                            Responses = new OpenApiResponses()
                            {
                                ["200"] = new OpenApiResponse() { Description = "OK" },
                                ["401"] = new OpenApiResponse() { Description = "Unauthorize" },
                                ["503"] = new OpenApiResponse() { Description = "Service Unavailable",
                                    Content = new Dictionary<string, OpenApiMediaType>()
                                    {
                                        ["text/plain"] = new OpenApiMediaType()
                                    }},
                            },
                            Security =
                            [
                                new OpenApiSecurityRequirement()
                                {
                                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                                }
                            ]
                        }
                    }
                },
        });

        return Task.CompletedTask;
    }
}