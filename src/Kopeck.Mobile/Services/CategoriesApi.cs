using Kopeck.Contracts;

namespace Kopeck.Mobile.Services;

public sealed class CategoriesApi(IApiClient api) : ICategoriesApi
{
    public Task<List<CategoryDto>> ListAsync(Guid budgetId, CancellationToken ct) => api.GetAsync<List<CategoryDto>>($"{Routes.Budget(budgetId)}/categories", ct);
    public Task<CategoryDto> CreateAsync(Guid budgetId, CategoryRequest request, CancellationToken ct) =>
        api.PostAsync<CategoryDto>($"{Routes.Budget(budgetId)}/categories", request, ct);
    public Task<CategoryDto> UpdateAsync(Guid budgetId, Guid categoryId, CategoryRequest request, CancellationToken ct) =>
        api.PutAsync<CategoryDto>($"{Routes.Budget(budgetId)}/categories/{categoryId}", request, ct);
    public Task SetStarredAsync(Guid budgetId, Guid categoryId, bool starred, CancellationToken ct) => starred
        ? api.PutAsync($"{Routes.Budget(budgetId)}/categories/{categoryId}/star", null, ct)
        : api.DeleteAsync($"{Routes.Budget(budgetId)}/categories/{categoryId}/star", ct);
    public Task DeleteAsync(Guid budgetId, Guid categoryId, CancellationToken ct) => api.DeleteAsync($"{Routes.Budget(budgetId)}/categories/{categoryId}", ct);
}
