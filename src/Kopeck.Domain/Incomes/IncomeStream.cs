using Kopeck.Domain.Common;

namespace Kopeck.Domain.Incomes;

/// <summary>A revenue stream such as "Main salary" or "Freelance".</summary>
public sealed class IncomeStream : IBudgetOwned
{
    private IncomeStream() { }   // EF Core

    public IncomeStream(Guid budgetId, string name)
    {
        BudgetId = budgetId;
        Name = name.Trim();
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid BudgetId { get; private set; }
    public string Name { get; private set; } = "";
}
