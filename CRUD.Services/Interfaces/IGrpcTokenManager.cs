namespace CRUD.Services.Interfaces;

/// <summary>
/// Сервис для работы с gRPC токенами.
/// </summary>
/// <remarks>
/// <para>Сервис должен быть зарегистрирован, как <c>Scoped</c>, чтобы токен правильно переиспользовался.</para>
/// <para>За один HTTP запрос, может быть несколько вызовов gRPC и у них будет использоваться один и тот же токен за счёт <c>Scoped</c>.</para>
/// </remarks>
public interface IGrpcTokenManager
{
    /// <summary>
    /// Генерирует токен аутентификации для микросервиса gRPC EmailSender.
    /// </summary>
    /// <returns>Сгенерированный токен.</returns>
    string GenerateAuthEmailSenderToken();
}