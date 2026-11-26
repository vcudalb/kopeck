using Kopeck.Contracts;

namespace Kopeck.Mobile.Services;

public sealed class BudgetsApi(IApiClient api) : IBudgetsApi
{
    public Task<List<BudgetDto>> ListAsync(CancellationToken ct) => api.GetAsync<List<BudgetDto>>("/api/budgets", ct);
    public Task<BudgetDto> CreateAsync(BudgetRequest request, CancellationToken ct) => api.PostAsync<BudgetDto>("/api/budgets", request, ct);
    public Task DeleteAsync(Guid budgetId, CancellationToken ct) => api.DeleteAsync(Routes.Budget(budgetId), ct);
    public Task LeaveAsync(Guid budgetId, CancellationToken ct) => api.DeleteAsync($"{Routes.Budget(budgetId)}/leave", ct);
    public Task<MonthSummaryDto> GetSummaryAsync(Guid budgetId, DateOnly month, CancellationToken ct) =>
        api.GetAsync<MonthSummaryDto>($"{Routes.Budget(budgetId)}/summary?month={Routes.Month(month)}", ct);
    public Task<YearSummaryDto> GetYearSummaryAsync(Guid budgetId, int year, CancellationToken ct) =>
        api.GetAsync<YearSummaryDto>($"{Routes.Budget(budgetId)}/summary/year?year={year}", ct);
}
