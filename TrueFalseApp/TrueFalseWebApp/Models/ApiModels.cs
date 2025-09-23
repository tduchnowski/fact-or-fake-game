using System.Text.Json.Serialization;

namespace TrueFalseWebApp.Models;

public record ApiResponse<T>(
    string Status,
    T Content
);

public record HubOperationResult(
    [property: JsonPropertyName("ok")] bool Ok,
    [property: JsonPropertyName("message")] string? Message
);