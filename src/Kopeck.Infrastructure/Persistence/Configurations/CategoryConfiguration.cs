using Kopeck.Domain.Categories;
using Kopeck.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kopeck.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> e)
    {
        e.BelongsToBudget();
        e.HasIndex(c => new { c.BudgetId, c.Name }).IsUnique();
        e.Property(c => c.Name).HasMaxLength(DomainRules.MaxNameLength);
        e.Property(c => c.Color).HasMaxLength(DomainRules.ColorLength);
    }
}
