using Kopeck.Contracts;
using Kopeck.Domain.Common;

namespace Kopeck.Application.Validation.Validators;

/// <summary>One category for one month; 0 clears the plan.</summary>
public sealed class PlanRequestValidator : IRequestValidator<PlanRequest>
{
    public Task<ValidationResult> ValidateAsync(PlanRequest request, CancellationToken cancellationToken = default) =>
        new ValidationResult()
            .AddErrorIf(request.CategoryId == Guid.Empty, nameof(request.CategoryId), ValidationMessages.CategoryRequired)
            .AddErrorIf(!DomainRules.IsValidAmount(request.Amount, allowZero: true), nameof(request.Amount), ValidationMessages.PlannedAmount)
            .AsTask();
}
