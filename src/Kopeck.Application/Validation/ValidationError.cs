namespace Kopeck.Application.Validation;

/// <summary>One reason a request was rejected: the offending property and a message the user can act on.</summary>
public sealed record ValidationError(string PropertyName, string Message)
{
    public override string ToString() => $"{PropertyName}: {Message}";
}
