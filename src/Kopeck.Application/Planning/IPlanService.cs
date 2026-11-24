using Kopeck.Application.Common;
using Kopeck.Contracts;

namespace Kopeck.Application.Planning;

public interface IPlanService
{
    Task<IReadOnlyList<PlanDto>> ListAsync(Guid budgetId, DateOnly month, CancellationToken ct);
    Task<Result> SetAsync(Guid budgetId, PlanRequest request, CancellationToken ct);
    Task<Result> SaveManyAsync(Guid budgetId, SavePlansRequest request, CancellationToken ct);
    Task<Result<CopyPlanResponse>> CopyMissingAsync(Guid budgetId, CopyPlanRequest request, CancellationToken ct);
}
