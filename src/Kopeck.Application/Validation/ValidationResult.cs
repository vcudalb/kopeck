namespace Kopeck.Application.Validation;

/// <summary>The outcome of validating one request. Valid when no error was added.</summary>
public sealed class ValidationResult
{
    private readonly List<ValidationError> _errors = [];

    public static ValidationResult Valid => new();

    public bool IsValid => _errors.Count == 0;
    public IReadOnlyList<ValidationError> Errors => _errors;

    /// <summary>The messages joined into one sentence-per-error text, as shown to the user.</summary>
    public string Message => string.Join(" ", _errors.Select(e => e.Message));

    public ValidationResult AddError(string propertyName, string message)
    {
        _errors.Add(new ValidationError(propertyName, message));
        return this;
    }

    /// <summary>Adds the error when <paramref name="failed"/> is true; keeps validators readable as a list of rules.</summary>
    public ValidationResult AddErrorIf(bool failed, string propertyName, string message) =>
        failed ? AddError(propertyName, message) : this;

    public Task<ValidationResult> AsTask() => Task.FromResult(this);
}
