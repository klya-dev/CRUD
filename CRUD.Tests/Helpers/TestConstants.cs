#nullable enable
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Claims;

namespace CRUD.Tests.Helpers;

/// <summary>
/// Статический класс с тестовыми константами для тестов.
/// </summary>
public static class TestConstants
{
    public const string TEST_FILES_PATH = "WebApi/test_files";

    /// <summary>
    /// Дефолтная аватарка из опции <see cref="AvatarManagerOptions.DefaultAvatarPath"/>.
    /// </summary>
    /// <remarks>
    /// Использование: <c>[MemberData(nameof(TestConstants.DefaultAvatarPathObject), MemberType = typeof(TestConstants))]</c>.
    /// </remarks>
    public static TheoryData<string> DefaultAvatarPathObject =>
    [
        TestSettingsHelper.GetConfigurationValue<AvatarManagerOptions, TestMarker>(AvatarManagerOptions.SectionName)!.DefaultAvatarPath,
    ];

    /// <summary>
    /// Дефолтная аватарка из опции <see cref="AvatarManagerOptions.DefaultAvatarPath"/>.
    /// </summary>
    /// <remarks>
    /// Достаётся из <see cref="DefaultAvatarPathObject"/>.
    /// </remarks>
    public static readonly string DefaultAvatarPath = DefaultAvatarPathObject.Cast<TheoryDataRow<string>>().First();

    public const string EmptyGuidString = "00000000-0000-0000-0000-000000000000";
    public const string PublicationTitleMore64Chars = "большебольшебольшебольшебольшебольшебольшебольшебольшебольшебольше";
    public const string PublicationContent = "ContentContentContentContentContentContentContentContentContentContentContentContentContentContentContentContentContentContentContent";
    public const string PublicationContentLess128Chars = "меньшеменьшеменьшеменьшеменьшеменьшеменьшеменьшеменьшеменьшеменьшеменьшеменьшеменьшеменьшеменьшеменьшеменьшеменьшеменьшемен";
    public const string PublicationContentMore1024Chars = "большебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольшебольше";
    public const string UserApiKey = "tF3LSpcrw32EUey0qW4exCxk6wa8qvBhCEB_qOhXVxarIRRP3i-WOjFAaPXVm6vqKops4tgWRoUPmrrhFDsECg31C4fQ3iY9K55Y";
    public const string UserApiKeyMore100Chars = "tF3LSpcrw32EUey0qW4exCxk6wa8qvBhCEB_qOhXVxarIRRP3i-WOjFAaPXVm6vqKops4tgWRoUPmrrhFDsECg31C4fQ3iY9K55Y1";
    public const string UserApiKeyLess100Chars = "tF3LSpcrw32EUey0qW4exCxk6wa8qvBhCEB";
    public const string Spaces100 = "                                                                                                    ";
    public const string UserApiKey2 = "aF3LSpcrw32EUey0qW4exCxk6wa8qvBhCEB_qOhXVxarIRRP3i-WOjFAaPXVm6vqKops4tgWRoUPmrrhFDsECg31C4fQ3iY9K55Y";
    public const string UserApiKey3 = "bF3LSpcrw32EUey0qW4exCxk6wa8qvBhCEB_qOhXVxarIRRP3i-WOjFAaPXVm6vqKops4tgWRoUPmrrhFDsECg31C4fQ3iY9K55Y";
    public const string UserApiKey4 = "cF3LSpcrw32EUey0qW4exCxk6wa8qvBhCEB_qOhXVxarIRRP3i-WOjFAaPXVm6vqKops4tgWRoUPmrrhFDsECg31C4fQ3iY9K55Y";
    public const string UserInvalidApiKey = "0F3LSpcrw32EUey0qW4exCxk6wa8qvBhCEB_qOhXVxarIRRP3i-WOjFAaPXVm6vqKops4tgWRoUPmrrhFDsECg31C4fQ3iY9K55Y";
    public const string UserDisposableApiKey = "GdQcR0plkfBb6ziBk0DeQRLrirPZIbJNMscm-7ZxORhkz-GjsAsAevE_mLafG18_CYnvbYjZTVTQ8t8oMNxgbJoNppbbLA46laHs";
    public const string UserDisposableApiKey2 = "adQcR0plkfBb6ziBk0DeQRLrirPZIbJNMscm-7ZxORhkz-GjsAsAevE_mLafG18_CYnvbYjZTVTQ8t8oMNxgbJoNppbbLA46laHs";
    public const string UserDisposableApiKey3 = "bdQcR0plkfBb6ziBk0DeQRLrirPZIbJNMscm-7ZxORhkz-GjsAsAevE_mLafG18_CYnvbYjZTVTQ8t8oMNxgbJoNppbbLA46laHs";
    public const string UserDisposableApiKey4 = "cdQcR0plkfBb6ziBk0DeQRLrirPZIbJNMscm-7ZxORhkz-GjsAsAevE_mLafG18_CYnvbYjZTVTQ8t8oMNxgbJoNppbbLA46laHs";
    public const string UserDisposableApiKeyMore100Chars = "GdQcR0plkfBb6ziBk0DeQRLrirPZIbJNMscm-7ZxORhkz-GjsAsAevE_mLafG18_CYnvbYjZTVTQ8t8oMNxgbJoNppbbLA46laHs1";
    public const string UserDisposableApiKeyLess100Chars = "GdQcR0plkfBb6ziBk0DeQRLrirPZIbJNMscm";
    public const string UserInvalidDisposableApiKey = "0dQcR0plkfBb6ziBk0DeQRLrirPZIbJNMscm-7ZxORhkz-GjsAsAevE_mLafG18_CYnvbYjZTVTQ8t8oMNxgbJoNppbbLA46laHs";
    public const string UserHashedPassword = "CY96YpMblMpKYgd1jSdAG7+Wa4I7S5S+KeWDq1lA7AQ=-/o8tRkGC1lQqWnMvZlA5Kw==";
    public const string UserEmail = "test@mail.ru";
    public const string UserEmailMore254Chars = "testtesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttest@mail.ru";
    public const string UserPhoneNumber = "123456789";
    public const string UserPhoneNumberMore15Chars = "1234567890123456";

