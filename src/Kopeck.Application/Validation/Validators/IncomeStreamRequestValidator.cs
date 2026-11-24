using Kopeck.Contracts;
using Kopeck.Domain.Common;

namespace Kopeck.Application.Validation.Validators;

public sealed class IncomeStreamRequestValidator : IRequestValidator<IncomeStreamRequest>
{
    public Task<ValidationResult> ValidateAsync(IncomeStreamRequest request, CancellationToken cancellationToken = default) =>
        new ValidationResult()
            .AddErrorIf(!DomainRules.IsValidName(request.Name), nameof(request.Name), ValidationMessages.NameRequired("stream"))
            .AsTask();
}
