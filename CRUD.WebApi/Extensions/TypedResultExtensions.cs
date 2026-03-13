using CRUD.WebApi.ApiError;
using FluentValidation.Results;

namespace CRUD.WebApi.Extensions;

/// <summary>
/// Статический класс, предназначенный для расширений к <see cref="TypedResults"/>.
/// </summary>
/// <remarks>
/// <para>Расширения находятся в свойстве <see cref="TypedResults.Extensions"/>.</para>
/// <para>Например, <c>return <see cref="TypedResults.Extensions"/>.Problem(apiError, localizer);</c></para>
/// </remarks>
public static class TypedResultExtensions
{
    // Чем меньше повторюшкиного кода - тем лучше
    // ValidationProblem и Problem сопровождаются почти в каждом endpoint'е, пусть логика будет написана единожды

    /// <summary>
    /// Расширение к <see cref="TypedResults"/> для удобства вывода ошибок валидации клиенту, используя результат валидации и локализатор.
    /// </summary>
    /// <remarks>
    /// <para>Результат валидации преобразуется в словарь с ошибками, локализируется заголовок.</para>
    /// Использование:
    /// <c>return <see cref="TypedResults.Extensions"/>.ValidationProblem(validationResult, localizer);</c>
    /// </remarks>
    /// <param name="validationResult">Результат валидации.</param>
    /// <param name="localizer">Ресурсный локализатор.</param>
    /// <returns><see cref="Microsoft.AspNetCore.Http.HttpResults.ValidationProblem"/>, созданный через <see cref="TypedResults.ValidationProblem(IDictionary{string, string[]}, string?, string?, string?, string?, IDictionary{string, object?}?)"/>.</returns>
    public static ValidationProblem ValidationProblem(this IResultExtensions _, ValidationResult validationResult, IResourceLocalizer localizer)
    {
        var exts = new Dictionary<string, object?>
        {
            { "code", ErrorCodes.VALIDATION_PROBLEM }
        };

        return TypedResults.ValidationProblem(validationResult.ToDictionary(), title: localizer[ResourceLocalizerConstants.OneOrMoreValidationErrorsOccurred], extensions: exts);
    }

    /// <summary>
    /// Расширение к <see cref="TypedResults"/> для удобства вывода проблем клиенту, используя ошибку сервиса и локализатор.
    /// </summary>
    /// <remarks>
    /// <para>Локализируется заголовок, локализируются детали, если есть агрументы к деталям, они применяются, вписывается статус.</para>
    /// Использование:
    /// <c>return <see cref="TypedResults.Extensions"/>.Problem(apiError, localizer);</c>
    /// </remarks>
    /// <param name="apiError">Ошибка для API ответа.</param>
    /// <param name="localizer">Ресурсный локализатор.</param>
    /// <returns><see cref="ProblemHttpResult"/>, созданный через <see cref="TypedResults.Problem(string?, string?, int?, string?, string?, IEnumerable{KeyValuePair{string, object?}}?)"/>.</returns>
    public static ProblemHttpResult Problem(this IResultExtensions _, CRUD.WebApi.ApiError.ApiError apiError, IResourceLocalizer localizer)
    {
        var error = apiError;
        var title = localizer[error.Title];
        var detail = error.Params != null ? localizer.ReplaceParams(error.Detail, error.Params) : localizer[error.Detail];
        var exts = new Dictionary<string, object?>
        {
            { "code", apiError.Code }
        };

        return TypedResults.Problem(title: title, detail: detail, statusCode: error.Status, extensions: exts);
    }

    /// <summary>
    /// Расширение к <see cref="TypedResults"/> для удобства вывода проблем клиенту, используя ошибку сервиса, локализатор и аргументы к ошибке.
    /// </summary>
    /// <remarks>
    /// <para>Локализируется заголовок, локализируются детали, если есть агрументы к деталям, они применяются, вписывается статус.</para>
    /// Использование:
    /// <c>return <see cref="TypedResults.Extensions"/>.Problem(apiError, localizer, _options.MaxFileSizeString);</c>
    /// </remarks>
    /// <param name="apiError">Ошибка для API ответа.</param>
    /// <param name="localizer">Ресурсный локализатор.</param>
    /// <param name="args">Аргументы к ошибкке.</param>
    /// <returns><see cref="ProblemHttpResult"/>, созданный через <see cref="TypedResults.Problem(string?, string?, int?, string?, string?, IEnumerable{KeyValuePair{string, object?}}?)"/>.</returns>
    public static ProblemHttpResult Problem(this IResultExtensions _, CRUD.WebApi.ApiError.ApiError apiError, IResourceLocalizer localizer, params object[] args)
    {
        var error = apiError;
        var title = localizer[error.Title];
        var detail = args != null ? localizer.ReplaceParams(error.Detail, args.Select(x => x.ToString()).ToList()!) : localizer[error.Detail];
        var exts = new Dictionary<string, object?>
        {
            { "code", apiError.Code }
        };

        return TypedResults.Problem(title: title, detail: detail, statusCode: error.Status, extensions: exts);
    }

