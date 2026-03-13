namespace CRUD.WebApi.Endpoints;

/// <summary>
/// Конечные точки админ-панели.
/// </summary>
public static class AdminEndpoints
{
    /// <summary>
    /// Регистрирует конечные точки.
    /// </summary>
    public static void Map(WebApplication app)
    {
        // Это админ-панель
        var adminMap = app.MapGroup("/admin")
            .RequireAuthorization(UserRoles.Admin)
            .WithTags(EndpointTags.Admin);
        adminMap.MapGet("/users/{userId:guid}", async Task<Results<ProblemHttpResult, JsonHttpResult<UserFullDto>>> ([FromRoute] Guid userId, IUserManager userManager, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            // Пустой GUID
            if (userId == Guid.Empty)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyUniqueIdentifier, localizer);

            // Вызов сервиса
            var result = await userManager.GetUserFullDtoAsync(userId, ct);

            // Нет ошибки
            if (result.ErrorMessage == null)
                return TypedResults.Json(result.Value);

            // Сопоставление ошибки
            return TypedResults.Extensions.Problem(result, localizer);
        })
            .WithSummary("Возвращает максимальные данные пользователя.")
            .WithDescription("Возвратимые данные: Id, Имя, Username, Код языка, Роль, IsPremium, API-ключ, Одноразовый API-ключ, AvatarURL, Email, IsEmailConfirm, Телефонный номер, IsPhoneNumberConfirm.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .Produces((int)HttpStatusCode.Forbidden)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces<UserFullDto>((int)HttpStatusCode.OK);

        // То, что тут используется UpdateUserDto - нормально
        // Firstname, Username, Language Code можно спокойно обновить, а вот премиум и роль, лучше отдельно, также, как и Email, и PhoneNumber, ведь по правильному их нужно подтвердить
        adminMap.MapPut("/users/{userId:guid}", async Task<Results<ProblemHttpResult, NoContent>> ([FromRoute] Guid userId, [FromBody] UpdateUserDto updateUserDto, IUserManager userManager, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            // Пустой GUID
            if (userId == Guid.Empty)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyUniqueIdentifier, localizer);

            try
            {
                // Вызов сервиса
                var result = await userManager.UpdateUserAsync(userId, updateUserDto, ct);

                // Нет ошибки
                if (result.ErrorMessage == null)
                    return TypedResults.NoContent();

                // Сопоставление ошибки
                return TypedResults.Extensions.Problem(result, localizer);
            }
            catch (DbUpdateException ex)
            {
                // Кто первый обновил - тот и остаётся в базе. Второму сообщение о конфликте и предложение попробовать позже
                if (DbExceptionHelper.IsConcurrencyConflict(ex))
                    return TypedResults.Extensions.Problem(ApiErrorConstants.ConcurrencyConflicts, localizer);

                throw;
            }
        })
            .WithIdempotency()
            .WithValidation<UpdateUserDto>()
            .WithSummary("Обновляет данные пользователя по указанной модели.")
            .WithDescription("Обновляемые данные: Имя, Username, Код языка.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .Produces((int)HttpStatusCode.Forbidden)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces((int)HttpStatusCode.Conflict);

        adminMap.MapDelete("/users/{userId:guid}", async Task<Results<ProblemHttpResult, NoContent>> ([FromRoute] Guid userId, IUserManager userManager, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            // Пустой GUID
            if (userId == Guid.Empty)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyUniqueIdentifier, localizer);

            try
            {
                // Вызов сервиса
                var result = await userManager.DeleteUserAsync(userId, ct);

                // Нет ошибки
                if (result.ErrorMessage == null)
                    return TypedResults.NoContent();

                // Сопоставление ошибки
                return TypedResults.Extensions.Problem(result, localizer);
            }
            catch (DbUpdateException ex)
            {
                // Кто первый удалил - тот и удалил в базе. Второму сообщение о конфликте и предложение попробовать позже
                if (DbExceptionHelper.IsConcurrencyConflict(ex))
                    return TypedResults.Extensions.Problem(ApiErrorConstants.ConcurrencyConflicts, localizer);

                throw;
            }
        })
            .WithIdempotency()
            .WithSummary("Удаляет пользователя из базы данных.")
            .WithDescription("Удаление безвозвратно, освободившийся Username сразу можно зарегистрировать.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .Produces((int)HttpStatusCode.Forbidden)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces((int)HttpStatusCode.Conflict);

        adminMap.MapPost("/users/{userId:guid}/avatar", async Task<Results<ProblemHttpResult, NoContent>> ([FromRoute] Guid userId, [FromForm] IFormFile file, IAvatarManager avatarManager, IOptions<AvatarManagerOptions> options, IResourceLocalizer localizer, CancellationToken ct) =>
        {
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

            try
            {
                // Вызов сервиса
                var result = await avatarManager.SetAvatarAsync(userId, stream, ct);

                // Нет ошибки
                if (result.ErrorMessage == null)
                    return TypedResults.NoContent();

                // Сопоставление ошибки
                return TypedResults.Extensions.Problem(result, localizer);
            }
            catch (DbUpdateException ex)
            {
                // Кто первый обновил - тот и остаётся в базе. Второму сообщение о конфликте и предложение попробовать позже
                if (DbExceptionHelper.IsConcurrencyConflict(ex))
                    return TypedResults.Extensions.Problem(ApiErrorConstants.ConcurrencyConflicts, localizer);

                throw;
            }
        })
            .DisableAntiforgery()
            .WithSummary("Устанавливает аватарку пользователю.")
            .WithDescription($"Размер файла может быть не более $AvatarManagerOptions.MaxFileSizeString$ МБ.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .Produces((int)HttpStatusCode.Forbidden)
            .ProducesProblem((int)HttpStatusCode.BadRequest)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces((int)HttpStatusCode.RequestEntityTooLarge)
            .Produces((int)HttpStatusCode.Conflict);

        adminMap.MapPost("/users/{userId:guid}/password", async Task<Results<ProblemHttpResult, NoContent>> ([FromRoute] Guid userId, [FromBody] SetPasswordDto setPasswordDto, IPasswordChanger passwordChanger, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            // Пустой GUID
            if (userId == Guid.Empty)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyUniqueIdentifier, localizer);

            try
            {
                // Вызов сервиса
                var result = await passwordChanger.SetPasswordAsync(userId, setPasswordDto, ct);

                // Нет ошибки
                if (result.ErrorMessage == null)
                    return TypedResults.NoContent();

                // Сопоставление ошибки
                return TypedResults.Extensions.Problem(result, localizer);
            }
            catch (DbUpdateException ex)
            {
                // Кто первый обновил - тот и остаётся в базе. Второму сообщение о конфликте и предложение попробовать позже
                if (DbExceptionHelper.IsConcurrencyConflict(ex))
                    return TypedResults.Extensions.Problem(ApiErrorConstants.ConcurrencyConflicts, localizer);

                throw;
            }
        })
            .WithValidation<SetPasswordDto>()
            .WithSummary("Меняет пароль пользователю.")
            .WithDescription("Задаваемые данные: Новый пароль.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .Produces((int)HttpStatusCode.Forbidden)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces((int)HttpStatusCode.Conflict);

        adminMap.MapPut("/users/{userId:guid}/premium", async Task<Results<ProblemHttpResult, NoContent>> ([FromRoute] Guid userId, IPremiumManager premiumManager, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            // Пустой GUID
            if (userId == Guid.Empty)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyUniqueIdentifier, localizer);

            try
            {
                // Вызов сервиса
                var result = await premiumManager.SetPremiumAsync(userId, ct);

                // Нет ошибки
                if (result.ErrorMessage == null)
                    return TypedResults.NoContent();

                // Сопоставление ошибки
                return TypedResults.Extensions.Problem(result, localizer);
            }
            catch (DbUpdateException ex)
            {
                // Кто первый создал заказ - тот и остаётся в базе. Второму сообщение о конфликте и предложение попробовать позже
                if (DbExceptionHelper.IsConcurrencyConflict(ex))
                    return TypedResults.Extensions.Problem(ApiErrorConstants.ConcurrencyConflicts, localizer);

                throw;
            }
        })
            .WithIdempotency()
            .WithSummary("Устанавливает премиум пользователю.")
            .WithDescription("Премиум остаётся пожизненным, доступны API-ключи и многое другое.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .Produces((int)HttpStatusCode.Forbidden)
            .ProducesProblem((int)HttpStatusCode.BadRequest)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces((int)HttpStatusCode.Conflict);

        adminMap.MapPut("/users/{userId:guid}/role", async Task<Results<ProblemHttpResult, NoContent>> ([FromRoute] Guid userId, [FromBody] SetRoleDto setRoleDto, IUserManager userManager, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            // Пустой GUID
            if (userId == Guid.Empty)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyUniqueIdentifier, localizer);

            try
            {
                // Вызов сервиса
                var result = await userManager.SetRoleUserAsync(userId, setRoleDto, ct);

                // Нет ошибки
                if (result.ErrorMessage == null)
                    return TypedResults.NoContent();

                // Сопоставление ошибки
                return TypedResults.Extensions.Problem(result, localizer);
            }
            catch (DbUpdateException ex)
            {
                // Кто первый создал заказ - тот и остаётся в базе. Второму сообщение о конфликте и предложение попробовать позже
                if (DbExceptionHelper.IsConcurrencyConflict(ex))
                    return TypedResults.Extensions.Problem(ApiErrorConstants.ConcurrencyConflicts, localizer);

                throw;
            }
        })
            .WithIdempotency()
            .WithValidation<SetRoleDto>()
            .WithSummary("Меняет роль пользователя.")
            .WithDescription("Задаваемые данные: Устанавливаемая роль пользователя.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .Produces((int)HttpStatusCode.Forbidden)
            .ProducesProblem((int)HttpStatusCode.BadRequest)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces((int)HttpStatusCode.Conflict);

        adminMap.MapDelete("/users/{userId:guid}/refresh-tokens", async Task<Results<ProblemHttpResult, NoContent>> ([FromRoute] Guid userId, IUserManager userManager, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            // Пустой GUID
            if (userId == Guid.Empty)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyUniqueIdentifier, localizer);

            try
            {
                // Вызов сервиса
                var result = await userManager.RevokeRefreshTokensAsync(userId, ct);

                // Нет ошибки
                if (result.ErrorMessage == null)
                    return TypedResults.NoContent();

                // Сопоставление ошибки
                return TypedResults.Extensions.Problem(result, localizer);
            }
            catch (DbUpdateException ex)
            {
                // Кто первый удалил - тот и удалил в базе. Второму сообщение о конфликте и предложение попробовать позже
                if (DbExceptionHelper.IsConcurrencyConflict(ex))
                    return TypedResults.Extensions.Problem(ApiErrorConstants.ConcurrencyConflicts, localizer);

                throw;
            }
        })
            .WithSummary("Удаляет все Refresh-токены пользователя.")
            .WithDescription("Удаление безвозвратно.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .Produces((int)HttpStatusCode.Forbidden)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces((int)HttpStatusCode.Conflict);

        adminMap.MapGet("/publications/{publicationId:guid}", async Task<Results<ProblemHttpResult, JsonHttpResult<PublicationFullDto>>> ([FromRoute] Guid publicationId, IPublicationManager publicationManager, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            // Пустой GUID
            if (publicationId == Guid.Empty)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyUniqueIdentifier, localizer);

            // Вызов сервиса
            var result = await publicationManager.GetPublicationFullDtoAsync(publicationId, ct);

            // Нет ошибки
            if (result.ErrorMessage == null)
                return TypedResults.Json(result.Value);

            // Сопоставление ошибки
            return TypedResults.Extensions.Problem(result, localizer);
        })
            .WithSummary("Возвращает полные данные об указанной публикации.")
            .WithDescription("Возвратимые данные: Id, Дата, Заголовок, Содержимое, Id автора (пользователя), Имя автора.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .Produces((int)HttpStatusCode.Forbidden)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces<PublicationFullDto>((int)HttpStatusCode.OK);

        adminMap.MapPatch("/publications/{publicationId:guid}", async Task<Results<ProblemHttpResult, NoContent>> ([FromRoute] Guid publicationId, [FromBody] UpdatePublicationFullDto updatePublicationFullDto, IPublicationManager publicationManager, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            // Пустой GUID
            if (publicationId == Guid.Empty)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyUniqueIdentifier, localizer);

            try
            {
                // Вызов сервиса
                var result = await publicationManager.UpdatePublicationAsync(publicationId, updatePublicationFullDto, ct);

                // Нет ошибки
                if (result.ErrorMessage == null)
                    return TypedResults.NoContent();

                // Сопоставление ошибки
                return TypedResults.Extensions.Problem(result, localizer);
            }
            catch (DbUpdateException ex)
            {
                // Кто первый обновил - тот и остаётся в базе. Второму сообщение о конфликте и предложение попробовать позже
                if (DbExceptionHelper.IsConcurrencyConflict(ex))
                    return TypedResults.Extensions.Problem(ApiErrorConstants.ConcurrencyConflicts, localizer);

                throw;
            }
        })
            .WithValidation<UpdatePublicationFullDto>()
            .WithSummary("Частично или полностью обновляет данные публикации по указанной модели.")
            .WithDescription("Обновляемые данные: Заголовок, Содержимое, Дата.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .Produces((int)HttpStatusCode.Forbidden)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces((int)HttpStatusCode.Conflict);

        adminMap.MapDelete("/publications/{publicationId:guid}", async Task<Results<ProblemHttpResult, NoContent>> ([FromRoute] Guid publicationId, IPublicationManager publicationManager, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            // Пустой GUID
            if (publicationId == Guid.Empty)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyUniqueIdentifier, localizer);

            try
            {
                // Вызов сервиса
                var result = await publicationManager.DeletePublicationAsync(publicationId, ct);

                // Нет ошибки
                if (result.ErrorMessage == null)
                    return TypedResults.NoContent();

                // Сопоставление ошибки
                return TypedResults.Extensions.Problem(result, localizer);
            }
            catch (DbUpdateException ex)
            {
                // Кто первый удалил - тот и удалил в базе. Второму сообщение о конфликте и предложение попробовать позже
                if (DbExceptionHelper.IsConcurrencyConflict(ex))
                    return TypedResults.Extensions.Problem(ApiErrorConstants.ConcurrencyConflicts, localizer);

                throw;
            }
        })
            .WithIdempotency()
            .WithSummary("Удаляет указанную публикацию из базы данных.")
            .WithDescription("Удаление безвозвратно.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .Produces((int)HttpStatusCode.Forbidden)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces((int)HttpStatusCode.Conflict);

        adminMap.MapDelete("/publications/authors/{userId}", async Task<Results<ProblemHttpResult, NoContent>> ([FromRoute] Guid userId, IPublicationManager publicationManager, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            // Пустой GUID
            if (userId == Guid.Empty)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyUniqueIdentifier, localizer);

            try
            {
                // Вызов сервиса
                var result = await publicationManager.DeletePublicationsAsync(userId, ct);

                // Нет ошибки
                if (result.ErrorMessage == null)
                    return TypedResults.NoContent();

                // Сопоставление ошибки
                return TypedResults.Extensions.Problem(result, localizer);
            }
            catch (DbUpdateException ex)
            {
                // Кто первый удалил - тот и удалил в базе. Второму сообщение о конфликте и предложение попробовать позже
                if (DbExceptionHelper.IsConcurrencyConflict(ex))
                    return TypedResults.Extensions.Problem(ApiErrorConstants.ConcurrencyConflicts, localizer);

                throw;
            }
        })
            .WithSummary("Удаляет все публикации пользователя из базы данных.")
            .WithDescription("Удаление безвозвратно.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .Produces((int)HttpStatusCode.Forbidden)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces((int)HttpStatusCode.Conflict);

        adminMap.MapGet("/notifications/users/{userId}", async Task<Results<ValidationProblem, ProblemHttpResult, JsonHttpResult<IEnumerable<UserNotificationDto>>>> ([FromRoute] Guid userId, [FromQuery] int count, IValidator<GetUserNotificationsDto> validator, INotificationManager notificationManager, IResourceLocalizer localizer, CancellationToken ct) =>
        {
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
            .WithSummary("Возвращает указанное количество уведомлений указанного пользователя.")
            .WithDescription("Возвратимые данные: Коллекция уведомлений пользователя.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .Produces((int)HttpStatusCode.Forbidden)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces<IEnumerable<UserNotificationDto>>((int)HttpStatusCode.OK);

        adminMap.MapPost("/notifications", async Task<Results<ProblemHttpResult, Created<NotificationDto>>> ([FromBody] CreateNotificationDto createNotificationDto, INotificationManager notificationManager, IHubContext<NotificationHub> notificationHub, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            try
            {
                // Вызов сервиса
                var result = await notificationManager.CreateNotificationAsync(createNotificationDto, ct);

                // Нет ошибки
                if (result.ErrorMessage == null)
                {
                    // Отправляем уведомление всем подключённым клиентам
                    await notificationHub.Clients.All.SendAsync(HubMethodNames.ReceiveNotification, result.Value, ct);
                    return TypedResults.Created((string?)null, result.Value);
                }

                // Сопоставление ошибки
                return TypedResults.Extensions.Problem(result, localizer);
            }
            catch (DbUpdateException ex)
            {
                // Кто первый обновил - тот и остаётся в базе. Второму сообщение о конфликте и предложение попробовать позже
                if (DbExceptionHelper.IsConcurrencyConflict(ex))
                    return TypedResults.Extensions.Problem(ApiErrorConstants.ConcurrencyConflicts, localizer);

                throw;
            }
        })
            .WithValidation<CreateNotificationDto>()
            .WithSummary("Создаёт в базе и отправляет всем клиентам новое уведомление.")
            .WithDescription("Задаваемые данные: Заголовок, Содержимое.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .Produces((int)HttpStatusCode.Forbidden)
            .Produces((int)HttpStatusCode.Conflict);

        adminMap.MapPost("/notifications/selected-users", async Task<Results<ProblemHttpResult, Created<NotificationDto>>> ([FromBody] CreateNotificationSelectedUsersDto createNotificationSelectedUsersDto, INotificationManager notificationManager, IHubContext<NotificationHub> notificationHub, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            try
            {
                // Вызов сервиса
                var result = await notificationManager.CreateNotificationAsync(createNotificationSelectedUsersDto, ct);

                // Нет ошибки
                if (result.ErrorMessage == null)
                {
                    // Отправляем уведомление только пользователям из коллекции среди подключённых клиентов
                    await notificationHub.Clients.Users(createNotificationSelectedUsersDto.UserIds.Select(x => x.ToString())).SendAsync(HubMethodNames.ReceiveNotification, result.Value, ct);
                    return TypedResults.Created((string?)null, result.Value);
                }

                // Сопоставление ошибки
                return TypedResults.Extensions.Problem(result, localizer);
            }
            catch (DbUpdateException ex)
            {
                // Кто первый обновил - тот и остаётся в базе. Второму сообщение о конфликте и предложение попробовать позже
                if (DbExceptionHelper.IsConcurrencyConflict(ex))
                    return TypedResults.Extensions.Problem(ApiErrorConstants.ConcurrencyConflicts, localizer);

                throw;
            }
        })
            .WithValidation<CreateNotificationSelectedUsersDto>()
            .WithSummary("Создаёт в базе и отправляет всем клиентам из указанного массива новое уведомление (персональное).")
            .WithDescription("Задаваемые данные: Массив Id пользователей, Заголовок уведомления, Содержимое уведомления.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .Produces((int)HttpStatusCode.Forbidden)
            .Produces((int)HttpStatusCode.Conflict);

        adminMap.MapDelete("/notifications/{notificationId}", async Task<Results<ValidationProblem, ProblemHttpResult, NoContent>> ([FromRoute] Guid notificationId, INotificationManager notificationManager, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            // Пустой GUID
            if (notificationId == Guid.Empty)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyUniqueIdentifier, localizer);

            try
            {
                // Вызов сервиса
                var result = await notificationManager.DeleteNotificationAsync(notificationId, ct);

                // Нет ошибки
                if (result.ErrorMessage == null)
                    return TypedResults.NoContent();

                // Сопоставление ошибки
                return TypedResults.Extensions.Problem(result, localizer);
            }
            catch (DbUpdateException ex)
            {
                // Кто первый обновил - тот и остаётся в базе. Второму сообщение о конфликте и предложение попробовать позже
                if (DbExceptionHelper.IsConcurrencyConflict(ex))
                    return TypedResults.Extensions.Problem(ApiErrorConstants.ConcurrencyConflicts, localizer);

                throw;
            }
        })
            .WithIdempotency()
            .WithSummary("Удаляет указанное уведомление полностью (даже у пользователей).")
            .WithDescription("Удаление безвозвратно.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .Produces((int)HttpStatusCode.Forbidden)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces((int)HttpStatusCode.Conflict);
    }
}