using Kopeck.Domain.Common;

namespace Kopeck.Domain.Savings;

/// <summary>Money moved into a pot (positive amount) or taken out of it (negative amount) on a date.</summary>
public sealed class SavingsEntry : AuditedEntity, IBudgetOwned
{
    private SavingsEntry() { }   // EF Core

    private SavingsEntry(Guid budgetId, Guid potId, DateOnly date, decimal amount, string? note)
    {
        BudgetId = budgetId;
        PotId = potId;
        Date = date;
        Amount = amount;
        Note = DomainRules.CleanNote(note);
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid BudgetId { get; private set; }
    public Guid PotId { get; private set; }
    public DateOnly Date { get; private set; }

    /// <summary>Signed: deposits are positive, withdrawals negative.</summary>
    public decimal Amount { get; private set; }

    public string? Note { get; private set; }

    public bool IsWithdrawal => Amount < 0;

    public static SavingsEntry Deposit(SavingsPot pot, DateOnly date, decimal amount, string? note) =>
        new(pot.BudgetId, pot.Id, date, amount, note);

    public static SavingsEntry Withdrawal(SavingsPot pot, DateOnly date, decimal amount, string? note) =>
        new(pot.BudgetId, pot.Id, date, -amount, note);
}
