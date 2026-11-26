using Kopeck.Contracts;
using Kopeck.Domain.Budgets;

namespace Kopeck.Application.Planning;

public interface ISummaryService
{
    Task<MonthSummaryDto> BuildAsync(Budget budget, DateOnly month, CancellationToken ct);

    /// <summary>Month-by-month totals and per-category spending for one calendar year (budget currency only).</summary>
    Task<YearSummaryDto> BuildYearAsync(Budget budget, int year, CancellationToken ct);
}
