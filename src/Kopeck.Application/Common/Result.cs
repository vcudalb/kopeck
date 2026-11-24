namespace Kopeck.Application.Common;

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
