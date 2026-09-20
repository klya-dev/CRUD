using Microsoft.AspNetCore.DataProtection;

namespace CRUD.WebApi.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> расширения DataProtection.
/// </summary>
public static class DataProtectionExtensions
{
    /// <summary>
    /// Добавляет и настраивает DataProtection.
    /// </summary>
    public static IServiceCollection AddCustomDataProtection(this IServiceCollection services)
    {
        services.AddDataProtection()
            .PersistKeysToDbContext<ApplicationDbContext>();
        // Чтобы ключи шифрования Data Protection не исчезали, можно сохранять их в хранилище, например - в Redis'е, в папке, в базе
        // И тогда, после перезагрузки приложения уже зашифрованная полезная нагрузка не пропадёт

        return services;
    }
}