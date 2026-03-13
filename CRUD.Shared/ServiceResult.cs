namespace CRUD.Shared;

/// <summary>
/// Универсальный класс, представляющий результат сервиса.
/// </summary>
/// <remarks>
/// Внутри <see cref="ServiceResult{T}"/> свойства: <see cref="Value"/>, <see cref="ErrorMessage"/>, <see cref="ErrorCode"/> и <see cref="ErrorParams"/>, значение, ошибка, код ошибки и аргументы соответственно.
/// </remarks>
/// <typeparam name="T">Значение работы сервиса.</typeparam>
public class ServiceResult<T>
{
    /// <summary>
    /// Успешный конструктор.
    /// </summary>
    /// <remarks>
    /// В <see cref="Value"/> вписывается <paramref name="value"/>, а в <see cref="ErrorMessage"/>, <see cref="ErrorCode"/>, и <see cref="ErrorParams"/> <see langword="null"/>.
    /// </remarks>
    /// <param name="value">Значение, которые вписывается в <see cref="Value"/>.</param>
    private ServiceResult(T value)
    {
        Value = value;
        ErrorMessage = null;
        ErrorCode = null;
        ErrorParams = null;
    }

    /// <summary>
    /// Неудачный конструктор.
    /// </summary>
    /// <remarks>
    /// Для <see cref="Value"/> задаётся значение по умолчанию, а в <see cref="ErrorMessage"/> вписывается <paramref name="error"/>.
    /// </remarks>
    /// <param name="error">Значение, которые вписывается в <see cref="ErrorMessage"/>.</param>
    private ServiceResult(string error)
    {
        Value = default;
        ErrorMessage = error;
        ErrorCode = null;
        ErrorParams = null;
    }

    /// <summary>
    /// Неудачный конструктор.
    /// </summary>
    /// <remarks>
    /// Для <see cref="Value"/> задаётся значение по умолчанию, а в <see cref="ErrorMessage"/> и <see cref="ErrorCode"/> вписывается <paramref name="error"/> и <paramref name="code"/> соотвественно.
    /// </remarks>
    /// <param name="error">Значение, которые вписывается в <see cref="ErrorMessage"/>.</param>
    /// <param name="code">Значение, которые вписывается в <see cref="ErrorCode"/>.</param>
    private ServiceResult(string error, string code)
        : this(error)
    {
        ErrorCode = code;
    }

    /// <summary>
    /// Неудачный конструктор.
    /// </summary>
    /// <remarks>
    /// <para>В <see cref="ErrorMessage"/> вписывается <paramref name="error"/>.</para>
    /// <para>В <see cref="ErrorCode"/> вписывается <paramref name="code"/>.</para>
    /// <para>В <see cref="ErrorParams"/> вписывается <paramref name="args"/>.</para>
    /// </remarks>
    /// <param name="error">Значение, которые вписывается в <see cref="ErrorMessage"/>.</param>
    /// <param name="code">Значение, которые вписывается в <see cref="ErrorCode"/>.</param>
    /// <param name="args">Аргументы, которые будут вписанны в <see cref="ErrorParams"/>.</param>
    private ServiceResult(string error, string code, params object[]? args)
        : this(error, code)
    {
        ErrorParams = args;
    }

    /// <summary>
    /// Неудачный конструктор.
    /// </summary>
    /// <remarks>
    /// <para>В <see cref="ErrorMessage"/> вписывается <paramref name="error"/>.</para>
    /// <para>В <see cref="ErrorParams"/> вписывается <paramref name="args"/>.</para>
    /// </remarks>
    /// <param name="error">Значение, которые вписывается в <see cref="ErrorMessage"/>.</param>
    /// <param name="args">Аргументы, которые будут вписанны в <see cref="ErrorParams"/>.</param>
    private ServiceResult(string error, params object[]? args)
        : this(error)
    {
        ErrorParams = args;
    }

    /// <summary>
    /// Ответ сервиса.
    /// </summary>
    /// <remarks>
    /// Например, <see cref="string"/>, User.
    /// </remarks>
    public T? Value { get; }

    /// <summary>
    /// Ошибка сервиса.
    /// </summary>
    /// <remarks>
    /// Например, <see cref="ErrorMessages.UserNotFound"/>.
    /// </remarks>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Код ошибки.
    /// </summary>
    /// <remarks>
    /// Например, <see cref="ErrorCodes.USER_NOT_FOUND"/>.
    /// </remarks>
    public string? ErrorCode { get; }

    /// <summary>
    /// Аргументы ошибки сервиса.
    /// </summary>
    /// <remarks>
    /// Используется для передачи важных сведений об ошибке, может быть применено к локализации.
    /// </remarks>
    public object[]? ErrorParams { get; }

