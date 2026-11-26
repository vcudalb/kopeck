using Kopeck.Api.Filters;
using Kopeck.Application.Savings;
using Kopeck.Contracts;
using Kopeck.Domain.Budgets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kopeck.Api.Controllers;

/// <summary>Deposits into and withdrawals from one savings pot.</summary>
[Authorize]
[Route("api/budgets/{budgetId:guid}/savings/{potId:guid}/entries")]
[BudgetAccess]
public sealed class SavingsEntriesController(ISavingsService savings) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid budgetId, Guid potId, CancellationToken ct) =>
        FromResult(await savings.ListEntriesAsync(budgetId, potId, ct));

    [HttpPost]
    [BudgetAccess(AccessLevel.Editor)]
    [ServiceFilter(typeof(RequestValidationFilter<SavingsEntryRequest>))]
    public async Task<IActionResult> Add(Guid budgetId, Guid potId, SavingsEntryRequest request, CancellationToken ct) =>
        CreatedFromResult(await savings.AddEntryAsync(budgetId, potId, request, ct), e => $"/api/budgets/{budgetId}/savings/{potId}/entries/{e.Id}");

    [HttpDelete("{entryId:guid}")]
    [BudgetAccess(AccessLevel.Editor)]
    public async Task<IActionResult> Delete(Guid budgetId, Guid potId, Guid entryId, CancellationToken ct) =>
        FromResult(await savings.DeleteEntryAsync(budgetId, potId, entryId, ct));
}
