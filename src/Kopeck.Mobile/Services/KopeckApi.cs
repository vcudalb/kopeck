using System.Globalization;
using Kopeck.Contracts;

namespace Kopeck.Mobile.Services;

// Typed gateways, one per feature (interface segregation): a view model depends only on the calls it makes,
// and every URL of the API is written in exactly one place.

public interface IAuthApi
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct);
    Task RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<AuthResponse> VerifyEmailAsync(EmailCodeRequest request, CancellationToken ct);
    Task ResendCodeAsync(string email, CancellationToken ct);
    Task ForgotPasswordAsync(string email, CancellationToken ct);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct);
}

public interface IBudgetsApi
{
    Task<List<BudgetDto>> ListAsync(CancellationToken ct);
    Task<BudgetDto> CreateAsync(BudgetRequest request, CancellationToken ct);
    Task DeleteAsync(Guid budgetId, CancellationToken ct);
    Task LeaveAsync(Guid budgetId, CancellationToken ct);
    Task<MonthSummaryDto> GetSummaryAsync(Guid budgetId, DateOnly month, CancellationToken ct);
}

public interface ICategoriesApi
{
    Task<List<CategoryDto>> ListAsync(Guid budgetId, CancellationToken ct);
    Task<CategoryDto> CreateAsync(Guid budgetId, CategoryRequest request, CancellationToken ct);
    Task<CategoryDto> UpdateAsync(Guid budgetId, Guid categoryId, CategoryRequest request, CancellationToken ct);
    Task DeleteAsync(Guid budgetId, Guid categoryId, CancellationToken ct);
}

public interface IExpensesApi
{
    Task<List<ExpenseDto>> ListAsync(Guid budgetId, DateOnly month, CancellationToken ct);
    Task<ExpenseDto> CreateAsync(Guid budgetId, ExpenseRequest request, CancellationToken ct);
    Task<ExpenseDto> UpdateAsync(Guid budgetId, Guid expenseId, ExpenseRequest request, CancellationToken ct);
    Task DeleteAsync(Guid budgetId, Guid expenseId, CancellationToken ct);
}

public interface IIncomeApi
{
    Task<List<IncomeStreamDto>> ListStreamsAsync(Guid budgetId, CancellationToken ct);
    Task<IncomeStreamDto> CreateStreamAsync(Guid budgetId, IncomeStreamRequest request, CancellationToken ct);
    Task<List<IncomeDto>> ListAsync(Guid budgetId, DateOnly month, CancellationToken ct);
    Task<IncomeDto> CreateAsync(Guid budgetId, IncomeRequest request, CancellationToken ct);
    Task DeleteAsync(Guid budgetId, Guid incomeId, CancellationToken ct);
}

public interface IPlansApi
{
    Task<List<PlanDto>> ListAsync(Guid budgetId, DateOnly month, CancellationToken ct);
    Task SaveAsync(Guid budgetId, SavePlansRequest request, CancellationToken ct);
    Task<CopyPlanResponse> CopyAsync(Guid budgetId, CopyPlanRequest request, CancellationToken ct);
}

public interface ISharesApi
{
    Task<List<ShareDto>> ListAsync(Guid budgetId, CancellationToken ct);
    Task<ShareDto> ShareAsync(Guid budgetId, ShareRequest request, CancellationToken ct);
    Task RemoveAsync(Guid budgetId, Guid shareId, CancellationToken ct);
}

internal static class Routes
{
    public static string Budget(Guid id) => $"/api/budgets/{id}";
    public static string Month(DateOnly m) => m.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}

public sealed class AuthApi(IApiClient api) : IAuthApi
{
    public Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct) => api.PostAsync<AuthResponse>("/api/auth/login", request, ct);
    public Task RegisterAsync(RegisterRequest request, CancellationToken ct) => api.PostAsync("/api/auth/register", request, ct);
    public Task<AuthResponse> VerifyEmailAsync(EmailCodeRequest request, CancellationToken ct) => api.PostAsync<AuthResponse>("/api/auth/verify-email", request, ct);
    public Task ResendCodeAsync(string email, CancellationToken ct) => api.PostAsync("/api/auth/resend-code", new EmailRequest(email), ct);
    public Task ForgotPasswordAsync(string email, CancellationToken ct) => api.PostAsync("/api/auth/forgot-password", new EmailRequest(email), ct);
    public Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct) => api.PostAsync("/api/auth/reset-password", request, ct);
}

