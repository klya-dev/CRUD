using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CRUD.Services;

public class ChangePasswordRequestManager : IChangePasswordRequestManager
{
    private readonly ApplicationDbContext _db;
    private readonly ITokenManager _tokenManager;
    private readonly IValidator<ChangePasswordRequest> _changePasswordRequestValidator;
    private readonly ChangePasswordRequestOptions _options;
    private readonly IQueueEmail _queueEmail;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ChangePasswordRequestManager(ApplicationDbContext db, ITokenManager tokenManager, IValidator<ChangePasswordRequest> changePasswordRequestValidator, IOptions<ChangePasswordRequestOptions> options, IQueueEmail queueEmail, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _tokenManager = tokenManager;
        _changePasswordRequestValidator = changePasswordRequestValidator;
        _options = options.Value;
        _queueEmail = queueEmail;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ServiceResult> AddTokenToDatabaseAndSendLetterAsync(Guid userId, string email, string languageCode, string newHashedPassword, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(languageCode);
        ArgumentNullException.ThrowIfNull(newHashedPassword);

        // Пустой GUID
        if (userId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Если есть прошлый токен
        var changePasswordRequestsFromDb = await _db.ChangePasswordRequests.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (changePasswordRequestsFromDb != null)
        {
            // И с момента создания запроса прошлого токена не прошло определённое время
            if (changePasswordRequestsFromDb.IsTimeout(_options, out var timeout))
                return ServiceResult.Fail(ErrorMessages.LetterAlreadySent, args: timeout.Minutes);
            else // Удаляем прошлый токен
            {
                _db.ChangePasswordRequests.Remove(changePasswordRequestsFromDb);
                await _db.SaveChangesAsync(ct);
            }
        }

        // Генерируем токен
        string token = _tokenManager.GenerateUniqueToken();

        var createdAt = DateTime.UtcNow;

        // Создаём запрос
        var changePasswordRequest = new ChangePasswordRequest()
        {
            UserId = userId,
            HashedNewPassword = newHashedPassword,
            Token = token,
            CreatedAt = createdAt,
            Expires = createdAt.Add(_options.Expires),
        };

        // Проверка валидности данных перед записью в базу
        var validationResultChangePasswordRequest = await _changePasswordRequestValidator.ValidateAsync(changePasswordRequest, ct);
        if (!validationResultChangePasswordRequest.IsValid)
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(ChangePasswordRequest), validationResultChangePasswordRequest.Errors));

        // Записываем токен в базу
        await _db.ChangePasswordRequests.AddAsync(changePasswordRequest, ct);

        // Данные письма
        var letter = EmailLetters.GetLetter(EmailLetters.ChangePasswordRequest, email, languageCode, _httpContextAccessor.GetBaseUrl(), token);

        // Добавляем письмо в очередь
        await _queueEmail.EnqueueAsync(letter, ct);

        await _db.SaveChangesAsync(CancellationToken.None); // Если уж отправили письмо, то сохраняем без отката

        return ServiceResult.Success();
    }
}