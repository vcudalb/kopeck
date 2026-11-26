using Kopeck.Domain.Common;
using Kopeck.Domain.Incomes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kopeck.Infrastructure.Persistence.Configurations;

internal sealed class IncomeConfiguration : IEntityTypeConfiguration<Income>
{
    public void Configure(EntityTypeBuilder<Income> e)
    {
        e.BelongsToBudget();
        e.HasIndex(x => new { x.BudgetId, x.Date });
        e.Property(x => x.Amount).IsMoney();
        e.Property(x => x.Note).HasMaxLength(DomainRules.MaxNoteLength);
        e.IsAudited();
        e.HasOne(x => x.Stream).WithMany().HasForeignKey(x => x.StreamId).OnDelete(DeleteBehavior.NoAction);
    }
}
