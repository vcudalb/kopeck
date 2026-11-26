using Kopeck.Contracts;

namespace Kopeck.Mobile.Services;

public sealed class SavingsApi(IApiClient api) : ISavingsApi
{
    private static string Pots(Guid budgetId) => $"{Routes.Budget(budgetId)}/savings";
    private static string Entries(Guid budgetId, Guid potId) => $"{Pots(budgetId)}/{potId}/entries";

    public Task<List<SavingsPotDto>> ListPotsAsync(Guid budgetId, CancellationToken ct) => api.GetAsync<List<SavingsPotDto>>(Pots(budgetId), ct);
    public Task<SavingsPotDto> CreatePotAsync(Guid budgetId, SavingsPotRequest request, CancellationToken ct) =>
        api.PostAsync<SavingsPotDto>(Pots(budgetId), request, ct);
    public Task<SavingsPotDto> UpdatePotAsync(Guid budgetId, Guid potId, SavingsPotRequest request, CancellationToken ct) =>
        api.PutAsync<SavingsPotDto>($"{Pots(budgetId)}/{potId}", request, ct);
    public Task DeletePotAsync(Guid budgetId, Guid potId, CancellationToken ct) => api.DeleteAsync($"{Pots(budgetId)}/{potId}", ct);
    public Task<List<SavingsEntryDto>> ListEntriesAsync(Guid budgetId, Guid potId, CancellationToken ct) =>
        api.GetAsync<List<SavingsEntryDto>>(Entries(budgetId, potId), ct);
    public Task<SavingsEntryDto> AddEntryAsync(Guid budgetId, Guid potId, SavingsEntryRequest request, CancellationToken ct) =>
        api.PostAsync<SavingsEntryDto>(Entries(budgetId, potId), request, ct);
    public Task DeleteEntryAsync(Guid budgetId, Guid potId, Guid entryId, CancellationToken ct) =>
        api.DeleteAsync($"{Entries(budgetId, potId)}/{entryId}", ct);
}
