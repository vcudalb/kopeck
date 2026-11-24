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
