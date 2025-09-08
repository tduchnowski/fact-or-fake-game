namespace TrueFalseBackend.Models;

public record OperationResult(bool Ok, string? Message)
{
    public static OperationResult Success() => new(true, null);
    public static OperationResult Fail(string? msg) => new(false, msg);
}
