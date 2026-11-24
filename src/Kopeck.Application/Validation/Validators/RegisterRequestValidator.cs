using Kopeck.Contracts;
using Kopeck.Domain.Common;
using Kopeck.Domain.Users;

namespace Kopeck.Application.Validation.Validators;

public sealed class RegisterRequestValidator : IRequestValidator<RegisterRequest>
{
    public Task<ValidationResult> ValidateAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult()
            .AddErrorIf(DomainRules.NormalizeEmail(request.Email) is null, nameof(request.Email), ValidationMessages.InvalidEmail)
            .AddErrorIf(!DomainRules.IsValidName(request.DisplayName), nameof(request.DisplayName),
                $"Enter a display name (max {DomainRules.MaxNameLength} characters).");

        if (PasswordPolicy.Validate(request.Password) is { } passwordError) result.AddError(nameof(request.Password), passwordError);
        return result.AsTask();
    }
}
