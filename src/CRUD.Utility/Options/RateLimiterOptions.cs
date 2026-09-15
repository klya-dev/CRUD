using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace CRUD.Utility.Options;

/// <summary>
/// Опции RateLimiter.
/// </summary>
public sealed class RateLimiterOptions
{
    /// <summary>
    /// Название секции.
    /// </summary>
    public const string SectionName = "RateLimiter";

    /// <summary>
    /// Глобальный лимитер.
    /// </summary>
    [ValidateObjectMembers] // Чтобы приложение смогло провалидировать вложенные поля (https://github.com/dotnet/runtime/issues/36093)
    public required Global Global { get; init; }

    /// <summary>
    /// Лимитер для эндпоинтов начинающихся с "/vX/publications" и имеющих метод GET.
    /// </summary>
    [ValidateObjectMembers]
    public required PublicationsGet PublicationsGet { get; init; }
}

// Можно вместо двух классов Global и PublicationsGet сделать один, т.к свойства одинаковые
// Но для большей гибкости, оставляю так

/// <summary>
/// Глобальный лимитер.
/// </summary>
public sealed class Global
{
    /// <summary>
    /// Максимальное количество запросов в окне <see cref="Window"/>.
    /// </summary>
    /// <remarks>
    /// <para>Допускается не более <see cref="PermitLimit"/> запросов в каждом <see cref="Window"/>-секундном окне.</para>
    /// <para>Например, не более 4-х запросов за 12 секунд.</para>
    /// </remarks>
    [Range(1, 10000)] // Не больше 10000 запросов в окне | Немного натянутые "проверки", но это для теста валидации конфигурации при запуске приложения
    public required int PermitLimit { get; init; }

    /// <summary>
    /// Временное окно, в течение которого принимаются запросы в секундах.
    /// </summary>
    [Range(1, 604800)] // Диапазон, от 1 секунды до 1 недели в секундах
    public required int Window { get; init; }

    /// <summary>
    /// Максимальное количество запросов в очереди.
    /// </summary>
    /// <remarks>
    /// 0, чтобы отключить очередь.
    /// </remarks>
    public required int QueueLimit { get; init; }
}

/// <summary>
/// Лимитер для эндпоинтов начинающихся с "/vX/publications" и имеющих метод GET.
/// </summary>
public sealed class PublicationsGet
{
    /// <summary>
    /// Максимальное количество запросов в окне <see cref="Window"/>.
    /// </summary>
    /// <remarks>
    /// <para>Допускается не более <see cref="PermitLimit"/> запросов в каждом <see cref="Window"/>-секундном окне.</para>
    /// <para>Например, не более 4-х запросов за 12 секунд.</para>
    /// </remarks>
    [Range(1, 20000)] // Не больше 20000 запросов в окне
    public required int PermitLimit { get; init; }

    /// <summary>
    /// Временное окно, в течение которого принимаются запросы в секундах.
    /// </summary>
    [Range(1, 604800)]
    public required int Window { get; init; }

    /// <summary>
    /// Максимальное количество запросов в очереди.
    /// </summary>
    /// <remarks>
    /// 0, чтобы отключить очередь.
    /// </remarks>
    public required int QueueLimit { get; init; }
}