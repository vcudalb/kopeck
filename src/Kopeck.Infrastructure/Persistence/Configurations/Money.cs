using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kopeck.Infrastructure.Persistence.Configurations;

internal static class Money
{
    /// <summary>Amounts are stored with fixed precision (max 2 decimals is enforced by the domain rules).</summary>
    public static PropertyBuilder<decimal> IsMoney(this PropertyBuilder<decimal> p) => p.HasPrecision(18, 2);
}
