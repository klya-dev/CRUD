using System.Globalization;

namespace CRUD.Models.Validators;

/// <summary>
/// Дополнительные правила для валидаторов.
/// </summary>
/// <remarks>
/// <c>
/// RuleFor(x => x.NewPassword).NewPassword(localizer);
/// </c>
/// </remarks>
public static class ValidatorExtensions
{
    /// <summary>
    /// Только кириллица.
    /// </summary>
    /// <remarks>
    /// <para>Используется регулярное выражение "^[а-яА-ЯёЁ]+$".</para>
    /// <para>Если не указан <paramref name="localizer"/>, то используется английский вариант локализации.</para>
    /// </remarks>
    public static IRuleBuilderOptions<T, string> Cyrillic<T>(this IRuleBuilder<T, string> ruleBuilder, IValidatorsLocalizer? localizer = null)
    {
        if (localizer == null)
        {
            return ruleBuilder
                .Matches("^[а-яА-ЯёЁ]+$")
                .WithMessage(EnglishValidatorsLanguage.GetTranslation(ValidatorsLocalizerConstants.OnlyCyrillic));
        }

        return ruleBuilder
                .Matches("^[а-яА-ЯёЁ]+$")
                .WithMessage(localizer[ValidatorsLocalizerConstants.OnlyCyrillic]);
    }

    /// <summary>
    /// Только нижний регистр латиницы.
    /// </summary>
    /// <remarks>
    /// <para>Используется регулярное выражение "^[a-z]+$".</para>
    /// <para>Если не указан <paramref name="localizer"/>, то используется английский вариант локализации.</para>
    /// </remarks>
    public static IRuleBuilderOptions<T, string> SmallCaseLatin<T>(this IRuleBuilder<T, string> ruleBuilder, IValidatorsLocalizer? localizer = null)
    {
        if (localizer == null)
        {
            return ruleBuilder
                .Matches("^[a-z]+$")
                .WithMessage(EnglishValidatorsLanguage.GetTranslation(ValidatorsLocalizerConstants.OnlySmallCaseLatin));
        }

        return ruleBuilder
            .Matches("^[a-z]+$")
            .WithMessage(localizer[ValidatorsLocalizerConstants.OnlySmallCaseLatin]);
    }

    /// <summary>
    /// Только латиница, цифры, нижнее подчёркивание и тире.
    /// </summary>
    /// <remarks>
    /// <para>Используется регулярное выражение "^[a-zA-z0-9_-]+$".</para>
    /// <para>Если не указан <paramref name="localizer"/>, то используется английский вариант локализации.</para>
    /// </remarks>
    public static IRuleBuilderOptions<T, string> LatinNumbersDashes<T>(this IRuleBuilder<T, string> ruleBuilder, IValidatorsLocalizer? localizer = null)
    {
        if (localizer == null)
        {
            return ruleBuilder
                .Matches("^[a-zA-z0-9_-]+$")
                .WithMessage(EnglishValidatorsLanguage.GetTranslation(ValidatorsLocalizerConstants.OnlyLatinNumbersDashes));
        }

        return ruleBuilder
            .Matches("^[a-zA-z0-9_-]+$")
            .WithMessage(localizer[ValidatorsLocalizerConstants.OnlyLatinNumbersDashes]);
    }

    /// <summary>
    /// Только латиница, цифры и специальные символы.
    /// </summary>
    /// <remarks>
    /// <para>Используется регулярное выражение "^[a-zA-Z0-9!;%:?*_+=\-@#$^&#38;]+$".</para>
    /// <para>Если не указан <paramref name="localizer"/>, то используется английский вариант локализации.</para>
    /// </remarks>
    public static IRuleBuilderOptions<T, string> LatinNumbersSpecialCharacters<T>(this IRuleBuilder<T, string> ruleBuilder, IValidatorsLocalizer? localizer = null)
    {
        if (localizer == null)
        {
            return ruleBuilder
                .Matches(@"^[a-zA-Z0-9!;%:?*_+=\-@#$^&]+$")
                .WithMessage(EnglishValidatorsLanguage.GetTranslation(ValidatorsLocalizerConstants.OnlyLatinNumbersSpecialCharacters));
        }

        return ruleBuilder
            .Matches(@"^[a-zA-Z0-9!;%:?*_+=\-@#$^&]+$")
            .WithMessage(localizer[ValidatorsLocalizerConstants.OnlyLatinNumbersSpecialCharacters]);
    }

