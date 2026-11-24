using Kopeck.Contracts;

namespace Kopeck.Mobile.Services;

public interface ICategoriesApi
{
    Task<List<CategoryDto>> ListAsync(Guid budgetId, CancellationToken ct);
    Task<CategoryDto> CreateAsync(Guid budgetId, CategoryRequest request, CancellationToken ct);
    Task<CategoryDto> UpdateAsync(Guid budgetId, Guid categoryId, CategoryRequest request, CancellationToken ct);
    Task DeleteAsync(Guid budgetId, Guid categoryId, CancellationToken ct);
}
