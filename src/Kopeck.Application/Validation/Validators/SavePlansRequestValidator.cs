using Kopeck.Contracts;
using Kopeck.Domain.Common;

namespace Kopeck.Application.Validation.Validators;

/// <summary>Every row is checked before anything is saved, so the bulk save stays all-or-nothing.</summary>
public sealed class SavePlansRequestValidator : IRequestValidator<SavePlansRequest>
{
    public Task<ValidationResult> ValidateAsync(SavePlansRequest request, CancellationToken cancellationToken = default)
    {
        var items = request.Items ?? [];
        return new ValidationResult()
            .AddErrorIf(items.Any(i => i.CategoryId == Guid.Empty), nameof(request.Items), ValidationMessages.CategoryRequired)
            .AddErrorIf(items.Any(i => !DomainRules.IsValidAmount(i.Amount, allowZero: true)), nameof(request.Items), ValidationMessages.PlannedAmount)
            .AddErrorIf(items.Select(i => i.CategoryId).Distinct().Count() != items.Count, nameof(request.Items), "Each category can appear only once.")
            .AsTask();
    }
}
