#nullable enable
namespace CRUD.Tests.Helpers;

/// <summary>
/// Статический класс для создания тестовых данных для тестов.
/// </summary>
public static class DI
{
    /// <summary>
    /// Создаёт пользователя.
    /// </summary>
    /// <remarks>
    /// <para>Пароль хэшируется через <see cref="PasswordHasher"/>.</para>
    /// <para>Если <paramref name="avatarUrl"/> <see langword="null"/>, то параметру устанавливается значение из <see cref="AvatarManagerOptions.DefaultAvatarPath"/>.</para>
    /// </remarks>
    public static async Task<User> CreateUserAsync(
        ApplicationDbContext db,
        string firstname = "Тест",
        string username = "username",
        string hashedPassword = "123",
        string languageCode = "ru",
        string role = UserRoles.User,
        bool isPremium = false,
        string? apiKey = null,
        string? disposableApiKey = null,
        string? avatarUrl = null,
        string email = "fan.ass95@mail.ru",
        bool isEmailConfirm = false,
        string phoneNumber = "12345",
        bool isPhoneNumberConfirm = false,
        CancellationToken ct = default)
    {
        // В базе лежит захешированный пароль
        hashedPassword = new PasswordHasher().GenerateHashedPassword(hashedPassword);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Firstname = firstname,
            Username = username,
            HashedPassword = hashedPassword,
            LanguageCode = languageCode,
            Role = role,
            IsPremium = isPremium,
            ApiKey = apiKey,
            DisposableApiKey = disposableApiKey,
            AvatarURL = avatarUrl ?? TestSettingsHelper.GetConfigurationValue<AvatarManagerOptions, TestMarker>(AvatarManagerOptions.SectionName)!.DefaultAvatarPath,
            Email = email,
            IsEmailConfirm = isEmailConfirm,
            PhoneNumber = phoneNumber,
            IsPhoneNumberConfirm = isPhoneNumberConfirm
        };

        await db.Users.AddAsync(user, ct);
        await db.SaveChangesAsync(ct);

