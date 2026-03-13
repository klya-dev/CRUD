namespace CRUD.WebApi.Helpers;

/// <summary>
/// Донастройка <see cref="JwtBearerOptions"/>.
/// </summary>
/// <remarks>
/// <para>Устанавливает значения для: <see cref="TokenValidationParameters.ValidIssuer"/>, <see cref="TokenValidationParameters.ValidAudience"/>, <see cref="TokenValidationParameters.IssuerSigningKey"/>.</para>
/// <para>Значения берутся из <see cref="IOptionsMonitor{TOptions}"/>, где <see cref="TOptions"/> - <see cref="AuthOptions"/> и <see cref="AuthWebApiOptions"/>.</para>
/// <para>При изменении конфигурации срабатывает <see cref="IOptionsMonitor{TOptions}.OnChange(Action{TOptions, string?})"/>, указанные значения обновляются.</para>
/// </remarks>
public class PostConfigureJwtBearerOptions : IPostConfigureOptions<JwtBearerOptions>
{
    // Т.е при изменении конфигурации Auth/AuthWebApi всё грамотно меняется в делегате (JwtBearerOptions, который в Program.cs)

    private readonly IOptionsMonitor<AuthOptions> _authOptions;
    private readonly IOptionsMonitor<AuthWebApiOptions> _authWebApiOptions;

    public PostConfigureJwtBearerOptions(IOptionsMonitor<AuthOptions> authOptions, IOptionsMonitor<AuthWebApiOptions> authWebApiOptions)
    {
        _authOptions = authOptions;
        _authWebApiOptions = authWebApiOptions;
    }

    public void PostConfigure(string? name, JwtBearerOptions options)
    {
        // Устанавливаем значения при запуске приложения (из конфигурации Auth и AuthWebApi)
        SetAuthParameters(options, _authOptions.CurrentValue);
        SetAuthWebApiParameters(options, _authWebApiOptions.CurrentValue);

        // Устанавливаем значения после изменения конфигурации Auth
        _authOptions.OnChange(newConfig =>
        {
            SetAuthParameters(options, newConfig);
        });

        // Устанавливаем значения после изменения конфигурации AuthWebApi
        _authWebApiOptions.OnChange(newConfig =>
        {
            SetAuthWebApiParameters(options, newConfig);
        });
    }

    /// <summary>
    /// Устанавливает значения <see cref="JwtBearerOptions"/>'у по <see cref="AuthOptions"/>.
    /// </summary>
    /// <remarks>
    /// <para>Устанавливаемые значения: <see cref="TokenValidationParameters.IssuerSigningKey"/>, <see cref="TokenValidationParameters.ValidIssuer"/>.</para>
    /// <para>Публичный ключ достаётся из <see cref="AuthOptions.GetPublicKey(bool)"/> без кэширования.</para>
    /// </remarks>
    /// <param name="options">Текущий <see cref="JwtBearerOptions"/>.</param>
    /// <param name="authOptions">Обновлённые опции.</param>
    private static void SetAuthParameters(JwtBearerOptions options, AuthOptions authOptions)
    {
        // Получаем публичный ключ (не кэшированное значение)
        var publicKey = authOptions.GetPublicKey(getCached: false);
        publicKey.KeyId = authOptions.KeyId;

        options.TokenValidationParameters.IssuerSigningKey = publicKey; // Установка ключа безопасности (публичного ключа)
        options.TokenValidationParameters.ValidIssuer = authOptions.Issuer; // Строка, представляющая издателя
    }

    /// <summary>
    /// Устанавливает значения <see cref="JwtBearerOptions"/>'у по <see cref="AuthOptions"/>.
    /// </summary>
    /// <remarks>
    /// <para>Устанавливаемые значения: <see cref="TokenValidationParameters.ValidAudience"/>.</para>
    /// </remarks>
    /// <param name="options">Текущий <see cref="JwtBearerOptions"/>.</param>
    /// <param name="authWebApiOptions">Обновлённые опции.</param>
    private static void SetAuthWebApiParameters(JwtBearerOptions options, AuthWebApiOptions authWebApiOptions)
    {
        options.TokenValidationParameters.ValidAudience = authWebApiOptions.Audience; // Установка потребителя токена
    }
}