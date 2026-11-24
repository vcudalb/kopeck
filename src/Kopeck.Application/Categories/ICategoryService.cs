using Kopeck.Application.Common;
using Kopeck.Contracts;

namespace Kopeck.Application.Categories;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> ListAsync(Guid budgetId, CancellationToken ct);
    Task<Result<CategoryDto>> CreateAsync(Guid budgetId, CategoryRequest request, CancellationToken ct);
    Task<Result<CategoryDto>> UpdateAsync(Guid budgetId, Guid categoryId, CategoryRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(Guid budgetId, Guid categoryId, CancellationToken ct);
}