    /// <summary>
    /// Расширение к <see cref="TypedResults"/> для удобства вывода проблем клиенту, используя результат сервиса и локализатор.
    /// </summary>
    /// <remarks>
    /// <para>Локализируется заголовок, локализируются детали, если есть агрументы к деталям, они применяются, вписывается статус.</para>
    /// Использование:
    /// <c>return <see cref="TypedResults.Extensions"/>.Problem(serviceResult, localizer);</c>
    /// </remarks>
    /// <param name="serviceResult">Результат сервиса.</param>
    /// <param name="localizer">Ресурсный локализатор.</param>
    /// <returns><see cref="ProblemHttpResult"/>, созданный через <see cref="TypedResults.Problem(string?, string?, int?, string?, string?, IDictionary{string, object?}?)"/>.</returns>
    public static ProblemHttpResult Problem(this IResultExtensions _, ServiceResult serviceResult, IResourceLocalizer localizer)
    {
        ArgumentNullException.ThrowIfNull(serviceResult);
        ArgumentNullException.ThrowIfNull(serviceResult.ErrorMessage);

        var apiError = ApiErrorConstants.Match(serviceResult.ErrorMessage);

        var error = apiError;
        var title = localizer[error.Title];

        var @params = serviceResult.ErrorParams;
        var detail = @params != null ? localizer.ReplaceParams(error.Detail, @params.Select(x => x.ToString()).ToList()!) : localizer[error.Detail];

        var code = apiError.Code; // Обычный код, который определён в константах ApiErrorConstants
        if (serviceResult.ErrorCode != null) // Если сервис переопределил код, то меняем на него
            code = serviceResult.ErrorCode;

        var exts = new Dictionary<string, object?>
        {
            { "code", code }
        };

        return TypedResults.Problem(title: title, detail: detail, statusCode: error.Status, extensions: exts);
    }

    /// <summary>
    /// Расширение к <see cref="TypedResults"/> для удобства вывода проблем клиенту, используя результат сервиса и локализатор.
    /// </summary>
    /// <remarks>
    /// <para>Локализируется заголовок, локализируются детали, если есть агрументы к деталям, они применяются, вписывается статус.</para>
    /// Использование:
    /// <c>return <see cref="TypedResults.Extensions"/>.Problem(serviceResult, localizer);</c>
    /// </remarks>
    /// <param name="serviceResult">Результат сервиса.</param>
    /// <param name="localizer">Ресурсный локализатор.</param>
    /// <returns><see cref="ProblemHttpResult"/>, созданный через <see cref="TypedResults.Problem(string?, string?, int?, string?, string?, IDictionary{string, object?}?)"/>.</returns>
    public static ProblemHttpResult Problem<T>(this IResultExtensions _, ServiceResult<T> serviceResult, IResourceLocalizer localizer)
    {
        ArgumentNullException.ThrowIfNull(serviceResult);
        ArgumentNullException.ThrowIfNull(serviceResult.ErrorMessage);

        var apiError = ApiErrorConstants.Match(serviceResult.ErrorMessage);

        var error = apiError;
        var title = localizer[error.Title];

        var @params = serviceResult.ErrorParams;
        var detail = @params != null ? localizer.ReplaceParams(error.Detail, @params.Select(x => x.ToString()).ToList()!) : localizer[error.Detail];

        var code = apiError.Code; // Обычный код, который определён в константах ApiErrorConstants
        if (serviceResult.ErrorCode != null) // Если сервис переопределил код, то меняем на него
            code = serviceResult.ErrorCode;

        var exts = new Dictionary<string, object?>
        {
            { "code", code }
        };

        return TypedResults.Problem(title: title, detail: detail, statusCode: error.Status, extensions: exts);
    }
}