    /// <summary>
    /// Только цифры.
    /// </summary>
    /// <remarks>
    /// <para>Используется регулярное выражение "^[0-9]+$".</para>
    /// <para>Если не указан <paramref name="localizer"/>, то используется английский вариант локализации.</para>
    /// </remarks>
    public static IRuleBuilderOptions<T, string> Numbers<T>(this IRuleBuilder<T, string> ruleBuilder, IValidatorsLocalizer? localizer = null)
    {
        if (localizer == null)
        {
            return ruleBuilder
                .Matches(@"^[0-9]+$")
                .WithMessage(EnglishValidatorsLanguage.GetTranslation(ValidatorsLocalizerConstants.OnlyNumbers));
        }

        return ruleBuilder
            .Matches(@"^[0-9]+$")
            .WithMessage(localizer[ValidatorsLocalizerConstants.OnlyNumbers]);
    }

    /// <summary>
    /// Строка не должна состоять из символов пробела.
    /// </summary>
    /// <remarks>
    /// <example>
    /// Используются правила
    /// <code>
    /// .Must(value =>
    /// {
    ///     if (value == null)
    ///         return true;
    /// 
    ///     for (int i = 0; i &lt; value.Length; i++)
    ///         if (!char.IsWhiteSpace(value[i]))
    ///             return true;
    ///     return false;
    /// })
    /// .WithMessage();
    /// </code>
    /// </example>
    /// <para>Значение <see langword="null"/> игнорируется.</para>
    /// <para>Если не указан <paramref name="localizer"/>, то используется английский вариант локализации.</para>
    /// </remarks>
    public static IRuleBuilderOptions<T, string?> NotWhiteSpace<T>(this IRuleBuilder<T, string?> ruleBuilder, IValidatorsLocalizer? localizer = null)
    {
        var builder = ruleBuilder
            .Must(value =>
            {
                if (value == null)
                    return true;

                for (int i = 0; i < value.Length; i++)
                    if (!char.IsWhiteSpace(value[i]))
                        return true;
                return false;
            });

        if (localizer == null)
            return builder
                .WithMessage(EnglishValidatorsLanguage.GetTranslation(ValidatorsLocalizerConstants.NotWhiteSpace));

        return builder
                .WithMessage(localizer[ValidatorsLocalizerConstants.NotWhiteSpace]);
    }

    /// <summary>
    /// Разница во времени с настоящим временем должна быть не более 5 минут (никаких 1900ых годов или вчерашних дней, без далёкого будущего).
    /// </summary>
    /// <remarks>
    /// <para>Используются правила <c>.NotEmpty().InclusiveBetween(DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddMinutes(5))</c>.</para>
    /// <para>Если не указан <paramref name="localizer"/>, то используется английский вариант локализации.</para>
    /// </remarks>
    public static IRuleBuilderOptions<T, DateTime> InclusiveBetweenInMunutes<T>(this IRuleBuilder<T, DateTime> ruleBuilder, IValidatorsLocalizer? localizer = null)
    {
        if (localizer == null)
        {
            return ruleBuilder
                .NotEmpty()
                .InclusiveBetween(DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddMinutes(5)); // Разница во времени 5 минут 
        }

        return ruleBuilder
            .NotEmpty()
            .InclusiveBetween(DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddMinutes(5));
    }

