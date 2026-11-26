using Kopeck.Contracts;

namespace Kopeck.Mobile.Services;

public interface IBudgetsApi
{
    Task<List<BudgetDto>> ListAsync(CancellationToken ct);
    Task<BudgetDto> CreateAsync(BudgetRequest request, CancellationToken ct);
    Task DeleteAsync(Guid budgetId, CancellationToken ct);
    Task LeaveAsync(Guid budgetId, CancellationToken ct);
    Task<MonthSummaryDto> GetSummaryAsync(Guid budgetId, DateOnly month, CancellationToken ct);
    Task<YearSummaryDto> GetYearSummaryAsync(Guid budgetId, int year, CancellationToken ct);
}
