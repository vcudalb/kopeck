using Kopeck.Application.Abstractions;
using Kopeck.Application.Common;
using Kopeck.Contracts;
using Kopeck.Domain.Budgets;
using Kopeck.Domain.Planning;
using Microsoft.EntityFrameworkCore;

namespace Kopeck.Application.Planning;

/// <summary>
/// The dashboard: planned vs real per category (maths in <see cref="PlanVsActual"/>), income per stream and money saved
/// per pot. Income streams and pots can be in other currencies than the budget; those amounts are reported per currency
/// and never converted. Sums run in memory for one month because SQLite can't SUM decimals server-side.
/// </summary>
public sealed class SummaryService(IKopeckDbContext db, ICurrentUser user) : ISummaryService
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
            .Select(i => new { i.StreamId, StreamName = i.Stream!.Name, i.Stream.Currency, i.Amount }).ToListAsync(ct);
        var savings = await (
                from e in db.SavingsEntries.AsNoTracking().InBudget(budget.Id)
                join p in db.SavingsPots.AsNoTracking() on e.PotId equals p.Id
                where e.Date >= start && e.Date < end
                select new { e.PotId, p.Name, p.Color, p.Currency, e.Amount })
            .ToListAsync(ct);

        var spent = expenses.GroupBy(e => e.CategoryId).ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
        var starred = await db.StarredInAsync(user.Id, budget.Id, ct);

        // the caller's starred categories first, then the most overspent
        var rows = categories
            .Select(c =>
            {
                var line = new PlanVsActual(planned.GetValueOrDefault(c.Id), spent.GetValueOrDefault(c.Id));
                return new CategorySummaryDto(c.Id, c.Name, c.Color, line.Planned, line.Spent, line.Left, line.OverBy, line.PercentUsed, line.Status.ToContract(), starred.Contains(c.Id));
            })
            .OrderByDescending(r => r.IsStarred).ThenByDescending(r => r.OverBy).ThenBy(r => r.Name)
            .ToList();

        var streams = incomes.GroupBy(i => (i.StreamId, i.StreamName, i.Currency))
            .Select(g => new IncomeStreamSummaryDto(g.Key.StreamId, g.Key.StreamName, g.Sum(x => x.Amount), g.Key.Currency))
            .OrderByDescending(s => s.Currency == budget.Currency).ThenByDescending(s => s.Amount)
            .ToList();

        var pots = savings.GroupBy(s => (s.PotId, s.Name, s.Color, s.Currency))
            .Select(g => new SavingsSummaryDto(g.Key.PotId, g.Key.Name, g.Key.Color, g.Sum(x => x.Amount), g.Key.Currency))
            .Where(p => p.Amount != 0)
            .OrderByDescending(p => p.Currency == budget.Currency).ThenByDescending(p => p.Amount)
            .ToList();

        var totalPlanned = rows.Sum(r => r.Planned);
        var totalSpent = rows.Sum(r => r.Spent);
        var totals = BuildTotals(budget.Currency, totalSpent, streams, pots);
        var main = totals[0];

        return new MonthSummaryDto(start, budget.Currency, main.Income, totalPlanned, totalSpent,
            rows.Sum(r => r.OverBy), main.Income - totalPlanned, main.Left, rows, streams,
            main.Saved, pots, totals);
    }

    public async Task<YearSummaryDto> BuildYearAsync(Budget budget, int year, CancellationToken ct)
    {
        var start = new DateOnly(year, 1, 1);
        var end = start.AddYears(1);
        var cur = budget.Currency;

        var categories = await db.Categories.AsNoTracking().InBudget(budget.Id).OrderBy(c => c.Name).ToListAsync(ct);
        var plans = await db.Plans.AsNoTracking().InBudget(budget.Id).Where(p => p.Month >= start && p.Month < end)
            .Select(p => new { p.CategoryId, p.Month.Month, p.Amount }).ToListAsync(ct);
        var expenses = await db.Expenses.AsNoTracking().InBudget(budget.Id).Where(e => e.Date >= start && e.Date < end)
            .Select(e => new { e.CategoryId, e.Date.Month, e.Amount }).ToListAsync(ct);
        var incomes = await db.Incomes.AsNoTracking().InBudget(budget.Id)
            .Where(i => i.Date >= start && i.Date < end && i.Stream!.Currency == cur)
            .Select(i => new { i.Date.Month, i.Amount }).ToListAsync(ct);
        var savings = await (
                from e in db.SavingsEntries.AsNoTracking().InBudget(budget.Id)
                join p in db.SavingsPots.AsNoTracking() on e.PotId equals p.Id
                where e.Date >= start && e.Date < end && p.Currency == cur
                select new { e.Date.Month, e.Amount })
            .ToListAsync(ct);

        static decimal[] ByMonth<T>(IEnumerable<T> rows, Func<T, int> month, Func<T, decimal> amount)
        {
            var totals = new decimal[12];
            foreach (var row in rows) totals[month(row) - 1] += amount(row);
            return totals;
        }

        var income = ByMonth(incomes, i => i.Month, i => i.Amount);
        var planned = ByMonth(plans, p => p.Month, p => p.Amount);
        var spent = ByMonth(expenses, e => e.Month, e => e.Amount);
        var saved = ByMonth(savings, s => s.Month, s => s.Amount);

        var months = Enumerable.Range(0, 12)
            .Select(m => new MonthTotalsDto(start.AddMonths(m), income[m], planned[m], spent[m], saved[m]))
            .ToList();

        var perCategory = categories
            .Select(c =>
            {
                var monthlySpent = ByMonth(expenses.Where(e => e.CategoryId == c.Id), e => e.Month, e => e.Amount);
                var monthlyPlanned = ByMonth(plans.Where(p => p.CategoryId == c.Id), p => p.Month, p => p.Amount);
                return new CategoryYearDto(c.Id, c.Name, monthlySpent.Sum(), monthlyPlanned.Sum(), monthlySpent, monthlyPlanned);
            })
            .ToList();

        return new YearSummaryDto(year, cur, months, perCategory);
    }

    /// <summary>One row per currency used this month; the budget currency always comes first (and carries the expenses).</summary>
    private static List<CurrencyTotalDto> BuildTotals(
        string budgetCurrency, decimal spent, IReadOnlyList<IncomeStreamSummaryDto> streams, IReadOnlyList<SavingsSummaryDto> pots)
    {
        var others = streams.Select(s => s.Currency).Concat(pots.Select(p => p.Currency))
            .Where(c => c != budgetCurrency).Distinct().Order();

        return new[] { budgetCurrency }.Concat(others)
            .Select(currency =>
            {
                var income = streams.Where(s => s.Currency == currency).Sum(s => s.Amount);
                var saved = pots.Where(p => p.Currency == currency).Sum(p => p.Amount);
                var spentHere = currency == budgetCurrency ? spent : 0;
                return new CurrencyTotalDto(currency, income, spentHere, saved, income - spentHere - saved);
            })
            .ToList();
    }
}
