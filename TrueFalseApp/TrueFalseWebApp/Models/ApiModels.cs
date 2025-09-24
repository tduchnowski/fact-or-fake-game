using System.Text.Json.Serialization;

namespace TrueFalseWebApp.Models;

public record ApiResponse<T>(
    bool Ok,
    T Content,
    string? Message
);

public record HubOperationResult(
    [property: JsonPropertyName("ok")] bool Ok,
    [property: JsonPropertyName("message")] string? Message
);