public sealed class BudgetsApi(IApiClient api) : IBudgetsApi
{
    public Task<List<BudgetDto>> ListAsync(CancellationToken ct) => api.GetAsync<List<BudgetDto>>("/api/budgets", ct);
    public Task<BudgetDto> CreateAsync(BudgetRequest request, CancellationToken ct) => api.PostAsync<BudgetDto>("/api/budgets", request, ct);
    public Task DeleteAsync(Guid budgetId, CancellationToken ct) => api.DeleteAsync(Routes.Budget(budgetId), ct);
    public Task LeaveAsync(Guid budgetId, CancellationToken ct) => api.DeleteAsync($"{Routes.Budget(budgetId)}/leave", ct);
    public Task<MonthSummaryDto> GetSummaryAsync(Guid budgetId, DateOnly month, CancellationToken ct) =>
        api.GetAsync<MonthSummaryDto>($"{Routes.Budget(budgetId)}/summary?month={Routes.Month(month)}", ct);
}

public sealed class CategoriesApi(IApiClient api) : ICategoriesApi
{
    public Task<List<CategoryDto>> ListAsync(Guid budgetId, CancellationToken ct) => api.GetAsync<List<CategoryDto>>($"{Routes.Budget(budgetId)}/categories", ct);
    public Task<CategoryDto> CreateAsync(Guid budgetId, CategoryRequest request, CancellationToken ct) =>
        api.PostAsync<CategoryDto>($"{Routes.Budget(budgetId)}/categories", request, ct);
    public Task<CategoryDto> UpdateAsync(Guid budgetId, Guid categoryId, CategoryRequest request, CancellationToken ct) =>
        api.PutAsync<CategoryDto>($"{Routes.Budget(budgetId)}/categories/{categoryId}", request, ct);
    public Task DeleteAsync(Guid budgetId, Guid categoryId, CancellationToken ct) => api.DeleteAsync($"{Routes.Budget(budgetId)}/categories/{categoryId}", ct);
}

public sealed class ExpensesApi(IApiClient api) : IExpensesApi
{
    public Task<List<ExpenseDto>> ListAsync(Guid budgetId, DateOnly month, CancellationToken ct) =>
        api.GetAsync<List<ExpenseDto>>($"{Routes.Budget(budgetId)}/expenses?month={Routes.Month(month)}", ct);
    public Task<ExpenseDto> CreateAsync(Guid budgetId, ExpenseRequest request, CancellationToken ct) =>
        api.PostAsync<ExpenseDto>($"{Routes.Budget(budgetId)}/expenses", request, ct);
    public Task<ExpenseDto> UpdateAsync(Guid budgetId, Guid expenseId, ExpenseRequest request, CancellationToken ct) =>
        api.PutAsync<ExpenseDto>($"{Routes.Budget(budgetId)}/expenses/{expenseId}", request, ct);
    public Task DeleteAsync(Guid budgetId, Guid expenseId, CancellationToken ct) => api.DeleteAsync($"{Routes.Budget(budgetId)}/expenses/{expenseId}", ct);
}

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

public sealed class PlansApi(IApiClient api) : IPlansApi
{
    public Task<List<PlanDto>> ListAsync(Guid budgetId, DateOnly month, CancellationToken ct) =>
        api.GetAsync<List<PlanDto>>($"{Routes.Budget(budgetId)}/plans?month={Routes.Month(month)}", ct);
    public Task SaveAsync(Guid budgetId, SavePlansRequest request, CancellationToken ct) => api.PutAsync($"{Routes.Budget(budgetId)}/plans/bulk", request, ct);
    public Task<CopyPlanResponse> CopyAsync(Guid budgetId, CopyPlanRequest request, CancellationToken ct) =>
        api.PostAsync<CopyPlanResponse>($"{Routes.Budget(budgetId)}/plans/copy", request, ct);
}

public sealed class SharesApi(IApiClient api) : ISharesApi
{
    public Task<List<ShareDto>> ListAsync(Guid budgetId, CancellationToken ct) => api.GetAsync<List<ShareDto>>($"{Routes.Budget(budgetId)}/shares", ct);
    public Task<ShareDto> ShareAsync(Guid budgetId, ShareRequest request, CancellationToken ct) =>
        api.PostAsync<ShareDto>($"{Routes.Budget(budgetId)}/shares", request, ct);
    public Task RemoveAsync(Guid budgetId, Guid shareId, CancellationToken ct) => api.DeleteAsync($"{Routes.Budget(budgetId)}/shares/{shareId}", ct);
}
