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
