using Kopeck.Contracts;
using Kopeck.Domain.Budgets;

namespace Kopeck.Application.Planning;

public interface ISummaryService
{
    Task<MonthSummaryDto> BuildAsync(Budget budget, DateOnly month, CancellationToken ct);
}
