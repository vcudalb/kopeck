using Kopeck.Application.Abstractions;
using Kopeck.Domain.Budgets;
using Kopeck.Domain.Categories;
using Kopeck.Domain.Expenses;
using Kopeck.Domain.Incomes;
using Kopeck.Domain.Planning;
using Kopeck.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Kopeck.Infrastructure.Persistence;

/// <summary>EF Core context. Mapping lives in one <c>IEntityTypeConfiguration</c> per entity under <c>Configurations/</c>.</summary>
public class KopeckDb(DbContextOptions<KopeckDb> options) : DbContext(options), IKopeckDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<BudgetShare> Shares => Set<BudgetShare>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<IncomeStream> IncomeStreams => Set<IncomeStream>();
    public DbSet<Income> Incomes => Set<Income>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<PlannedExpense> Plans => Set<PlannedExpense>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KopeckDb).Assembly);
}
