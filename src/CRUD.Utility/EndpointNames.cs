namespace CRUD.Utility;

/// <summary>
/// Имена конечных точек.
/// </summary>
/// <remarks>
/// Нужны, чтобы не хардкодить Url до конечной точки.
/// </remarks>
public static class EndpointNames
{
    /// <summary>
    /// Получить публикацию по Id.
    /// </summary>
    /// <remarks>
    /// "<c>/v1/publications/{publicationId}</c>".
    /// </remarks>
    public const string PublicationsGetById = "PublicationsGetById";
}