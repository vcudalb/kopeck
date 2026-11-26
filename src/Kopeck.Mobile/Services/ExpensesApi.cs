using Kopeck.Contracts;

namespace Kopeck.Mobile.Services;

public sealed class ExpensesApi(IApiClient api) : IExpensesApi
{
    public Task<List<ExpenseDto>> ListAsync(Guid budgetId, DateOnly month, CancellationToken ct) =>
        api.GetAsync<List<ExpenseDto>>($"{Routes.Budget(budgetId)}/expenses?month={Routes.Month(month)}", ct);
    public Task<ExpenseDto> CreateAsync(Guid budgetId, ExpenseRequest request, CancellationToken ct) =>
        api.PostAsync<ExpenseDto>($"{Routes.Budget(budgetId)}/expenses", request, ct);
    public Task<ExpenseDto> UpdateAsync(Guid budgetId, Guid expenseId, ExpenseRequest request, CancellationToken ct) =>
        api.PutAsync<ExpenseDto>($"{Routes.Budget(budgetId)}/expenses/{expenseId}", request, ct);
    public Task<List<EntryChangeDto>> HistoryAsync(Guid budgetId, Guid expenseId, CancellationToken ct) =>
        api.GetAsync<List<EntryChangeDto>>($"{Routes.Budget(budgetId)}/expenses/{expenseId}/history", ct);
    public Task DeleteAsync(Guid budgetId, Guid expenseId, CancellationToken ct) => api.DeleteAsync($"{Routes.Budget(budgetId)}/expenses/{expenseId}", ct);
}
