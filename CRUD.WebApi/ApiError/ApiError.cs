namespace CRUD.WebApi.ApiError;

/// <summary>
/// Ошибка для API ответа. Содержит в себе поля полностью описывающие ошибку.
/// </summary>
public class ApiError
{
    /// <summary>
    /// Создаёт <see cref="ApiError"/> с заданным заголовком, деталями и статусом.
    /// </summary>
    /// <param name="title">Заголовок ошибки.</param>
    /// <param name="detail">Детали ошибки.</param>
    /// <param name="status">Статус код ошибки.</param>
    /// <param name="code">Код ошибки.</param>
    public ApiError(string title, string detail, int status, string code)
    {
        Title = title;
        Detail = detail;
        Status = status;
        Code = code;
    }

    /// <summary>
    /// Создаёт <see cref="ApiError"/> с заданным заголовком, деталями, статусом и аргументами для вставки в сообщение.
    /// </summary>
    /// <param name="title">Заголовок ошибки.</param>
    /// <param name="detail">Детали ошибки.</param>
    /// <param name="status">Статус код ошибки.</param>
    /// <param name="code">Код ошибки.</param>
    /// <param name="params">Агрументы для вставки в сообщение.</param>
    public ApiError(string title, string detail, int status, string code, List<string> @params)
        : this(title, detail, status, code)
    {
        Params = @params;
    }

    /// <summary>
    /// Заголовок ошибки.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Детали ошибки.
    /// </summary>
    public string Detail { get; }

    /// <summary>
    /// Статус код ошибки.
    /// </summary>
    public int Status { get; }

    /// <summary>
    /// Код ошибки.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Аргументы для вставки в сообщение.
    /// </summary>
    /// <remarks>
    /// Например, "$A$ &lt; $B$", зависит от реализации.
    /// </remarks>
    public List<string>? Params { get; }

    /// <summary>
    /// Пересоздаёт <see cref="ApiError"/> с указанным <see cref="Status"/>.
    /// </summary>
    /// <param name="status">Новый <see cref="HttpStatusCode"/> статус.</param>
    /// <returns>Пересозданный <see cref="ApiError"/> с указанным <paramref name="status"/>.</returns>
    public ApiError ChangeStatus(HttpStatusCode status)
    {
        if (Params != null)
            return new ApiError(Title, Detail, (int)status, Code, Params);

        return new ApiError(Title, Detail, (int)status, Code);
    }
}