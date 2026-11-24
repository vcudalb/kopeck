using Kopeck.Contracts;

namespace Kopeck.Mobile.Services;

public sealed class SharesApi(IApiClient api) : ISharesApi
{
    public Task<List<ShareDto>> ListAsync(Guid budgetId, CancellationToken ct) => api.GetAsync<List<ShareDto>>($"{Routes.Budget(budgetId)}/shares", ct);
    public Task<ShareDto> ShareAsync(Guid budgetId, ShareRequest request, CancellationToken ct) =>
        api.PostAsync<ShareDto>($"{Routes.Budget(budgetId)}/shares", request, ct);
    public Task RemoveAsync(Guid budgetId, Guid shareId, CancellationToken ct) => api.DeleteAsync($"{Routes.Budget(budgetId)}/shares/{shareId}", ct);
}
