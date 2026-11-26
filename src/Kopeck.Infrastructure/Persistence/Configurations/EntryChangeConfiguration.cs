using System.Text.Json;
using Kopeck.Domain.Common;
using Kopeck.Domain.History;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kopeck.Infrastructure.Persistence.Configurations;

internal sealed class EntryChangeConfiguration : IEntityTypeConfiguration<EntryChange>
{
    public void Configure(EntityTypeBuilder<EntryChange> e)
    {
        // Kept when the entry is deleted; goes with the budget.
        e.BelongsToBudget();
        e.HasIndex(x => new { x.BudgetId, x.EntryId, x.At });
        e.Property(x => x.ByName).HasMaxLength(DomainRules.MaxNameLength);
        // The changed fields as a small JSON array: [{"Field":"Amount","Old":"20.00","New":"25.00"}]
        e.Property(x => x.Changes).HasConversion(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<FieldChange>>(v, (JsonSerializerOptions?)null) ?? new List<FieldChange>(),
            new ValueComparer<IReadOnlyList<FieldChange>>((a, b) => a!.SequenceEqual(b!), v => v.Count, v => v.ToList()));
    }
}
