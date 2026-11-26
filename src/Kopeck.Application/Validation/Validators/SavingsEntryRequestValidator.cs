using Kopeck.Contracts;
using Kopeck.Domain.Common;

namespace Kopeck.Application.Validation.Validators;

/// <summary>Whether a withdrawal fits in the pot depends on its balance, so the service checks that.</summary>
public sealed class SavingsEntryRequestValidator : IRequestValidator<SavingsEntryRequest>
{
    public Task<ValidationResult> ValidateAsync(SavingsEntryRequest request, CancellationToken cancellationToken = default) =>
        new ValidationResult()
            .AddErrorIf(!Enum.IsDefined(request.Kind), nameof(request.Kind), "Choose deposit or withdrawal.")
            .AddErrorIf(!DomainRules.IsValidAmount(request.Amount), nameof(request.Amount), ValidationMessages.PositiveAmount)
            .AsTask();
}
