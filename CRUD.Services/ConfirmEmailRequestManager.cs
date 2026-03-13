using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CRUD.Services;

/// <inheritdoc cref="IConfirmEmailRequestManager"/>
public class ConfirmEmailRequestManager : IConfirmEmailRequestManager
{
    private readonly ApplicationDbContext _db;
    private readonly ITokenManager _tokenManager;
    private readonly IValidator<ConfirmEmailRequest> _confirmEmailRequestValidator;
    private readonly ConfirmEmailRequestOptions _options;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IQueueEmail _queueEmail;

    public ConfirmEmailRequestManager(ApplicationDbContext db, ITokenManager tokenManager, IValidator<ConfirmEmailRequest> confirmEmailRequestValidator, IOptions<ConfirmEmailRequestOptions> options, IHttpContextAccessor httpContextAccessor, IQueueEmail queueEmail)
    {
        _db = db;
        _tokenManager = tokenManager;
        _confirmEmailRequestValidator = confirmEmailRequestValidator;
        _options = options.Value;
        _httpContextAccessor = httpContextAccessor;
        _queueEmail = queueEmail;
    }

    public async Task<ServiceResult> AddTokenToDatabaseAndSendLetterAsync(Guid userId, string email, string languageCode, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(languageCode);

        // Пустой GUID
        if (userId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Если есть прошлый токен
        var confirmEmailRequestsFromDb = await _db.ConfirmEmailRequests.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (confirmEmailRequestsFromDb != null)
        {
            // И с момента создания запроса прошлого токена не прошло определённое время
            if (confirmEmailRequestsFromDb.IsTimeout(_options, out var timeout))
                return ServiceResult.Fail(ErrorMessages.LetterAlreadySent, args: timeout.Minutes);
            else // Удаляем прошлый токен
            {
                _db.ConfirmEmailRequests.Remove(confirmEmailRequestsFromDb);
                await _db.SaveChangesAsync(ct);
            }
        }

        // Генерируем токен
        string token = _tokenManager.GenerateUniqueToken();

        var createdAt = DateTime.UtcNow;

        // Создаём запрос
        var confirmEmailRequest = new ConfirmEmailRequest()
        {
            UserId = userId,
            Token = token,
            CreatedAt = createdAt,
            Expires = createdAt.Add(_options.Expires),
        };

        // Проверка валидности данных перед записью в базу
        var validationResult = await _confirmEmailRequestValidator.ValidateAsync(confirmEmailRequest, ct);
        if (!validationResult.IsValid)
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(ConfirmEmailRequest), validationResult.Errors));

        // Записываем токен в базу
        await _db.ConfirmEmailRequests.AddAsync(confirmEmailRequest, ct);

        // Данные письма
        var letter = EmailLetters.GetLetter(EmailLetters.EmailConfirm, email, languageCode, _httpContextAccessor.GetBaseUrl(), token);

        // Добавляем письмо в очередь
        await _queueEmail.EnqueueAsync(letter, ct);

        await _db.SaveChangesAsync(CancellationToken.None); // Если уж отправили письмо, то сохраняем без отката

        return ServiceResult.Success();
    }
}