using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CRUD.Tests.Helpers;

/// <summary>
/// Реализация класса <see cref="WebApplicationFactory{T}"/> для тестов.
/// </summary>
/// <remarks>
/// Параметры:
/// <list type="bullet">
/// <item>Среда <c>Production</c>.</item>
/// <item>Конфигурация из файла <c>testsettings.json</c>.</item>
/// <item>
/// </item>
/// </list>
/// </remarks>
public class TestWebApplicationFactory : WebApplicationFactory<IApiMarker>, IAsyncLifetime
{
    public HttpClient HttpClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        HttpClient = CreateClient();

        await Task.CompletedTask;
    }

    /// <summary>
    /// Полностью пересоздаёт тестовую базу данных.
    /// </summary>
    /// <returns>Контекст базы данных.</returns>
    public static ApplicationDbContext RecreateDatabase()
    {
        return DbContextGenerator.GenerateDbContextTest();
    }

    public new async Task DisposeAsync()
    {
        await Task.CompletedTask;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        //Environment.SetEnvironmentVariable("ConnectionStrings:DefaultConnection", TestSettingsHelper.GetDbConnectionString());

        var testConfig = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "testsettings.json"))
            .AddUserSecrets<TestMarker>()
            .Build();
        builder.UseConfiguration(testConfig);

        //builder.ConfigureServices(services =>
        //{
        //    //services.AddHttpContextAccessor();
        //    //services.AddSingleton<IHttpContextAccessor, TestHttpContextAccessor>();
        //});

        //builder.ConfigureTestServices(services =>
        //{
        //    services.RemoveAll<SaveLogsToS3BackgroundService>();
        //    services.RemoveAll<ISaveLogsToS3BackgroundCore>();
        //});
    }
}