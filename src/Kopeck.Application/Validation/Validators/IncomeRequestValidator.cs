using Kopeck.Contracts;
using Kopeck.Domain.Common;

namespace Kopeck.Application.Validation.Validators;

/// <summary>Whether the stream belongs to the budget is checked by the service (it needs the database).</summary>
public sealed class IncomeRequestValidator : IRequestValidator<IncomeRequest>
{
    public Task<ValidationResult> ValidateAsync(IncomeRequest request, CancellationToken cancellationToken = default) =>
        new ValidationResult()
            .AddErrorIf(request.StreamId == Guid.Empty, nameof(request.StreamId), "Choose a revenue stream.")
            .AddErrorIf(!DomainRules.IsValidAmount(request.Amount), nameof(request.Amount), ValidationMessages.PositiveAmount)
            .AsTask();
}
