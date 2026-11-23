using Microsoft.EntityFrameworkCore;

namespace Kopeck.Api.Data;

public class KopeckDb(DbContextOptions<KopeckDb> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<BudgetEntity> Budgets => Set<BudgetEntity>();
    public DbSet<BudgetShare> Shares => Set<BudgetShare>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<IncomeStream> IncomeStreams => Set<IncomeStream>();
    public DbSet<Income> Incomes => Set<Income>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<PlannedExpense> Plans => Set<PlannedExpense>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).HasMaxLength(254);
            e.Property(u => u.DisplayName).HasMaxLength(80);
        });

        b.Entity<BudgetEntity>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(80);
            e.Property(x => x.Currency).HasMaxLength(5);
            e.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        });

        // Everything that belongs to a budget is removed by the database when the budget is deleted.
        ChildOfBudget<BudgetShare>(b);
        ChildOfBudget<Category>(b);
        ChildOfBudget<IncomeStream>(b);
        ChildOfBudget<Income>(b);
        ChildOfBudget<Expense>(b);
        ChildOfBudget<PlannedExpense>(b);

        b.Entity<BudgetShare>(e => { e.HasIndex(s => new { s.BudgetId, s.Email }).IsUnique(); e.Property(s => s.Email).HasMaxLength(254); });
        b.Entity<Category>(e => { e.HasIndex(c => new { c.BudgetId, c.Name }).IsUnique(); e.Property(c => c.Name).HasMaxLength(80); e.Property(c => c.Color).HasMaxLength(7); });
        b.Entity<IncomeStream>(e => { e.HasIndex(s => new { s.BudgetId, s.Name }).IsUnique(); e.Property(s => s.Name).HasMaxLength(80); });
        b.Entity<PlannedExpense>(e =>
        {
            e.HasIndex(p => new { p.BudgetId, p.CategoryId, p.Month }).IsUnique();
            e.Property(p => p.Amount).HasPrecision(18, 2);
            e.HasOne<Category>().WithMany().HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<Expense>(e =>
        {
            e.HasIndex(x => new { x.BudgetId, x.Date });
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Note).HasMaxLength(200);
            e.Property(x => x.CreatedByName).HasMaxLength(80);
            // A category with expenses can't be removed from under them.
            e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.NoAction);
        });
        b.Entity<Income>(e =>
        {
            e.HasIndex(x => new { x.BudgetId, x.Date });
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Note).HasMaxLength(200);
            e.HasOne(x => x.Stream).WithMany().HasForeignKey(x => x.StreamId).OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ChildOfBudget<T>(ModelBuilder b) where T : class =>
        b.Entity<T>().HasOne<BudgetEntity>().WithMany().HasForeignKey("BudgetId").OnDelete(DeleteBehavior.Cascade);
}
