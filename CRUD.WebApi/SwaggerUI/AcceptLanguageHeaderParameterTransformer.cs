using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CRUD.WebApi.SwaggerUI;

/// <summary>
/// Добавляет поле <c>Accept-Language</c> для заголовока запроса в Swagger UI.
/// </summary>
public sealed class AcceptLanguageHeaderParameterTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        operation.Parameters ??= [];

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "Accept-Language",
            In = ParameterLocation.Header,
            Description = "Код языка (ru, en)."
        });

        return Task.CompletedTask;
    }
}