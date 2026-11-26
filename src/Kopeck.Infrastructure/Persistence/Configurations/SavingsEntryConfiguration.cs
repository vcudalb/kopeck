using Kopeck.Domain.Common;
using Kopeck.Domain.Savings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kopeck.Infrastructure.Persistence.Configurations;

internal sealed class SavingsEntryConfiguration : IEntityTypeConfiguration<SavingsEntry>
{
    public void Configure(EntityTypeBuilder<SavingsEntry> e)
    {
        e.BelongsToBudget();
        e.HasIndex(x => new { x.BudgetId, x.Date });
        e.Property(x => x.Amount).IsMoney();
        e.Property(x => x.Note).HasMaxLength(DomainRules.MaxNoteLength);
        e.IsAudited();
        // Deleting a pot deletes its history (the app asks first).
        e.HasOne<SavingsPot>().WithMany().HasForeignKey(x => x.PotId).OnDelete(DeleteBehavior.Cascade);
        e.Ignore(x => x.IsWithdrawal);
    }
}
