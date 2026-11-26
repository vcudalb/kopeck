using Kopeck.Contracts;
using Kopeck.Domain.Common;

namespace Kopeck.Application.Validation.Validators;

/// <summary>Currency and target are optional. An invalid colour is not an error: the pot keeps its current one.</summary>
public sealed class SavingsPotRequestValidator : IRequestValidator<SavingsPotRequest>
{
    public Task<ValidationResult> ValidateAsync(SavingsPotRequest request, CancellationToken cancellationToken = default) =>
        new ValidationResult()
            .AddErrorIf(!DomainRules.IsValidName(request.Name), nameof(request.Name), ValidationMessages.NameRequired("pot"))
            .AddErrorIf(request.Currency is not null && DomainRules.NormalizeCurrency(request.Currency) is null,
                nameof(request.Currency), ValidationMessages.InvalidCurrency)
            .AddErrorIf(request.Target is { } target && !DomainRules.IsValidAmount(target, allowZero: true),
                nameof(request.Target), "Target must be 0 or more (max 2 decimals).")
            .AsTask();
}
