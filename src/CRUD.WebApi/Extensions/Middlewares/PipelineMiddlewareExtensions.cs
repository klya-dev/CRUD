namespace CRUD.WebApi.Extensions.Middlewares;

/// <summary>
/// <see cref="WebApplication"/> для настройки пайплайна веб-приложения.
/// </summary>
public static class PipelineMiddlewareExtensions
{
    /// <summary>
    /// Настраивает пайплайн веб-приложения.
    /// </summary>
    public static WebApplication UseWebApplicationPipeline(this WebApplication app, ProgramOptions programOptions)
    {
        // Пропускаем ли логирование
        if (!programOptions.SkipLogging)
            app.UseCustomRequestLogging();
        app.UseForwardedHeaders();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage(); // Этот Middleware и так захардкожен по дефолту для Development (https://github.com/dotnet/aspnetcore/blob/main/src/DefaultBuilder/src/WebApplicationBuilder.cs#L402-L405)
            // Для API генерируется грамотный, красивый ответ application/problem+json, учитывая, что я выше добавил .AddProblemDetails()

            app.MapOpenApi(); // Конечная точка "/openapi/v1.json"
            app.MapScalarApiReference();
            app.UseSwaggerUi(options =>
            {
                options.Path = "/openapi";
                options.DocumentPath = "/openapi/v1.json";
                options.DocumentTitle = "CRUD"; // Название вкладки
            });
        }

        app.UseRequestLocalization(); // В обработчиках исключений используется локализация

        // Добавить обработчики ошибок в pipeline (выше добавлены AddExceptionHandler)
        app.UseExceptionHandler(); // GlobalExceptionHandler, который скрывает внутренности включается только в Production, а остальные обработчики везде

        if (app.Environment.IsProduction())
        {
            app.UseHsts();
        }

        app.UseMiddleware<BasicAuthMetricsMiddleware>();

        //app.UseHttpsRedirection(); // Если не закомментировать, то ЮКасса не будет работать с Tuna (307 статус код). Приложение получает запрос от Tuna, а в ответ присылает редирект на https, но Tuna не умеет в редиректы
        app.UseReadyStaticFilesAndDirectoryBrowser();
        app.UseRouting();
        app.UseRequestTimeouts();
        app.UseCors();
        app.UseAuthentication();
        app.UseRateLimiter(); // Использует локализацию и аутентификацию
        app.UseAuthorization();
        app.UseOutputCache(); // Обязательно после UseCors и UseRouting

        return app;
    }
}