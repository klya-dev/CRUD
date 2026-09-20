namespace CRUD.WebApi.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> расширения базы данных.
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Добавляет и настраивает базу данных.
    /// </summary>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            string connectionString = configuration.GetConnectionString("DefaultConnection")!;
            var serverVersion = new MySqlServerVersion(new Version(8, 0, 25)); // Версия на главной странице phpMyAdmin
            options.UseMySql(connectionString, serverVersion, mySqlOptions =>
                    mySqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3, // Бывает не с первого раза подключается к базе +реконект полезен и в других случаях
                        maxRetryDelay: TimeSpan.FromSeconds(15),
                        errorNumbersToAdd: null)).EnableDetailedErrors();
        });

        return services;
    }
}