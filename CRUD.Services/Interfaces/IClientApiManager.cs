namespace CRUD.Services.Interfaces;

/// <summary>
/// Сервис для работы с клиентским API.
/// </summary>
public interface IClientApiManager
{
    /// <summary>
    /// Создаёт публикацию в базе по предоставленной модели, используя клиентский API-ключ.
    /// </summary>
    /// <remarks>
    /// <para>Вызывающий метод должен предоставить валидные, не пустые данные для <paramref name="clientApiCreatePublicationDto"/>.</para>
    /// <para>Для валидации <paramref name="clientApiCreatePublicationDto"/> используется <see cref="IValidator{ClientApiCreatePublicationDto}"/>.</para>
    /// <para>Для валидации <see cref="User"/> используется <see cref="IValidator{User}"/>.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="clientApiCreatePublicationDto"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="clientApiCreatePublicationDto"/> невалидна</term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если после изменений данных сущности <see cref="User"/>, сущность окажется невалидна, изменения не последуют</term>
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
    /// <term>Неверный API-ключ</term>
    /// <description><see cref="ErrorMessages.InvalidApiKey"/>.</description>
    /// </item>
    /// <item>
    /// <term>Пользователь не имеет премиума</term>
    /// <description><see cref="ErrorMessages.UserDoesNotHavePremium"/>.</description>
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
    /// <param name="clientApiCreatePublicationDto">DTO-модель для создания публикации через клиентский API-ключ.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="clientApiCreatePublicationDto"/> <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Если <paramref name="clientApiCreatePublicationDto"/> является невалидным или если после изменений данных сущности <see cref="User"/>, сущность окажется невалидна.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns><see cref="ServiceResult"/>, результат сервиса с <see cref="PublicationDto"/>.</returns>
    Task<ServiceResult<PublicationDto>> CreatePublicationAsync(ClientApiCreatePublicationDto clientApiCreatePublicationDto, CancellationToken ct = default);
}