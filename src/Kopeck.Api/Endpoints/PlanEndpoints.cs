using Kopeck.Api.Http;
using Kopeck.Application.Planning;
using Kopeck.Contracts;
using Kopeck.Domain.Budgets;

namespace Kopeck.Api.Endpoints;

/// <summary>Planned spend per category per month.</summary>
public sealed class PlanEndpoints : IEndpointModule
{
    public void Map(IEndpointRouteBuilder app)
    {
        var budget = app.MapBudgetGroup();
        var read = budget.MapGroup("/plans");
        var write = budget.RequireAccess(AccessLevel.Editor).MapGroup("/plans");

        read.MapGet("/", async (Guid budgetId, DateOnly month, IPlanService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(budgetId, month, ct)));

        // One category/month (0 clears it).
        write.MapPut("/", async (Guid budgetId, PlanRequest r, IPlanService svc, CancellationToken ct) =>
            (await svc.SetAsync(budgetId, r, ct)).ToHttp());

        // Several categories for one month, all or nothing (what the Plan screen's Save uses).
        write.MapPut("/bulk", async (Guid budgetId, SavePlansRequest r, IPlanService svc, CancellationToken ct) =>
            (await svc.SaveManyAsync(budgetId, r, ct)).ToHttp());

        write.MapPost("/copy", async (Guid budgetId, CopyPlanRequest r, IPlanService svc, CancellationToken ct) =>
            (await svc.CopyMissingAsync(budgetId, r, ct)).ToHttp());
    }
}
