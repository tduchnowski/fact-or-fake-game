namespace TrueFalseWebApp.Models;

public record ApiResponse<T>(
    string Status,
    T Content
);

public record HubOperationResult(
    bool Success,
    string? ErrorMessage
);