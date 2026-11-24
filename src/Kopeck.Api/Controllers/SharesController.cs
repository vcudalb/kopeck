using Kopeck.Api.Filters;
using Kopeck.Application.Budgets;
using Kopeck.Contracts;
using Kopeck.Domain.Budgets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kopeck.Api.Controllers;

/// <summary>Who else can see or edit a budget. Owner only.</summary>
[Authorize]
[Route("api/budgets/{budgetId:guid}/shares")]
[BudgetAccess(AccessLevel.Owner)]
public sealed class SharesController(ISharingService sharing) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid budgetId, CancellationToken ct) => Ok(await sharing.ListAsync(budgetId, ct));

    [HttpPost]
    public async Task<IActionResult> Share(Guid budgetId, ShareRequest request, CancellationToken ct) =>
        FromResult(await sharing.ShareAsync(budgetId, request, ct));

    [HttpDelete("{shareId:guid}")]
    public async Task<IActionResult> Revoke(Guid budgetId, Guid shareId, CancellationToken ct) =>
        FromResult(await sharing.RevokeAsync(budgetId, shareId, ct));
}
