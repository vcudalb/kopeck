using Kopeck.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Kopeck.Application.Budgets;

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
