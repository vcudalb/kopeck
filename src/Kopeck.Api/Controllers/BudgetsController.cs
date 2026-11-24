using Kopeck.Api.Filters;
using Kopeck.Application.Budgets;
using Kopeck.Application.Planning;
using Kopeck.Contracts;
using Kopeck.Domain.Budgets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Kopeck.Api.Controllers;

/// <summary>Budget lifecycle and the monthly summary (dashboard).</summary>
[Authorize]
[Route("api/budgets")]
public sealed class BudgetsController(IBudgetService budgets, ISummaryService summaries) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListMine(CancellationToken ct) => Ok(await budgets.ListMineAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create(BudgetRequest request, CancellationToken ct) =>
        CreatedFromResult(await budgets.CreateAsync(request, ct), b => $"/api/budgets/{b.Id}");

    [HttpGet("{budgetId:guid}/summary")]
    [BudgetAccess]
    public async Task<IActionResult> Summary(Guid budgetId, [FromQuery, BindRequired] DateOnly month, CancellationToken ct) =>
        Ok(await summaries.BuildAsync(BudgetAccessFilter.AccessOf(HttpContext).Budget, month, ct));

    [HttpDelete("{budgetId:guid}/leave")]
    [BudgetAccess]
    public async Task<IActionResult> Leave(Guid budgetId, CancellationToken ct) =>
        FromResult(await budgets.LeaveAsync(budgetId, ct));

    [HttpPut("{budgetId:guid}")]
    [BudgetAccess(AccessLevel.Owner)]
    public async Task<IActionResult> Rename(Guid budgetId, BudgetRequest request, CancellationToken ct) =>
        FromResult(await budgets.RenameAsync(budgetId, request, ct));

    [HttpDelete("{budgetId:guid}")]
    [BudgetAccess(AccessLevel.Owner)]
    public async Task<IActionResult> Delete(Guid budgetId, CancellationToken ct) =>
        FromResult(await budgets.DeleteAsync(budgetId, ct));
}
