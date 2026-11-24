using Kopeck.Contracts;
using Kopeck.Domain.Common;

namespace Kopeck.Application.Validation.Validators;

public sealed class EmailCodeRequestValidator : IRequestValidator<EmailCodeRequest>
{
    public Task<ValidationResult> ValidateAsync(EmailCodeRequest request, CancellationToken cancellationToken = default) =>
        new ValidationResult()
            .AddErrorIf(DomainRules.NormalizeEmail(request.Email) is null, nameof(request.Email), ValidationMessages.InvalidEmail)
            .AddErrorIf(!DomainRules.IsOneTimeCode(request.Code), nameof(request.Code), ValidationMessages.InvalidCode)
            .AsTask();
}
