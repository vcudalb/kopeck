using Kopeck.Domain.Common;

namespace Kopeck.Domain.Incomes;

/// <summary>Money received on a date, booked against one stream.</summary>
public sealed class Income : AuditedEntity, IBudgetOwned
{
    private Income() { }   // EF Core

    public Income(Guid budgetId, Guid streamId, DateOnly date, decimal amount, string? note)
    {
        BudgetId = budgetId;
        StreamId = streamId;
        Date = date;
        Amount = amount;
        Note = DomainRules.CleanNote(note);
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid BudgetId { get; private set; }
    public Guid StreamId { get; private set; }
    public IncomeStream? Stream { get; private set; }
    public DateOnly Date { get; private set; }
    public decimal Amount { get; private set; }
    public string? Note { get; private set; }
}
