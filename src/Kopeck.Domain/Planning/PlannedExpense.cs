using Kopeck.Domain.Common;

namespace Kopeck.Domain.Planning;

/// <summary>Planned spend for one category in one month (Month is always the 1st).</summary>
public sealed class PlannedExpense : IBudgetOwned
{
    private PlannedExpense() { }   // EF Core

    public PlannedExpense(Guid budgetId, Guid categoryId, DateOnly month, decimal amount)
    {
        BudgetId = budgetId;
        CategoryId = categoryId;
        Month = month.FirstOfMonth();
        Amount = amount;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid BudgetId { get; private set; }
    public Guid CategoryId { get; private set; }
    public DateOnly Month { get; private set; }
    public decimal Amount { get; private set; }

    public void ChangeAmount(decimal amount) => Amount = amount;

    /// <summary>The same plan for another month (used by "copy from previous month").</summary>
    public PlannedExpense CopyTo(DateOnly month) => new(BudgetId, CategoryId, month, Amount);
}
