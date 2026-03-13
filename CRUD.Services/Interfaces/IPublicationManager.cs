using CRUD.Models.Dtos;

namespace CRUD.Services.Interfaces;

/// <summary>
/// Основной сервис для работы с публикациями.
/// </summary>
public interface IPublicationManager
{
    /// <summary>
    /// Получает указанное количество публикаций из базы и преобразует в <see cref="PublicationDto"/>.
    /// </summary>
    /// <remarks>
    /// <para>Вызывающий метод должен предоставить валидные, не пустые данные для <paramref name="count"/>.</para>
    /// <para>Для валидации <paramref name="count"/> используется <see cref="IValidator{GetPublicationsDto}"/>.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="count"/> невалиден</term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="count">Количество публикаций.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="InvalidOperationException">Если <paramref name="count"/> невалиден.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see cref="IEnumerable{PublicationDto}"/>, коллекция DTO-моделей публикаций.</returns>
    Task<IEnumerable<PublicationDto>> GetPublicationsDtoAsync(int count, CancellationToken ct = default);

    /// <summary>
    /// Получает постраничный список DTO-моделей публикаций по указанным данным.
    /// </summary>
    /// <remarks>
    /// <para>Вызывающий метод должен предоставить валидные, не пустые данные для <paramref name="pageSize"/>.</para>
    /// <para>Для валидации <paramref name="pageSize"/> используется <see cref="IValidator{GetPaginatedListDto}"/>.</para>
    /// <para><paramref name="searchString"/> проходит очистку через <see cref="SearchStringValidator"/>.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="sortBy"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="pageSize"/> невалиден</term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="pageIndex">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="searchString">Строка поиска.</param>
    /// <param name="sortBy">Вариант сортировки.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="sortBy"/> <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Если <paramref name="pageSize"/> невалиден.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see cref="PaginatedListDto{PublicationDto}"/>, постраничный список DTO-моделей публикаций.</returns>
    Task<PaginatedListDto<PublicationDto>> GetPublicationsDtoAsync(int pageIndex, int pageSize, string? searchString = null, string sortBy = SortByVariables.date, CancellationToken ct = default);

    /// <summary>
    /// Получает указанное количество публикаций указанного автора в базе и преобразует в <see cref="PublicationDto"/>.
    /// </summary>
    /// <remarks>
    /// <para>Вызывающий метод должен предоставить валидные, не пустые данные для <paramref name="count"/>.</para>
    /// <para>Для валидации <paramref name="count"/> используется <see cref="IValidator{GetPublicationsDto}"/>.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="authorId"/> является <see cref="Guid.Empty"/></term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="count"/> невалиден</term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// Возможные ошибки сервиса:
    /// <list type="bullet">
    /// <item>
    /// <term>Автор не найден</term>
    /// <description><see cref="ErrorMessages.AuthorNotFound"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="count">Количество публикаций.</param>
    /// <param name="authorId">Id автора (пользователя).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="InvalidOperationException">Если <paramref name="authorId"/> является <see cref="Guid.Empty"/> или если <paramref name="count"/> невалиден.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see cref="ServiceResult{IEnumerable{PublicationDto}}"/>, результат сервиса с <see cref="IEnumerable{PublicationDto}"/>, если публикации найдены.</returns>
    Task<ServiceResult<IEnumerable<PublicationDto>>> GetPublicationsDtoAsync(int count, Guid authorId, CancellationToken ct = default);

    /// <summary>
    /// Получает DTO-модель публикации по предоставленному Id публикации.
    /// </summary>
    /// <remarks>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="publicationId"/> является <see cref="Guid.Empty"/></term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// Возможные ошибки сервиса:
    /// <list type="bullet">
    /// <item>
    /// <term>Публикация не найдена</term>
    /// <description><see cref="ErrorMessages.PublicationNotFound"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="publicationId">Id публикации.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="InvalidOperationException">Если <paramref name="publicationId"/> является <see cref="Guid.Empty"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see cref="ServiceResult{PublicationDto}"/>, результат сервиса с <see cref="PublicationDto"/>, если публикация найдена.</returns>
    Task<ServiceResult<PublicationDto>> GetPublicationDtoAsync(Guid publicationId, CancellationToken ct = default);

    /// <summary>
    /// Получает полную DTO-модель публикации по предоставленному Id публикации.
    /// </summary>
    /// <remarks>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="publicationId"/> является <see cref="Guid.Empty"/></term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// Возможные ошибки сервиса:
    /// <list type="bullet">
    /// <item>
    /// <term>Публикация не найдена</term>
    /// <description><see cref="ErrorMessages.PublicationNotFound"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="publicationId">Id публикации.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="InvalidOperationException">Если <paramref name="publicationId"/> является <see cref="Guid.Empty"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see cref="ServiceResult{PublicationFullDto}"/>, результат сервиса с <see cref="PublicationFullDto"/>, если публикация найдена.</returns>
    Task<ServiceResult<PublicationFullDto>> GetPublicationFullDtoAsync(Guid publicationId, CancellationToken ct = default);

