using Kopeck.Domain.Common;

namespace Kopeck.Domain.Budgets;

/// <summary>Keyed by (verified) email, so a share also works for someone who registers later.</summary>
public sealed class BudgetShare : IBudgetOwned
{
    private BudgetShare() { }   // EF Core

    public BudgetShare(Guid budgetId, string normalizedEmail, AccessLevel level)
    {
        BudgetId = budgetId;
        Email = normalizedEmail;
        ChangeRole(level);
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid BudgetId { get; private set; }
    public string Email { get; private set; } = "";            // lower-cased
    public AccessLevel Role { get; private set; }

    public void ChangeRole(AccessLevel level)
    {
        if (!level.IsShareable()) throw new ArgumentOutOfRangeException(nameof(level), level, "Only Viewer or Editor access can be shared.");
        Role = level;
    }
}
