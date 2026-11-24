using Kopeck.Contracts;
using Kopeck.Domain.Common;

namespace Kopeck.Application.Validation.Validators;

/// <summary>An invalid colour is not an error: the category keeps its current (or the default) colour.</summary>
public sealed class CategoryRequestValidator : IRequestValidator<CategoryRequest>
{
    public Task<ValidationResult> ValidateAsync(CategoryRequest request, CancellationToken cancellationToken = default) =>
        new ValidationResult()
            .AddErrorIf(!DomainRules.IsValidName(request.Name), nameof(request.Name), ValidationMessages.NameRequired("category"))
            .AsTask();
}
