using Kopeck.Application.Abstractions;
using Kopeck.Application.Common;
using Kopeck.Application.History;
using Kopeck.Contracts;
using Kopeck.Domain.History;
using Kopeck.Domain.Savings;
using Microsoft.EntityFrameworkCore;

namespace Kopeck.Application.Savings;

/// <summary>
/// Savings pots and the deposits/withdrawals booked on them. Balances are summed in memory because SQLite can't SUM
/// decimals server-side; a pot holds at most a few hundred rows, so this stays cheap.
/// </summary>
public sealed class SavingsService(IKopeckDbContext db, EntryAudit audit) : ISavingsService
{
    private static readonly Error NameTaken = Error.Conflict("A savings pot with this name already exists.");

    public async Task<IReadOnlyList<SavingsPotDto>> ListPotsAsync(Guid budgetId, CancellationToken ct)
    {
        var pots = await db.SavingsPots.AsNoTracking().InBudget(budgetId).OrderBy(p => p.Name).ToListAsync(ct);
        var balances = await BalancesAsync(budgetId, ct);
        return pots.Select(p => p.ToDto(balances.GetValueOrDefault(p.Id))).ToList();
    }

    public async Task<Result<SavingsPotDto>> CreatePotAsync(Guid budgetId, SavingsPotRequest request, CancellationToken ct)
    {
        if (await IsNameTakenAsync(budgetId, request.Name, except: null, ct)) return NameTaken;

        var currency = await db.CurrencyOrDefaultAsync(budgetId, request.Currency, ct);
        var pot = new SavingsPot(budgetId, request.Name, request.Color, currency, request.Target);
        db.SavingsPots.Add(pot);
        await db.SaveChangesAsync(ct);
        return pot.ToDto(0);
    }

    public async Task<Result<SavingsPotDto>> UpdatePotAsync(Guid budgetId, Guid potId, SavingsPotRequest request, CancellationToken ct)
    {
        var pot = await db.SavingsPots.InBudget(budgetId).FirstOrDefaultAsync(p => p.Id == potId, ct);
        if (pot is null) return Error.NotFound;
        if (await IsNameTakenAsync(budgetId, request.Name, except: potId, ct)) return NameTaken;

        var balance = await BalanceAsync(potId, ct);
        if (request.Currency is not null)
        {
            var currency = await db.CurrencyOrDefaultAsync(budgetId, request.Currency, ct);
            if (currency != pot.Currency)
            {
                if (await db.SavingsEntries.AnyAsync(e => e.PotId == potId, ct))
                    return Error.Conflict("The currency can only change while the pot has no entries.");
                pot.ChangeCurrency(currency);
            }
        }

        pot.Rename(request.Name);
        pot.Recolor(request.Color);
        pot.ChangeTarget(request.Target);
        await db.SaveChangesAsync(ct);
        return pot.ToDto(balance);
    }

    public async Task<Result> DeletePotAsync(Guid budgetId, Guid potId, CancellationToken ct)
    {
        var pot = await db.SavingsPots.InBudget(budgetId).FirstOrDefaultAsync(p => p.Id == potId, ct);
        if (pot is null) return Error.NotFound;

        db.SavingsPots.Remove(pot);   // its entries go with it (cascade)
        await db.SaveChangesAsync(ct);
        return Result.Success;
    }

    public async Task<Result<IReadOnlyList<SavingsEntryDto>>> ListEntriesAsync(Guid budgetId, Guid potId, CancellationToken ct)
    {
        if (!await db.SavingsPots.InBudget(budgetId).AnyAsync(p => p.Id == potId, ct)) return Error.NotFound;

        var entries = await db.SavingsEntries.AsNoTracking().InBudget(budgetId).Where(e => e.PotId == potId)
            .OrderByDescending(e => e.Date).ThenByDescending(e => e.CreatedAt).ToListAsync(ct);
        return entries.Select(e => e.ToDto()).ToList();
    }

    public async Task<Result<SavingsEntryDto>> AddEntryAsync(Guid budgetId, Guid potId, SavingsEntryRequest request, CancellationToken ct)
    {
        var pot = await db.SavingsPots.AsNoTracking().InBudget(budgetId).FirstOrDefaultAsync(p => p.Id == potId, ct);
        if (pot is null) return Error.NotFound;

        SavingsEntry entry;
        if (request.Kind == SavingsEntryKind.Withdrawal)
        {
            var balance = await BalanceAsync(potId, ct);
            if (request.Amount > balance)
                return Error.Validation($"You can't take out more than the pot holds ({balance:N2} {pot.Currency}).");
            entry = SavingsEntry.Withdrawal(pot, request.Date, request.Amount, request.Note);
        }
        else
        {
            entry = SavingsEntry.Deposit(pot, request.Date, request.Amount, request.Note);
        }

        db.SavingsEntries.Add(entry);
        audit.Created(entry, budgetId, EntryKind.SavingsEntry, entry.Id, Values(entry, pot.Name));
        await db.SaveChangesAsync(ct);
        return entry.ToDto();
    }

    public async Task<Result> DeleteEntryAsync(Guid budgetId, Guid potId, Guid entryId, CancellationToken ct)
    {
        var entry = await db.SavingsEntries.InBudget(budgetId).FirstOrDefaultAsync(e => e.Id == entryId && e.PotId == potId, ct);
        if (entry is null) return Error.NotFound;

        // Removing a deposit must not leave the pot below zero.
        if (!entry.IsWithdrawal && await BalanceAsync(potId, ct) - entry.Amount < 0)
            return Error.Conflict("Money was already taken out of this deposit. Delete the withdrawal first.");

        var potName = await db.SavingsPots.Where(p => p.Id == potId).Select(p => p.Name).FirstAsync(ct);
        audit.Deleted(budgetId, EntryKind.SavingsEntry, entry.Id, Values(entry, potName));
        db.SavingsEntries.Remove(entry);
        await db.SaveChangesAsync(ct);
        return Result.Success;
    }

    private static IReadOnlyList<(string, string?)> Values(SavingsEntry e, string potName) =>
        [("Pot", potName), ("Type", e.IsWithdrawal ? "Withdrawal" : "Deposit"), ("Date", EntryAudit.Date(e.Date)),
         ("Amount", EntryAudit.Amount(Math.Abs(e.Amount))), ("Note", e.Note)];

    private async Task<decimal> BalanceAsync(Guid potId, CancellationToken ct) =>
        (await db.SavingsEntries.AsNoTracking().Where(e => e.PotId == potId).Select(e => e.Amount).ToListAsync(ct)).Sum();

    private async Task<Dictionary<Guid, decimal>> BalancesAsync(Guid budgetId, CancellationToken ct)
    {
        var rows = await db.SavingsEntries.AsNoTracking().InBudget(budgetId).Select(e => new { e.PotId, e.Amount }).ToListAsync(ct);
        return rows.GroupBy(r => r.PotId).ToDictionary(g => g.Key, g => g.Sum(r => r.Amount));
    }

    private Task<bool> IsNameTakenAsync(Guid budgetId, string name, Guid? except, CancellationToken ct)
    {
        var lowered = name.Trim().ToLower();
        return db.SavingsPots.InBudget(budgetId).AnyAsync(p => p.Id != except && p.Name.ToLower() == lowered, ct);
    }
}
