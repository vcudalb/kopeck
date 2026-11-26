using Kopeck.Contracts;
using Kopeck.Domain.Common;

namespace Kopeck.Application.Validation.Validators;

/// <summary>The currency is optional (the budget's is used), but when given it has to be a real code.</summary>
public sealed class IncomeStreamRequestValidator : IRequestValidator<IncomeStreamRequest>
{
    public Task<ValidationResult> ValidateAsync(IncomeStreamRequest request, CancellationToken cancellationToken = default) =>
        new ValidationResult()
            .AddErrorIf(!DomainRules.IsValidName(request.Name), nameof(request.Name), ValidationMessages.NameRequired("stream"))
            .AddErrorIf(request.Currency is not null && DomainRules.NormalizeCurrency(request.Currency) is null,
                nameof(request.Currency), ValidationMessages.InvalidCurrency)
            .AsTask();
}
