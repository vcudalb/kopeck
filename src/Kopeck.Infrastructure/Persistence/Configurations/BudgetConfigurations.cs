using Kopeck.Domain.Budgets;
using Kopeck.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kopeck.Infrastructure.Persistence.Configurations;

internal sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> e)
    {
        e.Property(x => x.Name).HasMaxLength(DomainRules.MaxNameLength);
        e.Property(x => x.Currency).HasMaxLength(DomainRules.MaxCurrencyLength);
        e.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class BudgetShareConfiguration : IEntityTypeConfiguration<BudgetShare>
{
    public void Configure(EntityTypeBuilder<BudgetShare> e)
    {
        e.BelongsToBudget();
        e.HasIndex(s => new { s.BudgetId, s.Email }).IsUnique();
        e.Property(s => s.Email).HasMaxLength(DomainRules.MaxEmailLength);
    }
}

internal static class BudgetOwnedConfiguration
{
    /// <summary>Everything that belongs to a budget is removed by the database when the budget is deleted.</summary>
    public static void BelongsToBudget<T>(this EntityTypeBuilder<T> e) where T : class, IBudgetOwned =>
        e.HasOne<Budget>().WithMany().HasForeignKey(x => x.BudgetId).OnDelete(DeleteBehavior.Cascade);
}
