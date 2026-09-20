namespace CRUD.WebApi.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> расширения Option'ов.
/// </summary>
public static class OptionsExtensions
{
    /// <summary>
    /// Заполняет опции из <see cref="Utility.Options"/>, беря данные из <paramref name="configuration"/>.
    /// </summary>
    public static IServiceCollection AddOptions(this IServiceCollection services, IConfiguration configuration)
    {
        var optionsProgramSection = configuration.GetSection(ProgramOptions.SectionName);
        services.Configure<ProgramOptions>(optionsProgramSection); // Заполняем ProgramOptions

        var optionsS3Section = configuration.GetSection(S3Options.SectionName);
        services.Configure<S3Options>(optionsS3Section); // Заполняем S3Options

        var optionsS3InitializerSection = configuration.GetSection(S3InitializerOptions.SectionName);
        services.Configure<S3InitializerOptions>(optionsS3InitializerSection); // Заполняем S3InitializerOptions

        var optionsEmailSenderSection = configuration.GetSection(EmailSenderOptions.SectionName);
        services.Configure<EmailSenderOptions>(optionsEmailSenderSection); // Заполняем EmailSenderOptions

        var optionsSmsSenderSection = configuration.GetSection(SmsSenderOptions.SectionName);
        services.Configure<SmsSenderOptions>(optionsSmsSenderSection); // Заполняем SmsSenderOptions

        var optionsTelegramIntegrationSection = configuration.GetSection(TelegramIntegrationOptions.SectionName);
        services.Configure<TelegramIntegrationOptions>(optionsTelegramIntegrationSection); // Заполняем TelegramIntegrationOptions

        var optionsPayManagerSection = configuration.GetSection(PayManagerOptions.SectionName);
        services.Configure<PayManagerOptions>(optionsPayManagerSection); // Заполняем PayManagerOptions

        var optionsOAuthMailRuSection = configuration.GetSection(OAuthMailRuOptions.SectionName);
        services.Configure<OAuthMailRuOptions>(optionsOAuthMailRuSection); // Заполняем OAuthMailRuOptions

        var optionsRateLimiterSection = configuration.GetSection(RateLimiterOptions.SectionName);
        services.Configure<RateLimiterOptions>(optionsRateLimiterSection) // Заполняем RateLimiterOptions
            .AddOptionsWithValidateOnStart<RateLimiterOptions>().ValidateDataAnnotations().ValidateOnStart(); // И валидируем через атрибуты DataAnnotations при запуске

        var optionsMetricsSection = configuration.GetSection(MetricsOptions.SectionName);
        services.Configure<MetricsOptions>(optionsMetricsSection); // Заполняем MetricsOptions

        var optionsClientsSection = configuration.GetSection(ClientsOptions.SectionName);
        services.Configure<ClientsOptions>(optionsClientsSection); // Заполняем ClientsOptions

        var optionsProxiesOptionsSection = configuration.GetSection(ProxiesOptions.SectionName);
        services.Configure<ProxiesOptions>(optionsProxiesOptionsSection); // Заполняем ProxiesOptions

        // К сожалению, нет возможность провалидировать опции при изменении "на лету", точнее провалидировать можно (только если всё удачно спарсится),
        // а вот, если будет введено not parsing значение, то исключение выбросится только при попытке обратиться к полю через .CurrentValue
        // https://github.com/dotnet/runtime/issues/44381

        var optionsAuthSection = configuration.GetSection(AuthOptions.SectionName);
        services.Configure<AuthOptions>(optionsAuthSection); // Заполняем AuthOptions

        var optionsAuthWebApiSection = configuration.GetSection(AuthWebApiOptions.SectionName);
        services.Configure<AuthWebApiOptions>(optionsAuthWebApiSection); // Заполняем AuthWebApiOptions

        var optionsAuthEmailSenderSection = configuration.GetSection(AuthEmailSenderOptions.SectionName);
        services.Configure<AuthEmailSenderOptions>(optionsAuthEmailSenderSection); // Заполняем AuthEmailSenderOptions

        var optionsSaveLogsToS3BackgroundServiceSection = configuration.GetSection(SaveLogsToS3BackgroundServiceOptions.SectionName);
        services.Configure<SaveLogsToS3BackgroundServiceOptions>(optionsSaveLogsToS3BackgroundServiceSection); // Заполняем SaveLogsToS3BackgroundServiceOptions

        var optionsDeleteExpiredRequestsBackgroundServiceSection = configuration.GetSection(DeleteExpiredRequestsBackgroundServiceOptions.SectionName);
        services.Configure<DeleteExpiredRequestsBackgroundServiceOptions>(optionsDeleteExpiredRequestsBackgroundServiceSection); // Заполняем DeleteExpiredRequestsBackgroundServiceOptions

        var optionsRevokeExpiredRefreshTokensBackgroundServiceSection = configuration.GetSection(RevokeExpiredRefreshTokensBackgroundServiceOptions.SectionName);
        services.Configure<RevokeExpiredRefreshTokensBackgroundServiceOptions>(optionsRevokeExpiredRefreshTokensBackgroundServiceSection); // Заполняем RevokeExpiredRefreshTokensBackgroundServiceOptions

        var optionsChangePasswordRequestSection = configuration.GetSection(ChangePasswordRequestOptions.SectionName);
        services.Configure<ChangePasswordRequestOptions>(optionsChangePasswordRequestSection); // Заполняем ChangePasswordRequestOptions

        var optionsConfirmEmailRequestSection = configuration.GetSection(ConfirmEmailRequestOptions.SectionName);
        services.Configure<ConfirmEmailRequestOptions>(optionsConfirmEmailRequestSection); // Заполняем ConfirmEmailRequestOptions

        var optionsVerificationPhoneNumberRequestSection = configuration.GetSection(VerificationPhoneNumberRequestOptions.SectionName);
        services.Configure<VerificationPhoneNumberRequestOptions>(optionsVerificationPhoneNumberRequestSection); // Заполняем VerificationPhoneNumberRequestOptions

        var optionsAvatarManagerSection = configuration.GetSection(AvatarManagerOptions.SectionName);
        services.Configure<AvatarManagerOptions>(optionsAvatarManagerSection); // Заполняем AvatarManagerOptions

        return services;
    }
}