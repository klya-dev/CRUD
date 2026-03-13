using Microsoft.Extensions.Caching.Hybrid;

namespace CRUD.Services;

/// <inheritdoc cref="IPublicationManager"/>
public class PublicationManager : IPublicationManager
{
    private readonly ApplicationDbContext _db;
    private readonly IValidator<Publication> _publicationValidator;
    private readonly IValidator<GetPublicationsDto> _getPublicationsDtoValidator;
    private readonly IValidator<GetPaginatedListDto> _getPaginatedListDtoValidator;
    private readonly IValidator<GetAuthorsDto> _getAuthorsDtoValidator;
    private readonly IValidator<UpdatePublicationDto> _updatePublicationDtoValidator;
    private readonly IValidator<UpdatePublicationFullDto> _updatePublicationFullDtoValidator;
    private readonly IValidator<CreatePublicationDto> _createPublicationDtoValidator;
    private readonly IHtmlHelper _htmlHelper;
    private readonly HybridCache _cache;

    public PublicationManager(
        ApplicationDbContext db,
        IValidator<Publication> publicationValidator,
        IValidator<GetPublicationsDto> getPublicationsDtoValidator,
        IValidator<GetPaginatedListDto> getPaginatedListDtoValidator,
        IValidator<GetAuthorsDto> getAuthorsDtoValidator,
        IValidator<UpdatePublicationDto> updatePublicationDtoValidator,
        IValidator<UpdatePublicationFullDto> updatePublicationFullDtoValidator,
        IValidator<CreatePublicationDto> createPublicationDtoValidator,
        IHtmlHelper htmlHelper,
        HybridCache cache)
    {
        _db = db;
        _publicationValidator = publicationValidator;
        _getPublicationsDtoValidator = getPublicationsDtoValidator;
        _getPaginatedListDtoValidator = getPaginatedListDtoValidator;
        _getAuthorsDtoValidator = getAuthorsDtoValidator;
        _updatePublicationDtoValidator = updatePublicationDtoValidator;
        _updatePublicationFullDtoValidator = updatePublicationFullDtoValidator;
        _createPublicationDtoValidator = createPublicationDtoValidator;
        _htmlHelper = htmlHelper;
        _cache = cache;
    }

