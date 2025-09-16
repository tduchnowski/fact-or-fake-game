using System.Text.Json.Serialization;

namespace TrueFalseWebApp.Models;

public record WebAppInitDataUnsafe
(
    [property: JsonPropertyName("user")] WebAppUser? User,
    [property: JsonPropertyName("start_param")] string? StartParam,
    [property: JsonPropertyName("hash")] string Hash,
    [property: JsonPropertyName("signature")] string Signature
);

public record WebAppUser
(
    [property: JsonPropertyName("first_name")] string FirstName,
    [property: JsonPropertyName("last_name")] string? LastName,
    [property: JsonPropertyName("username")] string? Username,
    [property: JsonPropertyName("photo_url")] string? PhotoUrl
);