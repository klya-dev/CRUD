namespace CRUD.WebApi.Errors;

/// <summary>
/// Ошибка для API ответа. Содержит в себе поля полностью описывающие ошибку.
/// </summary>
public sealed record ApiError
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
    public ApiError(string title, string detail, int status, string code, IEnumerable<string> @params)
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
    public int Status { get; private set; }

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
    public IEnumerable<string>? Params { get; }

    /// <summary>
    /// Переназначает <see cref="Status"/> текущему объекту на указанный <paramref name="status"/>.
    /// </summary>
    /// <param name="status">Новый <see cref="HttpStatusCode"/> статус.</param>
    /// <returns><see cref="ApiError"/> с изменённым <see cref="Status"/>.</returns>
    public ApiError ChangeStatus(HttpStatusCode status)
    {
        this.Status = (int)status;
        return this;
    }
}