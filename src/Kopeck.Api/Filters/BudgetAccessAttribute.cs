using Kopeck.Domain.Budgets;
using Microsoft.AspNetCore.Mvc;

namespace Kopeck.Api.Filters;

/// <summary>
/// Requires the caller to have at least <paramref name="minimum"/> access to the budget in the route.
/// Runs before model-state validation, so a stranger gets 404 rather than learning anything from a 400.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class BudgetAccessAttribute : TypeFilterAttribute
{
    public BudgetAccessAttribute(AccessLevel minimum = AccessLevel.Viewer) : base(typeof(BudgetAccessFilter))
    {
        Arguments = [minimum];
        Order = -3000;
    }
}
