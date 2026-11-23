using Kopeck.Api.Auth;
using Kopeck.Api.Data;
using Kopeck.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Kopeck.Api.Endpoints;

/// <summary>Who the caller is for the budget in the route: loaded once per request by <see cref="LoadBudgetAccessFilter"/>.</summary>
public sealed record BudgetAccess(BudgetEntity Budget, BudgetRole Role);

/// <summary>
/// Authorizes every <c>/api/budgets/{budgetId}/...</c> endpoint in one place. Callers with no access get 404 (not 403)
/// so budget ids can't be probed. Per-endpoint minimum roles are enforced by <see cref="BudgetAccessExtensions.RequireRole"/>.
/// </summary>
public sealed class LoadBudgetAccessFilter(KopeckDb db) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var http = ctx.HttpContext;
        if (!Guid.TryParse(http.GetRouteValue("budgetId")?.ToString(), out var budgetId)) return Results.NotFound();

        var budget = await db.Budgets.AsNoTracking().FirstOrDefaultAsync(b => b.Id == budgetId, http.RequestAborted);
        if (budget is null) return Results.NotFound();

        BudgetRole? role = null;
        if (budget.OwnerId == http.User.UserId()) role = BudgetRole.Owner;
        else
        {
            var email = http.User.UserEmail();
            var share = await db.Shares.AsNoTracking()
                .FirstOrDefaultAsync(s => s.BudgetId == budgetId && s.Email == email, http.RequestAborted);
            if (share is not null) role = (BudgetRole)(int)share.Role;
        }
        if (role is null) return Results.NotFound();

        http.Items[BudgetAccessExtensions.Key] = new BudgetAccess(budget, role.Value);
        return await next(ctx);
    }
}

public static class BudgetAccessExtensions
{
    internal const string Key = "budget-access";

    public static BudgetAccess Access(this HttpContext http) => (BudgetAccess)http.Items[Key]!;

    /// <summary>Route group for everything under one budget; callers need at least Viewer access.</summary>
    public static RouteGroupBuilder MapBudgetGroup(this IEndpointRouteBuilder app) =>
        app.MapGroup("/api/budgets/{budgetId:guid}").RequireAuthorization().AddEndpointFilter<LoadBudgetAccessFilter>();

    /// <summary>Endpoints mapped on the returned group additionally require <paramref name="min"/>.</summary>
    public static RouteGroupBuilder RequireRole(this RouteGroupBuilder group, BudgetRole min)
    {
        var sub = group.MapGroup("");
        sub.AddEndpointFilter(async (ctx, next) => ctx.HttpContext.Access().Role >= min
            ? await next(ctx)
            : Results.Json(new ApiError(min == BudgetRole.Owner ? "Only the owner can do this." : "You have view-only access to this budget."),
                statusCode: StatusCodes.Status403Forbidden));
        return sub;
    }
}
