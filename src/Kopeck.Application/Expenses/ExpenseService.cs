using Kopeck.Application.Abstractions;
using Kopeck.Application.Common;
using Kopeck.Application.History;
using Kopeck.Contracts;
using Kopeck.Domain.Categories;
using Kopeck.Domain.Common;
using Kopeck.Domain.Expenses;
using Kopeck.Domain.History;
using Microsoft.EntityFrameworkCore;

namespace Kopeck.Application.Expenses;

/// <summary>
/// Every expense belongs to a category of the same budget. Adding, changing and deleting are stamped (who, when) and
/// recorded in the expense's history with the values involved.
/// </summary>
public sealed class ExpenseService(IKopeckDbContext db, EntryAudit audit) : IExpenseService
{
    private static readonly Error UnknownCategory = Error.Validation("Unknown category.");

    public async Task<IReadOnlyList<ExpenseDto>> ListAsync(Guid budgetId, DateOnly month, Guid? categoryId, CancellationToken ct)
    {
        var (start, end) = month.MonthRange();
        var query = db.Expenses.AsNoTracking().InBudget(budgetId).Where(e => e.Date >= start && e.Date < end);
        if (categoryId is { } c) query = query.Where(e => e.CategoryId == c);

        // newest day first; on the same day, the most recently added first
        var expenses = await query.Include(e => e.Category).OrderByDescending(e => e.Date).ThenByDescending(e => e.CreatedAt).ToListAsync(ct);
        return expenses.Select(e => e.ToDto(e.Category!.Name)).ToList();
    }

    public async Task<Result<ExpenseDto>> CreateAsync(Guid budgetId, ExpenseRequest request, CancellationToken ct)
    {
        var category = await FindCategoryAsync(budgetId, request.CategoryId, ct);
        if (category is null) return UnknownCategory;

        var expense = new Expense(budgetId, category.Id, request.Date, request.Amount, request.Note);
        db.Expenses.Add(expense);
        audit.Created(expense, budgetId, EntryKind.Expense, expense.Id, Values(expense, category.Name));
        await db.SaveChangesAsync(ct);
        return expense.ToDto(category.Name);
    }

    public async Task<Result<ExpenseDto>> UpdateAsync(Guid budgetId, Guid expenseId, ExpenseRequest request, CancellationToken ct)
    {
        var expense = await db.Expenses.InBudget(budgetId).Include(e => e.Category).FirstOrDefaultAsync(e => e.Id == expenseId, ct);
        if (expense is null) return Error.NotFound;
        var category = await FindCategoryAsync(budgetId, request.CategoryId, ct);
        if (category is null) return UnknownCategory;

        var before = Values(expense, expense.Category!.Name);
        expense.Update(category.Id, request.Date, request.Amount, request.Note);
        if (audit.Updated(expense, budgetId, EntryKind.Expense, expense.Id, before, Values(expense, category.Name)))
            await db.SaveChangesAsync(ct);
        return expense.ToDto(category.Name);
    }

    public async Task<Result> DeleteAsync(Guid budgetId, Guid expenseId, CancellationToken ct)
    {
        var expense = await db.Expenses.InBudget(budgetId).Include(e => e.Category).FirstOrDefaultAsync(e => e.Id == expenseId, ct);
        if (expense is null) return Error.NotFound;

        audit.Deleted(budgetId, EntryKind.Expense, expense.Id, Values(expense, expense.Category!.Name));
        db.Expenses.Remove(expense);
        await db.SaveChangesAsync(ct);
        return Result.Success;
    }

    /// <summary>
    /// What happened to an expense, newest first. Expenses added before history was kept show a single "added" step
    /// with who and when, but no values.
    /// </summary>
    public async Task<Result<IReadOnlyList<EntryChangeDto>>> HistoryAsync(Guid budgetId, Guid expenseId, CancellationToken ct)
    {
        var expense = await db.Expenses.AsNoTracking().InBudget(budgetId).FirstOrDefaultAsync(e => e.Id == expenseId, ct);
        var changes = await db.EntryChanges.AsNoTracking().InBudget(budgetId)
            .Where(c => c.Kind == EntryKind.Expense && c.EntryId == expenseId)
            .OrderByDescending(c => c.At).ToListAsync(ct);
        if (expense is null && changes.Count == 0) return Error.NotFound;

        var history = changes.Select(c => c.ToDto()).ToList();
        if (expense is not null && changes.All(c => c.Action != Domain.History.EntryAction.Created))
            history.Add(new EntryChangeDto(Contracts.EntryAction.Created, Mappings.Utc(expense.CreatedAt), expense.CreatedByName ?? "Someone", []));
        return history;
    }

    private static IReadOnlyList<(string, string?)> Values(Expense e, string categoryName) =>
        [("Category", categoryName), ("Date", EntryAudit.Date(e.Date)), ("Amount", EntryAudit.Amount(e.Amount)), ("Note", e.Note)];

    private Task<Category?> FindCategoryAsync(Guid budgetId, Guid categoryId, CancellationToken ct) =>
        db.Categories.AsNoTracking().InBudget(budgetId).FirstOrDefaultAsync(c => c.Id == categoryId, ct);
}
