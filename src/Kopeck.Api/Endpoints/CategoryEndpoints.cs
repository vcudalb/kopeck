using Kopeck.Api.Data;
using Kopeck.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Kopeck.Api.Endpoints;

public static class CategoryEndpoints
{
    public static void MapCategories(this IEndpointRouteBuilder app)
    {
        var read = app.MapBudgetGroup().MapGroup("/categories");
        var write = app.MapBudgetGroup().RequireRole(BudgetRole.Editor).MapGroup("/categories");

        read.MapGet("/", async (Guid budgetId, KopeckDb db, CancellationToken ct) =>
            Results.Ok(await db.Categories.AsNoTracking().Where(c => c.BudgetId == budgetId).OrderBy(c => c.Name)
                .Select(c => new CategoryDto(c.Id, c.Name, c.Color)).ToListAsync(ct)));

        write.MapPost("/", async (Guid budgetId, CategoryRequest r, KopeckDb db) =>
        {
            if (!Rules.IsValidName(r.Name)) return Rules.Bad("Enter a category name (max 80 characters).");
            var name = r.Name.Trim();
            if (await NameTaken(db, budgetId, name, except: null)) return Rules.Conflict("A category with this name already exists.");

            var category = new Category { BudgetId = budgetId, Name = name, Color = Rules.ValidColor(r.Color) ?? "#6C757D" };
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            return Results.Created($"/api/budgets/{budgetId}/categories/{category.Id}", new CategoryDto(category.Id, category.Name, category.Color));
        });

        write.MapPut("/{categoryId:guid}", async (Guid budgetId, Guid categoryId, CategoryRequest r, KopeckDb db) =>
        {
            var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId && c.BudgetId == budgetId);
            if (category is null) return Results.NotFound();
            if (!Rules.IsValidName(r.Name)) return Rules.Bad("Enter a category name (max 80 characters).");
            var name = r.Name.Trim();
            if (await NameTaken(db, budgetId, name, except: categoryId)) return Rules.Conflict("A category with this name already exists.");

            category.Name = name;
            category.Color = Rules.ValidColor(r.Color) ?? category.Color;
            await db.SaveChangesAsync();
            return Results.Ok(new CategoryDto(category.Id, category.Name, category.Color));
        });

        // Refused when expenses exist, so history is never lost silently. Its plans are removed by cascade.
        write.MapDelete("/{categoryId:guid}", async (Guid budgetId, Guid categoryId, KopeckDb db) =>
        {
            if (!await db.Categories.AnyAsync(c => c.Id == categoryId && c.BudgetId == budgetId)) return Results.NotFound();
            if (await db.Expenses.AnyAsync(e => e.CategoryId == categoryId)) return Rules.Conflict("This category has expenses. Move or delete them first.");
            await db.Categories.Where(c => c.Id == categoryId).ExecuteDeleteAsync();
            return Results.NoContent();
        });
    }

    private static Task<bool> NameTaken(KopeckDb db, Guid budgetId, string name, Guid? except) =>
        db.Categories.AnyAsync(c => c.BudgetId == budgetId && c.Id != except && c.Name.ToLower() == name.ToLower());
}
