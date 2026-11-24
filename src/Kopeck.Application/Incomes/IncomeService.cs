using Kopeck.Application.Abstractions;
using Kopeck.Application.Common;
using Kopeck.Contracts;
using Kopeck.Domain.Common;
using Kopeck.Domain.Incomes;
using Microsoft.EntityFrameworkCore;

namespace Kopeck.Application.Incomes;

/// <summary>Revenue streams (e.g. "Main salary", "Freelance") and the dated income entries booked against them.</summary>
public sealed class IncomeService(IKopeckDbContext db) : IIncomeService
{
    public async Task<IReadOnlyList<IncomeStreamDto>> ListStreamsAsync(Guid budgetId, CancellationToken ct)
    {
        var streams = await db.IncomeStreams.AsNoTracking().InBudget(budgetId).OrderBy(s => s.Name).ToListAsync(ct);
        return streams.Select(s => s.ToDto()).ToList();
    }

    public async Task<Result<IncomeStreamDto>> CreateStreamAsync(Guid budgetId, IncomeStreamRequest request, CancellationToken ct)
    {
        var lowered = request.Name.Trim().ToLower();
        if (await db.IncomeStreams.InBudget(budgetId).AnyAsync(s => s.Name.ToLower() == lowered, ct))
            return Error.Conflict("An income stream with this name already exists.");

        var stream = new IncomeStream(budgetId, request.Name);
        db.IncomeStreams.Add(stream);
        await db.SaveChangesAsync(ct);
        return stream.ToDto();
    }

    public async Task<Result> DeleteStreamAsync(Guid budgetId, Guid streamId, CancellationToken ct)
    {
        var stream = await db.IncomeStreams.InBudget(budgetId).FirstOrDefaultAsync(s => s.Id == streamId, ct);
        if (stream is null) return Error.NotFound;
        if (await db.Incomes.AnyAsync(i => i.StreamId == streamId, ct)) return Error.Conflict("This stream has income entries. Delete them first.");

        db.IncomeStreams.Remove(stream);
        await db.SaveChangesAsync(ct);
        return Result.Success;
    }

    public async Task<IReadOnlyList<IncomeDto>> ListAsync(Guid budgetId, DateOnly month, CancellationToken ct)
    {
        var (start, end) = month.MonthRange();
        return await db.Incomes.AsNoTracking().InBudget(budgetId).Where(i => i.Date >= start && i.Date < end)
            .OrderByDescending(i => i.Date)
            .Select(i => new IncomeDto(i.Id, i.StreamId, i.Stream!.Name, i.Date, i.Amount, i.Note))
            .ToListAsync(ct);
    }

    public async Task<Result<IncomeDto>> CreateAsync(Guid budgetId, IncomeRequest request, CancellationToken ct)
    {
        var stream = await db.IncomeStreams.AsNoTracking().InBudget(budgetId).FirstOrDefaultAsync(s => s.Id == request.StreamId, ct);
        if (stream is null) return Error.Validation("Unknown income stream.");

        var income = new Income(budgetId, stream.Id, request.Date, request.Amount, request.Note);
        db.Incomes.Add(income);
        await db.SaveChangesAsync(ct);
        return income.ToDto(stream.Name);
    }

    public async Task<Result> DeleteAsync(Guid budgetId, Guid incomeId, CancellationToken ct)
    {
        var income = await db.Incomes.InBudget(budgetId).FirstOrDefaultAsync(i => i.Id == incomeId, ct);
        if (income is null) return Error.NotFound;

        db.Incomes.Remove(income);
        await db.SaveChangesAsync(ct);
        return Result.Success;
    }
}
