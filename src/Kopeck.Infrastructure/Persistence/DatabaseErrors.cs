using Microsoft.EntityFrameworkCore;

namespace Kopeck.Infrastructure.Persistence;

public static class DatabaseErrors
{
    /// <summary>
    /// True when a save failed on a unique index. Two concurrent requests can both pass an "already exists?" check;
    /// the index then rejects the second one, which the API reports as 409 instead of 500.
    /// SQLite: "UNIQUE constraint failed"; PostgreSQL: SqlState 23505; SQL Server: error 2601/2627.
    /// </summary>
    public static bool IsUniqueViolation(Exception exception) =>
        exception is DbUpdateException { InnerException: { } inner } &&
        (inner.Message.Contains("UNIQUE constraint", StringComparison.OrdinalIgnoreCase)
         || inner.GetType().GetProperty("SqlState")?.GetValue(inner) as string == "23505"
         || inner.GetType().GetProperty("Number")?.GetValue(inner) is 2601 or 2627);
}
