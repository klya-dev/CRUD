using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace CRUD.Tests.Helpers;

/// <summary>
/// Статический класс с расширениями для <see cref="IWebHostBuilder"/>.
/// </summary>
public static class WebHostBuilderExtensions
{
    /// <summary>
    /// Заменяет <see cref="IHttpContextAccessor"/> на <see cref="TestHttpContextAccessor"/>.
    /// </summary>
    /// <remarks>
    /// Использование: <c>factory.WithWebHostBuilder(configuration => configuration.WithTestHttpContextAccessor());</c>
    /// </remarks>
    public static IWebHostBuilder WithTestHttpContextAccessor(this IWebHostBuilder webHostBuilder)
    {
        return webHostBuilder.ConfigureServices(x =>
        {
            x.AddSingleton<IHttpContextAccessor, TestHttpContextAccessor>();
        });
    }
}