using Kopeck.Domain.Categories;
using Kopeck.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kopeck.Infrastructure.Persistence.Configurations;

internal sealed class CategoryStarConfiguration : IEntityTypeConfiguration<CategoryStar>
{
    public void Configure(EntityTypeBuilder<CategoryStar> e)
    {
        e.HasKey(x => new { x.UserId, x.CategoryId });
        // A deleted category (or user) takes its stars with it.
        e.HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Cascade);
        e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
