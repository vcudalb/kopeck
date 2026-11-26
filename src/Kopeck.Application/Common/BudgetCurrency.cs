using Kopeck.Application.Abstractions;
using Kopeck.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Kopeck.Application.Common;

public static class BudgetCurrency
{
    /// <summary>
    /// The requested currency (normalized), or the budget's own currency when none was given. This is how a new income
    /// stream or savings pot starts in the budget's default currency.
    /// </summary>
    public static async Task<string> CurrencyOrDefaultAsync(this IKopeckDbContext db, Guid budgetId, string? requested, CancellationToken ct) =>
        DomainRules.NormalizeCurrency(requested)
        ?? await db.Budgets.Where(b => b.Id == budgetId).Select(b => b.Currency).FirstAsync(ct);
}
