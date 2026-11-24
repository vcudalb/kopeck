using Kopeck.Application.Common;
using Kopeck.Contracts;

namespace Kopeck.Application.Incomes;

public interface IIncomeService
{
    Task<IReadOnlyList<IncomeStreamDto>> ListStreamsAsync(Guid budgetId, CancellationToken ct);
    Task<Result<IncomeStreamDto>> CreateStreamAsync(Guid budgetId, IncomeStreamRequest request, CancellationToken ct);
    Task<Result> DeleteStreamAsync(Guid budgetId, Guid streamId, CancellationToken ct);

    Task<IReadOnlyList<IncomeDto>> ListAsync(Guid budgetId, DateOnly month, CancellationToken ct);
    Task<Result<IncomeDto>> CreateAsync(Guid budgetId, IncomeRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(Guid budgetId, Guid incomeId, CancellationToken ct);
}
