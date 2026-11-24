using Kopeck.Contracts;

namespace Kopeck.Mobile.Services;

public interface ISharesApi
{
    Task<List<ShareDto>> ListAsync(Guid budgetId, CancellationToken ct);
    Task<ShareDto> ShareAsync(Guid budgetId, ShareRequest request, CancellationToken ct);
    Task RemoveAsync(Guid budgetId, Guid shareId, CancellationToken ct);
}
