using Kopeck.Contracts;
using Kopeck.Domain.Common;

namespace Kopeck.Application.Validation.Validators;

/// <summary>Sharing with yourself depends on who is signed in, so the service checks that.</summary>
public sealed class ShareRequestValidator : IRequestValidator<ShareRequest>
{
    public Task<ValidationResult> ValidateAsync(ShareRequest request, CancellationToken cancellationToken = default) =>
        new ValidationResult()
            .AddErrorIf(DomainRules.NormalizeEmail(request.Email) is null, nameof(request.Email), ValidationMessages.InvalidEmail)
            .AddErrorIf(!Enum.IsDefined(request.Role), nameof(request.Role), "Unknown access level.")
            .AsTask();
}
