using System.Text.RegularExpressions;

namespace CRUD.Models;

/// <summary>
/// Статический класс расширений для маппинга сущностей в их DTO-модели.
/// </summary>
public static partial class MapperExtensions
{
    /// <summary>
    /// Возвращает DTO-модель пользователя созданную из <paramref name="user"/>.
    /// </summary>
    /// <param name="user">Пользователь.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="user"/> <see langword="null"/>.</exception>
    /// <returns>DTO-модель пользователя.</returns>
    public static UserDto ToUserDto(this User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserDto
        { 
            Firstname = user.Firstname,
            Username = user.Username,
            LanguageCode = user.LanguageCode
        };
    }

    /// <summary>
    /// Возвращает DTO-модель полных данных пользователя созданную из <paramref name="user"/>.
    /// </summary>
    /// <param name="user">Пользователь.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="user"/> <see langword="null"/>.</exception>
    /// <returns>DTO-модель полных данных о пользователе.</returns>
    public static UserFullDto ToUserFullDto(this User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserFullDto
        {
            Id = user.Id,
            Firstname = user.Firstname,
            Username = user.Username,
            LanguageCode = user.LanguageCode,
            Role = user.Role,
            IsPremium = user.IsPremium,
            ApiKey = user.ApiKey,
            DisposableApiKey = user.DisposableApiKey,
            AvatarURL = user.AvatarURL,
            Email = user.Email,
            IsEmailConfirm = user.IsEmailConfirm,
            PhoneNumber = user.PhoneNumber,
            IsPhoneNumberConfirm = user.IsPhoneNumberConfirm
        };
    }

    /// <summary>
    /// Возвращает коллекцию DTO-моделей пользователей созданных из <paramref name="users"/>.
    /// </summary>
    /// <param name="users">Пользователи.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="user"/> <see langword="null"/>.</exception>
    /// <returns>Коллекция из <see cref="UserDto"/>.</returns>
    public static IEnumerable<UserDto> ToUsersDto(this IEnumerable<User> users)
    {
        ArgumentNullException.ThrowIfNull(users);

        var usersDto = users.Select(x => x.ToUserDto());
        return usersDto;
    }

    /// <summary>
    /// Возвращает коллекцию DTO-моделей полных данных пользователей созданных из <paramref name="users"/>.
    /// </summary>
    /// <param name="users">Пользователи.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="user"/> <see langword="null"/>.</exception>
    /// <returns>Коллекция из <see cref="UserFullDto"/>.</returns>
    public static IEnumerable<UserFullDto> ToUsersFullDto(this IEnumerable<User> users)
    {
        ArgumentNullException.ThrowIfNull(users);

        var usersDto = users.Select(x => x.ToUserFullDto());
        return usersDto;
    }

    /// <summary>
    /// Возвращает DTO-модель публикации созданную из <paramref name="publication"/> и удаляет из даты все тики <see cref="ToWithoutTicks(DateTime)"/>.
    /// </summary>
    /// <remarks>
    /// Если <paramref name="authorFirstname"/> равен <see langword="null"/>, то <see cref="PublicationDto.AuthorFirstname"/> будет <c>"Автор удалён"</c>.
    /// </remarks>
    /// <param name="publication">Публикация.</param>
    /// <param name="authorFirstname">Имя автора.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="publication"/> <see langword="null"/>.</exception>
    /// <returns>DTO-модель публикации.</returns>
    public static PublicationDto ToPublicationDto(this Publication publication, string? authorFirstname)
    {
        ArgumentNullException.ThrowIfNull(publication);

        return new PublicationDto
        {
            Id = publication.Id,
            CreatedAt = publication.CreatedAt.ToWithoutTicks(),
            EditedAt = publication.EditedAt?.ToWithoutTicks(),
            Title = publication.Title,
            Content = publication.Content,
            AuthorId = publication.AuthorId,
            AuthorFirstname = authorFirstname ?? "Автор удалён"
        };
    }

    /// <summary>
    /// Возвращает коллекцию DTO-моделей публикаций созданных из <paramref name="publications"/>.
    /// </summary>
    /// <remarks>
    /// Скорее подходит, когда публикации имеют одного и того же автора.
    /// </remarks>
    /// <param name="publications">Публикации от одного и того же автора.</param>
    /// <param name="authorFirstname">Имя автора.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="publications"/> <see langword="null"/>.</exception>
    /// <returns>Коллекция из <see cref="PublicationDto"/>.</returns>
    public static IEnumerable<PublicationDto> ToPublicationsDto(this IEnumerable<Publication> publications, string? authorFirstname)
    {
        ArgumentNullException.ThrowIfNull(publications);

        var publicationsDto = publications.Select(x => x.ToPublicationDto(authorFirstname));
        return publicationsDto;
    }

