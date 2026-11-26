using Kopeck.Contracts;

namespace Kopeck.Mobile.Services;

public interface IExpensesApi
{
    Task<List<ExpenseDto>> ListAsync(Guid budgetId, DateOnly month, CancellationToken ct);
    Task<ExpenseDto> CreateAsync(Guid budgetId, ExpenseRequest request, CancellationToken ct);
    Task<ExpenseDto> UpdateAsync(Guid budgetId, Guid expenseId, ExpenseRequest request, CancellationToken ct);
    Task<List<EntryChangeDto>> HistoryAsync(Guid budgetId, Guid expenseId, CancellationToken ct);
    Task DeleteAsync(Guid budgetId, Guid expenseId, CancellationToken ct);
}
