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

        // DataProtection не хранит сами зашифрованные данные (только ключи для шифровки и расшифровки)
        // Например, при запросе на смену пароля, мы создаём полезную нагрузку, шифруем её и отдаём клиенту
        // Клиент присылает этот шифр (полезная нагрузка - токен) обратно, протектор расшифровывает токен и сервис меняет пароль по полезной нагрузке из токена
        // Сам токен не в кэше, не в базе не хранится, а ключи хранятся - в базе.

        return services;
    }
}