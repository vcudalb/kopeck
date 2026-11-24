using Kopeck.Application.Budgets;
using Kopeck.Contracts;
using Kopeck.Domain.Budgets;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kopeck.Api.Filters;

/// <summary>
/// Authorizes every action under <c>api/budgets/{budgetId}</c>. Callers with no access get 404 (not 403) so budget
/// ids can't be probed; callers below the required level get 403. Applied through <see cref="BudgetAccessAttribute"/>.
/// The resolved access is cached on the request, so a controller-level and an action-level attribute resolve it once.
/// </summary>
public sealed class BudgetAccessFilter(IBudgetAccessResolver resolver, AccessLevel minimum) : IAsyncActionFilter
{
    private const string ItemKey = "budget-access";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        if (http.Items[ItemKey] is not BudgetAccess access)
        {
            if (!Guid.TryParse(context.RouteData.Values["budgetId"]?.ToString(), out var budgetId))
            {
                context.Result = new NotFoundResult();
                return;
            }

            var resolved = await resolver.ResolveAsync(budgetId, http.RequestAborted);
            if (resolved is null)
            {
                context.Result = new NotFoundResult();
                return;
            }

            http.Items[ItemKey] = access = resolved;
        }

        if (!access.Allows(minimum))
        {
            var message = minimum == AccessLevel.Owner ? "Only the owner can do this." : "You have view-only access to this budget.";
            context.Result = new ObjectResult(new ApiError(message)) { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }

        await next();
    }

    /// <summary>The caller's access to the budget in the route (set by this filter).</summary>
    public static BudgetAccess AccessOf(HttpContext http) =>
        http.Items[ItemKey] as BudgetAccess
        ?? throw new InvalidOperationException("Budget access was not resolved: add [BudgetAccess] to this action or its controller.");
}