    public const string VERSION = "v1";
    public const string AUTH_LOGIN_URL = "login";
    public const string AUTH_REFRESH_LOGIN_URL = "refresh-login";
    public const string AUTH_REGISTER_URL = "register";
    public const string AUTH_OAUTH_LINK_URL = "oauth/link";
    public const string AUTH_OAUTH_LOGIN_URL = "oauth/login";
    public const string AUTH_OAUTH_REGISTRATION_URL = "oauth/registration";

    public const string ADMIN_URL = "admin";
    public const string ADMIN_USERS_USER_ID_URL = "admin/users/{0}";
    public const string ADMIN_USERS_USER_ID_AVATAR_URL = "admin/users/{0}/avatar";
    public const string ADMIN_USERS_USER_ID_PASSWORD_URL = "admin/users/{0}/password";
    public const string ADMIN_USERS_USER_ID_PREMIUM_URL = "admin/users/{0}/premium";
    public const string ADMIN_USERS_USER_ID_ROLE_URL = "admin/users/{0}/role";
    public const string ADMIN_USERS_USER_ID_REFRESH_TOKENS_URL = "admin/users/{0}/refresh-tokens";

    public const string ADMIN_PUBLICATIONS_PUBLICATION_ID_URL = "admin/publications/{0}";
    public const string ADMIN_PUBLICATIONS_AUTHORS_USER_ID_URL = "admin/publications/authors/{0}";

    public const string ADMIN_NOTIFICATIONS_USERS_USER_ID_URL = "admin/notifications/users/{0}";
    public const string ADMIN_NOTIFICATIONS_URL = "admin/notifications";
    public const string ADMIN_NOTIFICATIONS_SELECTED_USERS_URL = "admin/notifications/selected-users";
    public const string ADMIN_NOTIFICATIONS_NOTIFICATIONS_ID_URL = "admin/notifications/{0}";

