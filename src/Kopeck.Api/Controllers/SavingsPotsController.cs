using Kopeck.Api.Filters;
using Kopeck.Application.Savings;
using Kopeck.Contracts;
using Kopeck.Domain.Budgets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kopeck.Api.Controllers;

/// <summary>Savings pots (emergency fund, holiday, ...), each with its own currency and optional target.</summary>
[Authorize]
[Route("api/budgets/{budgetId:guid}/savings")]
[BudgetAccess]
public sealed class SavingsPotsController(ISavingsService savings) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid budgetId, CancellationToken ct) => Ok(await savings.ListPotsAsync(budgetId, ct));

    [HttpPost]
    [BudgetAccess(AccessLevel.Editor)]
    [ServiceFilter(typeof(RequestValidationFilter<SavingsPotRequest>))]
    public async Task<IActionResult> Create(Guid budgetId, SavingsPotRequest request, CancellationToken ct) =>
        CreatedFromResult(await savings.CreatePotAsync(budgetId, request, ct), p => $"/api/budgets/{budgetId}/savings/{p.Id}");

    [HttpPut("{potId:guid}")]
    [BudgetAccess(AccessLevel.Editor)]
    [ServiceFilter(typeof(RequestValidationFilter<SavingsPotRequest>))]
    public async Task<IActionResult> Update(Guid budgetId, Guid potId, SavingsPotRequest request, CancellationToken ct) =>
        FromResult(await savings.UpdatePotAsync(budgetId, potId, request, ct));

    [HttpDelete("{potId:guid}")]
    [BudgetAccess(AccessLevel.Editor)]
    public async Task<IActionResult> Delete(Guid budgetId, Guid potId, CancellationToken ct) =>
        FromResult(await savings.DeletePotAsync(budgetId, potId, ct));
}