    /// <summary>
    /// Создаёт успешный результат через приватный конструктор <see cref="ServiceResult{T}.ServiceResult(T)"/> с указанным значением.
    /// </summary>
    /// <param name="value">Значение, которое будет вписанно в <see cref="Value"/>.</param>
    /// <returns>Новый, созданный через <see cref="ServiceResult{T}.ServiceResult(T)"/> конструктор, экземпляр.</returns>
    public static ServiceResult<T> Success(T value) => new(value);

    /// <summary>
    /// Создаёт неудачный результат через приватный конструктор <see cref="ServiceResult{T}.ServiceResult(string)"/> с указанным значением.
    /// </summary>
    /// <param name="error">Значение, которое будет вписанно в <see cref="ErrorMessage"/>.</param>
    /// <returns>Новый, созданный через <see cref="ServiceResult{T}.ServiceResult(string)"/> конструктор, экземпляр.</returns>
    public static ServiceResult<T> Fail(string error) => new(error);

    /// <summary>
    /// Создаёт неудачный результат через приватный конструктор <see cref="ServiceResult{T}.ServiceResult(string, string)"/> с указанными значениями.
    /// </summary>
    /// <param name="error">Значение, которое будет вписанно в <see cref="ErrorMessage"/>.</param>
    /// <param name="code">Значение, которое будет вписанно в <see cref="ErrorCode"/>.</param>
    /// <returns>Новый, созданный через <see cref="ServiceResult{T}.ServiceResult(string, string)"/> конструктор, экземпляр.</returns>
    public static ServiceResult<T> Fail(string error, string code) => new(error, code);

    /// <summary>
    /// Создаёт неудачный результат через приватный конструктор <see cref="ServiceResult{T}.ServiceResult(string, string, object[])"/> с указанными значениями.
    /// </summary>
    /// <param name="error">Значение, которое будет вписанно в <see cref="ErrorMessage"/>.</param>
    /// <param name="code">Значение, которое будет вписанно в <see cref="ErrorCode"/>.</param>
    /// <param name="args">Аргументы, которые будут вписанны в <see cref="ErrorParams"/>.</param>
    /// <returns>Новый, созданный через <see cref="ServiceResult{T}.ServiceResult(string, string, object[])"/> конструктор, экземпляр.</returns>
    public static ServiceResult<T> Fail(string error, string code, params object[]? args) => new(error, code, args);

    /// <summary>
    /// Создаёт неудачный результат через приватный конструктор <see cref="ServiceResult{T}.ServiceResult(string, object[])"/> с указанными значениями.
    /// </summary>
    /// <param name="error">Значение, которое будет вписанно в <see cref="ErrorMessage"/>.</param>
    /// <param name="args">Аргументы, которые будут вписанны в <see cref="ErrorParams"/>.</param>
    /// <returns>Новый, созданный через <see cref="ServiceResult{T}.ServiceResult(string, object[])"/> конструктор, экземпляр.</returns>
    public static ServiceResult<T> Fail(string error, params object[]? args) => new(error, args);
}

/// <summary>
/// Результат сервиса.
/// </summary>
/// <remarks>
/// Внутри <see cref="ServiceResult"/> свойства: <see cref="ErrorMessage"/>, <see cref="ErrorCode"/> и <see cref="ErrorParams"/>, ошибка, код ошибки и аргументы соответственно.
/// </remarks>
public class ServiceResult
{
    /// <summary>
    /// Успешный конструктор.
    /// </summary>
    /// <remarks>
    /// В <see cref="ErrorMessage"/>, <see cref="ErrorCode"/> и <see cref="ErrorParams"/> вписывается <see langword="null"/>.
    /// </remarks>
    private ServiceResult()
    {
        ErrorMessage = null;
        ErrorCode = null;
        ErrorParams = null;
    }

    /// <summary>
    /// Неудачный конструктор.
    /// </summary>
    /// <remarks>
    /// В <see cref="ErrorMessage"/> вписывается <paramref name="error"/>.
    /// </remarks>
    /// <param name="error">Значение, которые вписывается в <see cref="ErrorMessage"/>.</param>
    private ServiceResult(string error)
    {
        ErrorMessage = error;
        ErrorCode = null;
        ErrorParams = null;
    }

    /// <summary>
    /// Неудачный конструктор.
    /// </summary>
    /// <remarks>
    /// <para>В <see cref="ErrorMessage"/> вписывается <paramref name="error"/>.</para>
    /// <para>В <see cref="ErrorCode"/> вписывается <paramref name="code"/>.</para>
    /// </remarks>
    /// <param name="error">Значение, которые вписывается в <see cref="ErrorMessage"/>.</param>
    /// <param name="code">Значение, которые вписывается в <see cref="ErrorCode"/>.</param>
    private ServiceResult(string error, string code)
        : this(error)
    {
        ErrorCode = code;
    }

