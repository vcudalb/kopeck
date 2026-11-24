using Kopeck.Contracts;
using Kopeck.Domain.Common;

namespace Kopeck.Application.Validation.Validators;

/// <summary>Whether the category belongs to the budget is checked by the service (it needs the database).</summary>
public sealed class ExpenseRequestValidator : IRequestValidator<ExpenseRequest>
{
    public Task<ValidationResult> ValidateAsync(ExpenseRequest request, CancellationToken cancellationToken = default) =>
        new ValidationResult()
            .AddErrorIf(request.CategoryId == Guid.Empty, nameof(request.CategoryId), ValidationMessages.CategoryRequired)
            .AddErrorIf(!DomainRules.IsValidAmount(request.Amount), nameof(request.Amount), ValidationMessages.PositiveAmount)
            .AsTask();
}
