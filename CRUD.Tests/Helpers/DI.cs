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
        bool isPhoneNumberConfirm = false)
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

        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

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
       DateTime? editedAt = null)
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

        await db.Publications.AddAsync(publication);
        await db.SaveChangesAsync();

        return publication;
    }

    /// <summary>
    /// Создаёт продукт.
    /// </summary>
    public static async Task<Product> CreateProductAsync(
        ApplicationDbContext db,
        string name = Products.Premium,
        decimal price = 750)
    {
        var product = new Product
        {
            Name = name,
            Price = price
        };

        await db.Products.AddAsync(product);
        await db.SaveChangesAsync();

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
        bool refundable = false)
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

        await db.Orders.AddAsync(order);
        await db.SaveChangesAsync();

        return order;
    }

    /// <summary>
    /// Создаёт запрос на изменение пароля.
    /// </summary>
    /// <remarks>
    /// Пароль хэшируется через <see cref="PasswordHasher"/>.
    /// </remarks>
    public static async Task<ChangePasswordRequest> CreateChangePasswordRequestAsync(
        ApplicationDbContext db,
        Guid userId,
        DateTime? createdAt = null,
        DateTime? expires = null,
        string hashedNewPassword = "12345",
        string token = "token")
    {
        // В базе лежит захешированный пароль
        hashedNewPassword = new PasswordHasher().GenerateHashedPassword(hashedNewPassword);

        var createdAtNow = DateTime.UtcNow;

        var changePasswordRequest = new ChangePasswordRequest
        {
            UserId = userId,
            HashedNewPassword = hashedNewPassword,
            Token = token,
            CreatedAt = createdAt ?? createdAtNow,
            Expires = expires ?? createdAtNow.Add(TestSettingsHelper.GetConfigurationValue<TimeSpan, TestMarker>($"{ChangePasswordRequestOptions.SectionName}:{nameof(ChangePasswordRequestOptions.Expires)}"))
        };

        await db.ChangePasswordRequests.AddAsync(changePasswordRequest);
        await db.SaveChangesAsync();

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
        string token = "token")
    {
        var createdAtNow = DateTime.UtcNow;

        var confirmEmailRequest = new ConfirmEmailRequest
        {
            UserId = userId,
            Token = token,
            CreatedAt = createdAt ?? createdAtNow,
            Expires = expires ?? createdAtNow.Add(TestSettingsHelper.GetConfigurationValue<TimeSpan, TestMarker>($"{ConfirmEmailRequestOptions.SectionName}:{nameof(ConfirmEmailRequestOptions.Expires)}"))
        };

        await db.ConfirmEmailRequests.AddAsync(confirmEmailRequest);
        await db.SaveChangesAsync();

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
        string code = "123456") // VerificationPhoneNumberRequestOptions.LengthCode
    {
        var createdAtNow = DateTime.UtcNow;

        var verificationPhoneNumberRequest = new VerificationPhoneNumberRequest
        {
            UserId = userId,
            Code = code,
            CreatedAt = createdAt ?? createdAtNow,
            Expires = expires ?? createdAtNow.Add(TestSettingsHelper.GetConfigurationValue<TimeSpan, TestMarker>($"{VerificationPhoneNumberRequestOptions.SectionName}:{nameof(VerificationPhoneNumberRequestOptions.Expires)}"))
        };

        await db.VerificationPhoneNumberRequests.AddAsync(verificationPhoneNumberRequest);
        await db.SaveChangesAsync();

        return verificationPhoneNumberRequest;
    }

    /// <summary>
    /// Создаёт уведомление.
    /// </summary>
    public static async Task<Notification> CreateNotificationAsync(
       ApplicationDbContext db,
       DateTime? date = null,
       string title = "title",
       string content = "content")
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            Title = title,
            Content = content,
            CreatedAt = date ?? DateTime.UtcNow
        };

        await db.Notifications.AddAsync(notification);
        await db.SaveChangesAsync();

        return notification;
    }

    /// <summary>
    /// Создаёт уведомление пользователя.
    /// </summary>
    public static async Task<UserNotification> CreateUserNotificationAsync(
        ApplicationDbContext db,
        Guid userId,
        Guid notificationId,
        bool isRead = false)
    {
        var userNotification = new UserNotification
        {
            UserId = userId,
            NotificationId = notificationId,
            IsRead = isRead,
        };

        await db.UserNotifications.AddAsync(userNotification);
        await db.SaveChangesAsync();

        return userNotification;
    }

    /// <summary>
    /// Создаёт Refresh-токен пользователя.
    /// </summary>
    public static async Task<AuthRefreshToken> CreateAuthRefreshTokenAsync(
        ApplicationDbContext db,
        Guid userId,
        string token = "sadawdwddw1231",
        DateTime? expires = null)
    {
        var authRefreshToken = new AuthRefreshToken
        {
            Token = token,
            UserId = userId,
            Expires = expires ?? DateTime.UtcNow.Add(TestSettingsHelper.GetConfigurationValue<TimeSpan, TestMarker>($"{AuthWebApiOptions.SectionName}:{nameof(AuthWebApiOptions.ExpiresRefreshToken)}"))
        };

        await db.AuthRefreshTokens.AddAsync(authRefreshToken);
        await db.SaveChangesAsync();

        return authRefreshToken;
    }
}