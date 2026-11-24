using Kopeck.Application.Common;
using Kopeck.Contracts;

namespace Kopeck.Application.Budgets;

public interface ISharingService
{
    Task<IReadOnlyList<ShareDto>> ListAsync(Guid budgetId, CancellationToken ct);
    Task<Result<ShareDto>> ShareAsync(Guid budgetId, ShareRequest request, CancellationToken ct);
    Task<Result> RevokeAsync(Guid budgetId, Guid shareId, CancellationToken ct);
}
