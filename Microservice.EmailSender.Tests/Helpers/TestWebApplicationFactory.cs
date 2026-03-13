using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Microservice.EmailSender.Tests.Helpers;

/// <summary>
/// Реализация класса <see cref="WebApplicationFactory{T}"/> для тестов.
/// </summary>
/// <remarks>
/// Параметры:
/// <list type="bullet">
/// <item>Среда <c>Production</c>.</item>
/// <item>Конфигурация из файла <c>testsettings.json</c>.</item>
/// <item>Изменённый <see cref="JwtBearerOptions"/> на тестовые значения.</item>
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

    public new async Task DisposeAsync()
    {
        await Task.CompletedTask;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");

        var testConfig = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "testsettings.json"))
            .AddUserSecrets<TestMarker>()
            .Build();
        builder.UseConfiguration(testConfig);

        builder.ConfigureTestServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                // Заменяем валидацию JWT-токенов на тестовую, чтобы не использовать настоящие ключи
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = "TestIssuer",
                    ValidateAudience = true,
                    ValidAudience = "TestAudience",
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    IssuerSigningKey = TokenManager.PublicKey // Тестовый публичный ключ
                };
            });
        });
    }
}