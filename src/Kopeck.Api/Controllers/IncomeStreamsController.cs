using Kopeck.Api.Filters;
using Kopeck.Application.Incomes;
using Kopeck.Contracts;
using Kopeck.Domain.Budgets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kopeck.Api.Controllers;

/// <summary>Revenue streams (salary, freelance, ...) that income entries are booked against.</summary>
[Authorize]
[Route("api/budgets/{budgetId:guid}/income-streams")]
[BudgetAccess]
public sealed class IncomeStreamsController(IIncomeService income) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid budgetId, CancellationToken ct) => Ok(await income.ListStreamsAsync(budgetId, ct));

    [HttpPost]
    [BudgetAccess(AccessLevel.Editor)]
    [ServiceFilter(typeof(RequestValidationFilter<IncomeStreamRequest>))]
    public async Task<IActionResult> Create(Guid budgetId, IncomeStreamRequest request, CancellationToken ct) =>
        CreatedFromResult(await income.CreateStreamAsync(budgetId, request, ct), s => $"/api/budgets/{budgetId}/income-streams/{s.Id}");

    [HttpDelete("{streamId:guid}")]
    [BudgetAccess(AccessLevel.Editor)]
    public async Task<IActionResult> Delete(Guid budgetId, Guid streamId, CancellationToken ct) =>
        FromResult(await income.DeleteStreamAsync(budgetId, streamId, ct));
}
