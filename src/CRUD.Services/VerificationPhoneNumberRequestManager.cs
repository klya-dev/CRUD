using Microsoft.AspNetCore.Http;

namespace CRUD.Services;

/// <inheritdoc cref="IVerificationPhoneNumberRequestManager"/>
public sealed class VerificationPhoneNumberRequestManager : IVerificationPhoneNumberRequestManager
{
    private readonly ApplicationDbContext _db;
    private readonly ITokenManager _tokenManager;
    private readonly VerificationPhoneNumberRequestOptions _options;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITelegramIntegrationManager _telegramIntegrationManager;
    private readonly ISmsSender _smsSender;

    public VerificationPhoneNumberRequestManager(ApplicationDbContext db, ITokenManager tokenManager, IOptions<VerificationPhoneNumberRequestOptions> options, IHttpContextAccessor httpContextAccessor, ITelegramIntegrationManager telegramIntegrationManager, ISmsSender smsSender)
    {
        _db = db;
        _tokenManager = tokenManager;
        _options = options.Value;
        _httpContextAccessor = httpContextAccessor;
        _telegramIntegrationManager = telegramIntegrationManager;
        _smsSender = smsSender;
    }

    public async Task<ServiceResult> AddCodeToDatabaseAndSendMessageAsync(Guid userId, string phoneNumber, string languageCode, MessageType messageType = MessageType.Telegram, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(phoneNumber);
        ArgumentNullException.ThrowIfNull(languageCode);

        // Пустой GUID
        if (userId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Если есть прошлый код
        var verificationPhoneNumberRequestFromDb = await _db.VerificationPhoneNumberRequests.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (verificationPhoneNumberRequestFromDb != null)
        {
            // И с момента создания запроса прошлого кода не прошло определённое время
            if (verificationPhoneNumberRequestFromDb.IsTimeout(_options, out var timeout))
                return ServiceResult.Fail(ErrorMessages.CodeAlreadySent, args: timeout.Minutes);
            else // Удаляем прошлый код
            {
                _db.VerificationPhoneNumberRequests.Remove(verificationPhoneNumberRequestFromDb);
                await _db.SaveChangesAsync(ct);
            }
        }

        // Генерируем код подтверждения
        var code = _tokenManager.GenerateCode(_options.LengthCode);

        var createdAt = DateTime.UtcNow;

        // Создаём запрос
        var verificationPhoneNumberRequest = new VerificationPhoneNumberRequest()
        {
            UserId = userId,
            Code = code,
            CreatedAt = createdAt,
            Expires = createdAt.Add(_options.Expires),
        };

        // Записываем токен в базу
        await _db.VerificationPhoneNumberRequests.AddAsync(verificationPhoneNumberRequest, ct);

        // Отправляем код (Телеграм или СМС)
        switch (messageType)
        {
            case MessageType.Sms:
                // Данные сообщения
                var message = PhoneMessages.GetMessage(PhoneMessages.VerificatePhoneNumber, languageCode, _httpContextAccessor.GetBaseUrl(), code);

                // Отправляем код
                await _smsSender.SendSmsAsync(phoneNumber, message, ct);
                break;

            case MessageType.Telegram:
                await _telegramIntegrationManager.SendVerificationCodeTelegramAsync(phoneNumber, code, ct);
                break;

            default:
                break;
        }

        await _db.SaveChangesAsync(CancellationToken.None); // Есть уж отправили код, то и сохраняем без отмены

        return ServiceResult.Success();
    }
}