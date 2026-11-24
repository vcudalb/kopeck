using Kopeck.Application.Common;
using Kopeck.Contracts;

namespace Kopeck.Application.Budgets;

public interface IBudgetService
{
    Task<IReadOnlyList<BudgetDto>> ListMineAsync(CancellationToken ct);
    Task<Result<BudgetDto>> CreateAsync(BudgetRequest request, CancellationToken ct);
    Task<Result> RenameAsync(Guid budgetId, BudgetRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(Guid budgetId, CancellationToken ct);
    Task<Result> LeaveAsync(Guid budgetId, CancellationToken ct);
}
