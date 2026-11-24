using Kopeck.Application.Budgets;
using Kopeck.Contracts;
using Kopeck.Domain.Budgets;

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

public static class BudgetRoutes
{
    /// <summary>The caller's access to the budget in the route (set by <see cref="BudgetAccessFilter"/>).</summary>
    public static BudgetAccess BudgetAccess(this HttpContext http) =>
        http.Items[BudgetAccessFilter.ItemKey] as BudgetAccess
        ?? throw new InvalidOperationException("Budget access was not resolved: map this endpoint under MapBudgetGroup().");

    /// <summary>Route group for everything under one budget; callers need at least Viewer access.</summary>
    public static RouteGroupBuilder MapBudgetGroup(this IEndpointRouteBuilder app) =>
        app.MapGroup("/api/budgets/{budgetId:guid}").RequireAuthorization().AddEndpointFilter<BudgetAccessFilter>();

    /// <summary>Endpoints mapped on the returned group additionally require <paramref name="minimum"/>.</summary>
    public static RouteGroupBuilder RequireAccess(this RouteGroupBuilder group, AccessLevel minimum)
    {
        var sub = group.MapGroup("");
        sub.AddEndpointFilter(async (ctx, next) => ctx.HttpContext.BudgetAccess().Allows(minimum)
            ? await next(ctx)
            : Results.Json(
                new ApiError(minimum == AccessLevel.Owner ? "Only the owner can do this." : "You have view-only access to this budget."),
                statusCode: StatusCodes.Status403Forbidden));
        return sub;
    }
}
