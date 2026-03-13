using Microsoft.Extensions.Logging;

namespace CRUD.Shared;

/// <summary>
/// Статический класс с расширениями для <see cref="ILogger"/>.
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Логирует информационное сообщение: "Запущен фоновый сервис «{serviceName}».".
    /// </summary>
    /// <param name="nameofService">Имя фонового сервиса.</param>
    public static void StartedBackgroundServiceLog(this ILogger logger, string nameofService) => logger.LogInformation("Запущен фоновый сервис «{serviceName}».", nameofService);

    /// <summary>
    /// Логирует предупреждающее сообщение: "Остановлен фоновый сервис «{serviceName}».".
    /// </summary>
    /// <param name="nameofService">Имя фонового сервиса.</param>
    public static void StopedBackgroundServiceLog(this ILogger logger, string nameofService) => logger.LogWarning("Остановлен фоновый сервис «{serviceName}».", nameofService);
}