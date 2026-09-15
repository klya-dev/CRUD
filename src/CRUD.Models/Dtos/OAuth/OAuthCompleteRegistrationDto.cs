using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.OAuth;

/// <summary>
/// DTO-модель завершения регистрации через OAuth.
/// </summary>
public sealed record OAuthCompleteRegistrationDto
{
    /// <summary>
    /// Телефонный номер.
    /// </summary>
    [JsonPropertyName("phoneNumber")]
    public required string PhoneNumber { get; init; }
}