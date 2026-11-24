using Kopeck.Contracts;
using Kopeck.Domain.Common;

namespace Kopeck.Application.Validation.Validators;

public sealed class CopyPlanRequestValidator : IRequestValidator<CopyPlanRequest>
{
    public Task<ValidationResult> ValidateAsync(CopyPlanRequest request, CancellationToken cancellationToken = default) =>
        new ValidationResult()
            .AddErrorIf(request.FromMonth.FirstOfMonth() == request.ToMonth.FirstOfMonth(), nameof(request.ToMonth), "Choose two different months.")
            .AsTask();
}
