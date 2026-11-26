using Kopeck.Application.Common;
using Kopeck.Contracts;

namespace Kopeck.Application.Expenses;

public interface IExpenseService
{
    Task<IReadOnlyList<ExpenseDto>> ListAsync(Guid budgetId, DateOnly month, Guid? categoryId, CancellationToken ct);
    Task<Result<ExpenseDto>> CreateAsync(Guid budgetId, ExpenseRequest request, CancellationToken ct);
    Task<Result<ExpenseDto>> UpdateAsync(Guid budgetId, Guid expenseId, ExpenseRequest request, CancellationToken ct);
    Task<Result<IReadOnlyList<EntryChangeDto>>> HistoryAsync(Guid budgetId, Guid expenseId, CancellationToken ct);
    Task<Result> DeleteAsync(Guid budgetId, Guid expenseId, CancellationToken ct);
}