    /// <summary>
    /// Получает указанное количество авторов в базе и преобразует в <see cref="AuthorDto"/>.
    /// </summary>
    /// <remarks>
    /// <para>Вызывающий метод должен предоставить валидные, не пустые данные для <paramref name="count"/>.</para>
    /// <para>Для валидации <paramref name="count"/> используется <see cref="IValidator{GetAuthorsDto}"/>.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="count"/> невалиден</term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="count">Количество авторов.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="InvalidOperationException">Если <paramref name="count"/> невалиден.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see cref="ServiceResult{IEnumerable{GetAuthorsDto}}"/>, результат сервиса с <see cref="IEnumerable{GetAuthorsDto}"/>, если авторы найдены.</returns>
    Task<IEnumerable<AuthorDto>> GetAuthorsDtoAsync(int count, CancellationToken ct = default);

    /// <summary>
    /// Частично или полностью обновляет данные публикации в базе по <see cref="UpdatePublicationDto"/>.
    /// </summary>
    /// <remarks>
    /// <para>Вызывающий метод должен предоставить валидные, не пустые данные для <paramref name="updatePublicationDto"/>.</para>
    /// <para>Для валидации <paramref name="updatePublicationDto"/> используется <see cref="IValidator{UpdatePublicationDto}"/>.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="updatePublicationDto"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="userId"/> является <see cref="Guid.Empty"/></term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="updatePublicationDto"/> невалиден</term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если после изменений данных сущности <see cref="Publication"/>, сущность окажется невалидна, изменения не последуют</term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если возник конфликт параллельности</term>
    /// <description>исключение <see cref="DbUpdateConcurrencyException"/> | <see cref="DbUpdateException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// Возможные ошибки сервиса:
    /// <list type="bullet">
    /// <item>
    /// <term>Автор не найден</term>
    /// <description><see cref="ErrorMessages.AuthorNotFound"/>.</description>
    /// </item>
    /// <item>
    /// <term>Публикация не найдена</term>
    /// <description><see cref="ErrorMessages.PublicationNotFound"/>.</description>
    /// </item>
    /// <item>
    /// <term>Пользователь не является автором этой публикации</term>
    /// <description><see cref="ErrorMessages.UserIsNotAuthorOfThisPublication"/>.</description>
    /// </item>
    /// <item>
    /// <term>Не обнаружено изменений</term>
    /// <description><see cref="ErrorMessages.NoChangesDetected"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="userId">Id пользователя.</param>
    /// <param name="updatePublicationDto">DTO-модель обновления публикации.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="updatePublicationDto"/> <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Если <paramref name="userId"/> является <see cref="Guid.Empty"/> или если <paramref name="updatePublicationDto"/> невалиден или если после изменений данных сущности <see cref="Publication"/>, сущность окажется невалидна.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns><see cref="ServiceResult"/>, результат сервиса.</returns>
    Task<ServiceResult> UpdatePublicationAsync(Guid userId, UpdatePublicationDto updatePublicationDto, CancellationToken ct = default);

    /// <summary>
    /// Частично или полностью обновляет данные публикации в базе по <see cref="UpdatePublicationFullDto"/>.
    /// </summary>
    /// <remarks>
    /// <para>Вызывающий метод должен предоставить валидные, не пустые данные для <paramref name="updatePublicationFullDto"/>.</para>
    /// <para>Для валидации <paramref name="updatePublicationFullDto"/> используется <see cref="IValidator{UpdatePublicationFullDto}"/>.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="updatePublicationFullDto"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="publicationId"/> является <see cref="Guid.Empty"/></term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="updatePublicationFullDto"/> невалиден</term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если после изменений данных сущности <see cref="Publication"/>, сущность окажется невалидна, изменения не последуют</term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если возник конфликт параллельности</term>
    /// <description>исключение <see cref="DbUpdateConcurrencyException"/> | <see cref="DbUpdateException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// Возможные ошибки сервиса:
    /// <list type="bullet">
    /// <item>
    /// <term>Публикация не найдена</term>
    /// <description><see cref="ErrorMessages.PublicationNotFound"/>.</description>
    /// </item>
    /// <item>
    /// <term>Не обнаружено изменений</term>
    /// <description><see cref="ErrorMessages.NoChangesDetected"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="publicationId">Id публикации.</param>
    /// <param name="updatePublicationFullDto">Полная DTO-модель обновления публикации.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="updatePublicationFullDto"/> <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Если <paramref name="publicationId"/> является <see cref="Guid.Empty"/> или если <paramref name="updatePublicationFullDto"/> невалиден или если после изменений данных сущности <see cref="Publication"/>, сущность окажется невалидна.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns><see cref="ServiceResult"/>, результат сервиса.</returns>
    Task<ServiceResult> UpdatePublicationAsync(Guid publicationId, UpdatePublicationFullDto updatePublicationFullDto, CancellationToken ct = default);

