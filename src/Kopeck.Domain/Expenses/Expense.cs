using Kopeck.Domain.Categories;
using Kopeck.Domain.Common;

namespace Kopeck.Domain.Expenses;

public sealed class Expense : IBudgetOwned
{
    private Expense() { }   // EF Core

    public Expense(Guid budgetId, Guid categoryId, DateOnly date, decimal amount, string? note, Guid createdByUserId, string createdByName)
    {
        BudgetId = budgetId;
        CreatedByUserId = createdByUserId;
        CreatedByName = createdByName;
        Update(categoryId, date, amount, note);
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid BudgetId { get; private set; }
    public Guid CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public DateOnly Date { get; private set; }
    public decimal Amount { get; private set; }
    public string? Note { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public string? CreatedByName { get; private set; }

    public void Update(Guid categoryId, DateOnly date, decimal amount, string? note) =>
        (CategoryId, Date, Amount, Note) = (categoryId, date, amount, DomainRules.CleanNote(note));
}
