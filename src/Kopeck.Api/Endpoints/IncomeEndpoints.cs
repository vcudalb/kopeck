using Kopeck.Api.Data;
using Kopeck.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Kopeck.Api.Endpoints;

/// <summary>Revenue streams (e.g. "Main salary", "Freelance") and the dated income entries booked against them.</summary>
public static class IncomeEndpoints
{
    public static void MapIncome(this IEndpointRouteBuilder app)
    {
        var read = app.MapBudgetGroup();
        var write = app.MapBudgetGroup().RequireRole(BudgetRole.Editor);

        read.MapGet("/income-streams", async (Guid budgetId, KopeckDb db, CancellationToken ct) =>
            Results.Ok(await db.IncomeStreams.AsNoTracking().Where(s => s.BudgetId == budgetId).OrderBy(s => s.Name)
                .Select(s => new IncomeStreamDto(s.Id, s.Name)).ToListAsync(ct)));

        write.MapPost("/income-streams", async (Guid budgetId, IncomeStreamRequest r, KopeckDb db) =>
        {
            if (!Rules.IsValidName(r.Name)) return Rules.Bad("Enter a stream name (max 80 characters).");
            var name = r.Name.Trim();
            if (await db.IncomeStreams.AnyAsync(s => s.BudgetId == budgetId && s.Name.ToLower() == name.ToLower()))
                return Rules.Conflict("An income stream with this name already exists.");

            var stream = new IncomeStream { BudgetId = budgetId, Name = name };
            db.IncomeStreams.Add(stream);
            await db.SaveChangesAsync();
            return Results.Created($"/api/budgets/{budgetId}/income-streams/{stream.Id}", new IncomeStreamDto(stream.Id, stream.Name));
        });

        write.MapDelete("/income-streams/{streamId:guid}", async (Guid budgetId, Guid streamId, KopeckDb db) =>
        {
            if (!await db.IncomeStreams.AnyAsync(s => s.Id == streamId && s.BudgetId == budgetId)) return Results.NotFound();
            if (await db.Incomes.AnyAsync(i => i.StreamId == streamId)) return Rules.Conflict("This stream has income entries. Delete them first.");
            await db.IncomeStreams.Where(s => s.Id == streamId).ExecuteDeleteAsync();
            return Results.NoContent();
        });

        read.MapGet("/incomes", async (Guid budgetId, DateOnly month, KopeckDb db, CancellationToken ct) =>
        {
            var start = Rules.FirstOfMonth(month); var end = start.AddMonths(1);
            return Results.Ok(await db.Incomes.AsNoTracking().Where(i => i.BudgetId == budgetId && i.Date >= start && i.Date < end)
                .OrderByDescending(i => i.Date)
                .Select(i => new IncomeDto(i.Id, i.StreamId, i.Stream!.Name, i.Date, i.Amount, i.Note)).ToListAsync(ct));
        });

        write.MapPost("/incomes", async (Guid budgetId, IncomeRequest r, KopeckDb db) =>
        {
            if (!Rules.IsValidAmount(r.Amount)) return Rules.Bad("Amount must be greater than 0 (max 2 decimals).");
            var stream = await db.IncomeStreams.AsNoTracking().FirstOrDefaultAsync(s => s.Id == r.StreamId && s.BudgetId == budgetId);
            if (stream is null) return Rules.Bad("Unknown income stream.");

            var income = new Income { BudgetId = budgetId, StreamId = stream.Id, Date = r.Date, Amount = r.Amount, Note = Rules.CleanNote(r.Note) };
            db.Incomes.Add(income);
            await db.SaveChangesAsync();
            return Results.Created($"/api/budgets/{budgetId}/incomes/{income.Id}",
                new IncomeDto(income.Id, stream.Id, stream.Name, income.Date, income.Amount, income.Note));
        });

        write.MapDelete("/incomes/{incomeId:guid}", async (Guid budgetId, Guid incomeId, KopeckDb db) =>
            await db.Incomes.Where(i => i.Id == incomeId && i.BudgetId == budgetId).ExecuteDeleteAsync() == 0
                ? Results.NotFound() : Results.NoContent());
    }
}
