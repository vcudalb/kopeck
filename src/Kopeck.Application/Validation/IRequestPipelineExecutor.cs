namespace Kopeck.Application.Validation;

/// <summary>Runs the validator registered for a request type, if any.</summary>
public interface IRequestPipelineExecutor
{
    Task<ValidationResult> ExecuteAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default);
}
