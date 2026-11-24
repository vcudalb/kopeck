using Kopeck.Application.Abstractions;
using Kopeck.Application.Common;
using Kopeck.Contracts;
using Kopeck.Domain.Categories;
using Kopeck.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Kopeck.Application.Categories;

/// <summary>Category names are unique per budget (case-insensitive). Two racing creates are caught by the unique index (409).</summary>
public sealed class CategoryService(IKopeckDbContext db) : ICategoryService
{
    private static readonly string NameRule = $"Enter a category name (max {DomainRules.MaxNameLength} characters).";
    private static readonly Error NameTaken = Error.Conflict("A category with this name already exists.");

    public async Task<IReadOnlyList<CategoryDto>> ListAsync(Guid budgetId, CancellationToken ct)
    {
        var categories = await db.Categories.AsNoTracking().InBudget(budgetId).OrderBy(c => c.Name).ToListAsync(ct);
        return categories.Select(c => c.ToDto()).ToList();
    }

    public async Task<Result<CategoryDto>> CreateAsync(Guid budgetId, CategoryRequest request, CancellationToken ct)
    {
        if (!DomainRules.IsValidName(request.Name)) return Error.Validation(NameRule);
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
        if (!DomainRules.IsValidName(request.Name)) return Error.Validation(NameRule);
        if (await IsNameTakenAsync(budgetId, request.Name, except: categoryId, ct)) return NameTaken;

        category.Rename(request.Name);
        category.Recolor(request.Color);
        await db.SaveChangesAsync(ct);
        return category.ToDto();
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