    /// <summary>
    /// Правило для поля Firstname.
    /// </summary>
    /// <remarks>
    /// <para>Используются правила <c>.NotEmpty().Length(2, 32).Cyrillic().WithName()</c>.</para>
    /// <para>Если не указан <paramref name="localizer"/>, то используется английский вариант локализации.</para>
    /// </remarks>
    public static IRuleBuilderOptions<T, string> Firstname<T>(this IRuleBuilder<T, string> ruleBuilder, IValidatorsLocalizer? localizer = null)
    {
        if (localizer == null)
        {
            return ruleBuilder
                .NotEmpty()
                .Length(2, 32)
                .Cyrillic()
                .WithName(EnglishValidatorsLanguage.GetTranslation(ValidatorsLocalizerConstants.PropertyFirstname));
        }

        return ruleBuilder
            .NotEmpty()
            .Length(2, 32)
            .Cyrillic(localizer)
            .WithName(localizer[ValidatorsLocalizerConstants.PropertyFirstname]);
    }

    /// <summary>
    /// Правило для поля Username.
    /// </summary>
    /// <remarks>
    /// <para>Используются правила <c>.NotEmpty().Length(4, 32).LatinNumbersDash()</c>.</para>
    /// <para>Если не указан <paramref name="localizer"/>, то используется английский вариант локализации.</para>
    /// </remarks>
    public static IRuleBuilderOptions<T, string> Username<T>(this IRuleBuilder<T, string> ruleBuilder, IValidatorsLocalizer? localizer = null)
    {
        if (localizer == null)
        {
            return ruleBuilder
                .NotEmpty()
                .Length(4, 32)
                .LatinNumbersDashes();
        }

        return ruleBuilder
            .NotEmpty()
            .Length(4, 32)
            .LatinNumbersDashes(localizer);
    }

    /// <summary>
    /// Правило для поля LanguageCode.
    /// </summary>
    /// <remarks>
    /// <para>Используются правила <c>.NotEmpty().Length(2).SmallCaseLatin().WithName()</c>.</para>
    /// <para>Если не указан <paramref name="localizer"/>, то используется английский вариант локализации.</para>
    /// </remarks>
    public static IRuleBuilderOptions<T, string> LanguageCode<T>(this IRuleBuilder<T, string> ruleBuilder, IValidatorsLocalizer? localizer = null)
    {
        if (localizer == null)
        {
            return ruleBuilder
                .NotEmpty()
                .Length(2)
                .SmallCaseLatin()
                .WithName(EnglishValidatorsLanguage.GetTranslation(ValidatorsLocalizerConstants.PropertyLanguageCode));
        }

        return ruleBuilder
            .NotEmpty()
            .Length(2)
            .SmallCaseLatin(localizer)
            .WithName(localizer[ValidatorsLocalizerConstants.PropertyLanguageCode]);
    }

    /// <summary>
    /// Правило для поля Email.
    /// </summary>
    /// <remarks>
    /// <para>Используются правила <c>.NotEmpty().Length(6, 254).Matches(@"^(\w|\d|\.|_|-)+@(\w|\d){1,}\.[\w]{1,}\.?[\w]*$").WithMessage()</c>.</para>
    /// <para>Если не указан <paramref name="localizer"/>, то используется английский вариант локализации.</para>
    /// </remarks>
    public static IRuleBuilderOptions<T, string> Email<T>(this IRuleBuilder<T, string> ruleBuilder, IValidatorsLocalizer? localizer = null)
    {
        if (localizer == null)
        {
            return ruleBuilder
                .NotEmpty()
                .Length(6, 254)
                .Matches(@"^[a-zA-Z0-9\-._]+@[a-zA-Z0-9\-._]+\.[a-z]+$")
                .WithMessage(EnglishValidatorsLanguage.GetTranslation(ValidatorsLocalizerConstants.Email));
        }

        return ruleBuilder
            .NotEmpty()
            .Length(6, 254)
            .Matches(@"^[a-zA-Z0-9\-._]+@[a-zA-Z0-9\-._]+\.[a-z]+$")
            .WithMessage(localizer[ValidatorsLocalizerConstants.Email]);
    }

