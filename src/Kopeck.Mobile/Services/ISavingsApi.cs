using Kopeck.Contracts;

namespace Kopeck.Mobile.Services;

public interface ISavingsApi
{
    Task<List<SavingsPotDto>> ListPotsAsync(Guid budgetId, CancellationToken ct);
    Task<SavingsPotDto> CreatePotAsync(Guid budgetId, SavingsPotRequest request, CancellationToken ct);
    Task<SavingsPotDto> UpdatePotAsync(Guid budgetId, Guid potId, SavingsPotRequest request, CancellationToken ct);
    Task DeletePotAsync(Guid budgetId, Guid potId, CancellationToken ct);
    Task<List<SavingsEntryDto>> ListEntriesAsync(Guid budgetId, Guid potId, CancellationToken ct);
    Task<SavingsEntryDto> AddEntryAsync(Guid budgetId, Guid potId, SavingsEntryRequest request, CancellationToken ct);
    Task DeleteEntryAsync(Guid budgetId, Guid potId, Guid entryId, CancellationToken ct);
}
