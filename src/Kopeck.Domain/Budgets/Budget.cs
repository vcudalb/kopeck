using Kopeck.Domain.Users;

namespace Kopeck.Domain.Budgets;

/// <summary>A budget is the dashboard: categories, plans, expenses and income all belong to one, and it is what gets shared.</summary>
public sealed class Budget
{
    private Budget() { }   // EF Core

    public Budget(string name, string currency, Guid ownerId)
    {
        Name = name.Trim();
        Currency = currency;
        OwnerId = ownerId;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = "";
    public string Currency { get; private set; } = "EUR";
    public Guid OwnerId { get; private set; }
    public User? Owner { get; private set; }

    public void Rename(string name) => Name = name.Trim();

    /// <summary>The caller's access: owner, the level of their share on this budget, or none.</summary>
    public AccessLevel? AccessFor(Guid userId, BudgetShare? share) =>
        OwnerId == userId ? AccessLevel.Owner
        : share is not null && share.BudgetId == Id ? share.Role
        : null;
}