    /// <summary>
    /// Правило для поля PhoneNumber.
    /// </summary>
    /// <remarks>
    /// <para>Используются правила <c>.NotEmpty().Length(5, 15).Numbers().WithName()</c>.</para>
    /// <para>Если не указан <paramref name="localizer"/>, то используется английский вариант локализации.</para>
    /// </remarks>
    public static IRuleBuilderOptions<T, string> PhoneNumber<T>(this IRuleBuilder<T, string> ruleBuilder, IValidatorsLocalizer? localizer = null)
    {
        if (localizer == null)
        {
            return ruleBuilder
                .NotEmpty()
                .Length(5, 15)
                .Numbers()
                .WithName(EnglishValidatorsLanguage.GetTranslation(ValidatorsLocalizerConstants.PropertyPhoneNumber));
        }

        return ruleBuilder
            .NotEmpty()
            .Numbers(localizer)
            .Length(5, 15)
            .WithName(localizer[ValidatorsLocalizerConstants.PropertyPhoneNumber]);
    }

    /// <summary>
    /// Правило для поля Password.
    /// </summary>
    /// <remarks>
    /// <para>Используются правила <c>.NotEmpty().WithName()</c>.</para>
    /// <para>Если не указан <paramref name="localizer"/>, то используется английский вариант локализации.</para>
    /// </remarks>
    public static IRuleBuilderOptions<T, string> Password<T>(this IRuleBuilder<T, string> ruleBuilder, IValidatorsLocalizer? localizer = null)
    {
        if (localizer == null)
        {
            return ruleBuilder
                .NotEmpty()
                .WithName(EnglishValidatorsLanguage.GetTranslation(ValidatorsLocalizerConstants.PropertyPassword));
        }

        return ruleBuilder
            .NotEmpty()
            .WithName(localizer[ValidatorsLocalizerConstants.PropertyPassword]);
    }

    /// <summary>
    /// Правило для поля HashedPassword.
    /// </summary>
    /// <remarks>
    /// <para>Используются правила <c>.NotEmpty().Length(69).WithName()</c>.</para>
    /// <para>Если не указан <paramref name="localizer"/>, то используется английский вариант локализации.</para>
    /// </remarks>
    public static IRuleBuilderOptions<T, string> HashedPassword<T>(this IRuleBuilder<T, string> ruleBuilder, IValidatorsLocalizer? localizer = null)
    {
        if (localizer == null)
        {
            return ruleBuilder
                .NotEmpty()
                .Length(69)
                .WithName(EnglishValidatorsLanguage.GetTranslation(ValidatorsLocalizerConstants.PropertyHashedPassword));
        }

        return ruleBuilder
            .NotEmpty()
            .Length(69)
            .WithName(localizer[ValidatorsLocalizerConstants.PropertyHashedPassword]);
    }

    /// <summary>
    /// Правило для поля Token.
    /// </summary>
    /// <remarks>
    /// <para>Используются правила <c>.NotEmpty().MaximumLength(100).WithName()</c>.</para>
    /// <para>Если не указан <paramref name="localizer"/>, то используется английский вариант локализации.</para>
    /// </remarks>
    public static IRuleBuilderOptions<T, string> Token<T>(this IRuleBuilder<T, string> ruleBuilder, IValidatorsLocalizer? localizer = null)
    {
        if (localizer == null)
        {
            return ruleBuilder
                .NotEmpty()
                .MaximumLength(100)
                .WithName(EnglishValidatorsLanguage.GetTranslation(ValidatorsLocalizerConstants.PropertyToken));
        }

        return ruleBuilder
            .NotEmpty()
            .MaximumLength(100)
            .WithName(localizer[ValidatorsLocalizerConstants.PropertyToken]);
    }

    /// <summary>
    /// Правило для поля ApiKey.
    /// </summary>
    /// <remarks>
    /// <para>Используются правила <c>.NotWhiteSpace().Length(100).WithName()</c>.</para>
    /// <para>Если значение <see langword="null"/>, то так и остаётся, а если не <see langword="null"/>, то проверяем пустая ли строка и длину.</para>
    /// <para>Если не указан <paramref name="localizer"/>, то используется английский вариант локализации.</para>
    /// </remarks>
    public static IRuleBuilderOptions<T, string?> ApiKey<T>(this IRuleBuilder<T, string?> ruleBuilder, IValidatorsLocalizer? localizer = null)
    {
        if (localizer == null)
        {
            return ruleBuilder
                .NotWhiteSpace(localizer)
                .Length(100)
                .WithName(EnglishValidatorsLanguage.GetTranslation(ValidatorsLocalizerConstants.PropertyApiKey));
        }

        return ruleBuilder
            .NotWhiteSpace(localizer)
            .Length(100)
            .WithName(localizer[ValidatorsLocalizerConstants.PropertyApiKey]);
    }

