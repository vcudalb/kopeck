using Kopeck.Application.Abstractions;
using Kopeck.Application.Common;
using Kopeck.Contracts;
using Kopeck.Domain.Budgets;
using Kopeck.Domain.Planning;
using Microsoft.EntityFrameworkCore;

namespace Kopeck.Application.Planning;

/// <summary>
/// The dashboard: planned vs real per category (maths in <see cref="PlanVsActual"/>) plus income per stream.
/// Sums run in memory for one month because SQLite can't SUM decimals server-side.
/// </summary>
public sealed class SummaryService(IKopeckDbContext db) : ISummaryService
{
    public async Task<MonthSummaryDto> BuildAsync(Budget budget, DateOnly month, CancellationToken ct)
    {
        var (start, end) = month.MonthRange();

        var categories = await db.Categories.AsNoTracking().InBudget(budget.Id).OrderBy(c => c.Name).ToListAsync(ct);
        var planned = await db.Plans.AsNoTracking().InBudget(budget.Id).Where(p => p.Month == start)
            .ToDictionaryAsync(p => p.CategoryId, p => p.Amount, ct);
        var expenses = await db.Expenses.AsNoTracking().InBudget(budget.Id).Where(e => e.Date >= start && e.Date < end)
            .Select(e => new { e.CategoryId, e.Amount }).ToListAsync(ct);
        var incomes = await db.Incomes.AsNoTracking().InBudget(budget.Id).Where(i => i.Date >= start && i.Date < end)
            .Select(i => new { i.StreamId, StreamName = i.Stream!.Name, i.Amount }).ToListAsync(ct);

        var spent = expenses.GroupBy(e => e.CategoryId).ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        var rows = categories
            .Select(c =>
            {
                var line = new PlanVsActual(planned.GetValueOrDefault(c.Id), spent.GetValueOrDefault(c.Id));
                return new CategorySummaryDto(c.Id, c.Name, c.Color, line.Planned, line.Spent, line.Left, line.OverBy, line.PercentUsed, line.Status.ToContract());
            })
            .OrderByDescending(r => r.OverBy).ThenBy(r => r.Name)
            .ToList();

        var streams = incomes.GroupBy(i => (i.StreamId, i.StreamName))
            .Select(g => new IncomeStreamSummaryDto(g.Key.StreamId, g.Key.StreamName, g.Sum(x => x.Amount)))
            .OrderByDescending(s => s.Amount)
            .ToList();

        var totalIncome = streams.Sum(s => s.Amount);
        var totalPlanned = rows.Sum(r => r.Planned);
        var totalSpent = rows.Sum(r => r.Spent);
        return new MonthSummaryDto(start, budget.Currency, totalIncome, totalPlanned, totalSpent,
            rows.Sum(r => r.OverBy), totalIncome - totalPlanned, totalIncome - totalSpent, rows, streams);
    }
}
