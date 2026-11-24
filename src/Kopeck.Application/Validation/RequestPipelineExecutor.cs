using Microsoft.Extensions.DependencyInjection;

namespace Kopeck.Application.Validation;

public sealed class RequestPipelineExecutor(IServiceProvider services) : IRequestPipelineExecutor
{
    public Task<ValidationResult> ExecuteAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default) =>
        services.GetService<IRequestValidator<TRequest>>() is { } validator
            ? validator.ValidateAsync(request, cancellationToken)
            : Task.FromResult(ValidationResult.Valid);
}