    public const string USERS_URL = VERSION + "/users";
    public const string USERS_USER_ID_URL = VERSION + "/users/{0}";
    public const string USERS_USER_ID_AVATAR_FILE_URL = VERSION + "/users/{0}/avatar-file";
    public const string USERS_USER_ID_AVATAR_URL_URL = VERSION + "/users/{0}/avatar-url";

    public const string USER_URL = VERSION + "/user";
    public const string USER_AVATAR_URL = VERSION + "/user/avatar";
    public const string USER_AVATAR_FILE_URL = VERSION + "/user/avatar-file";
    public const string USER_AVATAR_URL_URL = VERSION + "/user/avatar-url";
    public const string USER_PASSWORD_URL = VERSION + "/user/password";
    public const string USER_PREMIUM_URL = VERSION + "/user/premium";
    public const string USER_CONFIRMATION_EMAIL_URL = VERSION + "/user/confirmation/email";
    public const string USER_CONFIRMATION_PHONE_URL = VERSION + "/user/confirmation/phone";
    public const string USER_PUBLICATIONS_URL = VERSION + "/user/publications";
    public const string USER_NOTIFICATIONS_URL = VERSION + "/user/notifications";
    public const string USER_NOTIFICATIONS_NOTIFICATIONS_ID_READ_URL = VERSION + "/user/notifications/{0}/read";

    public const string CONFIRMATIONS_EMAIL_TOKEN_URL = VERSION + "/confirmations/email/{0}";
    public const string CONFIRMATIONS_PHONE_CODE_URL = VERSION + "/confirmations/phone/{0}";
    public const string CONFIRMATIONS_PASSWORD_TOKEN_URL = VERSION + "/confirmations/password/{0}";

    public const string PUBLICATIONS_URL = VERSION + "/publications";
    public const string PUBLICATIONS_PAGINATED_URL = VERSION + "/publications/paginated";
    public const string PUBLICATIONS_AUTHORS_URL = VERSION + "/publications/authors";
    public const string PUBLICATIONS_AUTHORS_AUTHOR_ID_URL = VERSION + "/publications/authors/{0}";
    public const string PUBLICATIONS_PUBLICATION_ID_URL = VERSION + "/publications/{0}";

    public const string CLIENT_API_PUBLICATIONS_URL = VERSION + "/client-api/publications";

    public const string WEBHOOKS_URL = "webhooks";
    public const string WEBHOOKS_PAYMENT_URL = "webhooks/payment";

    public const string HEALTHZ_URL = "healthz";

    public const string METRICS_URL = "metrics";

    public const string PUBLIC_URL = "public";
    public const string PUBLIC_README_URL = "public/readme.txt";

    public const string NOTIFICATION_HUB_URL = "notificationHub";

    /// <summary>
    /// Добавляет сгенерированный <c>Bearer</c> токен для авторизации и аутентификации.
    /// </summary>
    /// <remarks>
    /// Если <paramref name="userId"/> равен <see langword="null"/>, то значению присваивается <see cref="Guid.NewGuid"/>.
    /// </remarks>
    /// <param name="request">Запрос, к которому будет добавлен <c>Bearer</c> токен.</param>
    /// <param name="tokenManager"><see cref="ITokenManager"/> для генерации токена.</param>
    /// <param name="userId">Id пользователя.</param>
    /// <param name="name">Username пользователя.</param>
    /// <param name="role">Роль пользователя.</param>
    /// <param name="languageCode">Код языка пользователя.</param>
    /// <param name="isEmailConfirm">Подтверждена ли почта пользователя.</param>
    /// <param name="isPhoneNumberConfirm">Подтверждён ли номер телефона пользователя.</param>
    /// <param name="isPremium">Является ли пользователь премиумом.</param>
    /// <returns>Сгенерированный AccessToken.</returns>
    public static string AddBearerToken(HttpRequestMessage request, ITokenManager tokenManager, string? userId = null, string name = "username", string role = UserRoles.User, string languageCode = "ru", bool isEmailConfirm = true, bool isPhoneNumberConfirm = true, bool isPremium = false)
    {
        Claim[] claims =
        [
            new Claim(ClaimTypes.NameIdentifier, userId ?? Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, name),
            new Claim(ClaimTypes.Role, role),
            new Claim(UserClaimTypes.LanguageCode, languageCode),
            new Claim(UserClaimTypes.IsEmailConfirm, isEmailConfirm.ToString()),
            new Claim(UserClaimTypes.IsPhoneNumberConfirm, isPhoneNumberConfirm.ToString()),
            new Claim(UserClaimTypes.IsPremium, isPremium.ToString())
        ];
        var token = tokenManager.GenerateAuthResponse(claims, name).AccessToken;
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return token;
    }

