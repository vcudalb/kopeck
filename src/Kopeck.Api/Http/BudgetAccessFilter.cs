using Kopeck.Application.Budgets;

namespace Kopeck.Api.Http;

/// <summary>
/// Authorizes every <c>/api/budgets/{budgetId}/...</c> endpoint in one place. Callers with no access get 404 (not 403)
/// so budget ids can't be probed. Per-endpoint minimum levels are added with <see cref="BudgetRoutes.RequireAccess"/>.
/// </summary>
public sealed class BudgetAccessFilter(IBudgetAccessResolver resolver) : IEndpointFilter
{
    internal const string ItemKey = "budget-access";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var http = ctx.HttpContext;
        if (!Guid.TryParse(http.GetRouteValue("budgetId")?.ToString(), out var budgetId)) return Results.NotFound();

        var access = await resolver.ResolveAsync(budgetId, http.RequestAborted);
        if (access is null) return Results.NotFound();

        http.Items[ItemKey] = access;
        return await next(ctx);
    }
}
