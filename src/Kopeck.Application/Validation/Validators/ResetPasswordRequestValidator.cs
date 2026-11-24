using Kopeck.Contracts;
using Kopeck.Domain.Common;
using Kopeck.Domain.Users;

namespace Kopeck.Application.Validation.Validators;

public sealed class ResetPasswordRequestValidator : IRequestValidator<ResetPasswordRequest>
{
    public Task<ValidationResult> ValidateAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult()
            .AddErrorIf(DomainRules.NormalizeEmail(request.Email) is null, nameof(request.Email), ValidationMessages.InvalidEmail)
            .AddErrorIf(!DomainRules.IsOneTimeCode(request.Code), nameof(request.Code), ValidationMessages.InvalidCode);

        if (PasswordPolicy.Validate(request.NewPassword) is { } passwordError) result.AddError(nameof(request.NewPassword), passwordError);
        return result.AsTask();
    }
}