    /// <summary>
    /// Правило для поля DisposableApiKey.
    /// </summary>
    /// <remarks>
    /// <para>Используются правила <c>.NotWhiteSpace().Length(100).WithName()</c>.</para>
    /// <para>Если значение <see langword="null"/>, то так и остаётся, а если не <see langword="null"/>, то проверяем пустая ли строка и длину.</para>
    /// <para>Если не указан <paramref name="localizer"/>, то используется английский вариант локализации.</para>
    /// </remarks>
    public static IRuleBuilderOptions<T, string?> DisposableApiKey<T>(this IRuleBuilder<T, string?> ruleBuilder, IValidatorsLocalizer? localizer = null)
    {
        if (localizer == null)
        {
            return ruleBuilder
                .NotWhiteSpace(localizer)
                .Length(100)
                .WithName(EnglishValidatorsLanguage.GetTranslation(ValidatorsLocalizerConstants.PropertyDisposableApiKey));
        }

        return ruleBuilder
            .NotWhiteSpace(localizer)
            .Length(100)
            .WithName(localizer[ValidatorsLocalizerConstants.PropertyDisposableApiKey]);
    }

    /// <summary>
    /// Правило для поля ApiKey.
    /// </summary>
    /// <remarks>
    /// <para>Используются правила <c>.NotEmpty().Length(100).WithName()</c>.</para>
    /// <para>Если не указан <paramref name="localizer"/>, то используется английский вариант локализации.</para>
    /// </remarks>
    public static IRuleBuilderOptions<T, string> ApiKeyOrDisposableApiKey<T>(this IRuleBuilder<T, string> ruleBuilder, IValidatorsLocalizer? localizer = null)
    {
        if (localizer == null)
        {
            return ruleBuilder
                .NotEmpty()
                .Length(100)
                .WithName(EnglishValidatorsLanguage.GetTranslation(ValidatorsLocalizerConstants.PropertyApiKeyOrDisposableApiKey));
        }

        return ruleBuilder
            .NotEmpty()
            .Length(100)
            .WithName(localizer[ValidatorsLocalizerConstants.PropertyApiKeyOrDisposableApiKey]);
    }

    /// <summary>
    /// Правило для поля NewPassword.
    /// </summary>
    /// <remarks>
    /// <para>Используются правила <c>.NotEmpty().Length(4, 32).LatinNumbersSpecialCharacters().WithName()</c>.</para>
    /// <para>Если не указан <paramref name="localizer"/>, то используется английский вариант локализации.</para>
    /// <para>В некоторых случаях нужно, чтобы отображался "Пароль", вместо "Новый пароль". Для этого есть <paramref name="displayPasswordName"/>. Если <see langword="true"/>, то "Пароль".</para>
    /// </remarks>
    /// <param name="displayPasswordName">Отображать ли поле, как "Пароль", вместо "Новый пароль".</param>
    public static IRuleBuilderOptions<T, string> NewPassword<T>(this IRuleBuilder<T, string> ruleBuilder, IValidatorsLocalizer? localizer = null, bool displayPasswordName = false)
    {
        string key = ValidatorsLocalizerConstants.PropertyNewPassword;
        if (displayPasswordName)
            key = ValidatorsLocalizerConstants.PropertyPassword;

        if (localizer == null)
        {
            return ruleBuilder
                .NotEmpty()
                .Length(4, 32)
                .LatinNumbersSpecialCharacters()
                .WithName(EnglishValidatorsLanguage.GetTranslation(key));
        }

        return ruleBuilder
            .NotEmpty()
            .Length(4, 32)
            .LatinNumbersSpecialCharacters(localizer)
            .WithName(localizer[key]);
    }