        return user;
    }

    /// <summary>
    /// Создаёт публикацию.
    /// </summary>
    public static async Task<Publication> CreatePublicationAsync(
       ApplicationDbContext db,
       Guid? authorId,
       DateTime? createdAt = null,
       string title = "title",
       string content = TestConstants.PublicationContent,
       DateTime? editedAt = null,
        CancellationToken ct = default)
    {
        var publication = new Publication
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            Title = title,
            Content = content,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            EditedAt = editedAt,
        };

        await db.Publications.AddAsync(publication, ct);
        await db.SaveChangesAsync(ct);

        return publication;
    }

    /// <summary>
    /// Создаёт продукт.
    /// </summary>
    public static async Task<Product> CreateProductAsync(
        ApplicationDbContext db,
        string name = Products.Premium,
        decimal price = 750,
        CancellationToken ct = default)
    {
        var product = new Product
        {
            Name = name,
            Price = price
        };

        await db.Products.AddAsync(product, ct);
        await db.SaveChangesAsync(ct);

        return product;
    }

    /// <summary>
    /// Создаёт заказ.
    /// </summary>
    public static async Task<Order> CreateOrderAsync(
        ApplicationDbContext db,
        Guid? userId,
        DateTime? createdAt = null,
        string status = OrderStatuses.Accept,
        string paymentStatus = PaymentStatuses.Succeeded,
        string productName = Products.Premium,
        bool paid = true,
        decimal amount = 100,
        string currency = "RUB",
        string description = "Description",
        bool refundable = false,
        CancellationToken ct = default)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = status,
            PaymentStatus = paymentStatus,
            ProductName = productName,
            Paid = paid,
            Amount = amount,
            Currency = currency,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            Description = description,
            Refundable = refundable,
        };

        await db.Orders.AddAsync(order, ct);
        await db.SaveChangesAsync(ct);

        return order;
    }

    /// <summary>
    /// Создаёт полезную нагрузку токена на смену пароля.
    /// </summary>
    /// <remarks>
    /// Пароль хэшируется через <see cref="PasswordHasher"/>.
    /// </remarks>
    public static ChangePasswordPayload CreateChangePasswordPayload(
        Guid userId,
        string hashedNewPassword = "12345")
    {
        // В базе лежит захешированный пароль
        hashedNewPassword = new PasswordHasher().GenerateHashedPassword(hashedNewPassword);

        var changePasswordRequest = new ChangePasswordPayload
        {
            UserId = userId,
            HashedNewPassword = hashedNewPassword
        };

        return changePasswordRequest;
    }

    /// <summary>
    /// Создаёт запрос на подтверждение электронной почты.
    /// </summary>
    public static async Task<ConfirmEmailRequest> CreateConfirmEmailRequestAsync(
        ApplicationDbContext db,
        Guid userId,
        DateTime? createdAt = null,
        DateTime? expires = null,
        string token = "token",
        CancellationToken ct = default)
    {
        var createdAtNow = DateTime.UtcNow;

        var confirmEmailRequest = new ConfirmEmailRequest
        {
            UserId = userId,
            Token = token,
            CreatedAt = createdAt ?? createdAtNow,
            Expires = expires ?? createdAtNow.Add(TestSettingsHelper.GetConfigurationValue<TimeSpan, TestMarker>($"{ConfirmEmailRequestOptions.SectionName}:{nameof(ConfirmEmailRequestOptions.Expires)}"))
        };

        await db.ConfirmEmailRequests.AddAsync(confirmEmailRequest, ct);
        await db.SaveChangesAsync(ct);

        return confirmEmailRequest;
    }

    /// <summary>
    /// Создаёт запрос на подтверждение телефонного номера.
    /// </summary>
    public static async Task<VerificationPhoneNumberRequest> CreateVerificationPhoneNumberRequestAsync(
        ApplicationDbContext db,
        Guid userId,
        DateTime? createdAt = null,
        DateTime? expires = null,
        string code = "123456", // VerificationPhoneNumberRequestOptions.LengthCode,
        CancellationToken ct = default)
    {
        var createdAtNow = DateTime.UtcNow;

        var verificationPhoneNumberRequest = new VerificationPhoneNumberRequest
        {
            UserId = userId,
            Code = code,
            CreatedAt = createdAt ?? createdAtNow,
            Expires = expires ?? createdAtNow.Add(TestSettingsHelper.GetConfigurationValue<TimeSpan, TestMarker>($"{VerificationPhoneNumberRequestOptions.SectionName}:{nameof(VerificationPhoneNumberRequestOptions.Expires)}"))
        };

        await db.VerificationPhoneNumberRequests.AddAsync(verificationPhoneNumberRequest, ct);
        await db.SaveChangesAsync(ct);

        return verificationPhoneNumberRequest;
    }

    /// <summary>
    /// Создаёт уведомление.
    /// </summary>
    public static async Task<Notification> CreateNotificationAsync(
       ApplicationDbContext db,
       DateTime? date = null,
       string title = "title",
       string content = "content",
        CancellationToken ct = default)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            Title = title,
            Content = content,
            CreatedAt = date ?? DateTime.UtcNow
        };

        await db.Notifications.AddAsync(notification, ct);
        await db.SaveChangesAsync(ct);

        return notification;
    }

    /// <summary>
    /// Создаёт уведомление пользователя.
    /// </summary>
    public static async Task<UserNotification> CreateUserNotificationAsync(
        ApplicationDbContext db,
        Guid userId,
        Guid notificationId,
        bool isRead = false,
        CancellationToken ct = default)
    {
        var userNotification = new UserNotification
        {
            UserId = userId,
            NotificationId = notificationId,
            IsRead = isRead,
        };

        await db.UserNotifications.AddAsync(userNotification, ct);
        await db.SaveChangesAsync(ct);

        return userNotification;
    }

    /// <summary>
    /// Создаёт Refresh-токен пользователя.
    /// </summary>
    public static async Task<AuthRefreshToken> CreateAuthRefreshTokenAsync(
        ApplicationDbContext db,
        Guid userId,
        string token = "sadawdwddw1231",
        DateTime? expires = null,
        CancellationToken ct = default)
    {
        var authRefreshToken = new AuthRefreshToken
        {
            Token = token,
            UserId = userId,
            Expires = expires ?? DateTime.UtcNow.Add(TestSettingsHelper.GetConfigurationValue<TimeSpan, TestMarker>($"{AuthWebApiOptions.SectionName}:{nameof(AuthWebApiOptions.ExpiresRefreshToken)}"))
        };

        await db.AuthRefreshTokens.AddAsync(authRefreshToken, ct);
        await db.SaveChangesAsync(ct);

        return authRefreshToken;
    }
}