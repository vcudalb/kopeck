namespace Kopeck.Application.Common;

public sealed record Error(ErrorKind Kind, string Message)
{
    public static Error NotFound { get; } = new(ErrorKind.NotFound, "Not found.");

    public static Error Validation(string message) => new(ErrorKind.Validation, message);
    public static Error Conflict(string message) => new(ErrorKind.Conflict, message);
    public static Error Forbidden(string message) => new(ErrorKind.Forbidden, message);
    public static Error Unauthorized(string message) => new(ErrorKind.Unauthorized, message);
}
