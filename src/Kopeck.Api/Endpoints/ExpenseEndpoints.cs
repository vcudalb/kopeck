using System.Security.Claims;
using Kopeck.Api.Auth;
using Kopeck.Api.Data;
using Kopeck.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Kopeck.Api.Endpoints;

public static class ExpenseEndpoints
{
    public static void MapExpenses(this IEndpointRouteBuilder app)
    {
        var read = app.MapBudgetGroup().MapGroup("/expenses");
        var write = app.MapBudgetGroup().RequireRole(BudgetRole.Editor).MapGroup("/expenses");

        read.MapGet("/", async (Guid budgetId, DateOnly month, Guid? categoryId, KopeckDb db, CancellationToken ct) =>
        {
            var start = Rules.FirstOfMonth(month); var end = start.AddMonths(1);
            var query = db.Expenses.AsNoTracking().Where(e => e.BudgetId == budgetId && e.Date >= start && e.Date < end);
            if (categoryId is { } c) query = query.Where(e => e.CategoryId == c);
            return Results.Ok(await query.OrderByDescending(e => e.Date)
                .Select(e => new ExpenseDto(e.Id, e.CategoryId, e.Category!.Name, e.Date, e.Amount, e.Note, e.CreatedByName)).ToListAsync(ct));
        });

        write.MapPost("/", async (Guid budgetId, ExpenseRequest r, KopeckDb db, ClaimsPrincipal user) =>
        {
            if (Validate(r) is { } error) return error;
            var category = await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == r.CategoryId && c.BudgetId == budgetId);
            if (category is null) return Rules.Bad("Unknown category.");

            var expense = new Expense
            {
                BudgetId = budgetId, CategoryId = category.Id, Date = r.Date, Amount = r.Amount, Note = Rules.CleanNote(r.Note),
                CreatedByUserId = user.UserId(), CreatedByName = user.UserName(),
            };
            db.Expenses.Add(expense);
            await db.SaveChangesAsync();
            return Results.Created($"/api/budgets/{budgetId}/expenses/{expense.Id}", ToDto(expense, category.Name));
        });

        write.MapPut("/{expenseId:guid}", async (Guid budgetId, Guid expenseId, ExpenseRequest r, KopeckDb db) =>
        {
            var expense = await db.Expenses.FirstOrDefaultAsync(e => e.Id == expenseId && e.BudgetId == budgetId);
            if (expense is null) return Results.NotFound();
            if (Validate(r) is { } error) return error;
            var category = await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == r.CategoryId && c.BudgetId == budgetId);
            if (category is null) return Rules.Bad("Unknown category.");

            (expense.CategoryId, expense.Date, expense.Amount, expense.Note) = (category.Id, r.Date, r.Amount, Rules.CleanNote(r.Note));
            await db.SaveChangesAsync();
            return Results.Ok(ToDto(expense, category.Name));
        });

        write.MapDelete("/{expenseId:guid}", async (Guid budgetId, Guid expenseId, KopeckDb db) =>
            await db.Expenses.Where(e => e.Id == expenseId && e.BudgetId == budgetId).ExecuteDeleteAsync() == 0
                ? Results.NotFound() : Results.NoContent());
    }

    private static IResult? Validate(ExpenseRequest r) =>
        Rules.IsValidAmount(r.Amount) ? null : Rules.Bad("Amount must be greater than 0 (max 2 decimals).");

    private static ExpenseDto ToDto(Expense e, string categoryName) =>
        new(e.Id, e.CategoryId, categoryName, e.Date, e.Amount, e.Note, e.CreatedByName);
}
