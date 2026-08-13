using Testcontainers.MySql;

namespace CRUD.Tests.Fixtures;

/// <summary>
/// База данных на основе Docker-контейнера.
/// </summary>
/// <remarks>
/// <para>Docker-контейнер создаётся один раз на весь класс теста / сессии, если нужно пересоздавать базу данных используйте <see cref="DbContextGenerator.GenerateDbContextTestContainer(DbContextOptions{ApplicationDbContext}, bool)"/>.</para>
/// <para>Если нужно проинициализировать один Docker-контейнер на всю тестовую сессию — добавь атрибут <c>[Collection(nameof(GlobalDbContainerCollection))]</c> ко всем тестовым классам учавствующих в сессии и получи <c>fixture</c> в конструкторе.</para>
/// <para>Если нужно проинициализировать один Docker-контейнер на один тестовый класс — наследуйся от <c>IClassFixture&lt;DbContainerFixture&gt;</c> в тестовом классе и получи <c>fixture</c> в конструкторе (не рекомендуется из-за частой и долгой инициализации контейнера).</para>
/// <para>База данных: MySQL (8.0.25).</para>
/// </remarks>
public sealed class DbContainerFixture : IAsyncLifetime
{
    // Настраиваем контейнер (пароль и имя БД создадутся автоматически)
    private readonly MySqlContainer _dbContainer = new MySqlBuilder("mysql:8.0.25").Build();
    private static readonly MySqlServerVersion ServerVersion = new(new Version(8, 0, 25));

    // Свойство для получения опций DbContext в DbContextGenerator.GenerateDbContextContainerTest
    public DbContextOptions<ApplicationDbContext> DbOptions { get; private set; } = null;

    // Запускается один раз перед стартом всех тестов класса / сессии
    public async ValueTask InitializeAsync()
    {
        // Запускаем Docker-контейнер
        await _dbContainer.StartAsync();

        // Получаем строку подключения
        var connectionString = _dbContainer.GetConnectionString();

        // Настройки базы для EF
        DbOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseMySql(connectionString, ServerVersion, mySqlOptions =>
            mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(15),
                errorNumbersToAdd: null)).EnableDetailedErrors()
            .Options;
    }

    // Запускается один раз после завершения всех тестов класса / сессии
    public async ValueTask DisposeAsync()
    {
        // Останавливаем и полностью удаляем контейнер
        await _dbContainer.StopAsync();
    }
}