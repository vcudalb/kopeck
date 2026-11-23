using Kopeck.Api.Data;
using Kopeck.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Kopeck.Api.Services;

/// <summary>Pure rule for the colour/status of a category row, kept separate so it can be unit tested.</summary>
public static class CategoryStatusRule
{
    public const decimal WarningThreshold = 0.8m;

    public static CategoryStatus For(decimal planned, decimal spent) =>
        planned == 0 && spent == 0 ? CategoryStatus.Unused
        : planned == 0 ? CategoryStatus.Unplanned
        : spent > planned ? CategoryStatus.Over
        : spent >= planned * WarningThreshold ? CategoryStatus.Warning
        : CategoryStatus.OnTrack;
}

public interface ISummaryService
{
    Task<MonthSummaryDto> BuildAsync(BudgetEntity budget, DateOnly month, CancellationToken ct);
}

/// <summary>
/// The Excel "Left" column (<c>Planned - SUMIF(expenses in category)</c>) plus the part the spreadsheet lacked:
/// <c>OverBy</c> says by how much a category went over its plan.
/// Sums run in memory for a single month because SQLite can't SUM decimals server-side.
/// </summary>
public sealed class SummaryService(KopeckDb db) : ISummaryService
{
    public async Task<MonthSummaryDto> BuildAsync(BudgetEntity budget, DateOnly month, CancellationToken ct)
    {
        var start = new DateOnly(month.Year, month.Month, 1);
        var end = start.AddMonths(1);

        var categories = await db.Categories.AsNoTracking().Where(c => c.BudgetId == budget.Id).OrderBy(c => c.Name).ToListAsync(ct);
        var planned = await db.Plans.AsNoTracking().Where(p => p.BudgetId == budget.Id && p.Month == start)
            .ToDictionaryAsync(p => p.CategoryId, p => p.Amount, ct);
        var expenses = await db.Expenses.AsNoTracking().Where(e => e.BudgetId == budget.Id && e.Date >= start && e.Date < end)
            .Select(e => new { e.CategoryId, e.Amount }).ToListAsync(ct);
        var spent = expenses.GroupBy(e => e.CategoryId).ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
        var incomes = await db.Incomes.AsNoTracking().Where(i => i.BudgetId == budget.Id && i.Date >= start && i.Date < end)
            .Select(i => new { i.StreamId, StreamName = i.Stream!.Name, i.Amount }).ToListAsync(ct);

        var rows = categories.Select(c =>
        {
            var p = planned.GetValueOrDefault(c.Id);
            var s = spent.GetValueOrDefault(c.Id);
            var percent = p > 0 ? Math.Round(s / p * 100, 1) : s > 0 ? 100 : 0;
            return new CategorySummaryDto(c.Id, c.Name, c.Color, p, s, p - s, Math.Max(0, s - p), percent, CategoryStatusRule.For(p, s));
        }).OrderByDescending(r => r.OverBy).ThenBy(r => r.Name).ToList();

        var streams = incomes.GroupBy(i => (i.StreamId, i.StreamName))
            .Select(g => new IncomeStreamSummaryDto(g.Key.StreamId, g.Key.StreamName, g.Sum(x => x.Amount)))
            .OrderByDescending(s => s.Amount).ToList();

        var totalIncome = streams.Sum(s => s.Amount);
        var totalPlanned = rows.Sum(r => r.Planned);
        var totalSpent = rows.Sum(r => r.Spent);
        return new MonthSummaryDto(start, budget.Currency, totalIncome, totalPlanned, totalSpent,
            rows.Sum(r => r.OverBy), totalIncome - totalPlanned, totalIncome - totalSpent, rows, streams);
    }
}
