using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CRUD.WebApi.SwaggerUI;

/// <summary>
/// Добавляет поле <c>Idempotency-Key</c> для заголовока запроса в Swagger UI.
/// </summary>
public sealed class IdempotencyKeyHeaderParameterTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        operation.Parameters ??= [];

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "Idempotency-Key",
            In = ParameterLocation.Header,
            Description = "Идемпотентный ключ (GUID)."
        });

        return Task.CompletedTask;
    }
}