    public async Task<IEnumerable<PublicationDto>> GetPublicationsDtoAsync(int count, CancellationToken ct = default)
    {
        var getPublicationsDto = new GetPublicationsDto()
        { 
            Count = count
        };

        // Валидация модели
        var validationResult = await _getPublicationsDtoValidator.ValidateAsync(getPublicationsDto, ct);
        if (!validationResult.IsValid) // Эндпоинт должен предоставить валидные данные
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(GetPublicationsDto), validationResult.Errors));

        // Достаём публикации и сразу преобразуем в DTO на стороне базы
        var publications = await _db.Publications.AsNoTracking()
            .OrderBy(x => x.CreatedAt)
            .Take(count)
            .Select(x => x.ToPublicationDto(x.User!.Firstname)) // EF сам подтянет зависимость
            .ToListAsync(ct);

        return publications;
    }

    public async Task<PaginatedListDto<PublicationDto>> GetPublicationsDtoAsync(int pageIndex, int pageSize, string? searchString = null, string sortBy = SortByVariables.date, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sortBy);

        var getPaginatedListDto = new GetPaginatedListDto()
        {
            PageIndex = pageIndex,
            PageSize = pageSize
        };

        // Валидация номера и размера страницы
        var validationResult = await _getPaginatedListDtoValidator.ValidateAsync(getPaginatedListDto, ct);
        if (!validationResult.IsValid) // Эндпоинт должен предоставить валидные данные
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(GetPaginatedListDto), validationResult.Errors));

        // Создаём запрос, но пока не выполняем: Публикации из базы
        var publications = _db.Publications.AsNoTracking();

        // Если строка поиска состоит из символов пробела ("   ")
        if (searchString != null && searchString.IsWhiteSpace()) // То возвращаем пустой постраничный список
            return PaginatedList<PublicationDto>.Empty(pageIndex, pageSize, searchString, sortBy).ToPaginatedListDto();

        // Получаем очищенную строку поиска
        searchString = SearchStringValidator.GetSanitizedSearchString(searchString);

        // Если очищенная строка поиска не null
        if (searchString != null)
            publications = publications
                .Where(x => x.Id.ToString().Contains(searchString) // То добавляем в запрос поиск совпадений по Id или Title или Content или AuthorFirstname
                    || x.Title.Contains(searchString)
                    || x.Content.Contains(searchString)
                    || x.User!.Firstname.Contains(searchString)); // EF сам подтянет зависимость

        // Сопоставляем сортировку
        publications = sortBy.ToLower() switch
        {
            SortByVariables.date => publications.OrderBy(x => x.CreatedAt),
            SortByVariables.date_desc => publications.OrderByDescending(x => x.CreatedAt),
            SortByVariables.author_publications_count => publications.OrderBy(x => x.User!.Publications!.Count).ThenBy(x => x.CreatedAt), // По количеству публикаций автора и дополнительно по дате через ThenBy
            SortByVariables.author_publications_count_desc => publications.OrderByDescending(x => x.User!.Publications!.Count).ThenBy(x => x.CreatedAt),
            _ => publications.OrderBy(x => x.CreatedAt), // Если нет подходящего варианта сортировки, то сортируем по дате
        };

        // Преобразуем в DTO на стороне базы
        var publicationDtos = publications
                .Select(x => x.ToPublicationDto(x.User!.Firstname)); // EF сам подтянет зависимость

        // Создаём постраничный список публикаций
        var paginatedList = await PaginatedList<PublicationDto>.CreateAsync(publicationDtos, pageIndex, pageSize, searchString, sortBy, ct);

        // Преобразовываем в DTO и возвращаем
        return paginatedList.ToPaginatedListDto();
    }

    public async Task<ServiceResult<IEnumerable<PublicationDto>>> GetPublicationsDtoAsync(int count, Guid authorId, CancellationToken ct = default)
    {
        // Пустой GUID
        if (authorId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        var getPublicationsDto = new GetPublicationsDto()
        {
            Count = count
        };

        // Валидация модели
        var validationResult = await _getPublicationsDtoValidator.ValidateAsync(getPublicationsDto, ct);
        if (!validationResult.IsValid)
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(GetPublicationsDto), validationResult.Errors));

        // Автор не найден
        // Если писать Include в случае не найденого автора, EF всё равно будет пытаться прогрузить - лишние запросы
        var userExists = await _db.Users.AnyAsync(x => x.Id == authorId, ct);
        if (!userExists)
            return ServiceResult<IEnumerable<PublicationDto>>.Fail(ErrorMessages.AuthorNotFound);

        // Достаём публикации и преобразуем в DTO на стороне базы
        var publications = await _db.Publications.AsNoTracking()
            .Where(x => x.AuthorId == authorId)
            .OrderBy(x => x.CreatedAt)
            .Take(count)
            .Select(x => x.ToPublicationDto(x.User!.Firstname)) // EF сам подтянет зависимость
            .ToListAsync(ct);

        // Нет ни одной публикации
        if (publications.Count <= 0)
            return ServiceResult<IEnumerable<PublicationDto>>.Success([]); // Возвращаем пустую коллекцию

        return ServiceResult<IEnumerable<PublicationDto>>.Success(publications);
    }

    public async Task<ServiceResult<PublicationDto>> GetPublicationDtoAsync(Guid publicationId, CancellationToken ct = default)
    {
        // Пустой GUID
        if (publicationId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Публикация не найдена
        var publicationFromDb = await _db.Publications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == publicationId, ct);
        if (publicationFromDb == null)
            return ServiceResult<PublicationDto>.Fail(ErrorMessages.PublicationNotFound);

        // Ищем автора
        var userFromDb = await _db.Users.AsNoTracking().Where(x => x.Id == publicationFromDb.AuthorId).Select(x => new { x.Firstname }).FirstOrDefaultAsync(ct);

        var publicationDto = publicationFromDb.ToPublicationDto(userFromDb?.Firstname); // Если автор не найден, то будет написано "Автор удалён"

        return ServiceResult<PublicationDto>.Success(publicationDto);
    }

    public async Task<ServiceResult<PublicationFullDto>> GetPublicationFullDtoAsync(Guid publicationId, CancellationToken ct = default)
    {
        // Пустой GUID
        if (publicationId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Публикация не найдена
        var publicationFromDb = await _db.Publications.AsNoTracking().Include(x => x.User).FirstOrDefaultAsync(x => x.Id == publicationId, ct);
        if (publicationFromDb == null)
            return ServiceResult<PublicationFullDto>.Fail(ErrorMessages.PublicationNotFound);

        // Создаём DTO
        var publicationDto = publicationFromDb.ToPublicationFullDto(publicationFromDb.User);

        return ServiceResult<PublicationFullDto>.Success(publicationDto);
    }

    public async Task<IEnumerable<AuthorDto>> GetAuthorsDtoAsync(int count, CancellationToken ct = default)
    {
        var getAuthorsDto = new GetAuthorsDto()
        {
            Count = count
        };

        // Валидация модели
        var validationResult = await _getAuthorsDtoValidator.ValidateAsync(getAuthorsDto, ct);
        if (!validationResult.IsValid)
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(GetAuthorsDto), validationResult.Errors));

        // Достаём авторов и преобразуем в DTO
        var authors = await _db.Users.AsNoTracking()
            .Where(x => x.Publications!.Any())
            .OrderBy(x => x.Username)
            .Take(count)
            .Select(x => x.ToAuthorDto(x.Publications!.Count)) // EF сам подтянет зависимость
            .ToListAsync(ct);

        // Нет ни одного автора
        if (authors.Count <= 0)
            return []; // Возвращаем пустую коллекцию

        return authors;
    }

    public async Task<ServiceResult> UpdatePublicationAsync(Guid userId, UpdatePublicationDto updatePublicationDto, CancellationToken ct = default)
    {
        // Пустые данные
        ArgumentNullException.ThrowIfNull(updatePublicationDto);

        // Пустой GUID
        if (userId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Валидация модели
        var validationResult = await _updatePublicationDtoValidator.ValidateAsync(updatePublicationDto, ct);
        if (!validationResult.IsValid) // Эндпоинт должен предоставить валидные данные, это его ответственность, если исключение - значит разраб накосипорил, недотестил
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(UpdatePublicationDto), validationResult.Errors));

        // Пользователь не найден (который пытается обновить)
        var isExistsUser = await _db.Users.AsNoTracking().AnyAsync(x => x.Id == userId, ct);
        if (!isExistsUser)
            return ServiceResult.Fail(ErrorMessages.AuthorNotFound);

        // Публикация не найдена
        var publicationFromDb = await _db.Publications.FirstOrDefaultAsync(x => x.Id == updatePublicationDto.PublicationId, ct);
        if (publicationFromDb == null)
            return ServiceResult.Fail(ErrorMessages.PublicationNotFound);

        // Является ли пользователь автором этой публикации
        if (!await IsAuthorThisPublicationAsync(userId, updatePublicationDto.PublicationId, ct))
            return ServiceResult.Fail(ErrorMessages.UserIsNotAuthorOfThisPublication);

        // Если заголовок не задали (null), то берём из базы
        string title = updatePublicationDto.Title ?? publicationFromDb.Title;
        string content = updatePublicationDto.Content ?? publicationFromDb.Content;

        // Очистка Html от вредоносного кода
        content = _htmlHelper.SanitizeHtml(content);

        // Убираем лишние пробелы и отступы
        content = content.ReplaceExtraSpacesAndNewLines();

        // Не обнаружено изменений
        if (publicationFromDb.Title == title &&
           publicationFromDb.Content == content)
            return ServiceResult.Fail(ErrorMessages.NoChangesDetected);

        publicationFromDb.Title = title;
        publicationFromDb.Content = content;
        publicationFromDb.EditedAt = DateTime.UtcNow;

        // Проверка валидности данных перед записью в базу
        var validationResultPublication = await _publicationValidator.ValidateAsync(publicationFromDb, ct);
        if (!validationResultPublication.IsValid) // Если данные невалидны, то я уже ничего не сделаю - исключение
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(Publication), validationResultPublication.Errors));

        _db.Publications.Update(publicationFromDb);
        await _db.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UpdatePublicationAsync(Guid publicationId, UpdatePublicationFullDto updatePublicationFullDto, CancellationToken ct = default)
    {
        // Пустые данные
        ArgumentNullException.ThrowIfNull(updatePublicationFullDto);

        // Пустой GUID
        if (publicationId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Валидация модели
        var validationResult = await _updatePublicationFullDtoValidator.ValidateAsync(updatePublicationFullDto, ct);
        if (!validationResult.IsValid) // Эндпоинт должен предоставить валидные данные, это его ответственность, если исключение - значит разраб накосипорил, недотестил
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(UpdatePublicationFullDto), validationResult.Errors));

        // Публикация не найдена
        var publicationFromDb = await _db.Publications.FirstOrDefaultAsync(x => x.Id == publicationId, ct);
        if (publicationFromDb == null)
            return ServiceResult.Fail(ErrorMessages.PublicationNotFound);

        // Если заголовок не задали (null), то берём из базы
        string title = updatePublicationFullDto.Title ?? publicationFromDb.Title;
        string content = updatePublicationFullDto.Content ?? publicationFromDb.Content;

        // Очистка Html от вредоносного кода
        content = _htmlHelper.SanitizeHtml(content);

        // Убираем лишние пробелы и отступы
        content = content.ReplaceExtraSpacesAndNewLines();

        // Если дату не удалось пропарсить (скорее всего она не задана), то берём из базы
        DateTime date = DateTime.TryParse(updatePublicationFullDto.CreatedAt, out DateTime outDate) ? outDate : publicationFromDb.CreatedAt;

        // Не обнаружено изменений
        if (publicationFromDb.Title == title
            && publicationFromDb.Content == content
            && publicationFromDb.CreatedAt == date)
            return ServiceResult.Fail(ErrorMessages.NoChangesDetected);

        publicationFromDb.Title = title;
        publicationFromDb.Content = content;
        publicationFromDb.CreatedAt = date;

        // Проверка валидности данных перед записью в базу
        var validationResultPublication = await _publicationValidator.ValidateAsync(publicationFromDb, ct);
        if (!validationResultPublication.IsValid) // Если данные невалидны, то я уже ничего не сделаю - исключение
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(Publication), validationResultPublication.Errors));

        _db.Publications.Update(publicationFromDb);
        await _db.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<PublicationDto>> CreatePublicationAsync(Guid userId, CreatePublicationDto createPublicationDto, CancellationToken ct = default)
    {
        // Пустые данные
        ArgumentNullException.ThrowIfNull(createPublicationDto);

        // Пустой GUID
        if (userId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Валидация модели
        var validationResult = await _createPublicationDtoValidator.ValidateAsync(createPublicationDto, ct);
        if (!validationResult.IsValid)
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(CreatePublicationDto), validationResult.Errors));

        // Очистка Html от вредоносного кода
        createPublicationDto.Content = _htmlHelper.SanitizeHtml(createPublicationDto.Content);

        // Убираем лишние пробелы и отступы
        createPublicationDto.Content = createPublicationDto.Content.ReplaceExtraSpacesAndNewLines();

        // Пользователь не найден (который пытается создать)
        var userFromDb = await _db.Users.AsNoTracking().Where(x => x.Id == userId).Select(x => new { x.IsEmailConfirm, x.IsPhoneNumberConfirm, x.Firstname }).FirstOrDefaultAsync(ct);
        if (userFromDb == null)
            return ServiceResult<PublicationDto>.Fail(ErrorMessages.UserNotFound);

        // У пользователя не подтверждена электронная почта
        if (!userFromDb.IsEmailConfirm)
            return ServiceResult<PublicationDto>.Fail(ErrorMessages.UserHasNotConfirmedEmail);

        // У пользователя не подтверждён телефонный номер
        if (!userFromDb.IsPhoneNumberConfirm)
            return ServiceResult<PublicationDto>.Fail(ErrorMessages.UserHasNotConfirmedPhoneNumber);

        var publication = new Publication
        {
            CreatedAt = DateTime.UtcNow,
            Title = createPublicationDto.Title,
            Content = createPublicationDto.Content,
            AuthorId = userId
        };

        // Проверка валидности данных перед записью в базу
        var validationResultPublication = await _publicationValidator.ValidateAsync(publication, ct);
        if (!validationResultPublication.IsValid) // Если данные невалидны, то я уже ничего не сделаю - исключение
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(Publication), validationResultPublication.Errors));

        await _db.Publications.AddAsync(publication, ct);
        await _db.SaveChangesAsync(ct);

        return ServiceResult<PublicationDto>.Success(publication.ToPublicationDto(userFromDb.Firstname));
    }

    public async Task<ServiceResult> DeletePublicationAsync(Guid userId, Guid publicationId, CancellationToken ct = default)
    {
        // Пустой GUID
        if (userId == Guid.Empty || publicationId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Пользователь не найден (который пытается удалить)
        var isExistsUser = await _db.Users.AsNoTracking().AnyAsync(x => x.Id == userId, ct);
        if (!isExistsUser)
            return ServiceResult.Fail(ErrorMessages.UserNotFound);

        // Публикация не найдена
        var publicationFromDb = await _db.Publications.FirstOrDefaultAsync(x => x.Id == publicationId, ct);
        if (publicationFromDb == null)
            return ServiceResult.Fail(ErrorMessages.PublicationNotFound);

        // Является ли пользователь автором этой публикации
        if (!await IsAuthorThisPublicationAsync(userId, publicationId, ct))
            return ServiceResult.Fail(ErrorMessages.UserIsNotAuthorOfThisPublication);

        _db.Publications.Remove(publicationFromDb);
        await _db.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeletePublicationAsync(Guid publicationId, CancellationToken ct = default)
    {
        // Пустой GUID
        if (publicationId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Публикация не найдена
        var publicationFromDb = await _db.Publications.FirstOrDefaultAsync(x => x.Id == publicationId, ct);
        if (publicationFromDb == null)
            return ServiceResult.Fail(ErrorMessages.PublicationNotFound);

        _db.Publications.Remove(publicationFromDb);
        await _db.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeletePublicationsAsync(Guid userId, CancellationToken ct = default)
    {
        // Пустой GUID
        if (userId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Пользователь не найден (публикации, которого нужно удалить)
        var isExistsUser = await _db.Users.AsNoTracking().AnyAsync(x => x.Id == userId, ct);
        if (!isExistsUser)
            return ServiceResult.Fail(ErrorMessages.UserNotFound);

        // Публикации автора
        var publicationsFromDb = _db.Publications.Where(x => x.AuthorId == userId);

        _db.Publications.RemoveRange(publicationsFromDb);
        await _db.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }

    public async Task<bool> IsAuthorThisPublicationAsync(Guid userId, Guid publicationId, CancellationToken ct = default)
    {
        // Закэшированно, что такой-то пользователь не является автором такой-то публикации
        // Нет функционала передать публикацию другому автору
        // И если автор удалён ничего плохого не случится

        var options = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(30),
            LocalCacheExpiration = TimeSpan.FromMinutes(30)
        };

        // Есть ли хоть одна публикация с таким Id от этого пользователя
        return await _cache.GetOrCreateAsync(
            $"{CacheKeys.IsAuthorThisPublication}-{userId}:{publicationId}",
            async ct => await _db.Publications.AnyAsync(x => x.AuthorId == userId && x.Id == publicationId, ct),
            options, cancellationToken: ct);
    }
}