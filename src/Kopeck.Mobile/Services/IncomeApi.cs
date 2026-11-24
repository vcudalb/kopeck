using Kopeck.Contracts;

namespace Kopeck.Mobile.Services;

public sealed class IncomeApi(IApiClient api) : IIncomeApi
{
    public Task<List<IncomeStreamDto>> ListStreamsAsync(Guid budgetId, CancellationToken ct) =>
        api.GetAsync<List<IncomeStreamDto>>($"{Routes.Budget(budgetId)}/income-streams", ct);
    public Task<IncomeStreamDto> CreateStreamAsync(Guid budgetId, IncomeStreamRequest request, CancellationToken ct) =>
        api.PostAsync<IncomeStreamDto>($"{Routes.Budget(budgetId)}/income-streams", request, ct);
    public Task<List<IncomeDto>> ListAsync(Guid budgetId, DateOnly month, CancellationToken ct) =>
        api.GetAsync<List<IncomeDto>>($"{Routes.Budget(budgetId)}/incomes?month={Routes.Month(month)}", ct);
    public Task<IncomeDto> CreateAsync(Guid budgetId, IncomeRequest request, CancellationToken ct) =>
        api.PostAsync<IncomeDto>($"{Routes.Budget(budgetId)}/incomes", request, ct);
    public Task DeleteAsync(Guid budgetId, Guid incomeId, CancellationToken ct) => api.DeleteAsync($"{Routes.Budget(budgetId)}/incomes/{incomeId}", ct);
}
