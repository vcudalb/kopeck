namespace Kopeck.Domain.Categories;

/// <summary>
/// A category one person marked as a favourite. Stars are personal: in a shared budget each member has their own,
/// so starring never reorders anyone else's lists.
/// </summary>
public sealed class CategoryStar
{
    private CategoryStar() { }   // EF Core

    public CategoryStar(Guid userId, Guid categoryId)
    {
        UserId = userId;
        CategoryId = categoryId;
    }

    public Guid UserId { get; private set; }
    public Guid CategoryId { get; private set; }
    public DateTime StarredAt { get; private set; } = DateTime.UtcNow;
}
