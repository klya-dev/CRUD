namespace CRUD.WebApi.Helpers;

/// <summary>
/// Теги конечных точек.
/// </summary>
public static class EndpointTags
{
    /// <summary>
    /// Все конечные точки для клиента.
    /// </summary>
    public const string AllEndpointsForClient = "All endpoint's for client";

    /// <summary>
    /// Все конечные точки для бизнеса.
    /// </summary>
    public const string AllEndpointsForBusiness = "All endpoint's for business";

    /// <summary>
    /// Авторизация/регистрация, генерация токенов.
    /// </summary>
    public const string Auth = "Auth";

    /// <summary>
    /// Админ-панель.
    /// </summary>
    public const string Admin = "Admin";

    /// <summary>
    /// Пользователи (public).
    /// </summary>
    public const string Users = "Users";

    /// <summary>
    /// Авторизированный (текущий) пользователь.
    /// </summary>
    public const string User = "User";

    /// <summary>
    /// Подтверждения.
    /// </summary>
    public const string Confirmations = "Confirmations";

    /// <summary>
    /// Публикации.
    /// </summary>
    public const string Publications = "Publications";

    /// <summary>
    /// Клиентский API.
    /// </summary>
    public const string ClientApi = "Client API";

    /// <summary>
    /// Вебхуки.
    /// </summary>
    public const string WebHooks = "WebHooks";

    /// <summary>
    /// Проверка работоспособности.
    /// </summary>
    public const string Healthz = "Healthz";

    /// <summary>
    /// Метрики.
    /// </summary>
    public const string Metrics = "Metrics";

    /// <summary>
    /// Общеизвестные конечные точки.
    /// </summary>
    public const string WellKnown = "WellKnown";
}