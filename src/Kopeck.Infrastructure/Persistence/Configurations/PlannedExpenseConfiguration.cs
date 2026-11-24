using Kopeck.Domain.Categories;
using Kopeck.Domain.Planning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kopeck.Infrastructure.Persistence.Configurations;

internal sealed class PlannedExpenseConfiguration : IEntityTypeConfiguration<PlannedExpense>
{
    public void Configure(EntityTypeBuilder<PlannedExpense> e)
    {
        e.BelongsToBudget();
        e.HasIndex(p => new { p.BudgetId, p.CategoryId, p.Month }).IsUnique();
        e.Property(p => p.Amount).IsMoney();
        e.HasOne<Category>().WithMany().HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Cascade);
    }
}
