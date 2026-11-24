using Kopeck.Contracts;

namespace Kopeck.Mobile.Services;

public interface IPlansApi
{
    Task<List<PlanDto>> ListAsync(Guid budgetId, DateOnly month, CancellationToken ct);
    Task SaveAsync(Guid budgetId, SavePlansRequest request, CancellationToken ct);
    Task<CopyPlanResponse> CopyAsync(Guid budgetId, CopyPlanRequest request, CancellationToken ct);
}
