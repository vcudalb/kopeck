namespace Kopeck.Application.Validation;

/// <summary>
/// Checks the shape of one request type (required values, lengths, formats, ranges) before it reaches a service.
/// Rules that need the database (does the category exist, is the name taken) stay in the services.
/// Implementations are registered automatically by <see cref="DependencyInjection.AddApplication"/>.
/// </summary>
public interface IRequestValidator<in TRequest>
{
    Task<ValidationResult> ValidateAsync(TRequest request, CancellationToken cancellationToken = default);
}
