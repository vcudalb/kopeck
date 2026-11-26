using Kopeck.Contracts;

namespace Kopeck.Mobile.Services;

public interface ICategoriesApi
{
    Task<List<CategoryDto>> ListAsync(Guid budgetId, CancellationToken ct);
    Task<CategoryDto> CreateAsync(Guid budgetId, CategoryRequest request, CancellationToken ct);
    Task<CategoryDto> UpdateAsync(Guid budgetId, Guid categoryId, CategoryRequest request, CancellationToken ct);
    /// <summary>Stars or unstars a category for the signed-in person only.</summary>
    Task SetStarredAsync(Guid budgetId, Guid categoryId, bool starred, CancellationToken ct);

    Task DeleteAsync(Guid budgetId, Guid categoryId, CancellationToken ct);
}
