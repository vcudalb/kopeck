namespace Kopeck.Application.Common;

/// <summary>Why a use case refused a request. The API maps each kind to one HTTP status code.</summary>
public enum ErrorKind
{
    Validation,
    NotFound,
    Conflict,
    Forbidden,
    Unauthorized,
}

public sealed record Error(ErrorKind Kind, string Message)
{
    public static Error NotFound { get; } = new(ErrorKind.NotFound, "Not found.");

    public static Error Validation(string message) => new(ErrorKind.Validation, message);
    public static Error Conflict(string message) => new(ErrorKind.Conflict, message);
    public static Error Forbidden(string message) => new(ErrorKind.Forbidden, message);
    public static Error Unauthorized(string message) => new(ErrorKind.Unauthorized, message);
}

/// <summary>
/// Outcome of a use case: success, or an expected failure described by <see cref="Common.Error"/>.
/// Expected failures are values, not exceptions; exceptions stay for the unexpected.
/// </summary>
public class Result
{
    protected Result(Error? error) => Error = error;

    public static Result Success { get; } = new(null);

    public Error? Error { get; }
    public bool IsSuccess => Error is null;

    public static implicit operator Result(Error error) => new(error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value) : base(null) => _value = value;
    private Result(Error error) : base(error) { }

    public T Value => IsSuccess ? _value! : throw new InvalidOperationException($"No value: the operation failed ({Error!.Message}).");

    public static implicit operator Result<T>(T value) => new(value);
    public static implicit operator Result<T>(Error error) => new(error);
}
