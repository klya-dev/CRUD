using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace CRUD.WebApi.SwaggerUI;

/// <summary>
/// Добавляет информацию об API, авторе в SwaggerUI.
/// </summary>
public class InfoTransformer : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Version = context.DocumentName, // v1, v2. Указываю в AddOpenApi("v1", ...)
            Title = "CRUD API",
            Description = "CRUD API by Klya.\n\n" +
            "Чтобы декодировать Base64: atob().",
            TermsOfService = new Uri("https://example.com/terms"),
            Contact = new OpenApiContact
            {
                Name = "Klya",
                Url = new Uri("https://t.me/klya_official"),
                Email = "fan.ass95@mail.ru"
            },
            License = new OpenApiLicense
            {
                Name = "License",
                Url = new Uri("https://example.com/license")
            }
        };

        await Task.CompletedTask;
    }
}