    /// <summary>
    /// Правило для поля Role.
    /// </summary>
    /// <remarks>
    /// <example>
    /// Используются правила
    /// <code>
    /// .NotEmpty()
    /// .Custom((role, context) =>
    /// {
    ///     foreach (var userRole in UserRoles.GetAllRoles())
    ///         if (role == userRole)
    ///             return;
    ///             
    ///     context.AddFailure(translation);
    /// });
    /// </code>
    /// </example>
    /// <para>Если не указан <paramref name="localizer"/>, то используется английский вариант локализации.</para>
    /// </remarks>
    public static IRuleBuilderOptionsConditions<T, string> Role<T>(this IRuleBuilder<T, string> ruleBuilder, IValidatorsLocalizer? localizer = null)
    {
        if (localizer == null)
        {
            return ruleBuilder
                .NotEmpty()
                .Custom((role, context) =>
                {
                    foreach (var userRole in UserRoles.GetAllRoles())
                        if (role == userRole)
                            return;

                    context.AddFailure(EnglishValidatorsLanguage.GetTranslation(ValidatorsLocalizerConstants.InvalidRole));
                });
        }

        return ruleBuilder
            .NotEmpty()
            .Custom((role, context) =>
            {
                foreach (var userRole in UserRoles.GetAllRoles())
                    if (role == userRole)
                        return;

                context.AddFailure(localizer[ValidatorsLocalizerConstants.InvalidRole]);
            });
    }

    /// <summary>
    /// Правило для поля OrderStatus.
    /// </summary>
    /// <remarks>
    /// <example>
    /// Используются правила
    /// <code>
    /// .NotEmpty()
    /// .Custom((status, context) =>
    /// {
    ///     foreach (var orderStatus in OrderStatuses.GetAllStatuses())
    ///         if (status == orderStatus)
    ///             return;
    ///             
    ///     context.AddFailure(translation);
    /// });
    /// </code>
    /// </example>
    /// <para>Если не указан <paramref name="localizer"/>, то используется английский вариант локализации.</para>
    /// </remarks>
    public static IRuleBuilderOptionsConditions<T, string> OrderStatus<T>(this IRuleBuilder<T, string> ruleBuilder, IValidatorsLocalizer? localizer = null)
    {
        if (localizer == null)
        {
            return ruleBuilder
                .NotEmpty()
                .Custom((status, context) =>
                {
                    foreach (var orderStatus in OrderStatuses.GetAllStatuses())
                        if (status == orderStatus)
                            return;

                    context.AddFailure(EnglishValidatorsLanguage.GetTranslation(ValidatorsLocalizerConstants.InvalidOrderStatus));
                });
        }

        return ruleBuilder
            .NotEmpty()
            .Custom((status, context) =>
            {
                foreach (var orderStatus in OrderStatuses.GetAllStatuses())
                    if (status == orderStatus)
                        return;

                context.AddFailure(localizer[ValidatorsLocalizerConstants.InvalidOrderStatus]);
            });
    }

    /// <summary>
    /// Правило для поля PaymentStatus.
    /// </summary>
    /// <remarks>
    /// <example>
    /// Используются правила
    /// <code>
    /// .NotEmpty()
    /// .Custom((status, context) =>
    /// {
    ///     foreach (var orderStatus in PaymentStatuses.GetAllStatuses())
    ///         if (status == orderStatus)
    ///             return;    
    ///             
    ///     context.AddFailure(translation);
    /// });
    /// </code>
    /// </example>
    /// <para>Если не указан <paramref name="localizer"/>, то используется английский вариант локализации.</para>
    /// </remarks>
    public static IRuleBuilderOptionsConditions<T, string> PaymentStatus<T>(this IRuleBuilder<T, string> ruleBuilder, IValidatorsLocalizer? localizer = null)
    {
        if (localizer == null)
        {
            return ruleBuilder
                .NotEmpty()
                .Custom((status, context) =>
                {
                    foreach (var orderStatus in PaymentStatuses.GetAllStatuses())
                        if (status == orderStatus)
                            return;

                    context.AddFailure(EnglishValidatorsLanguage.GetTranslation(ValidatorsLocalizerConstants.InvalidPaymentStatus));
                });
        }

        return ruleBuilder
            .NotEmpty()
            .Custom((status, context) =>
            {
                foreach (var orderStatus in PaymentStatuses.GetAllStatuses())
                    if (status == orderStatus)
                        return;

                context.AddFailure(localizer[ValidatorsLocalizerConstants.InvalidPaymentStatus]);
            });
    }

