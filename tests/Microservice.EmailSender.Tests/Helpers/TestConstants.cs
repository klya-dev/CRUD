namespace Microservice.EmailSender.Tests.Helpers;

/// <summary>
/// Статический класс с тестовыми константами для тестов.
/// </summary>
public static class TestConstants
{
    public const string TEST_FILES_PATH = "EmailSender/test_files";

    public const string HEALTHZ_URL = "healthz";
    public const string METRICS_URL = "metrics";

    /// <summary>
    /// Добавляет сгенерированный <c>Bearer</c> токен для авторизации и аутентификации.
    /// </summary>
    /// <param name="request">Запрос, к которому будет добавлен <c>Bearer</c> токен.</param>
    public static string AddBearerToken(HttpRequestMessage request)
    {
        var token = TokenManager.GenerateEmailSenderAuthToken();
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return token;
    }
}