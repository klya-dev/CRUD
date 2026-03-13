using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.OAuth;

/// <summary>
/// DTO-модель завершения регистрации через OAuth.
/// </summary>
public class OAuthCompleteRegistrationDto
{
    /// <summary>
    /// Телефонный номер.
    /// </summary>
    [JsonPropertyName("phoneNumber")]
    public required string PhoneNumber { get; set; }
}