    /// <summary>
    /// Правило для поля ProductName.
    /// </summary>
    /// <remarks>
    /// <example>
    /// Используются правила
    /// <code>
    /// .NotEmpty()
    /// .Custom((name, context) =>
    /// {
    ///     foreach (var productName in Products.GetAllProductNames())
    ///         if (name == productName)
    ///             return;
    ///             
    ///     context.AddFailure(translation);
    /// });
    /// </code>
    /// </example>
    /// <para>Если не указан <paramref name="localizer"/>, то используется английский вариант локализации.</para>
    /// </remarks>
    public static IRuleBuilderOptionsConditions<T, string> ProductName<T>(this IRuleBuilder<T, string> ruleBuilder, IValidatorsLocalizer? localizer = null)
    {
        if (localizer == null)
        {
            return ruleBuilder
                .NotEmpty()
                .Custom((name, context) =>
                {
                    foreach (var productName in Products.GetAllProductNames())
                        if (name == productName)
                            return;

                    context.AddFailure(EnglishValidatorsLanguage.GetTranslation(ValidatorsLocalizerConstants.InvalidProductName));
                });
        }

        return ruleBuilder
            .NotEmpty()
            .Custom((name, context) =>
            {
                foreach (var productName in Products.GetAllProductNames())
                    if (name == productName)
                        return;

                context.AddFailure(localizer[ValidatorsLocalizerConstants.InvalidProductName]);
            });
    }

    /// <summary>
    /// Правило для поля Title.
    /// </summary>
    /// <remarks>
    /// <para>Используются правила <c>(.NotEmpty() | .NotWhiteSpace()).Length(3, 64).WithName()</c>.</para>
    /// <para>Если <paramref name="required"/> <see langword="true"/>, то используется <c>.NotEmpty()</c> (не допускается <see langword="null"/>). Иначе используется <c>.NotWhiteSpace()</c> (допускается <see langword="null"/>).</para>
    /// <para>Если не указан <paramref name="localizer"/>, то используется английский вариант локализации.</para>
    /// </remarks>
    /// <param name="required">Обязателен ли параметр.</param>
    public static IRuleBuilderOptions<T, string?> Title<T>(this IRuleBuilder<T, string?> ruleBuilder, IValidatorsLocalizer? localizer = null, bool required = true)
    {
        // Если значение обязательно, то значение должно быть не пустым. Если значение необязательно, то значение не должно состоять из пробелов
        var setupBuilder = required ? ruleBuilder.NotEmpty() : ruleBuilder.NotWhiteSpace(localizer);

        setupBuilder.Length(3, 64);

        if (localizer == null)
            return setupBuilder
                .WithName(EnglishValidatorsLanguage.GetTranslation(ValidatorsLocalizerConstants.PropertyTitle));

        return setupBuilder
            .WithName(localizer[ValidatorsLocalizerConstants.PropertyTitle]);
    }

    /// <summary>
    /// Правило для поля Content.
    /// </summary>
    /// <remarks>
    /// <para>Используются правила <c>(.NotEmpty() | .NotWhiteSpace()).Length(128, 1024).WithName()</c>.</para>
    /// <para>Если не указан <paramref name="localizer"/>, то используется английский вариант локализации.</para>
    /// <para>Если <paramref name="required"/> <see langword="true"/>, то используется <c>.NotEmpty()</c> (не допускается <see langword="null"/>). Иначе используется <c>.NotWhiteSpace()</c> (допускается <see langword="null"/>).</para>
    /// </remarks>
    /// <param name="required">Обязателен ли параметр.</param>
    public static IRuleBuilderOptions<T, string?> Content<T>(this IRuleBuilder<T, string?> ruleBuilder, IValidatorsLocalizer? localizer = null, bool required = true)
    {
        var setupBuilder = required ? ruleBuilder.NotEmpty() : ruleBuilder.NotWhiteSpace(localizer);

        setupBuilder.Length(128, 1024);

        if (localizer == null)
            return setupBuilder
                .WithName(EnglishValidatorsLanguage.GetTranslation(ValidatorsLocalizerConstants.PropertyContent));

        return setupBuilder
            .WithName(localizer[ValidatorsLocalizerConstants.PropertyContent]);
    }

