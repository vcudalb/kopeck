using Kopeck.Application.Abstractions;
using Kopeck.Domain.Budgets;
using Microsoft.EntityFrameworkCore;

namespace Kopeck.Application.Budgets;

/// <summary>Who the caller is for one budget.</summary>
public sealed record BudgetAccess(Budget Budget, AccessLevel Level)
{
    public bool Allows(AccessLevel minimum) => Level >= minimum;
}

public interface IBudgetAccessResolver
{
    /// <summary>The caller's access to the budget, or null when they have none (or it doesn't exist; the two are not told apart).</summary>
    Task<BudgetAccess?> ResolveAsync(Guid budgetId, CancellationToken ct);
}

public sealed class BudgetAccessResolver(IKopeckDbContext db, ICurrentUser user) : IBudgetAccessResolver
{
    public async Task<BudgetAccess?> ResolveAsync(Guid budgetId, CancellationToken ct)
    {
        var budget = await db.Budgets.AsNoTracking().FirstOrDefaultAsync(b => b.Id == budgetId, ct);
        if (budget is null) return null;

        var share = budget.OwnerId == user.Id
            ? null
            : await db.Shares.AsNoTracking().FirstOrDefaultAsync(s => s.BudgetId == budgetId && s.Email == user.Email, ct);

        return budget.AccessFor(user.Id, share) is { } level ? new BudgetAccess(budget, level) : null;
    }
}