    /// <summary>
    /// Неудачный конструктор.
    /// </summary>
    /// <remarks>
    /// <para>В <see cref="ErrorMessage"/> вписывается <paramref name="error"/>.</para>
    /// <para>В <see cref="ErrorParams"/> вписывается <paramref name="args"/>.</para>
    /// </remarks>
    /// <param name="error">Значение, которые вписывается в <see cref="ErrorMessage"/>.</param>
    /// <param name="args">Аргументы, которые будут вписанны в <see cref="ErrorParams"/>.</param>
    private ServiceResult(string error, params object[]? args)
        : this(error)
    {
        ErrorParams = args;
    }

    /// <summary>
    /// Неудачный конструктор.
    /// </summary>
    /// <remarks>
    /// <para>В <see cref="ErrorMessage"/> вписывается <paramref name="error"/>.</para>
    /// <para>В <see cref="ErrorCode"/> вписывается <paramref name="code"/>.</para>
    /// <para>В <see cref="ErrorParams"/> вписывается <paramref name="args"/>.</para>
    /// </remarks>
    /// <param name="error">Значение, которые вписывается в <see cref="ErrorMessage"/>.</param>
    /// <param name="code">Значение, которые вписывается в <see cref="ErrorCode"/>.</param>
    /// <param name="args">Аргументы, которые будут вписанны в <see cref="ErrorParams"/>.</param>
    private ServiceResult(string error, string code, params object[]? args)
        : this(error, code)
    {
        ErrorParams = args;
    }

    /// <summary>
    /// Ошибка сервиса.
    /// </summary>
    /// <remarks>
    /// Например, <see cref="ErrorMessages.UserNotFound"/>.
    /// </remarks>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Код ошибки.
    /// </summary>
    /// <remarks>
    /// Например, <see cref="ErrorCodes.USER_NOT_FOUND"/>.
    /// </remarks>
    public string? ErrorCode { get; }

    /// <summary>
    /// Аргументы ошибки сервиса.
    /// </summary>
    /// <remarks>
    /// Используется для передачи важных сведений об ошибке, может быть применено к локализации.
    /// </remarks>
    public object[]? ErrorParams { get; }

    /// <summary>
    /// Создаёт успешный результат через приватный конструктор <see cref="ServiceResult()"/>.
    /// </summary>
    /// <returns>Новый, созданный через <see cref="ServiceResult()"/> конструктор, экземпляр.</returns>
    public static ServiceResult Success() => new();

    /// <summary>
    /// Создаёт неудачный результат через приватный конструктор <see cref="ServiceResult(string)"/> с указанным значением.
    /// </summary>
    /// <param name="error">Значение, которое будет вписанно в <see cref="ErrorMessage"/>.</param>
    /// <returns>Новый, созданный через <see cref="ServiceResult(string)"/> конструктор, экземпляр.</returns>
    public static ServiceResult Fail(string error) => new(error);

    /// <summary>
    /// Создаёт неудачный результат через приватный конструктор <see cref="ServiceResult(string, string)"/> с указанными значениями.
    /// </summary>
    /// <param name="error">Значение, которое будет вписанно в <see cref="ErrorMessage"/>.</param>
    /// <param name="code">Аргументы, которые будут вписанны в <see cref="ErrorCode"/>.</param>
    /// <returns>Новый, созданный через <see cref="ServiceResult(string, string)"/> конструктор, экземпляр.</returns>
    public static ServiceResult Fail(string error, string code) => new(error, code);

    /// <summary>
    /// Создаёт неудачный результат через приватный конструктор <see cref="ServiceResult(string, object[])"/> с указанными значениями.
    /// </summary>
    /// <param name="error">Значение, которое будет вписанно в <see cref="ErrorMessage"/>.</param>
    /// <param name="args">Аргументы, которые будут вписанны в <see cref="ErrorParams"/>.</param>
    /// <returns>Новый, созданный через <see cref="ServiceResult(string, object[])"/> конструктор, экземпляр.</returns>
    public static ServiceResult Fail(string error, params object[]? args) => new(error, args);

    /// <summary>
    /// Создаёт неудачный результат через приватный конструктор <see cref="ServiceResult(string, string, object[])"/> с указанными значениями.
    /// </summary>
    /// <param name="error">Значение, которое будет вписанно в <see cref="ErrorMessage"/>.</param>
    /// <param name="code">Аргументы, которые будут вписанны в <see cref="ErrorCode"/>.</param>
    /// <param name="args">Аргументы, которые будут вписанны в <see cref="ErrorParams"/>.</param>
    /// <returns>Новый, созданный через <see cref="ServiceResult(string, string, object[])"/> конструктор, экземпляр.</returns>
    public static ServiceResult Fail(string error, string code, params object[]? args) => new(error, code, args);
}