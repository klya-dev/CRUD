using Microsoft.Extensions.FileProviders;

namespace CRUD.WebApi.Extensions.Middlewares;

/// <summary>
/// <see cref="WebApplication"/> для настройки слоя статических файлов.
/// </summary>
public static class StaticFilesMiddlewareExtensions
{
    /// <summary>
    /// Настраивает слой статических файлов и отображение иерархии файлов в браузере.
    /// </summary>
    /// <remarks>
    /// <see cref="StaticFileExtensions.UseStaticFiles(IApplicationBuilder, StaticFileOptions)"/> и <see cref="DirectoryBrowserExtensions.UseDirectoryBrowser(IApplicationBuilder, DirectoryBrowserOptions)"/>.
    /// </remarks>
    public static WebApplication UseCustomStaticFilesAndDirectoryBrowser(this WebApplication app)
    {
        // Забавный момент, согласно документации, UseStaticFiles не использует сжатие при публикации, но в .NET 9 сжимает, т.к это на уровне SDK, немного непредсказуемое поведение, но меня устраивает
        // https://github.com/dotnet/aspnetcore/issues/59518

        var fileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.WebRootPath, "public"));
        app.UseStaticFiles(new StaticFileOptions
        {
            // Клиент может получить только файлы из папки public
            FileProvider = fileProvider,
            OnPrepareResponse = context =>
            {
                if (context.File.Name == "readme.txt")
                {
                    context.Context.Response.Headers.Append("Content-Type", "text/plain; charset=utf-8"); // Файл на кириллице, поэтому utf-8
                    context.Context.Response.Headers.Append("Cache-Control", "public, max-age=604800"); // Файл может хранится в кэше неделю
                }
            },
            RequestPath = "/public" // Используем путь "https://localhost:7260/public/readme.txt", а не https://localhost:7260/readme.txt
        });

        // Отображение иерархии папок в браузере для удобства
        app.UseDirectoryBrowser(new DirectoryBrowserOptions
        {
            FileProvider = fileProvider, // Отображаем только папку public
            RequestPath = "/public" // Чтобы отобразить иерархию нужно перейти в "/public", а не как по умолчанию "/"
        });

        return app;
    }
}