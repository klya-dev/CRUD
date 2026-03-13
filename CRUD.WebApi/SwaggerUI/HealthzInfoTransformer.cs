using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace CRUD.WebApi.SwaggerUI;

/// <summary>
/// Добавляет "/healthz" в Swagger UI.
/// </summary>
public class HealthzInfoTransformer : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Paths.Add("/healthz", new OpenApiPathItem()
        {
            Operations =
                new Dictionary<OperationType, OpenApiOperation>()
                {
                    {
                        OperationType.Get,
                        new OpenApiOperation()
                        { 
                            Tags = [new OpenApiTag() { Name = EndpointTags.AllEndpointsForBusiness }, new OpenApiTag() { Name = EndpointTags.Healthz } ],
                            Summary = "Проверяет состояние работоспособности сервера и его зависимостей.",
                            Description = "Требуется авторизация.",
                            Responses =
                            {
                                ["200"] = new OpenApiResponse() { Description = "OK" },
                                ["503"] = new OpenApiResponse() { Description = "Service Unavailable", Content = { ["text/plain"] = new OpenApiMediaType() } },
                            },
                            Security =
                            {
                                new OpenApiSecurityRequirement()
                                {
                                    [new OpenApiSecurityScheme()
                                    {
                                        Type = SecuritySchemeType.Http,
                                        Scheme = "bearer",
                                        In = ParameterLocation.Header,
                                        BearerFormat = "Json Web Token",
                                        Reference = new OpenApiReference { Id = "Bearer", Type = ReferenceType.SecurityScheme }
                                    }] = []
                                }
                            }
                        }
                    }
                },
        });

        await Task.CompletedTask;
    }
}