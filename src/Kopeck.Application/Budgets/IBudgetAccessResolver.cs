namespace Kopeck.Application.Budgets;

public interface IBudgetAccessResolver
{
    /// <summary>The caller's access to the budget, or null when they have none (or it doesn't exist; the two are not told apart).</summary>
    Task<BudgetAccess?> ResolveAsync(Guid budgetId, CancellationToken ct);
}
