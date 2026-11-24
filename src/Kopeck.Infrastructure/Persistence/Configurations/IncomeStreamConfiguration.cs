using Kopeck.Domain.Common;
using Kopeck.Domain.Incomes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kopeck.Infrastructure.Persistence.Configurations;

internal sealed class IncomeStreamConfiguration : IEntityTypeConfiguration<IncomeStream>
{
    public void Configure(EntityTypeBuilder<IncomeStream> e)
    {
        e.BelongsToBudget();
        e.HasIndex(s => new { s.BudgetId, s.Name }).IsUnique();
        e.Property(s => s.Name).HasMaxLength(DomainRules.MaxNameLength);
    }
}
