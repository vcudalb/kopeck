using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Kopeck.Contracts;

namespace Kopeck.Api.Tests;

public class YearSummaryTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _f;
    public YearSummaryTests(ApiFactory f) => _f = f;

    [Fact]
    public async Task Year_summary_totals_each_month_and_each_category_in_the_budget_currency()
    {
        var (c, b) = await BudgetAsync("year@test.dev");
        var api = $"/api/budgets/{b.Id}";
        var food = await Ok<CategoryDto>(await c.PostAsJsonAsync($"{api}/categories", new CategoryRequest("Food", null), Json.Options));
        var fun = await Ok<CategoryDto>(await c.PostAsJsonAsync($"{api}/categories", new CategoryRequest("Fun", null), Json.Options));
        var salary = await Ok<IncomeStreamDto>(await c.PostAsJsonAsync($"{api}/income-streams", new IncomeStreamRequest("Salary"), Json.Options));
        var dollars = await Ok<IncomeStreamDto>(await c.PostAsJsonAsync($"{api}/income-streams", new IncomeStreamRequest("Remote", "USD"), Json.Options));
        var pot = await Ok<SavingsPotDto>(await c.PostAsJsonAsync($"{api}/savings", new SavingsPotRequest("Fund"), Json.Options));

        async Task Post(string path, object body) => (await c.PostAsJsonAsync($"{api}/{path}", body, Json.Options)).EnsureSuccessStatusCode();
        await Post("incomes", new IncomeRequest(salary.Id, new(2026, 1, 5), 1000, null));
        await Post("incomes", new IncomeRequest(salary.Id, new(2026, 3, 5), 1200, null));
        await Post("incomes", new IncomeRequest(dollars.Id, new(2026, 3, 6), 500, null));        // other currency: left out
        await Post("expenses", new ExpenseRequest(food.Id, new(2026, 1, 10), 100, null));
        await Post("expenses", new ExpenseRequest(food.Id, new(2026, 3, 10), 50, null));
        await Post("expenses", new ExpenseRequest(fun.Id, new(2026, 3, 11), 30, null));
        await Post("expenses", new ExpenseRequest(food.Id, new(2025, 12, 31), 999, null));        // other year
        await Post($"savings/{pot.Id}/entries", new SavingsEntryRequest(SavingsEntryKind.Deposit, new(2026, 3, 20), 100, null));
        (await c.PutAsJsonAsync($"{api}/plans", new PlanRequest(food.Id, new(2026, 1, 1), 200), Json.Options)).EnsureSuccessStatusCode();

        var y = await Ok<YearSummaryDto>(await c.GetAsync($"{api}/summary/year?year=2026"));

        Assert.Equal((2026, "EUR", 12), (y.Year, y.Currency, y.Months.Count));
        Assert.Equal(new MonthTotalsDto(new(2026, 1, 1), 1000, 200, 100, 0), y.Months[0]);
        Assert.Equal(new MonthTotalsDto(new(2026, 3, 1), 1200, 0, 80, 100), y.Months[2]);
        Assert.Equal(new MonthTotalsDto(new(2026, 12, 1), 0, 0, 0, 0), y.Months[11]);

        var foodYear = y.Categories.Single(x => x.Name == "Food");
        Assert.Equal((150m, 200m), (foodYear.Spent, foodYear.Planned));
        Assert.Equal([100m, 0, 50m, 0, 0, 0, 0, 0, 0, 0, 0, 0], foodYear.MonthlySpent);
        Assert.Equal(30m, y.Categories.Single(x => x.Name == "Fun").Spent);
    }

    [Fact]
    public async Task Year_outside_the_supported_range_is_rejected()
    {
        var (c, b) = await BudgetAsync("year-range@test.dev");
        var r = await c.GetAsync($"/api/budgets/{b.Id}/summary/year?year=1999");
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Contains("between 2000 and 2100", (await r.Content.ReadFromJsonAsync<ApiError>(Json.Options))!.Error);
    }

    private async Task<(HttpClient Client, BudgetDto Budget)> BudgetAsync(string email)
    {
        var c = _f.CreateClient();
        (await c.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Passw0rd!x", "Yearly"), Json.Options)).EnsureSuccessStatusCode();
        var auth = await Ok<AuthResponse>(await c.PostAsJsonAsync("/api/auth/verify-email", new EmailCodeRequest(email, _f.Mail.LastCode(email)), Json.Options));
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return (c, await Ok<BudgetDto>(await c.PostAsJsonAsync("/api/budgets", new BudgetRequest("Home", "EUR"), Json.Options)));
    }

    private static async Task<T> Ok<T>(HttpResponseMessage r)
    {
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        return (await r.Content.ReadFromJsonAsync<T>(Json.Options))!;
    }
}
