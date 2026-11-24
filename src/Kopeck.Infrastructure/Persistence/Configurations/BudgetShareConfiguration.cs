using Kopeck.Domain.Budgets;
using Kopeck.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kopeck.Infrastructure.Persistence.Configurations;

internal sealed class BudgetShareConfiguration : IEntityTypeConfiguration<BudgetShare>
{
    public void Configure(EntityTypeBuilder<BudgetShare> e)
    {
        e.BelongsToBudget();
        e.HasIndex(s => new { s.BudgetId, s.Email }).IsUnique();
        e.Property(s => s.Email).HasMaxLength(DomainRules.MaxEmailLength);
    }
}