    /// <summary>
    /// Возвращает коллекцию DTO-моделей публикаций созданных из <paramref name="publications"/>.
    /// </summary>
    /// <remarks>
    /// <para>Скорее подходит, когда публикации имеют разных авторов.</para>
    /// <para>Т.к. имя автора достаётся из самой сущности, необходимо прогрузить свойство <see cref="Publication.User"/> <c>(x => x.User?.Firstname)</c>.</para>
    /// </remarks>
    /// <param name="publications">Публикации от разных авторов.</param>
    /// <param name="authorFirstnameFunc">Прогруженное имя автора <c>(x => x.User?.Firstname)</c>.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="publications"/> <see langword="null"/>.</exception>
    /// <returns>Коллекция из <see cref="PublicationDto"/>.</returns>
    public static IEnumerable<PublicationDto> ToPublicationsDto(this IEnumerable<Publication> publications, Func<Publication, string?> authorFirstnameFunc)
    {
        ArgumentNullException.ThrowIfNull(publications);

        var publicationsDto = publications.Select(x => x.ToPublicationDto(authorFirstnameFunc(x)));
        return publicationsDto;
    }

    /// <summary>
    /// Возвращает полную DTO-модель публикации созданную из <paramref name="publication"/>.
    /// </summary>
    /// <param name="publication">Публикация.</param>
    /// <param name="author">Сущность пользователя/автора.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="publication"/> <see langword="null"/>.</exception>
    /// <returns>Полная DTO-модель публикации.</returns>
    public static PublicationFullDto ToPublicationFullDto(this Publication publication, User? author)
    {
        ArgumentNullException.ThrowIfNull(publication);

        return new PublicationFullDto
        {
            Id = publication.Id,
            CreatedAt = publication.CreatedAt,
            EditedAt = publication.EditedAt,
            Title = publication.Title,
            Content = publication.Content,
            Author = author?.ToUserFullDto(),
        };
    }

    /// <summary>
    /// Возвращает коллекцию полных DTO-моделей публикаций созданных из <paramref name="publications"/>.
    /// </summary>
    /// <remarks>
    /// Скорее подходит, когда публикации имеют одного и того же автора.
    /// </remarks>
    /// <param name="publications">Публикации от одного и того же автора.</param>
    /// <param name="author">Сущность пользователя/автора.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="publications"/> <see langword="null"/>.</exception>
    /// <returns>Коллекция из <see cref="PublicationFullDto"/>.</returns>
    public static IEnumerable<PublicationFullDto> ToPublicationsFullDto(this IEnumerable<Publication> publications, User? author)
    {
        ArgumentNullException.ThrowIfNull(publications);

        var publicationsDto = publications.Select(x => x.ToPublicationFullDto(author));
        return publicationsDto;
    }

    /// <summary>
    /// Возвращает коллекцию полных DTO-моделей публикаций созданных из <paramref name="publications"/>.
    /// </summary>
    /// <remarks>
    /// <para>Скорее подходит, когда публикации имеют разных авторов.</para>
    /// <para>Т.к. имя автора достаётся из самой сущности, необходимо прогрузить свойство <see cref="Publication.User"/> <c>(x => x.User?.Firstname)</c>.</para>
    /// </remarks>
    /// <param name="publications">Публикации от разных авторов.</param>
    /// <param name="authorFunc">Прогруженная сущность пользователя/автора <c>(x => x.User)</c>.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="publications"/> <see langword="null"/>.</exception>
    /// <returns>Коллекция из <see cref="PublicationFullDto"/>.</returns>
    public static IEnumerable<PublicationFullDto> ToPublicationsFullDto(this IEnumerable<Publication> publications, Func<Publication, User?> authorFunc)
    {
        ArgumentNullException.ThrowIfNull(publications);

        var publicationsDto = publications.Select(x => x.ToPublicationFullDto(authorFunc(x)));
        return publicationsDto;
    }

