using Kopeck.Domain.Common;
using Kopeck.Domain.Expenses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kopeck.Infrastructure.Persistence.Configurations;

internal sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> e)
    {
        e.BelongsToBudget();
        e.HasIndex(x => new { x.BudgetId, x.Date });
        e.Property(x => x.Amount).IsMoney();
        e.Property(x => x.Note).HasMaxLength(DomainRules.MaxNoteLength);
        e.IsAudited();
        // A category with expenses can't be removed from under them.
        e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.NoAction);
    }
}
