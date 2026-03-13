using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CRUD.Services;

/// <inheritdoc cref="IVerificationPhoneNumberRequestManager"/>
public class VerificationPhoneNumberRequestManager : IVerificationPhoneNumberRequestManager
{
    private readonly ApplicationDbContext _db;
    private readonly ITokenManager _tokenManager;
    private readonly IValidator<VerificationPhoneNumberRequest> _verificationPhoneNumberRequestValidator;
    private readonly VerificationPhoneNumberRequestOptions _options;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITelegramIntegrationManager _telegramIntegrationManager;
    private readonly ISmsSender _smsSender;

    public VerificationPhoneNumberRequestManager(ApplicationDbContext db, ITokenManager tokenManager, IValidator<VerificationPhoneNumberRequest> verificationPhoneNumberRequestValidator, IOptions<VerificationPhoneNumberRequestOptions> options, IHttpContextAccessor httpContextAccessor, ITelegramIntegrationManager telegramIntegrationManager, ISmsSender smsSender)
    {
        _db = db;
        _tokenManager = tokenManager;
        _verificationPhoneNumberRequestValidator = verificationPhoneNumberRequestValidator;
        _options = options.Value;
        _httpContextAccessor = httpContextAccessor;
        _telegramIntegrationManager = telegramIntegrationManager;
        _smsSender = smsSender;
    }

    public async Task<ServiceResult> AddCodeToDatabaseAndSendSmsAsync(Guid userId, string phoneNumber, string languageCode, bool isTelegram = true, CancellationToken ct = default)
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

        // Проверка валидности данных перед записью в базу
        var validationResult = await _verificationPhoneNumberRequestValidator.ValidateAsync(verificationPhoneNumberRequest, ct);
        if (!validationResult.IsValid)
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(VerificationPhoneNumberRequest), validationResult.Errors));

        // Записываем токен в базу
        await _db.VerificationPhoneNumberRequests.AddAsync(verificationPhoneNumberRequest, ct);

        // Отправляем код (Телеграм или СМС)
        if (isTelegram)
            await _telegramIntegrationManager.SendVerificationCodeTelegramAsync(phoneNumber, code, ct);
        else
        {
            // Данные сообщения
            var message = PhoneMessages.GetMessage(PhoneMessages.VerificatePhoneNumber, languageCode, _httpContextAccessor.GetBaseUrl(), code);

            // Отправляем код
            await _smsSender.SendSmsAsync(phoneNumber, message, ct);
        }

        await _db.SaveChangesAsync(CancellationToken.None); // Есть уж отправили код, то и сохраняем без отмены

        return ServiceResult.Success();
    }
}