    /// <summary>
    /// Возвращает DTO-модель автора созданную из <paramref name="user"/>.
    /// </summary>
    /// <param name="user">Пользователь.</param>
    /// <param name="publicationsCount">Количество публикаций автора.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="user"/> <see langword="null"/>.</exception>
    /// <returns>DTO-модель пользователя.</returns>
    public static AuthorDto ToAuthorDto(this User user, int publicationsCount)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new AuthorDto
        {
            Firstname = user.Firstname,
            Username = user.Username,
            LanguageCode = user.LanguageCode,
            PublicationsCount = publicationsCount
        };
    }

    /// <summary>
    /// Возвращает коллекцию DTO-моделей авторов созданных из <paramref name="users"/>.
    /// </summary>
    /// <remarks>
    /// <para>Количество публикаций достаётся из самой сущности, необходимо прогрузить свойство <see cref="User.Publications"/> <c>(x => x.Publications.Count)</c>.</para>
    /// </remarks>
    /// <param name="users">Пользователи.</param>
    /// <param name="publicationsCount">Прогруженное количество публикаций автора <c>(x => x.Publications.Count)</c>.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="users"/> <see langword="null"/>.</exception>
    /// <returns>Коллекция из <see cref="UserDto"/>.</returns>
    public static IEnumerable<AuthorDto> ToAuthorsDto(this IEnumerable<User> users, Func<User, int> publicationsCountFunc)
    {
        ArgumentNullException.ThrowIfNull(users);

        var authorsDto = users.Select(x => x.ToAuthorDto(publicationsCountFunc(x)));
        return authorsDto;
    }

    /// <summary>
    /// Возвращает DTO-модель уведомления созданную из <paramref name="notification"/>.
    /// </summary>
    /// <param name="notification">Уведомление.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="notification"/> <see langword="null"/>.</exception>
    /// <returns>DTO-модель уведомления.</returns>
    public static NotificationDto ToNotificationDto(this Notification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);

        return new NotificationDto
        {
            Id = notification.Id,
            Title = notification.Title,
            Content = notification.Content,
            CreatedAt = notification.CreatedAt.ToWithoutTicks()
        };
    }

    /// <summary>
    /// Возвращает коллекцию DTO-моделей уведомлений созданных из <paramref name="notifications"/>.
    /// </summary>
    /// <param name="notifications">Уведомления.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="notifications"/> <see langword="null"/>.</exception>
    /// <returns>Коллекция из <see cref="NotificationDto"/>.</returns>
    public static IEnumerable<NotificationDto> ToNotificationsDto(this IEnumerable<Notification> notifications)
    {
        ArgumentNullException.ThrowIfNull(notifications);

        var notificationsDto = notifications.Select(x => x.ToNotificationDto());
        return notificationsDto;
    }

    /// <summary>
    /// Возвращает DTO-модель уведомления пользователя созданную из <paramref name="userNotification"/>.
    /// </summary>
    /// <param name="userNotification">Уведомление пользователя.</param>
    /// <param name="notification">Уведомление.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="userNotification"/> <see langword="null"/>.</exception>
    /// <returns>DTO-модель уведомления пользователя.</returns>
    public static UserNotificationDto ToUserNotificationDto(this UserNotification userNotification, Notification notification)
    {
        ArgumentNullException.ThrowIfNull(userNotification);

        return new UserNotificationDto
        {
            Id = notification.Id,
            Title = notification.Title,
            Content = notification.Content,
            CreatedAt = notification.CreatedAt.ToWithoutTicks(),
            IsRead = userNotification.IsRead
        };
    }

    /// <summary>
    /// Возвращает коллекцию DTO-моделей уведомлений пользователя созданных из <paramref name="userNotifications"/>.
    /// </summary>
    /// <remarks>
    /// <para>Т.к. уведомление достаётся из самой сущности, необходимо прогрузить свойство <see cref="UserNotification.Notification"/> <c>(x => x.Notification)</c>.</para>
    /// </remarks>
    /// <param name="userNotifications">Уведомления пользователей.</param>
    /// <param name="notificationFunc">Прогруженная сущность уведомления <c>(x => x.Notification)</c>.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="userNotifications"/> <see langword="null"/>.</exception>
    /// <returns>Коллекция из <see cref="UserNotificationDto"/>.</returns>
    public static IEnumerable<UserNotificationDto> ToUserNotificationsDto(this IEnumerable<UserNotification> userNotifications, Func<UserNotification, Notification> notificationFunc)
    {
        ArgumentNullException.ThrowIfNull(userNotifications);

        var userNotificationsDto = userNotifications.Select(x => x.ToUserNotificationDto(notificationFunc(x)));
        return userNotificationsDto;
    }

    /// <summary>
    /// Возвращает DTO-модель постраничного списка из <paramref name="paginatedList"/>.
    /// </summary>
    /// <param name="paginatedList">Постраничный список.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="paginatedList"/> <see langword="null"/>.</exception>
    /// <returns>DTO-модель постраничного списка.</returns>
    public static PaginatedListDto<T> ToPaginatedListDto<T>(this PaginatedList<T> paginatedList)
    {
        ArgumentNullException.ThrowIfNull(paginatedList);

        return new PaginatedListDto<T>()
        {
            Items = paginatedList,
            PageIndex = paginatedList.PageIndex,
            PageSize = paginatedList.PageSize,
            TotalPages = paginatedList.TotalPages,
            SearchString = paginatedList.SearchString,
            SortBy = paginatedList.SortBy,
            HasPreviousPage = paginatedList.HasPreviousPage,
            HasNextPage = paginatedList.HasNextPage
        };
    }

    /// <summary>
    /// Возвращает <see cref="DateTime"/> без тиков (миллисекунд, микросекунд, наносекунд).
    /// </summary>
    /// <param name="dateTime"><see cref="DateTime"/>.</param>
    public static DateTime ToWithoutTicks(this DateTime dateTime)
    {
        return dateTime.AddTicks(-(dateTime.Ticks % TimeSpan.TicksPerSecond));
    }

    /// <summary>
    /// Создаёт <see cref="CreateUserDto"/> из <see cref="OpenIdUserInfo"/> и <see cref="OAuthCompleteRegistrationDto"/>.
    /// </summary>
    /// <remarks>
    /// <para>Необходимо проверять модель через валидатор.</para>
    /// <para>Метод учитывает невалидность предоставленных данных и пытается привести возвращаемую модель к валидному состоянию.</para>
    /// <list type="bullet">
    /// <item>Если предоставленное имя состоит не из кириллицы (без пробелов), то в <see cref="CreateUserDto.Firstname"/> вписывается "Неизвестно".</item>
    /// <item>Если предоставленный никнейм не из латиницы, то генерируется рандомный <see cref="CreateUserDto.Username"/>.</item>
    /// <item>В <see cref="CreateUserDto.Password"/> вписывается рандомный пароль.</item>
    /// <item>В <see cref="CreateUserDto.LanguageCode"/> вписывается первые две буквы из <see cref="OpenIdUserInfo.Locale"/>.</item>
    /// </list>
    /// </remarks>
    /// <param name="userInfo">Информация о пользователе OpenId.</param>
    /// <param name="oAuthCompleteRegistrationDto">DTO-модель завершения регистрации через OAuth.</param>
    /// <returns><see cref="CreateUserDto"/>.</returns>
    public static CreateUserDto ToCreateUserDto(this OpenIdUserInfo userInfo, OAuthCompleteRegistrationDto oAuthCompleteRegistrationDto)
    {
        bool isCyrillicFirstname = IsCyrillicRegex().IsMatch(userInfo.GivenName);
        bool isLatinUsername = IsLatinRegex().IsMatch(userInfo.Nickname);

        return new CreateUserDto
        {
            Firstname = isCyrillicFirstname ? userInfo.GivenName : "Неизвестно", // Если предоставленное имя состоит не из кириллицы (без пробелов), то вписываем "Неизвестно"
            Username = isLatinUsername ? userInfo.Nickname.ToLower() : RandomDataGenerator.GenerateRandomUsername(), // Если предоставленный никнейм не из латиницы, то генерируем рандомный Username
            Password = RandomDataGenerator.GenerateRandomPassword(), // В будущем пользователь не сможет поменять пароль :). Т.к для смены нужно указать текущий пароль, ну так-то смысл OAuth как раз в этом - не указывать пароль. Поэтому ставим заглушку
            LanguageCode = userInfo.Locale.Remove(2), // Ограничиваем до двух символов
            Email = userInfo.Email,
            PhoneNumber = oAuthCompleteRegistrationDto.PhoneNumber
        };
    }

    [GeneratedRegex("[а-яА-ЯёЁ]+$")]
    private static partial Regex IsCyrillicRegex(); // Кириллица

    [GeneratedRegex("[a-zA-Z]+$")]
    private static partial Regex IsLatinRegex(); // Латиница
}