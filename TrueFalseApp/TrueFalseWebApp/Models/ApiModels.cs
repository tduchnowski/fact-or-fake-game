namespace TrueFalseWebApp.Models;

public record ApiResponse<T>(
    string Status,
    T Content
);