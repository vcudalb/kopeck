using Kopeck.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Kopeck.Application.Common;

internal static class CategoryStars
{
    /// <summary>The categories of a budget that this person starred.</summary>
    public static async Task<HashSet<Guid>> StarredInAsync(this IKopeckDbContext db, Guid userId, Guid budgetId, CancellationToken ct) =>
        (await (from s in db.CategoryStars.AsNoTracking()
                join c in db.Categories.AsNoTracking() on s.CategoryId equals c.Id
                where s.UserId == userId && c.BudgetId == budgetId
                select s.CategoryId).ToListAsync(ct)).ToHashSet();
}
