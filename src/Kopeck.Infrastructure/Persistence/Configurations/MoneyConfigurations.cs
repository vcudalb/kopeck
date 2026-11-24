using Kopeck.Domain.Categories;
using Kopeck.Domain.Common;
using Kopeck.Domain.Expenses;
using Kopeck.Domain.Incomes;
using Kopeck.Domain.Planning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kopeck.Infrastructure.Persistence.Configurations;

internal static class Money
{
    /// <summary>Amounts are stored with fixed precision (max 2 decimals is enforced by the domain rules).</summary>
    public static PropertyBuilder<decimal> IsMoney(this PropertyBuilder<decimal> p) => p.HasPrecision(18, 2);
}

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> e)
    {
        e.BelongsToBudget();
        e.HasIndex(c => new { c.BudgetId, c.Name }).IsUnique();
        e.Property(c => c.Name).HasMaxLength(DomainRules.MaxNameLength);
        e.Property(c => c.Color).HasMaxLength(DomainRules.ColorLength);
    }
}

internal sealed class IncomeStreamConfiguration : IEntityTypeConfiguration<IncomeStream>
{
    public void Configure(EntityTypeBuilder<IncomeStream> e)
    {
        e.BelongsToBudget();
        e.HasIndex(s => new { s.BudgetId, s.Name }).IsUnique();
        e.Property(s => s.Name).HasMaxLength(DomainRules.MaxNameLength);
    }
}

internal sealed class IncomeConfiguration : IEntityTypeConfiguration<Income>
{
    public void Configure(EntityTypeBuilder<Income> e)
    {
        e.BelongsToBudget();
        e.HasIndex(x => new { x.BudgetId, x.Date });
        e.Property(x => x.Amount).IsMoney();
        e.Property(x => x.Note).HasMaxLength(DomainRules.MaxNoteLength);
        e.HasOne(x => x.Stream).WithMany().HasForeignKey(x => x.StreamId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> e)
    {
        e.BelongsToBudget();
        e.HasIndex(x => new { x.BudgetId, x.Date });
        e.Property(x => x.Amount).IsMoney();
        e.Property(x => x.Note).HasMaxLength(DomainRules.MaxNoteLength);
        e.Property(x => x.CreatedByName).HasMaxLength(DomainRules.MaxNameLength);
        // A category with expenses can't be removed from under them.
        e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class PlannedExpenseConfiguration : IEntityTypeConfiguration<PlannedExpense>
{
    public void Configure(EntityTypeBuilder<PlannedExpense> e)
    {
        e.BelongsToBudget();
        e.HasIndex(p => new { p.BudgetId, p.CategoryId, p.Month }).IsUnique();
        e.Property(p => p.Amount).IsMoney();
        e.HasOne<Category>().WithMany().HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Cascade);
    }
}
