namespace CRUD.Utility;

/// <summary>
/// Имена/политики для создания <see cref="HttpClient"/>.
/// </summary>
public static class HttpClientNames
{
    /// <summary>
    /// Клиент для сервиса оплаты.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Указан базовый адрес.</item>
    /// <item>Авторизация для каждого запроса.</item>
    /// <item>Неудачные запросы повторяются до трех раз с задержкой 600 мс между попытками.</item>
    /// </list>
    /// </remarks>
    public const string PayManager = "PayManager";

    /// <summary>
    /// Клиент для сервиса отправки SMS.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Указан базовый адрес.</item>
    /// <item>Авторизация для каждого запроса.</item>
    /// <item>Неудачные запросы повторяются до трех раз с задержкой 600 мс между попытками.</item>
    /// </list>
    /// </remarks>
    public const string SmsSender = "SmsSender";

    /// <summary>
    /// Клиент для сервиса интеграции телеграма.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Указан базовый адрес.</item>
    /// <item>Авторизация для каждого запроса.</item>
    /// <item>Неудачные запросы повторяются до трех раз с задержкой 600 мс между попытками.</item>
    /// </list>
    /// </remarks>
    public const string TelegramIntegration = "TelegramIntegration";

    /// <summary>
    /// EmailSender клиент.
    /// </summary>
    /// <remarks>
    /// <para>Для gRPC использовать другой клиент - <see cref="GrpcClientNames.GrpcEmailSender"/> через <see cref="GrpcClientFactory"/>.</para>
    /// <para>Этот клиент для вызова конечных точек, например, "/healthz".</para>
    /// <list type="bullet">
    /// <item>Указан базовый адрес.</item>
    /// <item>Авторизация для каждого запроса.</item>
    /// <item>Неудачные запросы повторяются до трех раз с задержкой 600 мс между попытками.</item>
    /// </list>
    /// </remarks>
    public const string EmailSender = "EmailSender";

    /// <summary>
    /// Prometheus клиент.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Указан базовый адрес.</item>
    /// <item>Неудачные запросы повторяются до трех раз с задержкой 600 мс между попытками.</item>
    /// </list>
    /// </remarks>
    public const string Prometheus = "Prometheus";

    /// <summary>
    /// Неудачные запросы повторяются до трех раз с задержкой 600 мс между попытками.
    /// </summary>
    /// <remarks>
    /// Неудачными запросами считаются: 5XX, 408, <see cref="System.Net.Http.HttpRequestException"/>.
    /// </remarks>
    public const string PollyWaitAndRetry = "PollyWaitAndRetry";

    /// <summary>
    /// Если исходящий запрос является запросом GET, применяется время ожидания 10 секунд. Для остальных методов время ожидания — 20 секунд.
    /// </summary>
    public const string PollyDynamic = "PollyDynamic";
}