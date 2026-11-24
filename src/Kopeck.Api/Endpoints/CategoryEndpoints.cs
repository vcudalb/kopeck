using Kopeck.Api.Http;
using Kopeck.Application.Categories;
using Kopeck.Contracts;
using Kopeck.Domain.Budgets;

namespace Kopeck.Api.Endpoints;

public sealed class CategoryEndpoints : IEndpointModule
{
    public void Map(IEndpointRouteBuilder app)
    {
        var budget = app.MapBudgetGroup();
        var read = budget.MapGroup("/categories");
        var write = budget.RequireAccess(AccessLevel.Editor).MapGroup("/categories");

        read.MapGet("/", async (Guid budgetId, ICategoryService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(budgetId, ct)));

        write.MapPost("/", async (Guid budgetId, CategoryRequest r, ICategoryService svc, CancellationToken ct) =>
            (await svc.CreateAsync(budgetId, r, ct)).ToCreated(c => $"/api/budgets/{budgetId}/categories/{c.Id}"));

        write.MapPut("/{categoryId:guid}", async (Guid budgetId, Guid categoryId, CategoryRequest r, ICategoryService svc, CancellationToken ct) =>
            (await svc.UpdateAsync(budgetId, categoryId, r, ct)).ToHttp());

        write.MapDelete("/{categoryId:guid}", async (Guid budgetId, Guid categoryId, ICategoryService svc, CancellationToken ct) =>
            (await svc.DeleteAsync(budgetId, categoryId, ct)).ToHttp());
    }
}
