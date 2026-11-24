using Kopeck.Api.Http;
using Kopeck.Application.Budgets;
using Kopeck.Application.Planning;
using Kopeck.Contracts;
using Kopeck.Domain.Budgets;

namespace Kopeck.Api.Endpoints;

/// <summary>Budget lifecycle, the monthly summary (dashboard) and sharing.</summary>
public sealed class BudgetEndpoints : IEndpointModule
{
    public void Map(IEndpointRouteBuilder app)
    {
        // ---- collection (no budget in the route yet) ----
        var budgets = app.MapGroup("/api/budgets").RequireAuthorization();

        budgets.MapGet("/", async (IBudgetService svc, CancellationToken ct) => Results.Ok(await svc.ListMineAsync(ct)));

        budgets.MapPost("/", async (BudgetRequest r, IBudgetService svc, CancellationToken ct) =>
            (await svc.CreateAsync(r, ct)).ToCreated(b => $"/api/budgets/{b.Id}"));

        // ---- one budget: any access level ----
        var budget = app.MapBudgetGroup();

        budget.MapGet("/summary", async (DateOnly month, HttpContext http, ISummaryService svc, CancellationToken ct) =>
            Results.Ok(await svc.BuildAsync(http.BudgetAccess().Budget, month, ct)));

        budget.MapDelete("/leave", async (Guid budgetId, IBudgetService svc, CancellationToken ct) =>
            (await svc.LeaveAsync(budgetId, ct)).ToHttp());

        // ---- owner only ----
        var owner = budget.RequireAccess(AccessLevel.Owner);

        owner.MapPut("/", async (Guid budgetId, BudgetRequest r, IBudgetService svc, CancellationToken ct) =>
            (await svc.RenameAsync(budgetId, r, ct)).ToHttp());

        owner.MapDelete("/", async (Guid budgetId, IBudgetService svc, CancellationToken ct) =>
            (await svc.DeleteAsync(budgetId, ct)).ToHttp());

        owner.MapGet("/shares", async (Guid budgetId, ISharingService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(budgetId, ct)));

        owner.MapPost("/shares", async (Guid budgetId, ShareRequest r, ISharingService svc, CancellationToken ct) =>
            (await svc.ShareAsync(budgetId, r, ct)).ToHttp());

        owner.MapDelete("/shares/{shareId:guid}", async (Guid budgetId, Guid shareId, ISharingService svc, CancellationToken ct) =>
            (await svc.RevokeAsync(budgetId, shareId, ct)).ToHttp());
    }
}
