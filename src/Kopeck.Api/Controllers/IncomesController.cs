using Kopeck.Api.Filters;
using Kopeck.Application.Incomes;
using Kopeck.Contracts;
using Kopeck.Domain.Budgets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Kopeck.Api.Controllers;

/// <summary>Dated income entries, each booked against a revenue stream.</summary>
[Authorize]
[Route("api/budgets/{budgetId:guid}/incomes")]
[BudgetAccess]
public sealed class IncomesController(IIncomeService income) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid budgetId, [FromQuery, BindRequired] DateOnly month, CancellationToken ct) =>
        Ok(await income.ListAsync(budgetId, month, ct));

    [HttpPost]
    [BudgetAccess(AccessLevel.Editor)]
    public async Task<IActionResult> Create(Guid budgetId, IncomeRequest request, CancellationToken ct) =>
        CreatedFromResult(await income.CreateAsync(budgetId, request, ct), i => $"/api/budgets/{budgetId}/incomes/{i.Id}");

    [HttpDelete("{incomeId:guid}")]
    [BudgetAccess(AccessLevel.Editor)]
    public async Task<IActionResult> Delete(Guid budgetId, Guid incomeId, CancellationToken ct) =>
        FromResult(await income.DeleteAsync(budgetId, incomeId, ct));
}
