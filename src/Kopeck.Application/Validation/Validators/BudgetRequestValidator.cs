using Kopeck.Contracts;
using Kopeck.Domain.Common;

namespace Kopeck.Application.Validation.Validators;

/// <summary>Used for create and rename. Rename ignores the currency, but the request still has to be well formed.</summary>
public sealed class BudgetRequestValidator : IRequestValidator<BudgetRequest>
{
    public Task<ValidationResult> ValidateAsync(BudgetRequest request, CancellationToken cancellationToken = default) =>
        new ValidationResult()
            .AddErrorIf(!DomainRules.IsValidName(request.Name), nameof(request.Name), ValidationMessages.NameRequired("budget"))
            .AddErrorIf(DomainRules.NormalizeCurrency(request.Currency) is null, nameof(request.Currency), "Enter a currency code such as EUR or MDL.")
            .AsTask();
}
