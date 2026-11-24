using Kopeck.Domain.Common;
using Kopeck.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kopeck.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> e)
    {
        e.HasIndex(u => u.Email).IsUnique();
        e.Property(u => u.Email).HasMaxLength(DomainRules.MaxEmailLength);
        e.Property(u => u.DisplayName).HasMaxLength(DomainRules.MaxNameLength);
    }
}
