using Kopeck.Application.Abstractions;
using Kopeck.Application.Common;
using Kopeck.Contracts;
using Kopeck.Domain.Categories;
using Kopeck.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Kopeck.Application.Categories;

/// <summary>
/// Category names are unique per budget (case-insensitive). Two racing creates are caught by the unique index (409).
/// Lists put the caller's starred categories first, then the rest by name.
/// </summary>
public sealed class CategoryService(IKopeckDbContext db, ICurrentUser user) : ICategoryService
{
    private static readonly Error NameTaken = Error.Conflict("A category with this name already exists.");

    public async Task<IReadOnlyList<CategoryDto>> ListAsync(Guid budgetId, CancellationToken ct)
    {
        var categories = await db.Categories.AsNoTracking().InBudget(budgetId).OrderBy(c => c.Name).ToListAsync(ct);
        var starred = await db.StarredInAsync(user.Id, budgetId, ct);
        return categories.Select(c => c.ToDto(starred.Contains(c.Id))).OrderByDescending(c => c.IsStarred).ToList();
    }

    /// <summary>Stars or unstars a category for the caller only. Repeating either is harmless.</summary>
    public async Task<Result<CategoryDto>> StarAsync(Guid budgetId, Guid categoryId, bool starred, CancellationToken ct)
    {
        var category = await db.Categories.AsNoTracking().InBudget(budgetId).FirstOrDefaultAsync(c => c.Id == categoryId, ct);
        if (category is null) return Error.NotFound;

        var star = await db.CategoryStars.FirstOrDefaultAsync(s => s.UserId == user.Id && s.CategoryId == categoryId, ct);
        if (starred && star is null) db.CategoryStars.Add(new CategoryStar(user.Id, categoryId));
        if (!starred && star is not null) db.CategoryStars.Remove(star);
        await db.SaveChangesAsync(ct);
        return category.ToDto(starred);
    }

    public async Task<Result<CategoryDto>> CreateAsync(Guid budgetId, CategoryRequest request, CancellationToken ct)
    {
        if (await IsNameTakenAsync(budgetId, request.Name, except: null, ct)) return NameTaken;

        var category = new Category(budgetId, request.Name, request.Color);
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        return category.ToDto();
    }

    public async Task<Result<CategoryDto>> UpdateAsync(Guid budgetId, Guid categoryId, CategoryRequest request, CancellationToken ct)
    {
        var category = await db.Categories.InBudget(budgetId).FirstOrDefaultAsync(c => c.Id == categoryId, ct);
        if (category is null) return Error.NotFound;
        if (await IsNameTakenAsync(budgetId, request.Name, except: categoryId, ct)) return NameTaken;

        category.Rename(request.Name);
        category.Recolor(request.Color);
        await db.SaveChangesAsync(ct);
        return category.ToDto(await db.CategoryStars.AnyAsync(s => s.UserId == user.Id && s.CategoryId == categoryId, ct));
    }

    /// <summary>Refused while expenses use the category, so history is never lost silently. Its plans go with it (cascade).</summary>
    public async Task<Result> DeleteAsync(Guid budgetId, Guid categoryId, CancellationToken ct)
    {
        var category = await db.Categories.InBudget(budgetId).FirstOrDefaultAsync(c => c.Id == categoryId, ct);
        if (category is null) return Error.NotFound;
        if (await db.Expenses.AnyAsync(e => e.CategoryId == categoryId, ct)) return Error.Conflict("This category has expenses. Move or delete them first.");

        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);
        return Result.Success;
    }

    private Task<bool> IsNameTakenAsync(Guid budgetId, string name, Guid? except, CancellationToken ct)
    {
        var lowered = name.Trim().ToLower();
        return db.Categories.InBudget(budgetId).AnyAsync(c => c.Id != except && c.Name.ToLower() == lowered, ct);
    }
}
