using Kopeck.Application.Abstractions;
using Kopeck.Application.Common;
using Kopeck.Contracts;
using Kopeck.Domain.Budgets;
using Kopeck.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Kopeck.Application.Budgets;

/// <summary>The owner shares a budget by email as Viewer or Editor. Sharing again with the same email changes the role.</summary>
public sealed class SharingService(IKopeckDbContext db, ICurrentUser user) : ISharingService
{
    public async Task<IReadOnlyList<ShareDto>> ListAsync(Guid budgetId, CancellationToken ct)
    {
        var shares = await db.Shares.AsNoTracking().InBudget(budgetId).OrderBy(s => s.Email).ToListAsync(ct);
        return shares.Select(s => s.ToDto()).ToList();
    }

    public async Task<Result<ShareDto>> ShareAsync(Guid budgetId, ShareRequest request, CancellationToken ct)
    {
        var email = DomainRules.NormalizeEmail(request.Email);
        if (email is null) return Error.Validation("Enter a valid email address.");
        if (email == user.Email) return Error.Validation("You already own this budget.");
        if (request.Role.ToAccessLevel() is not { } level) return Error.Validation("Unknown access level.");

        var share = await db.Shares.InBudget(budgetId).FirstOrDefaultAsync(s => s.Email == email, ct);
        if (share is null) db.Shares.Add(share = new BudgetShare(budgetId, email, level));
        else share.ChangeRole(level);

        await db.SaveChangesAsync(ct);
        return share.ToDto();
    }

    public async Task<Result> RevokeAsync(Guid budgetId, Guid shareId, CancellationToken ct)
    {
        var share = await db.Shares.InBudget(budgetId).FirstOrDefaultAsync(s => s.Id == shareId, ct);
        if (share is null) return Error.NotFound;

        db.Shares.Remove(share);
        await db.SaveChangesAsync(ct);
        return Result.Success;
    }
}
