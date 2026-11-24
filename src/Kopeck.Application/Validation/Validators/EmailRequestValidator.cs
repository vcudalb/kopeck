using Kopeck.Contracts;
using Kopeck.Domain.Common;

namespace Kopeck.Application.Validation.Validators;

/// <summary>Resend code / forgot password. Only the format is checked, so the answer still never reveals whether the email is registered.</summary>
public sealed class EmailRequestValidator : IRequestValidator<EmailRequest>
{
    public Task<ValidationResult> ValidateAsync(EmailRequest request, CancellationToken cancellationToken = default) =>
        new ValidationResult()
            .AddErrorIf(DomainRules.NormalizeEmail(request.Email) is null, nameof(request.Email), ValidationMessages.InvalidEmail)
            .AsTask();
}
