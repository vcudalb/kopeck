using Kopeck.Domain.Common;

namespace Kopeck.Domain.Incomes;

/// <summary>A revenue stream such as "Main salary" or "Freelance". Every entry booked on it is in the stream's currency.</summary>
public sealed class IncomeStream : IBudgetOwned
{
    private IncomeStream() { }   // EF Core

    public IncomeStream(Guid budgetId, string name, string currency)
    {
        BudgetId = budgetId;
        Name = name.Trim();
        Currency = currency;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid BudgetId { get; private set; }
    public string Name { get; private set; } = "";
    public string Currency { get; private set; } = "";
}
