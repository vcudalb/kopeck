using Kopeck.Contracts;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace Kopeck.Api.Services;

/// <summary>
/// Two concurrent requests can both pass an "already exists?" check; the unique index then rejects the second one.
/// Report that as 409 (what the user would have got a moment later) instead of a 500.
/// </summary>
public sealed class UniqueConstraintExceptionHandler(ILogger<UniqueConstraintExceptionHandler> log) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
    {
        if (ex is not DbUpdateException { InnerException: { } inner } || !IsUniqueViolation(inner)) return false;

        log.LogInformation("Unique constraint violation on {Path}", http.Request.Path);
        http.Response.StatusCode = StatusCodes.Status409Conflict;
        await http.Response.WriteAsJsonAsync(new ApiError("That item already exists."), Json.Options, ct);
        return true;
    }

    // SQLite: "UNIQUE constraint failed"; PostgreSQL: SqlState 23505; SQL Server: error 2601/2627.
    private static bool IsUniqueViolation(Exception inner) =>
        inner.Message.Contains("UNIQUE constraint", StringComparison.OrdinalIgnoreCase)
        || inner.GetType().GetProperty("SqlState")?.GetValue(inner) as string == "23505"
        || inner.GetType().GetProperty("Number")?.GetValue(inner) is 2601 or 2627;
}
