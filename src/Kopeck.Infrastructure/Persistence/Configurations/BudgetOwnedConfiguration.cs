using Kopeck.Domain.Budgets;
using Kopeck.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kopeck.Infrastructure.Persistence.Configurations;

internal static class BudgetOwnedConfiguration
{
    /// <summary>Everything that belongs to a budget is removed by the database when the budget is deleted.</summary>
    public static void BelongsToBudget<T>(this EntityTypeBuilder<T> e) where T : class, IBudgetOwned =>
        e.HasOne<Budget>().WithMany().HasForeignKey(x => x.BudgetId).OnDelete(DeleteBehavior.Cascade);
}