    /// <summary>
    /// Создаёт публикацию в базе по <see cref="CreatePublicationDto"/>.
    /// </summary>
    /// <remarks>
    /// <para>Вызывающий метод должен предоставить валидные, не пустые данные для <paramref name="createPublicationDto"/>.</para>
    /// <para>Для валидации <paramref name="createPublicationDto"/> используется <see cref="IValidator{CreatePublicationDto}"/>.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="createPublicationDto"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="userId"/> является <see cref="Guid.Empty"/></term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="createPublicationDto"/> невалиден</term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если перед записью сущности <see cref="Publication"/> в базу, сущность окажется невалидна, изменения не последуют</term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если возник конфликт параллельности</term>
    /// <description>исключение <see cref="DbUpdateConcurrencyException"/> | <see cref="DbUpdateException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// Возможные ошибки сервиса:
    /// <list type="bullet">
    /// <item>
    /// <term>Пользователь не найден</term>
    /// <description><see cref="ErrorMessages.UserNotFound"/>.</description>
    /// </item>
    /// <item>
    /// <term>У пользователя не подтверждена электронная почта</term>
    /// <description><see cref="ErrorMessages.UserHasNotConfirmedEmail"/>.</description>
    /// </item>
    /// <item>
    /// <term>У пользователя не подтверждён телефонный номер</term>
    /// <description><see cref="ErrorMessages.UserHasNotConfirmedPhoneNumber"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="userId">Id пользователя.</param>
    /// <param name="createPublicationDto">DTO-модель создания публикации.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="createPublicationDto"/> <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Если <paramref name="userId"/> является <see cref="Guid.Empty"/> или если <paramref name="createPublicationDto"/> невалиден или если после изменений данных сущности <see cref="Publication"/>, сущность окажется невалидна.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns><see cref="ServiceResult"/>, результат сервиса с <see cref="PublicationDto"/>.</returns>
    Task<ServiceResult<PublicationDto>> CreatePublicationAsync(Guid userId, CreatePublicationDto createPublicationDto, CancellationToken ct = default);

    /// <summary>
    /// Удаляет публикацию из базы по указанному Id.
    /// </summary>
    /// <remarks>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="userId"/> или <paramref name="publicationId"/> является <see cref="Guid.Empty"/></term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если возник конфликт параллельности</term>
    /// <description>исключение <see cref="DbUpdateConcurrencyException"/> | <see cref="DbUpdateException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// Возможные ошибки сервиса:
    /// <list type="bullet">
    /// <item>
    /// <term>Пользователь не найден</term>
    /// <description><see cref="ErrorMessages.UserNotFound"/>.</description>
    /// </item>
    /// <item>
    /// <term>Публикация не найдена</term>
    /// <description><see cref="ErrorMessages.PublicationNotFound"/>.</description>
    /// </item>
    /// <item>
    /// <term>Пользователь не является автором этой публикации</term>
    /// <description><see cref="ErrorMessages.UserIsNotAuthorOfThisPublication"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="userId">Id пользователя.</param>
    /// <param name="publicationId">Id публикации.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="InvalidOperationException">Если <paramref name="userId"/> или <paramref name="publicationId"/> является <see cref="Guid.Empty"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns><see cref="ServiceResult"/>, результат сервиса.</returns>
    Task<ServiceResult> DeletePublicationAsync(Guid userId, Guid publicationId, CancellationToken ct = default);

    /// <summary>
    /// Удаляет публикацию из базы по указанному Id.
    /// </summary>
    /// <remarks>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="publicationId"/> является <see cref="Guid.Empty"/></term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если возник конфликт параллельности</term>
    /// <description>исключение <see cref="DbUpdateConcurrencyException"/> | <see cref="DbUpdateException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// Возможные ошибки сервиса:
    /// <list type="bullet">
    /// <item>
    /// <term>Публикация не найдена</term>
    /// <description><see cref="ErrorMessages.PublicationNotFound"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="publicationId">Id публикации.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="InvalidOperationException">Если <paramref name="publicationId"/> является <see cref="Guid.Empty"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns><see cref="ServiceResult"/>, результат сервиса.</returns>
    Task<ServiceResult> DeletePublicationAsync(Guid publicationId, CancellationToken ct = default);

    /// <summary>
    /// Удаляет все публикации пользователя из базы.
    /// </summary>
    /// <remarks>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="userId"/> является <see cref="Guid.Empty"/></term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если возник конфликт параллельности</term>
    /// <description>исключение <see cref="DbUpdateConcurrencyException"/> | <see cref="DbUpdateException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// Возможные ошибки сервиса:
    /// <list type="bullet">
    /// <item>
    /// <term>Пользователь не найден</term>
    /// <description><see cref="ErrorMessages.UserNotFound"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="userId">Id пользователя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="InvalidOperationException">Если <paramref name="userId"/> является <see cref="Guid.Empty"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns><see cref="ServiceResult"/>, результат сервиса.</returns>
    Task<ServiceResult> DeletePublicationsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Определяет является ли указанный пользователь автором указанной публикации.
    /// </summary>
    /// <param name="userId">Id пользователя.</param>
    /// <param name="publicationId">Id публикации.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see langword="true"/>, если является.</returns>
    Task<bool> IsAuthorThisPublicationAsync(Guid userId, Guid publicationId, CancellationToken ct = default);
}