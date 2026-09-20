namespace CRUD.WebApi.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> расширения прикладных элементов приложения.
/// </summary>
public static class ApplicationCoreExtensions
{
    /// <summary>
    /// Регистрирует прикладные валидаторы.
    /// </summary>
    public static IServiceCollection AddApplicationCoreValidators(this IServiceCollection services)
    {
        services.AddScoped<IValidator<UpdateUserDto>, UpdateUserDtoValidator>();
        services.AddScoped<IValidator<CreateUserDto>, CreateUserDtoValidator>();
        services.AddScoped<IValidator<DeleteUserDto>, DeleteUserDtoValidator>();
        services.AddScoped<IValidator<LoginDataDto>, LoginDataDtoValidator>();
        services.AddScoped<IValidator<ChangePasswordDto>, ChangePasswordDtoValidator>();
        services.AddScoped<IValidator<SetPasswordDto>, SetPasswordDtoValidator>();
        services.AddScoped<IValidator<SetRoleDto>, SetRoleDtoValidator>();
        services.AddScoped<IValidator<GetPublicationsDto>, GetPublicationsDtoValidator>();
        services.AddScoped<IValidator<GetAuthorsDto>, GetAuthorsDtoValidator>();
        services.AddScoped<IValidator<UpdatePublicationDto>, UpdatePublicationDtoValidator>();
        services.AddScoped<IValidator<UpdatePublicationFullDto>, UpdatePublicationFullDtoValidator>();
        services.AddScoped<IValidator<CreatePublicationDto>, CreatePublicationDtoValidator>();
        services.AddScoped<IValidator<ClientApiCreatePublicationDto>, ClientApiCreatePublicationDtoValidator>();
        services.AddScoped<IValidator<CreateNotificationDto>, CreateNotificationDtoValidator>();
        services.AddScoped<IValidator<CreateNotificationSelectedUsersDto>, CreateNotificationSelectedUsersDtoValidator>();
        services.AddScoped<IValidator<GetUserNotificationsDto>, GetUserNotificationsDtoValidator>();
        services.AddScoped<IValidator<GetPaginatedListDto>, GetPaginatedListDtoValidator>();
        services.AddScoped<IValidator<GetCursorPaginatedListDto>, GetCursorPaginatedListDtoValidator>();
        services.AddScoped<IValidator<OAuthCompleteRegistrationDto>, OAuthCompleteRegistrationDtoValidator>();

        return services;
    }

    /// <summary>
    /// Регистрирует прикладные сервисы бизнес-логики.
    /// </summary>
    public static IServiceCollection AddApplicationCoreServices(this IServiceCollection services, ProgramOptions programOptions)
    {
        services.AddScoped<IClientApiManager, ClientApiManager>();
        services.AddScoped<IPremiumManager, PremiumManager>();
        services.AddSingleton<IUserApiKeyManager, UserApiKeyManager>();
        services.AddScoped<IPasswordChanger, PasswordChanger>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenManager, TokenManager>();
        if (!programOptions.SkipInitializers) // Пропускаем ли инициализаторы
        {
            services.AddScoped<IDbInitializer, DbInitializer>();
            services.AddScoped<IS3Initializer, S3Initializer>();
        }
        services.AddScoped<IUserManager, UserManager>();
        services.AddScoped<IPublicationManager, PublicationManager>();
        services.AddSingleton<IS3Manager, S3Manager>();
        services.AddScoped<IAvatarManager, AvatarManager>();
        services.AddScoped<IAuthManager, AuthManager>();
        services.AddSingleton<IHtmlHelper, HtmlHelper>();
        services.AddSingleton<ISaveLogsToS3BackgroundCore, SaveLogsToS3BackgroundCore>();
        services.AddSingleton<IQueueEmail, QueueEmail>();
        services.AddSingleton<ISmsSender, SmsSender>();
        services.AddSingleton<ITelegramIntegrationManager, TelegramIntegrationManager>();
        services.AddScoped<IPayManager, PayManager>();
        services.AddScoped<IOrderUpdater, OrderUpdater>();
        services.AddScoped<IProductManager, ProductManager>();
        services.AddScoped<IOrderIssuer, OrderIssuer>();
        services.AddScoped<IOrderCreator, OrderCreator>();
        services.AddScoped<IConfirmEmailRequestManager, ConfirmEmailRequestManager>();
        services.AddScoped<IVerificationPhoneNumberRequestManager, VerificationPhoneNumberRequestManager>();
        services.AddScoped<IChangePasswordRequestManager, ChangePasswordRequestManager>();
        services.AddSingleton<IImageSingnatureChecker, ImageSingnatureChecker>();
        services.AddScoped<INotificationManager, NotificationManager>();
        services.AddScoped<IGrpcTokenManager, GrpcTokenManager>();
        services.AddScoped<IRevokeExpiredRefreshTokensBackgroundCore, RevokeExpiredRefreshTokensBackgroundCore>();
        services.AddScoped<IDeleteExpiredRequestsBackgroundCore, DeleteExpiredRequestsBackgroundCore>();
        services.AddScoped<IAuthRefreshTokenManager, AuthRefreshTokenManager>();
        services.AddSingleton<IOAuthMailRuProvider, OAuthMailRuProvider>();
        services.AddSingleton<IPremiumInformator, PremiumInformator>();

        services.AddSingleton<IAuthorizationHandler, LanguageDenyHandler>();

        services.AddHostedService<SaveLogsToS3BackgroundService>();
        services.AddHostedService<RevokeExpiredRefreshTokensBackgroundService>();
        services.AddHostedService<DeleteExpiredRequestsBackgroundService>();

        services.AddSingleton<ApiMeters>();

        return services;
    }
}