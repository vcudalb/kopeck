using Kopeck.Contracts;
using Kopeck.Domain.Budgets;
using Kopeck.Domain.Categories;
using Kopeck.Domain.Common;
using Kopeck.Domain.Expenses;
using Kopeck.Domain.Incomes;
using Kopeck.Domain.Planning;
using Kopeck.Domain.Savings;

namespace Kopeck.Application.Common;

/// <summary>Domain -> wire contract mapping in one place. Enum maps are explicit so reordering one side can't silently corrupt the other.</summary>
public static class Mappings
{
    public static BudgetRole ToContract(this AccessLevel level) => level switch
    {
        AccessLevel.Viewer => BudgetRole.Viewer,
        AccessLevel.Editor => BudgetRole.Editor,
        AccessLevel.Owner => BudgetRole.Owner,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null),
    };

    public static ShareRole ToShareRole(this AccessLevel level) => level switch
    {
        AccessLevel.Viewer => ShareRole.Viewer,
        AccessLevel.Editor => ShareRole.Editor,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Ownership is not a share."),
    };

    public static AccessLevel? ToAccessLevel(this ShareRole role) => role switch
    {
        ShareRole.Viewer => AccessLevel.Viewer,
        ShareRole.Editor => AccessLevel.Editor,
        _ => null,
    };

    public static CategoryStatus ToContract(this SpendingStatus status) => status switch
    {
        SpendingStatus.Unused => CategoryStatus.Unused,
        SpendingStatus.OnTrack => CategoryStatus.OnTrack,
        SpendingStatus.Warning => CategoryStatus.Warning,
        SpendingStatus.Over => CategoryStatus.Over,
        SpendingStatus.Unplanned => CategoryStatus.Unplanned,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    public static CategoryDto ToDto(this Category c, bool isStarred = false) => new(c.Id, c.Name, c.Color, isStarred);
    public static IncomeStreamDto ToDto(this IncomeStream s) => new(s.Id, s.Name, s.Currency);
    public static ShareDto ToDto(this BudgetShare s) => new(s.Id, s.Email, s.Role.ToShareRole());
    public static ExpenseDto ToDto(this Expense e, string categoryName) => new(e.Id, e.CategoryId, categoryName, e.Date, e.Amount, e.Note, e.CreatedByName,
        Utc(e.CreatedAt), e.UpdatedAt is { } u ? Utc(u) : null, e.UpdatedByName);
    public static IncomeDto ToDto(this Income i, IncomeStream stream) => new(i.Id, i.StreamId, stream.Name, i.Date, i.Amount, i.Note, stream.Currency,
        i.CreatedByName, Utc(i.CreatedAt));
    public static SavingsPotDto ToDto(this SavingsPot p, decimal balance) => new(p.Id, p.Name, p.Color, p.Currency, p.Target, balance);
    public static SavingsEntryDto ToDto(this SavingsEntry e) => new(e.Id, e.PotId, e.Date, e.Amount, e.Note, e.CreatedByName, Utc(e.CreatedAt));

    public static EntryChangeDto ToDto(this Kopeck.Domain.History.EntryChange c) => new(c.Action switch
        {
            Kopeck.Domain.History.EntryAction.Created => EntryAction.Created,
            Kopeck.Domain.History.EntryAction.Updated => EntryAction.Updated,
            _ => EntryAction.Deleted,
        }, Utc(c.At), c.ByName, c.Changes.Select(f => new FieldChangeDto(f.Field, f.Old, f.New)).ToList());

    /// <summary>SQLite gives times back without a kind; they are stored as UTC.</summary>
    public static DateTime Utc(DateTime t) => DateTime.SpecifyKind(t, DateTimeKind.Utc);

    /// <summary>Rows of one budget. Every query on budget data goes through here, so none can forget the budget filter.</summary>
    public static IQueryable<T> InBudget<T>(this IQueryable<T> query, Guid budgetId) where T : class, IBudgetOwned =>
        query.Where(x => x.BudgetId == budgetId);

    /// <summary>Rows dated within the month that contains <paramref name="month"/>.</summary>
    public static (DateOnly Start, DateOnly End) MonthRange(this DateOnly month)
    {
        var start = month.FirstOfMonth();
        return (start, start.AddMonths(1));
    }
}
