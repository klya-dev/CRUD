using CRUD.DataAccess.Converters;
using CRUD.Models.Domains;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace CRUD.DataAccess.Data;

/// <summary>
/// Контекст базы данных приложения.
/// </summary>
public class ApplicationDbContext : DbContext
{
    private readonly ILogger<ApplicationDbContext> _logger;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ILogger<ApplicationDbContext> logger) : base(options)
    {
        _logger = logger;

        //this.ChangeTracker.LazyLoadingEnabled = false;
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Publication> Publications { get; set; }
    public DbSet<Request> Requests { get; set; }
    public DbSet<ChangePasswordRequest> ChangePasswordRequests { get; set; }
    public DbSet<ConfirmEmailRequest> ConfirmEmailRequests { get; set; }
    public DbSet<VerificationPhoneNumberRequest> VerificationPhoneNumberRequests { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<OrderNumberSequence> OrderNumberSequences { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<UserNotification> UserNotifications { get; set; }
    public DbSet<AuthRefreshToken> AuthRefreshTokens { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Мне достаточно логировать команды (sql-запросы) и миграции | https://learn.microsoft.com/ru-ru/ef/core/logging-events-diagnostics/simple-logging
        optionsBuilder.LogTo(action =>
        {
            _logger.LogInformation("{action}", action);
        },
        [
            DbLoggerCategory.Database.Command.Name,
            DbLoggerCategory.Migrations.Name
        ], LogLevel.Information, DbContextLoggerOptions.SingleLine);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        SetupUsers(modelBuilder);

        SetupPublications(modelBuilder);

        modelBuilder.Entity<Request>().UseTptMappingStrategy(); // Устанавливаем подход TPT (Наследование, Table Per Type)
        SetupRequests(modelBuilder);

        SetupChangePasswordRequests(modelBuilder);

        SetupConfirmEmailRequests(modelBuilder);

        SetupVerificationPhoneNumberRequests(modelBuilder);

        SetupOrders(modelBuilder);

        SetupProducts(modelBuilder);

        SetupOrderNumberSequences(modelBuilder);

        SetupNotifications(modelBuilder);

        SetupUserNotifications(modelBuilder);

        SetupAuthRefreshTokens(modelBuilder);

        ApplyUtcDateTimeConverter(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private static void SetupUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasKey(x => x.Id); // Первичный ключ
        modelBuilder.Entity<User>().Property(x => x.Id)
            .ValueGeneratedNever(); // Без автоинкремента

        // tinytext, text, mediumtext, longtext - https://www.mysqltutorial.org/mysql-basics/mysql-text/
        // Если не ограничивать длину, будет использоваться самый большой тип (longtext, mysql), что приводит к снижению производительности базы данных
        // Также, ограничение выкинет исключение, если превышу лимит, что тоже хорошо

        // Если есть проблемы с миграцией, а именно с выполнением запросов
        // Чтобы понять ошибку, нужно самому отправить запрос и MySQl вернёт нормальную, информативную ошибку. А не, как в логах просто "FAIL"

        modelBuilder.Entity<User>().Property(x => x.Firstname)
            .IsRequired()
            .HasMaxLength(32); // Максимальная длина 32 символа

        modelBuilder.Entity<User>().HasIndex(x => x.Username)
            .IsUnique(); // Уникальный ключ, не первичный
        modelBuilder.Entity<User>().Property(x => x.Username)
            .IsRequired()
            .HasMaxLength(32);

        modelBuilder.Entity<User>().Property(x => x.HashedPassword)
            .IsRequired()
            .HasMaxLength(69); // В данный момент длина любого захэшированного пароля 69 символов

        modelBuilder.Entity<User>().Property(x => x.LanguageCode)
            .IsRequired()
            .HasMaxLength(2);

        modelBuilder.Entity<User>().Property(x => x.Role)
            .IsRequired()
            .HasMaxLength(20);

        modelBuilder.Entity<User>().Property(x => x.IsPremium)
            .IsRequired();

        modelBuilder.Entity<User>().HasIndex(x => x.ApiKey)
            .IsUnique();
        modelBuilder.Entity<User>().Property(x => x.ApiKey)
            .IsRequired(false)
            .HasMaxLength(100);

        modelBuilder.Entity<User>().HasIndex(x => x.DisposableApiKey)
            .IsUnique();
        modelBuilder.Entity<User>().Property(x => x.DisposableApiKey)
            .IsRequired(false)
            .HasMaxLength(100);

        modelBuilder.Entity<User>().Property(x => x.RowVersion)
            .IsRequired(false)
            .IsRowVersion();

        modelBuilder.Entity<User>().Property(x => x.AvatarURL)
            .IsRequired()
            .HasMaxLength(64);

        modelBuilder.Entity<User>().HasIndex(x => x.Email)
            .IsUnique();
        modelBuilder.Entity<User>().Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(254); // https://stackoverflow.com/questions/386294/what-is-the-maximum-length-of-a-valid-email-address#:~:text=%F0%9F%91%89%20An%20email%20address%20must%20not%20exceed%20254%20characters.

        modelBuilder.Entity<User>().HasIndex(x => x.PhoneNumber)
            .IsUnique();
        modelBuilder.Entity<User>().Property(x => x.PhoneNumber)
           .IsRequired()
           .HasMaxLength(15); // https://en.wikipedia.org/wiki/Telephone_numbering_plan#:~:text=The%20International%20Telecommunication%20Union%20(ITU,15%20digits%20to%20telephone%20numbers.
    }

    private static void SetupPublications(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Publication>()
            .HasKey(x => x.Id); // Первичный ключ
        modelBuilder.Entity<Publication>().Property(x => x.Id)
            .ValueGeneratedNever(); // Без автоинкремента

        modelBuilder.Entity<Publication>().Property(x => x.CreatedAt)
            .IsRequired();

        modelBuilder.Entity<Publication>().Property(x => x.EditedAt)
            .IsRequired(false);

        modelBuilder.Entity<Publication>().Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(64);

        modelBuilder.Entity<Publication>().Property(x => x.Content)
            .IsRequired()
            .HasMaxLength(1024);

        modelBuilder.Entity<Publication>().Property(x => x.AuthorId)
            .IsRequired(false); // Может быть null

        // Была ошибка, связанная с тем, что AuthorId публикаций ссылались на несуществующих пользователей (необходимо, чтобы все ключи ссылались на существующие данные)
        // Ещё был момент, что почему-то Cascade Mode не применялся, тогда я поменял на NoAction и потом опять поставил SetNull (проверить какой мод уже в базе "SHOW CREATE TABLE `Publications`")
        // один-ко-многим
        modelBuilder.Entity<Publication>()
            .HasOne(x => x.User) // Каждый экземпляр Publication связан с одним User
            .WithMany(x => x.Publications) // Указывает, что User может иметь несколько публикаций
            .HasForeignKey(x => x.AuthorId) // Указывает, какой ключ используется для связи
            .HasPrincipalKey(x => x.Id) // AuthorId это и есть Id пользователя
            .OnDelete(DeleteBehavior.SetNull); // При удалении пользователя все связанные AuthorId публикаций будут null

        modelBuilder.Entity<Publication>().Property(x => x.RowVersion)
            .IsRequired(false)
            .IsRowVersion();
    }

    private static void SetupRequests(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Request>()
            .HasKey(x => x.Id); // Первичный ключ
        modelBuilder.Entity<Request>().Property(x => x.Id)
            .ValueGeneratedNever(); // Без автоинкремента

        modelBuilder.Entity<Request>().Property(x => x.CreatedAt)
            .IsRequired();

        modelBuilder.Entity<Request>().Property(x => x.Expires)
            .IsRequired();

        modelBuilder.Entity<Request>().Property(x => x.RowVersion)
            .IsRequired(false)
            .IsRowVersion();
    }

    private static void SetupChangePasswordRequests(ModelBuilder modelBuilder)
    {
        // один-к-одному
        modelBuilder.Entity<ChangePasswordRequest>()
            .HasOne(x => x.User) // Каждый экземпляр ChangePasswordRequest связан с одним User
            .WithOne() // Указывает, что User может иметь только один запрос
            .HasForeignKey<ChangePasswordRequest>(x => x.UserId) // Указывает, какой ключ используется для связи
            .HasPrincipalKey<User>(x => x.Id) // UserId это и есть Id пользователя
            .OnDelete(DeleteBehavior.Cascade); // При удалении пользователя все связанные запросы будут удалены

        modelBuilder.Entity<ChangePasswordRequest>().Property(x => x.HashedNewPassword)
            .HasMaxLength(69) // В данный момент длина любого захэшированного пароля 69 символов
            .IsRequired();

        modelBuilder.Entity<ChangePasswordRequest>().Property(x => x.Token)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<ChangePasswordRequest>().HasIndex(x => x.Token)
            .IsUnique();
    }

    private static void SetupConfirmEmailRequests(ModelBuilder modelBuilder)
    {
        // один-к-одному
        modelBuilder.Entity<ConfirmEmailRequest>()
            .HasOne(x => x.User) // Каждый экземпляр ConfirmEmailRequest связан с одним User
            .WithOne() // Указывает, что User может иметь только один запрос
            .HasForeignKey<ConfirmEmailRequest>(x => x.UserId) // Указывает, какой ключ используется для связи
            .HasPrincipalKey<User>(x => x.Id) // UserId это и есть Id пользователя
            .OnDelete(DeleteBehavior.Cascade); // При удалении пользователя все связанные запросы будут удалены

        modelBuilder.Entity<ConfirmEmailRequest>().Property(x => x.Token)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<ConfirmEmailRequest>().HasIndex(x => x.Token)
            .IsUnique();
    }

    private static void SetupVerificationPhoneNumberRequests(ModelBuilder modelBuilder)
    {
        // один-к-одному
        modelBuilder.Entity<VerificationPhoneNumberRequest>()
            .HasOne(x => x.User) // Каждый экземпляр VerificationPhoneNumberRequest связан с одним User
            .WithOne() // Указывает, что User может иметь только один запрос
            .HasForeignKey<VerificationPhoneNumberRequest>(x => x.UserId) // Указывает, какой ключ используется для связи
            .HasPrincipalKey<User>(x => x.Id) // UserId это и есть Id пользователя
            .OnDelete(DeleteBehavior.Cascade); // При удалении пользователя все связанные запросы будут удалены

        modelBuilder.Entity<VerificationPhoneNumberRequest>().Property(x => x.Code)
            .HasMaxLength(10)
            .IsRequired();
    }

    private static void SetupOrders(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>()
            .HasKey(x => x.Id); // Первичный ключ
        modelBuilder.Entity<Order>().Property(x => x.Id)
            .ValueGeneratedNever(); // Без автоинкремента

        modelBuilder.Entity<Order>().Property(x => x.UserId)
            .IsRequired(false);

        // один-ко-многим
        modelBuilder.Entity<Order>()
            .HasOne(x => x.User) // Каждый экземпляр Order связан с одним User
            .WithMany(x => x.Orders) // Указывает, что User может иметь несколько заказов
            .HasForeignKey(x => x.UserId) // Указывает, какой ключ используется для связи
            .HasPrincipalKey(x => x.Id) // UserId это и есть Id пользователя
            .OnDelete(DeleteBehavior.SetNull); // При удалении пользователя все связанные UserId заказов будут null

        modelBuilder.Entity<Order>().Property(x => x.Status)
            .HasMaxLength(25)
            .IsRequired();

        modelBuilder.Entity<Order>().Property(x => x.PaymentStatus)
            .HasMaxLength(25)
            .IsRequired();

        modelBuilder.Entity<Order>().Property(x => x.ProductName)
            .HasMaxLength(50)
            .IsRequired();

        // один-ко-многим
        modelBuilder.Entity<Order>()
            .HasOne(x => x.Product) // Каждый экземпляр Order связан с одним Product
            .WithMany(x => x.Orders) // Указывает, что Product может иметь несколько заказов
            .HasForeignKey(x => x.ProductName) // Указывает, какой ключ используется для связи
            .HasPrincipalKey(x => x.Name) // ProductName это и есть Name продукта
            .OnDelete(DeleteBehavior.NoAction); // При удалении продукта всем связанным продуктам заказов ничего не будет

        // Не сопоставлять колонку Product
        //modelBuilder.Entity<Order>()
        //    .Ignore(x => x.Product);
        // Сработал только [NotMapped]

        modelBuilder.Entity<Order>().Property(x => x.Paid)
            .IsRequired();

        modelBuilder.Entity<Order>().Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        modelBuilder.Entity<Order>().Property(x => x.Currency)
            .IsRequired();

        modelBuilder.Entity<Order>().Property(x => x.CreatedAt)
            .IsRequired();

        modelBuilder.Entity<Order>().Property(x => x.Description)
            .HasMaxLength(25)
            .IsRequired();

        modelBuilder.Entity<Order>().Property(x => x.Refundable)
            .IsRequired();

        modelBuilder.Entity<Order>().Property(x => x.RowVersion)
            .IsRequired(false)
            .IsRowVersion();
    }

    private static void SetupProducts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>()
            .HasKey(x => x.Name); // Первичный ключ

        modelBuilder.Entity<Product>().Property(x => x.Name)
            .HasMaxLength(50);

        modelBuilder.Entity<Product>().Property(x => x.Price)
            .HasPrecision(18, 2) // До 18 цифр и 2х после запятой
            .IsRequired();

        modelBuilder.Entity<Product>().Property(x => x.RowVersion)
            .IsRequired(false)
            .IsRowVersion();
    }

    private static void SetupOrderNumberSequences(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderNumberSequence>()
            .HasKey(x => x.Number); // Первичный ключ
        modelBuilder.Entity<OrderNumberSequence>().Property(x => x.Number)
            .ValueGeneratedOnAdd(); // С автоинкрементом
    }

    private static void SetupNotifications(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>()
            .HasKey(x => x.Id); // Первичный ключ
        modelBuilder.Entity<Notification>().Property(x => x.Id)
            .ValueGeneratedNever(); // Без автоинкремента

        modelBuilder.Entity<Notification>().Property(x => x.CreatedAt)
            .IsRequired();

        modelBuilder.Entity<Notification>().Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(48);

        modelBuilder.Entity<Notification>().Property(x => x.Content)
            .IsRequired()
            .HasMaxLength(96);

        modelBuilder.Entity<Notification>().Property(x => x.RowVersion)
            .IsRequired(false)
            .IsRowVersion();
    }

    private static void SetupUserNotifications(ModelBuilder modelBuilder)
    {
        // + если удалить пользователя или уведомление, UserNotification, связывающий их, тоже удалится

        // https://learn.microsoft.com/en-us/ef/core/modeling/relationships/many-to-many#many-to-many-with-class-for-join-entity

        // многие-ко-многим
        modelBuilder.Entity<User>()
            .HasMany(x => x.Notifications) // Пользователь может иметь несколько уведомлений
            .WithMany(x => x.Users) // Уведомление может иметь несколько пользователей
            .UsingEntity<UserNotification>(); // Для связи используем таблицу UserNotification

        // Прописывать для Notification необязательно, различий нет
        //modelBuilder.Entity<Notification>()
        //    .HasMany(x => x.Users)
        //    .WithMany(x => x.Notifications)
        //    .UsingEntity<UserNotification>();

        modelBuilder.Entity<UserNotification>().Property(x => x.IsRead)
            .IsRequired();
    }

    private static void SetupAuthRefreshTokens(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuthRefreshToken>()
            .HasKey(x => x.Id); // Первичный ключ
        modelBuilder.Entity<AuthRefreshToken>().Property(x => x.Id)
            .ValueGeneratedNever(); // Без автоинкремента

        modelBuilder.Entity<AuthRefreshToken>().Property(x => x.Token)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<AuthRefreshToken>().HasIndex(x => x.Token)
            .IsUnique();

        // один-ко-многим
        modelBuilder.Entity<AuthRefreshToken>()
            .HasOne(x => x.User) // Каждый экземпляр AuthRefreshToken связан с одним User
            .WithMany(x => x.AuthRefreshTokens) // Указывает, что User может иметь несколько токенов
            .HasForeignKey(x => x.UserId) // Указывает, какой ключ используется для связи
            .HasPrincipalKey(x => x.Id) // UserId это и есть Id пользователя
            .OnDelete(DeleteBehavior.Cascade); // При удалении пользователя все связанные токены будут удалены

        modelBuilder.Entity<AuthRefreshToken>().Property(x => x.Expires)
            .IsRequired();
    }

    /// <summary>
    /// Добавляет каждому типу <see cref="DateTime"/> конвертер в <c>UTC</c> и количество знаков после запятой.
    /// </summary>
    /// <remarks>
    /// <para>Чтобы в базе всегда было <c>UTC</c>, и из базы тоже.</para>
    /// <para><see href="https://github.com/dotnet/efcore/issues/4711"/>, <see href="https://www.roundthecode.com/dotnet-tutorials/use-ef-core-easily-save-dates-utc-show-local-time"/>.</para>
    /// </remarks>
    private static void ApplyUtcDateTimeConverter(ModelBuilder modelBuilder)
    {
        var dateTimeUtcConverter = new DateTimeUtcConverter();
        var dateTimeUtcNullableConverter = new DateTimeUtcNullableConverter();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetPrecision(6); // По дефолту 6 знаков после запятой, но я явно укажу
                    property.SetValueConverter(dateTimeUtcConverter);
                }
                if (property.ClrType == typeof(DateTime?))
                {
                    property.SetPrecision(6);
                    property.SetValueConverter(dateTimeUtcNullableConverter);
                }
            }
        }
    }
}