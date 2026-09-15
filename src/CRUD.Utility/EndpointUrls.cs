namespace CRUD.Utility;

/// <summary>
/// Url конечных точек.
/// </summary>
/// <remarks>
/// Нужны, чтобы не хардкодить Url до конечной точки.
/// </remarks>
public static class EndpointUrls
{
    /// <summary>
    /// Подтвердить почту по токену.
    /// </summary>
    public const string ConfirmationsEmailByToken = "/v1/confirmations/email/{0}?idmkey={1}";

    /// <summary>
    /// Подтвердить смену пароля по токену.
    /// </summary>
    public const string ConfirmationsPasswordByToken = "/v1/confirmations/password/{0}?idmkey={1}";
}