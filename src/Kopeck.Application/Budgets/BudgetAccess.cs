using Kopeck.Domain.Budgets;

namespace Kopeck.Application.Budgets;

/// <summary>Who the caller is for one budget.</summary>
public sealed record BudgetAccess(Budget Budget, AccessLevel Level)
{
    public bool Allows(AccessLevel minimum) => Level >= minimum;
}
