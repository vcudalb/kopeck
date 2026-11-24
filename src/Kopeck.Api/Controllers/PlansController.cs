using Kopeck.Api.Filters;
using Kopeck.Application.Planning;
using Kopeck.Contracts;
using Kopeck.Domain.Budgets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Kopeck.Api.Controllers;

/// <summary>Planned spend per category per month.</summary>
[Authorize]
[Route("api/budgets/{budgetId:guid}/plans")]
[BudgetAccess]
public sealed class PlansController(IPlanService plans) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid budgetId, [FromQuery, BindRequired] DateOnly month, CancellationToken ct) =>
        Ok(await plans.ListAsync(budgetId, month, ct));

    /// <summary>One category for one month (0 clears it).</summary>
    [HttpPut]
    [BudgetAccess(AccessLevel.Editor)]
    public async Task<IActionResult> Set(Guid budgetId, PlanRequest request, CancellationToken ct) =>
        FromResult(await plans.SetAsync(budgetId, request, ct));

    /// <summary>Several categories for one month, all or nothing (what the Plan screen's Save uses).</summary>
    [HttpPut("bulk")]
    [BudgetAccess(AccessLevel.Editor)]
    public async Task<IActionResult> SaveMany(Guid budgetId, SavePlansRequest request, CancellationToken ct) =>
        FromResult(await plans.SaveManyAsync(budgetId, request, ct));

    [HttpPost("copy")]
    [BudgetAccess(AccessLevel.Editor)]
    public async Task<IActionResult> CopyMissing(Guid budgetId, CopyPlanRequest request, CancellationToken ct) =>
        FromResult(await plans.CopyMissingAsync(budgetId, request, ct));
}
