using Kopeck.Contracts;

namespace Kopeck.Mobile.Services;

public interface IIncomeApi
{
    Task<List<IncomeStreamDto>> ListStreamsAsync(Guid budgetId, CancellationToken ct);
    Task<IncomeStreamDto> CreateStreamAsync(Guid budgetId, IncomeStreamRequest request, CancellationToken ct);
    Task<List<IncomeDto>> ListAsync(Guid budgetId, DateOnly month, CancellationToken ct);
    Task<IncomeDto> CreateAsync(Guid budgetId, IncomeRequest request, CancellationToken ct);
    Task DeleteAsync(Guid budgetId, Guid incomeId, CancellationToken ct);
}