    /// <summary>
    /// Добавляет заголовок <c>Idempotency-Key</c> к запросу.
    /// </summary>
    /// <remarks>
    /// Если <paramref name="idempotencyKey"/> равен <see langword="null"/>, то значению присваивается <see cref="Guid.NewGuid"/>.
    /// </remarks>
    /// <param name="idempotencyKey">Ключ идемпотентности.</param>
    /// <returns>Добавленый в запрос IdempotencyKey.</returns>
    public static string AddIdempotencyKey(HttpRequestMessage request, string? idempotencyKey = null)
    {
        idempotencyKey ??= Guid.NewGuid().ToString();
        request.Headers.Add("Idempotency-Key", idempotencyKey);

        return idempotencyKey;
    }

    /// <summary>
    /// Добавляет строку запроса <c>idmkey</c> к запросу.
    /// </summary>
    /// <remarks>
    /// Если <paramref name="idempotencyKey"/> равен <see langword="null"/>, то значению присваивается <see cref="Guid.NewGuid"/>.
    /// </remarks>
    /// <param name="idempotencyKey">Ключ идемпотентности.</param>
    /// <returns>Добавленый в запрос IdempotencyKey.</returns>
    public static string AddIdempotencyKeyQuery(HttpRequestMessage request, string? idempotencyKey = null)
    {
        ArgumentNullException.ThrowIfNull(request.RequestUri);

        idempotencyKey ??= Guid.NewGuid().ToString();

        var newUri = QueryHelpers.AddQueryString(request.RequestUri.ToString(), "idmkey", idempotencyKey);

        // Перезаписываем URI в запросе
        request.RequestUri = new Uri(newUri, UriKind.RelativeOrAbsolute);

        return idempotencyKey;
    }

    /// <summary>
    /// Сравнивает два объекта <see cref="ApiError"/> по значению.
    /// </summary>
    /// <param name="expected">Ожидаемый объект.</param>
    /// <param name="actual">Актуальный объект.</param>
    /// <returns><see langword="true"/>, если все поля равны.</returns>
    public static bool EqualsByValue(ApiError expected, ApiError actual)
    {
        // Все значения равны
        if (expected.Title == actual.Title
            && expected.Detail == actual.Detail
            && expected.Status == actual.Status
            && expected.Params == actual.Params)
            return true;

        return false;
    }

    /// <summary>
    /// Создаёт новый экземпляр <see cref="TestHttpContextAccessor"/>, со всеми зависимостями.
    /// </summary>
    /// <returns>Новый экземпляр <see cref="TestHttpContextAccessor"/>.</returns>
    public static IHttpContextAccessor CreateHttpContextAccessor()
    {
        var httpContextAccessor = new TestHttpContextAccessor();
        return httpContextAccessor;
    }

    /// <summary>
    /// Создаёт экземпляр <see cref="TestHttpClientFactory"/>, со всеми зависимостями.
    /// </summary>
    /// <returns>Экземпляр <see cref="TestHttpClientFactory"/>.</returns>
    public static TestHttpClientFactory CreateHttpClientFactory()
    {
        var httpClientFactory = new TestHttpClientFactory();
        return httpClientFactory;
    }
}