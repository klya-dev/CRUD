namespace CRUD.WebApi.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> расширения RequestTimeouts.
/// </summary>
public static class RequestTimeoutExtensions
{
    /// <summary>
    /// Добавляет и настраивает RequestTimeouts.
    /// </summary>
    public static IServiceCollection AddCustomRequestTimeouts(this IServiceCollection services)
    {
        services.AddRequestTimeouts(options =>
        {
            options.DefaultPolicy = new RequestTimeoutPolicy
            {
                Timeout = TimeSpan.FromSeconds(25) // На каждый запрос (мой ответ) отводится небольше 25 секунд, иначе 504 ошибка | RequestTimeoutsSystemTest
            };
        });

        return services;
    }
}