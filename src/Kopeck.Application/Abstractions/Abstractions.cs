using Kopeck.Domain.Budgets;
using Kopeck.Domain.Categories;
using Kopeck.Domain.Expenses;
using Kopeck.Domain.Incomes;
using Kopeck.Domain.Planning;
using Kopeck.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Kopeck.Application.Abstractions;

/// <summary>
/// Unit of work + queryable sets. Use cases depend on this, not on the EF Core provider or the concrete context,
/// so persistence can be swapped (SQLite now, PostgreSQL later) without touching business code.
/// </summary>
public interface IKopeckDbContext
{
    DbSet<User> Users { get; }
    DbSet<Budget> Budgets { get; }
    DbSet<BudgetShare> Shares { get; }
    DbSet<Category> Categories { get; }
    DbSet<IncomeStream> IncomeStreams { get; }
    DbSet<Income> Incomes { get; }
    DbSet<Expense> Expenses { get; }
    DbSet<PlannedExpense> Plans { get; }

    /// <summary>Commits every pending change in one transaction.</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>The signed-in caller. Implemented by the web layer from the JWT claims.</summary>
public interface ICurrentUser
{
    Guid Id { get; }

    /// <summary>Lower-cased, verified email.</summary>
    string Email { get; }

    string DisplayName { get; }
}

/// <summary>Sends (or queues) a plain-text email.</summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}

public interface IPasswordHasher
{
    string Hash(User user, string password);

    /// <summary>Returns false for a null user, after spending the same time as a real check (no account enumeration by timing).</summary>
    bool Verify(User? user, string password);
}

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);

public interface ITokenService
{
    AccessToken Create(User user);
}
