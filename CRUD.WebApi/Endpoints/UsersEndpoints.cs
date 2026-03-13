using Asp.Versioning.Builder;

namespace CRUD.WebApi.Endpoints;

/// <summary>
/// Конечные точки пользователей (общедоступно).
/// </summary>
public static class UsersEndpoints
{
    /// <summary>
    /// Регистрирует конечные точки.
    /// </summary>
    /// <param name="apiVersionSet"><see cref="ApiVersionSet"/> версия API.</param>
    public static void Map(WebApplication app, ApiVersionSet apiVersionSet)
    {
        // Это общедоступный API (клиент, админ, бизнес)
        var usersMap = app.MapGroup("/v{version:apiVersion}/users")
            .WithApiVersionSet(apiVersionSet)
            .WithTags(EndpointTags.Users, EndpointTags.AllEndpointsForClient);
        usersMap.MapGet("/{userId:guid}", async Task<Results<UnauthorizedHttpResult, ProblemHttpResult, JsonHttpResult<UserDto>>> ([FromRoute] Guid userId, IUserManager userManager, IResourceLocalizer localizer, CancellationToken ct) =>
        {
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
            .WithSummary("Возвращает минимальные данные указанного пользователя.")
            .WithDescription("Возвратимые данные: Имя, Username, Код языка.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces<UserDto>((int)HttpStatusCode.OK);

        usersMap.MapGet("/{userId:guid}/avatar", async Task<Results<UnauthorizedHttpResult, ProblemHttpResult, FileStreamHttpResult>> ([FromRoute] Guid userId, IAvatarManager avatarManager, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            // Пустой GUID
            if (userId == Guid.Empty)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyUniqueIdentifier, localizer);

            // Вызов сервиса
            var result = await avatarManager.GetAvatarAsync(userId, ct);

            // Нет ошибки
            if (result.ErrorMessage == null)
                return TypedResults.File(result.Value.Stream, fileDownloadName: $"avatar.{result.Value.FileExtension}");

            // Сопоставление ошибки
            return TypedResults.Extensions.Problem(result, localizer);
        })
            .WithSummary("Получает аватарку указанного пользователя файлом.")
            .WithDescription($"Размер файла может быть не более $AvatarManagerOptions.MaxFileSizeString$ МБ.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .ProducesProblem((int)HttpStatusCode.BadRequest)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces((int)HttpStatusCode.Conflict);
    }
}