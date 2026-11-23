using Kopeck.Api.Data;
using Kopeck.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Kopeck.Api.Endpoints;

/// <summary>Planned spend per category per month.</summary>
public static class PlanEndpoints
{
    public static void MapPlans(this IEndpointRouteBuilder app)
    {
        var read = app.MapBudgetGroup().MapGroup("/plans");
        var write = app.MapBudgetGroup().RequireRole(BudgetRole.Editor).MapGroup("/plans");

        // Every category with its planned amount for the month (0 when not planned yet).
        read.MapGet("/", async (Guid budgetId, DateOnly month, KopeckDb db, CancellationToken ct) =>
        {
            var m = Rules.FirstOfMonth(month);
            var categories = await db.Categories.AsNoTracking().Where(c => c.BudgetId == budgetId).OrderBy(c => c.Name).ToListAsync(ct);
            var plans = await db.Plans.AsNoTracking().Where(p => p.BudgetId == budgetId && p.Month == m).ToDictionaryAsync(p => p.CategoryId, p => p.Amount, ct);
            return Results.Ok(categories.Select(c => new PlanDto(c.Id, c.Name, plans.GetValueOrDefault(c.Id))));
        });

        // Upsert: sets the planned amount for one category/month (0 clears it).
        write.MapPut("/", async (Guid budgetId, PlanRequest r, KopeckDb db) =>
        {
            if (!Rules.IsValidAmount(r.Amount, allowZero: true)) return Rules.Bad("Planned amount must be 0 or more (max 2 decimals).");
            if (!await db.Categories.AnyAsync(c => c.Id == r.CategoryId && c.BudgetId == budgetId)) return Rules.Bad("Unknown category.");

            var m = Rules.FirstOfMonth(r.Month);
            var plan = await db.Plans.FirstOrDefaultAsync(p => p.BudgetId == budgetId && p.CategoryId == r.CategoryId && p.Month == m);
            if (plan is null) db.Plans.Add(new PlannedExpense { BudgetId = budgetId, CategoryId = r.CategoryId, Month = m, Amount = r.Amount });
            else plan.Amount = r.Amount;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // Same workflow as duplicating last month's sheet in the Excel file. Never overwrites existing plans.
        write.MapPost("/copy", async (Guid budgetId, CopyPlanRequest r, KopeckDb db) =>
        {
            var from = Rules.FirstOfMonth(r.FromMonth); var to = Rules.FirstOfMonth(r.ToMonth);
            if (from == to) return Rules.Bad("Choose two different months.");

            var existing = await db.Plans.Where(p => p.BudgetId == budgetId && p.Month == to).Select(p => p.CategoryId).ToListAsync();
            var copies = await db.Plans.AsNoTracking().Where(p => p.BudgetId == budgetId && p.Month == from && !existing.Contains(p.CategoryId))
                .Select(p => new PlannedExpense { BudgetId = budgetId, CategoryId = p.CategoryId, Month = to, Amount = p.Amount }).ToListAsync();
            db.Plans.AddRange(copies);
            await db.SaveChangesAsync();
            return Results.Ok(new { copied = copies.Count });
        });
    }
}
