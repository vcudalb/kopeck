using Kopeck.Application.Common;
using Kopeck.Contracts;

namespace Kopeck.Application.Savings;

public interface ISavingsService
{
    Task<IReadOnlyList<SavingsPotDto>> ListPotsAsync(Guid budgetId, CancellationToken ct);
    Task<Result<SavingsPotDto>> CreatePotAsync(Guid budgetId, SavingsPotRequest request, CancellationToken ct);
    Task<Result<SavingsPotDto>> UpdatePotAsync(Guid budgetId, Guid potId, SavingsPotRequest request, CancellationToken ct);
    Task<Result> DeletePotAsync(Guid budgetId, Guid potId, CancellationToken ct);

    Task<Result<IReadOnlyList<SavingsEntryDto>>> ListEntriesAsync(Guid budgetId, Guid potId, CancellationToken ct);
    Task<Result<SavingsEntryDto>> AddEntryAsync(Guid budgetId, Guid potId, SavingsEntryRequest request, CancellationToken ct);
    Task<Result> DeleteEntryAsync(Guid budgetId, Guid potId, Guid entryId, CancellationToken ct);
}
