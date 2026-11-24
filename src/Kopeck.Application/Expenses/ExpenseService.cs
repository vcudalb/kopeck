using Kopeck.Application.Abstractions;
using Kopeck.Application.Common;
using Kopeck.Contracts;
using Kopeck.Domain.Categories;
using Kopeck.Domain.Common;
using Kopeck.Domain.Expenses;
using Microsoft.EntityFrameworkCore;

namespace Kopeck.Application.Expenses;

public interface IExpenseService
{
    Task<IReadOnlyList<ExpenseDto>> ListAsync(Guid budgetId, DateOnly month, Guid? categoryId, CancellationToken ct);
    Task<Result<ExpenseDto>> CreateAsync(Guid budgetId, ExpenseRequest request, CancellationToken ct);
    Task<Result<ExpenseDto>> UpdateAsync(Guid budgetId, Guid expenseId, ExpenseRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(Guid budgetId, Guid expenseId, CancellationToken ct);
}

/// <summary>Every expense belongs to a category of the same budget.</summary>
public sealed class ExpenseService(IKopeckDbContext db, ICurrentUser user) : IExpenseService
{
    private static readonly Error InvalidAmount = Error.Validation("Amount must be greater than 0 (max 2 decimals).");
    private static readonly Error UnknownCategory = Error.Validation("Unknown category.");

    public async Task<IReadOnlyList<ExpenseDto>> ListAsync(Guid budgetId, DateOnly month, Guid? categoryId, CancellationToken ct)
    {
        var (start, end) = month.MonthRange();
        var query = db.Expenses.AsNoTracking().InBudget(budgetId).Where(e => e.Date >= start && e.Date < end);
        if (categoryId is { } c) query = query.Where(e => e.CategoryId == c);

        return await query.OrderByDescending(e => e.Date)
            .Select(e => new ExpenseDto(e.Id, e.CategoryId, e.Category!.Name, e.Date, e.Amount, e.Note, e.CreatedByName))
            .ToListAsync(ct);
    }

    public async Task<Result<ExpenseDto>> CreateAsync(Guid budgetId, ExpenseRequest request, CancellationToken ct)
    {
        if (!DomainRules.IsValidAmount(request.Amount)) return InvalidAmount;
        var category = await FindCategoryAsync(budgetId, request.CategoryId, ct);
        if (category is null) return UnknownCategory;

        var expense = new Expense(budgetId, category.Id, request.Date, request.Amount, request.Note, user.Id, user.DisplayName);
        db.Expenses.Add(expense);
        await db.SaveChangesAsync(ct);
        return expense.ToDto(category.Name);
    }

    public async Task<Result<ExpenseDto>> UpdateAsync(Guid budgetId, Guid expenseId, ExpenseRequest request, CancellationToken ct)
    {
        var expense = await db.Expenses.InBudget(budgetId).FirstOrDefaultAsync(e => e.Id == expenseId, ct);
        if (expense is null) return Error.NotFound;
        if (!DomainRules.IsValidAmount(request.Amount)) return InvalidAmount;
        var category = await FindCategoryAsync(budgetId, request.CategoryId, ct);
        if (category is null) return UnknownCategory;

        expense.Update(category.Id, request.Date, request.Amount, request.Note);
        await db.SaveChangesAsync(ct);
        return expense.ToDto(category.Name);
    }

    public async Task<Result> DeleteAsync(Guid budgetId, Guid expenseId, CancellationToken ct)
    {
        var expense = await db.Expenses.InBudget(budgetId).FirstOrDefaultAsync(e => e.Id == expenseId, ct);
        if (expense is null) return Error.NotFound;

        db.Expenses.Remove(expense);
        await db.SaveChangesAsync(ct);
        return Result.Success;
    }

    private Task<Category?> FindCategoryAsync(Guid budgetId, Guid categoryId, CancellationToken ct) =>
        db.Categories.AsNoTracking().InBudget(budgetId).FirstOrDefaultAsync(c => c.Id == categoryId, ct);
}
