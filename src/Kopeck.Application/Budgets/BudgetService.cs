using Kopeck.Application.Abstractions;
using Kopeck.Application.Common;
using Kopeck.Contracts;
using Kopeck.Domain.Budgets;
using Kopeck.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Kopeck.Application.Budgets;

public interface IBudgetService
{
    Task<IReadOnlyList<BudgetDto>> ListMineAsync(CancellationToken ct);
    Task<Result<BudgetDto>> CreateAsync(BudgetRequest request, CancellationToken ct);
    Task<Result> RenameAsync(Guid budgetId, BudgetRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(Guid budgetId, CancellationToken ct);
    Task<Result> LeaveAsync(Guid budgetId, CancellationToken ct);
}

/// <summary>
/// Budget lifecycle. Methods taking a budget id assume the caller's access was already checked
/// (the API's budget access filter does that for every <c>/api/budgets/{budgetId}</c> route).
/// </summary>
public sealed class BudgetService(IKopeckDbContext db, ICurrentUser user) : IBudgetService
{
    private static readonly string NameRule = $"Enter a budget name (max {DomainRules.MaxNameLength} characters).";

    public async Task<IReadOnlyList<BudgetDto>> ListMineAsync(CancellationToken ct)
    {
        var shares = await db.Shares.AsNoTracking().Where(s => s.Email == user.Email).ToDictionaryAsync(s => s.BudgetId, s => s.Role, ct);
        var sharedIds = shares.Keys.ToList();
        var budgets = await db.Budgets.AsNoTracking().Include(b => b.Owner)
            .Where(b => b.OwnerId == user.Id || sharedIds.Contains(b.Id)).OrderBy(b => b.Name).ToListAsync(ct);

        return budgets
            .Select(b => new BudgetDto(b.Id, b.Name, b.Currency,
                (b.OwnerId == user.Id ? AccessLevel.Owner : shares[b.Id]).ToContract(), b.Owner!.Email))
            .ToList();
    }

    public async Task<Result<BudgetDto>> CreateAsync(BudgetRequest request, CancellationToken ct)
    {
        if (!DomainRules.IsValidName(request.Name)) return Error.Validation(NameRule);
        if (DomainRules.NormalizeCurrency(request.Currency) is not { } currency) return Error.Validation("Enter a currency code such as EUR or MDL.");

        var budget = new Budget(request.Name, currency, user.Id);
        db.Budgets.Add(budget);
        await db.SaveChangesAsync(ct);
        return new BudgetDto(budget.Id, budget.Name, budget.Currency, BudgetRole.Owner, user.Email);
    }

    /// <summary>Only the name can change: the currency is fixed once amounts exist in it.</summary>
    public async Task<Result> RenameAsync(Guid budgetId, BudgetRequest request, CancellationToken ct)
    {
        if (!DomainRules.IsValidName(request.Name)) return Error.Validation(NameRule);
        var budget = await db.Budgets.FirstOrDefaultAsync(b => b.Id == budgetId, ct);
        if (budget is null) return Error.NotFound;

        budget.Rename(request.Name);
        await db.SaveChangesAsync(ct);
        return Result.Success;
    }

    /// <summary>Everything under the budget is removed by the database cascade.</summary>
    public async Task<Result> DeleteAsync(Guid budgetId, CancellationToken ct)
    {
        var budget = await db.Budgets.FirstOrDefaultAsync(b => b.Id == budgetId, ct);
        if (budget is null) return Error.NotFound;

        db.Budgets.Remove(budget);
        await db.SaveChangesAsync(ct);
        return Result.Success;
    }

    /// <summary>A shared user removes themself from someone else's budget.</summary>
    public async Task<Result> LeaveAsync(Guid budgetId, CancellationToken ct)
    {
        var share = await db.Shares.InBudget(budgetId).FirstOrDefaultAsync(s => s.Email == user.Email, ct);
        if (share is null) return Error.NotFound;

        db.Shares.Remove(share);
        await db.SaveChangesAsync(ct);
        return Result.Success;
    }
}
