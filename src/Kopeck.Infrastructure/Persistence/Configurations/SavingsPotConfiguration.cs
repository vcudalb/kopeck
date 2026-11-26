using Kopeck.Domain.Common;
using Kopeck.Domain.Savings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kopeck.Infrastructure.Persistence.Configurations;

internal sealed class SavingsPotConfiguration : IEntityTypeConfiguration<SavingsPot>
{
    public void Configure(EntityTypeBuilder<SavingsPot> e)
    {
        e.BelongsToBudget();
        e.HasIndex(p => new { p.BudgetId, p.Name }).IsUnique();
        e.Property(p => p.Name).HasMaxLength(DomainRules.MaxNameLength);
        e.Property(p => p.Color).HasMaxLength(DomainRules.ColorLength);
        e.Property(p => p.Currency).HasMaxLength(DomainRules.MaxCurrencyLength);
        e.Property(p => p.Target).IsMoney();
    }
}
