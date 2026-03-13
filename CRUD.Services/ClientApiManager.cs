using CRUD.Services.Interfaces;

namespace CRUD.Services;

/// <inheritdoc cref="IClientApiManager"/>
public class ClientApiManager : IClientApiManager
{
    private readonly ApplicationDbContext _db;
    private readonly IPublicationManager _publicationManager;
    private readonly IUserApiKeyManager _userApiKeyManager;
    private readonly IValidator<ClientApiCreatePublicationDto> _clientApiCreatePublicationDtoValidator;
    private readonly IValidator<User> _userValidator;

    public ClientApiManager(ApplicationDbContext db, IPublicationManager publicationManager, IUserApiKeyManager userApiKeyManager, IValidator<ClientApiCreatePublicationDto> clientApiCreatePublicationDtoValidator, IValidator<User> userValidator)
    {
        _db = db;
        _publicationManager = publicationManager;
        _userApiKeyManager = userApiKeyManager;
        _clientApiCreatePublicationDtoValidator = clientApiCreatePublicationDtoValidator;
        _userValidator = userValidator;
    }

    public async Task<ServiceResult<PublicationDto>> CreatePublicationAsync(ClientApiCreatePublicationDto clientApiCreatePublicationDto, CancellationToken ct = default)
    {
        // Пустые данные
        ArgumentNullException.ThrowIfNull(clientApiCreatePublicationDto);

        // Валидация модели
        var validationResult = await _clientApiCreatePublicationDtoValidator.ValidateAsync(clientApiCreatePublicationDto, ct);
        if (!validationResult.IsValid)
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(ClientApiCreatePublicationDto), validationResult.Errors));

        // Ищем пользователя по ключу | Неверный API-ключ
        var userFromDb = await _db.Users.FirstOrDefaultAsync(x => x.ApiKey == clientApiCreatePublicationDto.ApiKey || x.DisposableApiKey == clientApiCreatePublicationDto.ApiKey, ct);
        if (userFromDb == null)
            return ServiceResult<PublicationDto>.Fail(ErrorMessages.InvalidApiKey);

        // Нет премиума
        if (!userFromDb.IsPremium)
            return ServiceResult<PublicationDto>.Fail(ErrorMessages.UserDoesNotHavePremium);

        // У пользователя не подтверждена электронная почта
        if (!userFromDb.IsEmailConfirm)
            return ServiceResult<PublicationDto>.Fail(ErrorMessages.UserHasNotConfirmedEmail);

        // У пользователя не подтверждён телефонный номер
        if (!userFromDb.IsPhoneNumberConfirm)
            return ServiceResult<PublicationDto>.Fail(ErrorMessages.UserHasNotConfirmedPhoneNumber);

        var publicationDto = new CreatePublicationDto
        {
            Title = clientApiCreatePublicationDto.Title,
            Content = clientApiCreatePublicationDto.Content
        };

        // Создание публикации
        var result = await _publicationManager.CreatePublicationAsync(userFromDb.Id, publicationDto, CancellationToken.None); // Без отмены, иначе, например, публикация создастся, а ключ не обновится
        if (result.ErrorMessage != null) // Есть ошибка
            return ServiceResult<PublicationDto>.Fail(result.ErrorMessage);

        // Если это был одноразовый ключ, создаём новый
        if (userFromDb.DisposableApiKey == clientApiCreatePublicationDto.ApiKey)
            userFromDb.DisposableApiKey = _userApiKeyManager.GenerateDisposableUserApiKey();

        // Проверка валидности данных перед записью в базу
        var validationResultUser = await _userValidator.ValidateAsync(userFromDb, CancellationToken.None);
        if (!validationResultUser.IsValid) // Если данные невалидны, то я уже ничего не сделаю - исключение
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(User), validationResultUser.Errors));

        _db.Users.Update(userFromDb);
        await _db.SaveChangesAsync(CancellationToken.None);

        return ServiceResult<PublicationDto>.Success(result.Value!);
    }
}