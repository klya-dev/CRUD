namespace CRUD.WebApi.Filters;

/// <summary>
/// Фильтр валидации.
/// </summary>
/// <remarks>
/// <para>Для модели <typeparamref name="T"/> должен быть написан <see cref="IValidator{T}"/> и добавлен в <c>DI</c>.</para>
/// <para>Используется локализация <see cref="IResourceLocalizer"/>.</para>
/// <para>Исключение, если в аргументах  конечной точки нет модели <typeparamref name="T"/>.</para>
/// </remarks>
/// <typeparam name="T">Модель, которую нужно провалидировать.</typeparam>
public class ValidationFilter<T> : IEndpointFilter where T : class
{
    private readonly IValidator<T> _validator;

    public ValidationFilter(IValidator<T> validator)
    {
        _validator = validator;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // Пример аргументов для /login: LoginDataDto, IAuthManager, IResourceLocalizer, CancellationToken

        var model = context.Arguments.OfType<T>().First(); // Исключение, если не найдет. Мой косяк, если в аргументах эндпоинта почему-то нет модели

        var localizer = context.HttpContext.RequestServices.GetRequiredService<IResourceLocalizer>();
        var ct = context.HttpContext.RequestAborted;

        // Валидация модели
        var validationResult = await _validator.ValidateAsync(model, ct);
        if (!validationResult.IsValid)
            return TypedResults.Extensions.ValidationProblem(validationResult, localizer);

        return await next(context);
    }
}