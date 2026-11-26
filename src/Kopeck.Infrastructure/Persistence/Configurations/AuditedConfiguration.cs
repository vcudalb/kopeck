using Kopeck.Domain.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kopeck.Infrastructure.Persistence.Configurations;

internal static class AuditedConfiguration
{
    /// <summary>Who added / last changed the entry and when (see <see cref="AuditedEntity"/>).</summary>
    public static void IsAudited<T>(this EntityTypeBuilder<T> e) where T : AuditedEntity
    {
        e.Property(x => x.CreatedByName).HasMaxLength(DomainRules.MaxNameLength);
        e.Property(x => x.UpdatedByName).HasMaxLength(DomainRules.MaxNameLength);
    }
}