    /// <summary>
    /// Правило для поля Count.
    /// </summary>
    /// <remarks>
    /// <para>Используются правила <c>.NotEmpty().InclusiveBetween(1, 100).WithName()</c>.</para>
    /// <para>Если не указан <paramref name="localizer"/>, то используется английский вариант локализации.</para>
    /// </remarks>
    public static IRuleBuilderOptions<T, int> Count<T>(this IRuleBuilder<T, int> ruleBuilder, int min = 1, int max = 100, IValidatorsLocalizer? localizer = null)
    {
        if (localizer == null)
        {
            return ruleBuilder
                .NotEmpty()
                .InclusiveBetween(min, max) // От 1-100 включительно
                .WithName(EnglishValidatorsLanguage.GetTranslation(ValidatorsLocalizerConstants.PropertyCount));
        }

        return ruleBuilder
            .NotEmpty()
            .InclusiveBetween(min, max)
            .WithName(localizer[ValidatorsLocalizerConstants.PropertyCount]);
    }

    /// <summary>
    /// Правило для поля даты <see cref="DateTime"/>, соответствует ли свойство указанному формату.
    /// </summary>
    /// <remarks>
    /// <example>
    /// Используются правила
    /// <code>
    /// .Custom((stringDate, context) =>
    /// {
    ///     // Модель допускает необязательность параметра
    ///     if (stringDate == null)
    ///         return;
    /// 
    ///     if (!DateTime.TryParseExact(stringDate, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime _))
    ///         context.AddFailure(string.Format(EnglishValidatorsLanguage.GetTranslation(ValidatorsLocalizerConstants.InvalidDateJson), format));
    /// });
    /// </code>
    /// </example>
    /// <para>Если не указан <paramref name="localizer"/>, то используется английский вариант локализации.</para>
    /// </remarks>
    /// <param name="format">Разрешённый формат даты из констант <see cref="DateTimeFormats"/>.</param>
    public static IRuleBuilderOptionsConditions<T, string?> DateTimeMatchFormat<T>(this IRuleBuilder<T, string?> ruleBuilder, string format, IValidatorsLocalizer? localizer = null)
    {
        if (localizer == null)
        {
            return ruleBuilder.Custom((stringDate, context) =>
            {
                // Модель допускает необязательность параметра
                if (stringDate == null)
                    return;

                if (!DateTime.TryParseExact(stringDate, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                    context.AddFailure(string.Format(EnglishValidatorsLanguage.GetTranslation(ValidatorsLocalizerConstants.InvalidDateJson), format));
            });
        }

        return ruleBuilder.Custom((stringDate, context) =>
        {
            if (stringDate == null)
                return;

            if (!DateTime.TryParseExact(stringDate, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                context.AddFailure(string.Format(localizer[ValidatorsLocalizerConstants.InvalidDateJson], format));
        });
    }

    /// <summary>
    /// Содержится ли в ошибках результата только указанное свойство <paramref name="propertyName"/>.
    /// </summary>
    /// <remarks>
    /// Например, указано свойство "Title", значит вернётся <see langword="true"/>, если в ошибках содержится только это свойство (только это свойство оказалось невалидным).
    /// </remarks>
    /// <param name="propertyName">Имя свойства.</param>
    /// <returns><see langword="true"/>, если содержит.</returns>
    public static bool IsSingleProperty(this FluentValidation.Results.ValidationResult validationResult, string propertyName)
    {
        if (validationResult.Errors.Count == 1 && validationResult.Errors.Select(x => x.PropertyName).Contains(propertyName))
            return true;

        return false;
    }
}