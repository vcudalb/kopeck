using Kopeck.Contracts;
using Kopeck.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics;

namespace Kopeck.Api.Http;

/// <summary>A save that lost a race on a unique index is reported as 409 (what the user would have got a moment later), not 500.</summary>
public sealed class UniqueConstraintExceptionHandler(ILogger<UniqueConstraintExceptionHandler> log) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception exception, CancellationToken ct)
    {
        if (!DatabaseErrors.IsUniqueViolation(exception)) return false;

        log.LogInformation("Unique constraint violation on {Path}", http.Request.Path);
        http.Response.StatusCode = StatusCodes.Status409Conflict;
        await http.Response.WriteAsJsonAsync(new ApiError("That item already exists."), Json.Options, ct);
        return true;
    }
}
