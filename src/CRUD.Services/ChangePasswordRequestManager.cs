using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;
using System.Text.Json;

namespace CRUD.Services;

public sealed class ChangePasswordRequestManager : IChangePasswordRequestManager
{
    // Назначение протектора (уникальное название для этого кейса)
    public const string Purpose = "User.PasswordChange.Confirmation.v1";

    private readonly ChangePasswordRequestOptions _options;
    private readonly IQueueEmail _queueEmail;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITimeLimitedDataProtector _protector;
    private readonly HybridCache _cache;

    public ChangePasswordRequestManager(IOptions<ChangePasswordRequestOptions> options, IQueueEmail queueEmail, IHttpContextAccessor httpContextAccessor, IDataProtectionProvider provider, HybridCache cache)
    {
        _options = options.Value;
        _queueEmail = queueEmail;
        _httpContextAccessor = httpContextAccessor;
        _cache = cache;

        // Создаём протектор с ограничением времени
        _protector = provider.CreateProtector(Purpose).ToTimeLimitedDataProtector();
    }

    public async Task<ServiceResult> AddTokenToStorageAndSendLetterAsync(Guid userId, string email, string languageCode, string newHashedPassword, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(languageCode);
        ArgumentNullException.ThrowIfNull(newHashedPassword);

        // Пустой GUID
        if (userId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        string cacheKey = $"{CacheKeys.RateLimitSendEmailPasswordChange}-{userId}";

        // Пытаемся получить время отправки последнего письма на смену пароля из кэша
        // Если ключа нет, вернется дефолтное значение DateTime (DateTime.MinValue)
        DateTime createdAt = await _cache.GetOrCreateAsync(
            key: cacheKey,
            factory: _ => ValueTask.FromResult(DateTime.MinValue), // Заглушка, если ключа нет
            cancellationToken: ct
        );

        // Если время валидное (не дефолтное), проверяем таймаут
        if (createdAt != DateTime.MinValue)
        {
            // С момента создания прошлого токена не прошло определённое время
            if (ChangePasswordPayload.IsTimeout(_options, createdAt, out TimeSpan timeout))
                return ServiceResult.Fail(ErrorMessages.LetterAlreadySent, args: timeout);
        }

        // Полезная нагрузка токена
        var payload = new ChangePasswordPayload() { UserId = userId, HashedNewPassword = newHashedPassword };

        // Сериализируем в Json
        string payloadJson = JsonSerializer.Serialize(payload);

        // Шифруем полезную нагрузку (создаём токен) со сроком жизни
        string token = _protector.Protect(payloadJson, _options.Expires);

        // Данные письма
        var letter = EmailLetters.GetLetter(EmailLetters.ChangePasswordRequest, email, languageCode, _httpContextAccessor.GetBaseUrl(), token);

        // Добавляем письмо в очередь
        await _queueEmail.EnqueueAsync(letter, ct);

        // Добавляем время отправки письма в кэш
        var cacheOptions = new HybridCacheEntryOptions
        {
            Expiration = _options.Timeout, // Время жизни в распределенном кэше (L2)
            LocalCacheExpiration = _options.Timeout // Время жизни в локальной памяти (L1)
        };

        await _cache.SetAsync(cacheKey, DateTime.UtcNow, cacheOptions, cancellationToken: ct);

        return ServiceResult.Success();
    }
}