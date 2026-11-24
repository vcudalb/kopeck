using Kopeck.Api.Filters;
using Kopeck.Application.Categories;
using Kopeck.Contracts;
using Kopeck.Domain.Budgets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kopeck.Api.Controllers;

[Authorize]
[Route("api/budgets/{budgetId:guid}/categories")]
[BudgetAccess]
public sealed class CategoriesController(ICategoryService categories) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid budgetId, CancellationToken ct) => Ok(await categories.ListAsync(budgetId, ct));

    [HttpPost]
    [BudgetAccess(AccessLevel.Editor)]
    [ServiceFilter(typeof(RequestValidationFilter<CategoryRequest>))]
    public async Task<IActionResult> Create(Guid budgetId, CategoryRequest request, CancellationToken ct) =>
        CreatedFromResult(await categories.CreateAsync(budgetId, request, ct), c => $"/api/budgets/{budgetId}/categories/{c.Id}");

    [HttpPut("{categoryId:guid}")]
    [BudgetAccess(AccessLevel.Editor)]
    [ServiceFilter(typeof(RequestValidationFilter<CategoryRequest>))]
    public async Task<IActionResult> Update(Guid budgetId, Guid categoryId, CategoryRequest request, CancellationToken ct) =>
        FromResult(await categories.UpdateAsync(budgetId, categoryId, request, ct));

    [HttpDelete("{categoryId:guid}")]
    [BudgetAccess(AccessLevel.Editor)]
    public async Task<IActionResult> Delete(Guid budgetId, Guid categoryId, CancellationToken ct) =>
        FromResult(await categories.DeleteAsync(budgetId, categoryId, ct));
}
