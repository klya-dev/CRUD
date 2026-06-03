using Asp.Versioning.Builder;

namespace CRUD.WebApi.Endpoints;

/// <summary>
/// Конечные точки текущего (авторизированного) пользователя.
/// </summary>
public static class UserEndpoints
{
    /// <summary>
    /// Регистрирует конечные точки.
    /// </summary>
    /// <param name="apiVersionSet"><see cref="ApiVersionSet"/> версия API.</param>
    public static void Map(WebApplication app, ApiVersionSet apiVersionSet)
    {
        // Это текущий (авторизированный) пользователь
        var userMap = app.MapGroup("/v{version:apiVersion}/user")
            .WithApiVersionSet(apiVersionSet)
            .RequireAuthorization() // Требуем авторизацию, могу убрать, если указанна FallbackPolicy с RequireAuthenticatedUser, но тогда DefaultPolicy не сработает (и проверки языка не будет)
            .WithTags(EndpointTags.User, EndpointTags.AllEndpointsForClient);
        userMap.MapGet("/", async Task<Results<UnauthorizedHttpResult, ProblemHttpResult, JsonHttpResult<UserDto>>> (HttpContext httpContext, IUserManager userManager, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            // Ищем userId в claim'ах и пытаемся пропарсить Id, т.к может прийти "" или вообще любая строчка
            if (!httpContext.User.Claims.GetNameIdentifierGuid(out Guid userId))
                return TypedResults.Unauthorized();

            // Пустой GUID
            if (userId == Guid.Empty)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyUniqueIdentifier, localizer);

            // Вызов сервиса
            var result = await userManager.GetUserDtoAsync(userId, ct);

            // Нет ошибки
            if (result.ErrorMessage == null)
                return TypedResults.Json(result.Value);

            // Сопоставление ошибки
            return TypedResults.Extensions.Problem(result, localizer);
        })
            .WithSummary("Возвращает минимальные данные текущего пользователя.")
            .WithDescription("Возвратимые данные: Имя, Username, Код языка.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces<UserDto>((int)HttpStatusCode.OK);

        userMap.MapPut("/", async Task<Results<UnauthorizedHttpResult, ProblemHttpResult, NoContent>> ([FromBody] UpdateUserDto updateUserDto, IUserManager userManager, IResourceLocalizer localizer, HttpContext httpContext, CancellationToken ct) =>
        {
            // Ищем userId в claim'ах и пытаемся пропарсить Id, т.к может прийти "" или вообще любая строчка
            if (!httpContext.User.Claims.GetNameIdentifierGuid(out Guid userId))
                return TypedResults.Unauthorized();

            // Пустой GUID
            if (userId == Guid.Empty)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyUniqueIdentifier, localizer);

            // Вызов сервиса
            var result = await userManager.UpdateUserAsync(userId, updateUserDto, ct);

            // Нет ошибки
            if (result.ErrorMessage == null)
                return TypedResults.NoContent();

            // Сопоставление ошибки
            return TypedResults.Extensions.Problem(result, localizer);
        })
            .WithIdempotency()
            .WithValidation<UpdateUserDto>()
            .WithSummary("Обновляет данные текущего пользователя по указанной модели.")
            .WithDescription("Обновляемые данные: Имя, Username, Код языка.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces((int)HttpStatusCode.Conflict);

        userMap.MapDelete("/", async Task<Results<UnauthorizedHttpResult, ProblemHttpResult, NoContent>> ([FromBody] DeleteUserDto deleteUserDto, IUserManager userManager, IResourceLocalizer localizer, HttpContext httpContext, CancellationToken ct) =>
        {
            // Ищем userId в claim'ах и пытаемся пропарсить Id, т.к может прийти "" или вообще любая строчка
            if (!httpContext.User.Claims.GetNameIdentifierGuid(out Guid userId))
                return TypedResults.Unauthorized();

            // Пустой GUID
            if (userId == Guid.Empty)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyUniqueIdentifier, localizer);

            // Вызов сервиса
            var result = await userManager.DeleteUserAsync(userId, deleteUserDto, ct);

            // Нет ошибки
            if (result.ErrorMessage == null)
                return TypedResults.NoContent();

            // Сопоставление ошибки
            return TypedResults.Extensions.Problem(result, localizer);
        })
            .WithIdempotency()
            .WithValidation<DeleteUserDto>()
            .WithSummary("Удаляет текущего пользователя из базы данных через пароль этого пользователя.")
            .WithDescription("Удаление безвозвратно, освободившийся Username сразу можно зарегистрировать.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces((int)HttpStatusCode.Conflict);

        userMap.MapGet("/avatar-file", async Task<Results<UnauthorizedHttpResult, ProblemHttpResult, FileStreamHttpResult>> (IAvatarManager avatarManager, IResourceLocalizer localizer, HttpContext httpContext, CancellationToken ct) =>
        {
            // Ищем userId в claim'ах и пытаемся пропарсить Id, т.к может прийти "" или вообще любая строчка
            if (!httpContext.User.Claims.GetNameIdentifierGuid(out Guid userId))
                return TypedResults.Unauthorized();

            // Пустой GUID
            if (userId == Guid.Empty)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyUniqueIdentifier, localizer);

            // Вызов сервиса
            var result = await avatarManager.GetAvatarAsync(userId, ct);

            // Если расширение не указано, то "avatar", если указано, то "avatar.XXX"
            string fileDownloadName = result.Value.FileExtension == string.Empty ? "avatar" : $"avatar.{result.Value.FileExtension}";
            // *Если я захочу опять возвращать расширения для файлов (в методе GetAvatarAsync), то логика уже написана

            // Нет ошибки
            if (result.ErrorMessage == null)
                return TypedResults.File(result.Value.Stream, fileDownloadName: fileDownloadName);

            // Сопоставление ошибки
            return TypedResults.Extensions.Problem(result, localizer);
        })
            .WithSummary("Получает аватарку текущего пользователя файлом.")
            .WithDescription("Размер файла может быть не более $AvatarManagerOptions.MaxFileSizeString$ МБ.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .ProducesProblem((int)HttpStatusCode.BadRequest)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces((int)HttpStatusCode.Conflict);

        userMap.MapGet("/avatar-url", async Task<Results<UnauthorizedHttpResult, ProblemHttpResult, Ok<string>>> (IAvatarManager avatarManager, IResourceLocalizer localizer, HttpContext httpContext, CancellationToken ct) =>
        {
            // Ищем userId в claim'ах и пытаемся пропарсить Id, т.к может прийти "" или вообще любая строчка
            if (!httpContext.User.Claims.GetNameIdentifierGuid(out Guid userId))
                return TypedResults.Unauthorized();

            // Пустой GUID
            if (userId == Guid.Empty)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyUniqueIdentifier, localizer);

            // Вызов сервиса
            var result = await avatarManager.GetPresignedUrlAvatarAsync(userId, ct: ct);

            // Нет ошибки
            if (result.ErrorMessage == null)
                return TypedResults.Ok(result.Value);

            // Сопоставление ошибки
            return TypedResults.Extensions.Problem(result, localizer);
        })
            .WithSummary("Получает аватарку текущего пользователя ссылкой.")
            .WithDescription("По умолчанию срок действия ссылки 1 час.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .ProducesProblem((int)HttpStatusCode.BadRequest)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces((int)HttpStatusCode.Conflict);

        userMap.MapPost("/avatar", async Task<Results<UnauthorizedHttpResult, ProblemHttpResult, NoContent>> ([FromForm] IFormFile file, IAvatarManager avatarManager, IOptions<AvatarManagerOptions> options, IResourceLocalizer localizer, HttpContext httpContext, CancellationToken ct) =>
        {
            // Ищем userId в claim'ах и пытаемся пропарсить Id, т.к может прийти "" или вообще любая строчка
            if (!httpContext.User.Claims.GetNameIdentifierGuid(out Guid userId))
                return TypedResults.Unauthorized();

            // Пустой GUID
            if (userId == Guid.Empty)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyUniqueIdentifier, localizer);

            // Пустой файл
            if (file.Length <= 0)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyFile, localizer);

            // Проверка на размер файла (не открывая поток)
            if (file.Length > options.Value.MaxFileSize)
                return TypedResults.Extensions.Problem(ApiErrorConstants.FileSizeLimitExceeded, localizer, options.Value.MaxFileSizeString);

            // Открываем поток
            await using var stream = file.OpenReadStream();

            // Вызов сервиса
            var result = await avatarManager.SetAvatarAsync(userId, stream, ct);

            // Нет ошибки
            if (result.ErrorMessage == null)
                return TypedResults.NoContent();

            // Сопоставление ошибки
            return TypedResults.Extensions.Problem(result, localizer);
        })
            .DisableAntiforgery() // Отключаем Antiforgery (CSRF), т.к я не использую cookie, у меня есть JWT авторизация. Для IFormFile по умолчанию CSRF включён
            .WithSummary("Устанавливает аватарку текущему пользователю.")
            .WithDescription($"Размер файла может быть не более $AvatarManagerOptions.MaxFileSizeString$ МБ.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .ProducesProblem((int)HttpStatusCode.BadRequest)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces((int)HttpStatusCode.RequestEntityTooLarge)
            .Produces((int)HttpStatusCode.Conflict);

        userMap.MapPost("/password", async Task<Results<UnauthorizedHttpResult, ProblemHttpResult, NoContent>> ([FromBody] ChangePasswordDto changePasswordDto, IPasswordChanger passwordChanger, IResourceLocalizer localizer, HttpContext httpContext, CancellationToken ct) =>
        {
            // Ищем userId в claim'ах и пытаемся пропарсить Id, т.к может прийти "" или вообще любая строчка
            if (!httpContext.User.Claims.GetNameIdentifierGuid(out Guid userId))
                return TypedResults.Unauthorized();

            // Пустой GUID
            if (userId == Guid.Empty)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyUniqueIdentifier, localizer);

            // Вызов сервиса
            var result = await passwordChanger.ChangePasswordAsync(userId, changePasswordDto, ct);

            // Нет ошибки
            if (result.ErrorMessage == null)
                return TypedResults.NoContent();

            // Сопоставление ошибки
            return TypedResults.Extensions.Problem(result, localizer);
        })
            .WithValidation<ChangePasswordDto>()
            .WithSummary("Отправляет письмо для смены пароля на электронную почту текущего пользователя.")
            .WithDescription("Задаваемые данные: Пароль, Новый пароль.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces((int)HttpStatusCode.Conflict);

        userMap.MapPost("/premium", async Task<Results<UnauthorizedHttpResult, ProblemHttpResult, Ok<string>>> (IPremiumManager premiumManager, HttpContext httpContext, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            // Ищем userId в claim'ах и пытаемся пропарсить Id, т.к может прийти "" или вообще любая строчка
            if (!httpContext.User.Claims.GetNameIdentifierGuid(out Guid userId))
                return TypedResults.Unauthorized();

            // Пустой GUID
            if (userId == Guid.Empty)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyUniqueIdentifier, localizer);

            // Вызов сервиса
            var result = await premiumManager.BuyPremiumAsync(userId, ct);

            // Нет ошибки
            if (result.ErrorMessage == null)
                return TypedResults.Ok(result.Value);

            // Сопоставление ошибки
            return TypedResults.Extensions.Problem(result, localizer);
        })
            .WithSummary("Генерирует ссылку на покупку премиума для текущего пользователя.")
            .WithDescription("Премиум остаётся пожизненным, доступны API-ключи и многое другое.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .ProducesProblem((int)HttpStatusCode.BadRequest)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces((int)HttpStatusCode.InternalServerError)
            .Produces((int)HttpStatusCode.Conflict);

        userMap.MapPost("/confirmation/email", async Task<Results<UnauthorizedHttpResult, ProblemHttpResult, NoContent>> (IAuthManager authManager, IResourceLocalizer localizer, HttpContext httpContext, CancellationToken ct) =>
        {
            // Ищем userId в claim'ах и пытаемся пропарсить Id, т.к может прийти "" или вообще любая строчка
            if (!httpContext.User.Claims.GetNameIdentifierGuid(out Guid userId))
                return TypedResults.Unauthorized();

            // Пустой GUID
            if (userId == Guid.Empty)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyUniqueIdentifier, localizer);

            // Вызов сервиса
            var result = await authManager.SendConfirmEmailAsync(userId, ct);

            // Нет ошибки
            if (result.ErrorMessage == null)
                return TypedResults.NoContent();

            // Сопоставление ошибки
            return TypedResults.Extensions.Problem(result, localizer);
        })
            .WithSummary("Отправляет на электронную почту письмо для подтверждения почты текущего пользователя.")
            .WithDescription("Подтверждение единоразовое, дополнительных подтверждений не требуется.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .ProducesProblem((int)HttpStatusCode.BadRequest)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces((int)HttpStatusCode.Conflict);

        userMap.MapPost("/confirmation/phone", async Task<Results<UnauthorizedHttpResult, ProblemHttpResult, NoContent>> ([FromQuery] bool isTelegram, IAuthManager authManager, IResourceLocalizer localizer, HttpContext httpContext, CancellationToken ct) =>
        {
            // Ищем userId в claim'ах и пытаемся пропарсить Id, т.к может прийти "" или вообще любая строчка
            if (!httpContext.User.Claims.GetNameIdentifierGuid(out Guid userId))
                return TypedResults.Unauthorized();

            // Пустой GUID
            if (userId == Guid.Empty)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyUniqueIdentifier, localizer);

            // Вызов сервиса
            var result = await authManager.SendVerificationCodePhoneNumberAsync(userId, isTelegram, ct);

            // Нет ошибки
            if (result.ErrorMessage == null)
                return TypedResults.NoContent();

            // Сопоставление ошибки
            return TypedResults.Extensions.Problem(result, localizer);
        })
            .WithSummary("Отправляет на телефонный номер сообщение для подтверждения номера текущего пользователя.")
            .WithDescription("Если isTelegram = true, то сообщение отправляется в Телеграме, иначе СМС.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .ProducesProblem((int)HttpStatusCode.BadRequest)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces((int)HttpStatusCode.Conflict);

        userMap.MapGet("/publications", async Task<Results<UnauthorizedHttpResult, ValidationProblem, ProblemHttpResult, JsonHttpResult<IEnumerable<PublicationDto>>>> ([FromQuery] int count, HttpContext httpContext, IPublicationManager publicationManager, IValidator<GetPublicationsDto> validator, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            // Ищем userId в claim'ах и пытаемся пропарсить Id, т.к может прийти "" или вообще любая строчка
            if (!httpContext.User.Claims.GetNameIdentifierGuid(out Guid userId))
                return TypedResults.Unauthorized();

            // Пустой GUID
            if (userId == Guid.Empty)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyUniqueIdentifier, localizer);

            var getPublicationsDto = new GetPublicationsDto()
            {
                Count = count
            };

            // Валидация модели
            var validationResult = await validator.ValidateAsync(getPublicationsDto, ct);
            if (!validationResult.IsValid)
                return TypedResults.Extensions.ValidationProblem(validationResult, localizer);

            // Вызов сервиса
            var result = await publicationManager.GetPublicationsDtoAsync(count, userId, ct);

            // Нет ошибки
            if (result.ErrorMessage == null)
                return TypedResults.Json(result.Value);

            // Сопоставление ошибки
            return TypedResults.Extensions.Problem(result, localizer);
        })
            .WithSummary("Возвращает указанное количество публикаций текущего автора (пользователя).")
            .WithDescription("Возвратимые данные каждой публикации: Дата, Заголовок, Содержимое, Id автора (пользователя), Имя автора.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces<IEnumerable<PublicationDto>>((int)HttpStatusCode.OK);

        userMap.MapGet("/notifications", async Task<Results<UnauthorizedHttpResult, ValidationProblem, ProblemHttpResult, JsonHttpResult<IEnumerable<UserNotificationDto>>>> ([FromQuery] int count, HttpContext httpContext, IValidator<GetUserNotificationsDto> validator, INotificationManager notificationManager, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            // Ищем userId в claim'ах и пытаемся пропарсить Id, т.к может прийти "" или вообще любая строчка
            if (!httpContext.User.Claims.GetNameIdentifierGuid(out Guid userId))
                return TypedResults.Unauthorized();

            // Пустой GUID
            if (userId == Guid.Empty)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyUniqueIdentifier, localizer);

            var getUserNotificationsDto = new GetUserNotificationsDto()
            {
                Count = count
            };

            // Валидация модели
            var validationResult = await validator.ValidateAsync(getUserNotificationsDto, ct);
            if (!validationResult.IsValid)
                return TypedResults.Extensions.ValidationProblem(validationResult, localizer);

            // Вызов сервиса
            var result = await notificationManager.GetUserNotificationsDtoAsync(userId, count, ct);

            // Нет ошибки
            if (result.ErrorMessage == null)
                return TypedResults.Json(result.Value);

            // Сопоставление ошибки
            return TypedResults.Extensions.Problem(result, localizer);
        })
            .WithSummary("Возвращает указанное количество уведомлений текущего пользователя.")
            .WithDescription("Возвратимые данные: Коллекция уведомлений пользователя.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces<IEnumerable<UserNotificationDto>>((int)HttpStatusCode.OK);

        userMap.MapPut("/notifications/{notificationId}/read", async Task<Results<UnauthorizedHttpResult, ProblemHttpResult, NoContent>> ([FromRoute] Guid notificationId, HttpContext httpContext, INotificationManager notificationManager, IHubContext<NotificationHub> notificationHub, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            // Ищем userId в claim'ах и пытаемся пропарсить Id, т.к может прийти "" или вообще любая строчка
            if (!httpContext.User.Claims.GetNameIdentifierGuid(out Guid userId))
                return TypedResults.Unauthorized();

            // Пустой GUID
            if (userId == Guid.Empty || notificationId == Guid.Empty)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyUniqueIdentifier, localizer);

            // Вызов сервиса
            var result = await notificationManager.SetIsReadNotificationAsync(userId, notificationId, ct);

            // Нет ошибки
            if (result.ErrorMessage == null)
                return TypedResults.NoContent();

            // Сопоставление ошибки
            return TypedResults.Extensions.Problem(result, localizer);
        })
            .WithIdempotency()
            .WithSummary("Задаёт указанному уведомлению текущего пользователя статус \"прочитано\".")
            .WithDescription($"Свойство {nameof(UserNotification.IsRead)} уставливается на true.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .Produces((int)HttpStatusCode.Forbidden)
            .Produces((int)HttpStatusCode.Conflict);
    }
}