namespace CRUD.WebApi.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> расширения CORS.
/// </summary>
public static class CorsExtensions
{
    /// <summary>
    /// Добавляет и настраивает CORS.
    /// </summary>
    public static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration configuration)
    {
        // "Сервер всегда физически отправляет заголовки в HTTP-ответе,
        // просто браузер является единственным клиентом,
        // который их сознательно прячет от JavaScript-кода ради безопасности пользователя."

        // В микросервисе EmailSender вообще не нужен CORS, т.к его клиенты это только Prometheus и API, ничто из них не является браузером.
        // Так называемый Server-to-Server

        var clientsOptions = configuration.GetSection(ClientsOptions.SectionName).Get<ClientsOptions>()!;

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                // Разрешаем только нашему клиенту (сайту)
                builder.WithOrigins(clientsOptions.WebClientURLs)
                    .AllowAnyMethod() // Разрешаем любые HTTP-методы в запросе
                    .AllowAnyHeader() // Разрешаем любые заголовки в запросе
                    .AllowCredentials() // Разрешаем отправку данных
                    .WithExposedHeaders("X-REQUIRE-UPDATE-AUTH-TOKEN"); // Разрешаем кастомные заголовки в ответе
                // JS-код клиента (браузера) не сможет получить мой кастомный заголовок, если явно его не разрешить (не даст прочитать - null)
            });

            options.AddPolicy(CorsPolicyNames.AllowAll, builder =>
            {
                builder.AllowAnyOrigin() // Принимаем запросы с любого адреса
                       .AllowAnyMethod() // С любыми методами (GET, POST...)
                       .AllowAnyHeader(); // С любыми заголовками
            });

            // Политики CORS для Prometheus не нужны, т.к это не браузер, и он не использует JavaScript для отправки HTTP-запросов
        });

        return services;
    }
}