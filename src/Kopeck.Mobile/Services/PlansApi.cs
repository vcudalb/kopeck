using Kopeck.Contracts;

namespace Kopeck.Mobile.Services;

public sealed class PlansApi(IApiClient api) : IPlansApi
{
    public Task<List<PlanDto>> ListAsync(Guid budgetId, DateOnly month, CancellationToken ct) =>
        api.GetAsync<List<PlanDto>>($"{Routes.Budget(budgetId)}/plans?month={Routes.Month(month)}", ct);
    public Task SaveAsync(Guid budgetId, SavePlansRequest request, CancellationToken ct) => api.PutAsync($"{Routes.Budget(budgetId)}/plans/bulk", request, ct);
    public Task<CopyPlanResponse> CopyAsync(Guid budgetId, CopyPlanRequest request, CancellationToken ct) =>
        api.PostAsync<CopyPlanResponse>($"{Routes.Budget(budgetId)}/plans/copy", request, ct);
}
