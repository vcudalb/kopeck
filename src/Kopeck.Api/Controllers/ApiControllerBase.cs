using Kopeck.Application.Common;
using Kopeck.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Kopeck.Api.Controllers;

/// <summary>
/// Base for every API controller. It is the one place where use-case outcomes (<see cref="Result"/>) become HTTP
/// status codes, so controllers stay thin: bind the request, call the service, return the mapped result.
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>204 on success.</summary>
    protected IActionResult FromResult(Result result) => FromResult(result, NoContent);

    /// <summary>The given success response, or the mapped error.</summary>
    protected IActionResult FromResult(Result result, Func<IActionResult> onSuccess) =>
        result.IsSuccess ? onSuccess() : FromError(result.Error!);

    /// <summary>200 with the value on success.</summary>
    protected IActionResult FromResult<T>(Result<T> result) =>
        result.IsSuccess ? Ok(result.Value) : FromError(result.Error!);

    /// <summary>201 with a Location header on success.</summary>
    protected IActionResult CreatedFromResult<T>(Result<T> result, Func<T, string> location) =>
        result.IsSuccess ? Created(location(result.Value), result.Value) : FromError(result.Error!);

    protected IActionResult FromError(Error error) => error.Kind switch
    {
        ErrorKind.NotFound => NotFound(),
        ErrorKind.Validation => BadRequest(new ApiError(error.Message)),
        ErrorKind.Conflict => Conflict(new ApiError(error.Message)),
        ErrorKind.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new ApiError(error.Message)),
        ErrorKind.Unauthorized => Unauthorized(new ApiError(error.Message)),
        _ => throw new ArgumentOutOfRangeException(nameof(error), error.Kind, null),
    };
}
