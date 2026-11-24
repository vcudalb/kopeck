using Kopeck.Contracts;
using Kopeck.Domain.Budgets;
using Kopeck.Domain.Categories;
using Kopeck.Domain.Common;
using Kopeck.Domain.Expenses;
using Kopeck.Domain.Incomes;
using Kopeck.Domain.Planning;

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

    public static CategoryDto ToDto(this Category c) => new(c.Id, c.Name, c.Color);
    public static IncomeStreamDto ToDto(this IncomeStream s) => new(s.Id, s.Name);
    public static ShareDto ToDto(this BudgetShare s) => new(s.Id, s.Email, s.Role.ToShareRole());
    public static ExpenseDto ToDto(this Expense e, string categoryName) => new(e.Id, e.CategoryId, categoryName, e.Date, e.Amount, e.Note, e.CreatedByName);
    public static IncomeDto ToDto(this Income i, string streamName) => new(i.Id, i.StreamId, streamName, i.Date, i.Amount, i.Note);

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
