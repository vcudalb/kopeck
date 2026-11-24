using Kopeck.Application.Validation;
using Kopeck.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kopeck.Api.Filters;

/// <summary>
/// Runs the <see cref="IRequestValidator{TRequest}"/> for the action's <typeparamref name="TRequest"/> argument before the
/// action executes. Apply it with <c>[ServiceFilter(typeof(RequestValidationFilter&lt;TRequest&gt;))]</c>.
/// A failure short-circuits with 400 and the usual <see cref="ApiError"/> body.
/// </summary>
public sealed class RequestValidationFilter<TRequest>(IRequestPipelineExecutor executor) : IAsyncActionFilter
    where TRequest : class
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Found by type, so the action parameter can have any name. A missing body is already rejected by [ApiController].
        var request = context.ActionArguments.Values.OfType<TRequest>().FirstOrDefault();
        if (request is not null)
        {
            var result = await executor.ExecuteAsync(request, context.HttpContext.RequestAborted);
            if (!result.IsValid)
            {
                context.Result = new BadRequestObjectResult(new ApiError(result.Message));
                return;
            }
        }

        await next();
    }
}
