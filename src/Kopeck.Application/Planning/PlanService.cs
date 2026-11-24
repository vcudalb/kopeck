using Kopeck.Application.Abstractions;
using Kopeck.Application.Common;
using Kopeck.Contracts;
using Kopeck.Domain.Common;
using Kopeck.Domain.Planning;
using Microsoft.EntityFrameworkCore;

namespace Kopeck.Application.Planning;

/// <summary>Planned spend per category per month (the Excel "Planned" column).</summary>
public sealed class PlanService(IKopeckDbContext db) : IPlanService
{
    private static readonly Error UnknownCategory = Error.Validation("Unknown category.");

    /// <summary>Every category with its planned amount for the month (0 when not planned yet).</summary>
    public async Task<IReadOnlyList<PlanDto>> ListAsync(Guid budgetId, DateOnly month, CancellationToken ct)
    {
        var m = month.FirstOfMonth();
        var categories = await db.Categories.AsNoTracking().InBudget(budgetId).OrderBy(c => c.Name).ToListAsync(ct);
        var plans = await db.Plans.AsNoTracking().InBudget(budgetId).Where(p => p.Month == m).ToDictionaryAsync(p => p.CategoryId, p => p.Amount, ct);
        return categories.Select(c => new PlanDto(c.Id, c.Name, plans.GetValueOrDefault(c.Id))).ToList();
    }

    /// <summary>Upsert of one category/month (0 clears it).</summary>
    public Task<Result> SetAsync(Guid budgetId, PlanRequest request, CancellationToken ct) =>
        SaveManyAsync(budgetId, new SavePlansRequest(request.Month, [new PlanAmount(request.CategoryId, request.Amount)]), ct);

    /// <summary>Upserts several categories at once. Everything is validated first and committed in one transaction: all or nothing.</summary>
    public async Task<Result> SaveManyAsync(Guid budgetId, SavePlansRequest request, CancellationToken ct)
    {
        if (request.Items is not { Count: > 0 }) return Result.Success;

        var ids = request.Items.Select(i => i.CategoryId).ToList();
        var known = await db.Categories.InBudget(budgetId).CountAsync(c => ids.Contains(c.Id), ct);
        if (known != ids.Count) return UnknownCategory;

        var month = request.Month.FirstOfMonth();
        var existing = await db.Plans.InBudget(budgetId).Where(p => p.Month == month && ids.Contains(p.CategoryId)).ToDictionaryAsync(p => p.CategoryId, ct);
        foreach (var item in request.Items)
        {
            if (existing.TryGetValue(item.CategoryId, out var plan)) plan.ChangeAmount(item.Amount);
            else db.Plans.Add(new PlannedExpense(budgetId, item.CategoryId, month, item.Amount));
        }
        await db.SaveChangesAsync(ct);
        return Result.Success;
    }

    /// <summary>Same workflow as duplicating last month's sheet in the Excel file. Never overwrites existing plans.</summary>
    public async Task<Result<CopyPlanResponse>> CopyMissingAsync(Guid budgetId, CopyPlanRequest request, CancellationToken ct)
    {
        var from = request.FromMonth.FirstOfMonth();
        var to = request.ToMonth.FirstOfMonth();

        var alreadyPlanned = await db.Plans.InBudget(budgetId).Where(p => p.Month == to).Select(p => p.CategoryId).ToListAsync(ct);
        var sources = await db.Plans.AsNoTracking().InBudget(budgetId)
            .Where(p => p.Month == from && !alreadyPlanned.Contains(p.CategoryId)).ToListAsync(ct);

        db.Plans.AddRange(sources.Select(p => p.CopyTo(to)));
        await db.SaveChangesAsync(ct);
        return new CopyPlanResponse(sources.Count);
    }
}
