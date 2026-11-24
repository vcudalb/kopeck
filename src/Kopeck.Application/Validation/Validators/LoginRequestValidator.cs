using Kopeck.Contracts;

namespace Kopeck.Application.Validation.Validators;

/// <summary>
/// Only checks that both values are present. A wrong email or password is the service's answer (401), so the response
/// never hints at which part was wrong or whether the account exists.
/// </summary>
public sealed class LoginRequestValidator : IRequestValidator<LoginRequest>
{
    public Task<ValidationResult> ValidateAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
        new ValidationResult()
            .AddErrorIf(string.IsNullOrWhiteSpace(request.Email), nameof(request.Email), "Enter your email.")
            .AddErrorIf(string.IsNullOrEmpty(request.Password), nameof(request.Password), "Enter your password.")
            .AsTask();
}
