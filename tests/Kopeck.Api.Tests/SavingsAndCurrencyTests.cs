using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Kopeck.Contracts;

namespace Kopeck.Api.Tests;

public class SavingsAndCurrencyTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _f;
    public SavingsAndCurrencyTests(ApiFactory f) => _f = f;

    [Fact]
    public async Task New_streams_and_pots_take_the_budget_currency_unless_another_is_chosen()
    {
        var (c, b) = await BudgetAsync("cur-default@test.dev", "MDL");

        var salary = await Ok<IncomeStreamDto>(await c.PostAsJsonAsync($"/api/budgets/{b.Id}/income-streams", new IncomeStreamRequest("Salary"), Json.Options));
        var remote = await Ok<IncomeStreamDto>(await c.PostAsJsonAsync($"/api/budgets/{b.Id}/income-streams", new IncomeStreamRequest("Remote job", "usd"), Json.Options));
        var pot = await Ok<SavingsPotDto>(await c.PostAsJsonAsync($"/api/budgets/{b.Id}/savings", new SavingsPotRequest("Holiday"), Json.Options));

        Assert.Equal("MDL", salary.Currency);
        Assert.Equal("USD", remote.Currency);
        Assert.Equal("MDL", pot.Currency);
        Assert.Null(pot.Target);

        var bad = await c.PostAsJsonAsync($"/api/budgets/{b.Id}/income-streams", new IncomeStreamRequest("Bad", "E1"), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    [Fact]
    public async Task Pot_balance_follows_deposits_and_withdrawals_and_never_goes_below_zero()
    {
        var (c, b) = await BudgetAsync("pot-balance@test.dev", "EUR");
        var pot = await Ok<SavingsPotDto>(await c.PostAsJsonAsync($"/api/budgets/{b.Id}/savings",
            new SavingsPotRequest("Emergency fund", Target: 1000), Json.Options));
        var entries = $"/api/budgets/{b.Id}/savings/{pot.Id}/entries";

        var deposit = await Ok<SavingsEntryDto>(await c.PostAsJsonAsync(entries, new SavingsEntryRequest(SavingsEntryKind.Deposit, new(2026, 11, 1), 300, "first"), Json.Options));
        var withdrawal = await Ok<SavingsEntryDto>(await c.PostAsJsonAsync(entries, new SavingsEntryRequest(SavingsEntryKind.Withdrawal, new(2026, 11, 2), 120, null), Json.Options));
        Assert.Equal(-120, withdrawal.Amount);
        Assert.Equal(180, (await PotAsync(c, b.Id, pot.Id)).Balance);

        // more than the pot holds
        var tooMuch = await c.PostAsJsonAsync(entries, new SavingsEntryRequest(SavingsEntryKind.Withdrawal, new(2026, 11, 3), 500, null), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, tooMuch.StatusCode);
        Assert.Contains("180.00 EUR", (await tooMuch.Content.ReadFromJsonAsync<ApiError>(Json.Options))!.Error);

        // removing the deposit would leave -120
        Assert.Equal(HttpStatusCode.Conflict, (await c.DeleteAsync($"{entries}/{deposit.Id}")).StatusCode);

        // the currency is fixed once there is history
        var change = await c.PutAsJsonAsync($"/api/budgets/{b.Id}/savings/{pot.Id}", new SavingsPotRequest("Emergency fund", Currency: "USD", Target: 1000), Json.Options);
        Assert.Equal(HttpStatusCode.Conflict, change.StatusCode);

        // rename and new target are fine
        var renamed = await Ok<SavingsPotDto>(await c.PutAsJsonAsync($"/api/budgets/{b.Id}/savings/{pot.Id}", new SavingsPotRequest("Rainy day", Target: 2000), Json.Options));
        Assert.Equal(("Rainy day", 2000m, 180m), (renamed.Name, renamed.Target!.Value, renamed.Balance));

        // deleting the pot takes its history with it
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/budgets/{b.Id}/savings/{pot.Id}")).StatusCode);
        Assert.Empty(await Ok<List<SavingsPotDto>>(await c.GetAsync($"/api/budgets/{b.Id}/savings")));
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync(entries)).StatusCode);
    }

    [Fact]
    public async Task Summary_subtracts_savings_and_keeps_each_currency_separate()
    {
        var (c, b) = await BudgetAsync("summary-cur@test.dev", "EUR");
        var food = await Ok<CategoryDto>(await c.PostAsJsonAsync($"/api/budgets/{b.Id}/categories", new CategoryRequest("Food", null), Json.Options));
        var salary = await Ok<IncomeStreamDto>(await c.PostAsJsonAsync($"/api/budgets/{b.Id}/income-streams", new IncomeStreamRequest("Salary"), Json.Options));
        var remote = await Ok<IncomeStreamDto>(await c.PostAsJsonAsync($"/api/budgets/{b.Id}/income-streams", new IncomeStreamRequest("Remote", "USD"), Json.Options));
        var eurPot = await Ok<SavingsPotDto>(await c.PostAsJsonAsync($"/api/budgets/{b.Id}/savings", new SavingsPotRequest("Fund"), Json.Options));
        var usdPot = await Ok<SavingsPotDto>(await c.PostAsJsonAsync($"/api/budgets/{b.Id}/savings", new SavingsPotRequest("Dollars", Currency: "USD"), Json.Options));

        (await c.PostAsJsonAsync($"/api/budgets/{b.Id}/incomes", new IncomeRequest(salary.Id, new(2026, 11, 5), 1000, null), Json.Options)).EnsureSuccessStatusCode();
        (await c.PostAsJsonAsync($"/api/budgets/{b.Id}/incomes", new IncomeRequest(remote.Id, new(2026, 11, 6), 200, null), Json.Options)).EnsureSuccessStatusCode();
        (await c.PostAsJsonAsync($"/api/budgets/{b.Id}/expenses", new ExpenseRequest(food.Id, new(2026, 11, 7), 300, null), Json.Options)).EnsureSuccessStatusCode();
        (await c.PostAsJsonAsync($"/api/budgets/{b.Id}/savings/{eurPot.Id}/entries", new SavingsEntryRequest(SavingsEntryKind.Deposit, new(2026, 11, 8), 100, null), Json.Options)).EnsureSuccessStatusCode();
        (await c.PostAsJsonAsync($"/api/budgets/{b.Id}/savings/{usdPot.Id}/entries", new SavingsEntryRequest(SavingsEntryKind.Deposit, new(2026, 11, 9), 50, null), Json.Options)).EnsureSuccessStatusCode();
        (await c.PostAsJsonAsync($"/api/budgets/{b.Id}/savings/{eurPot.Id}/entries", new SavingsEntryRequest(SavingsEntryKind.Deposit, new(2026, 10, 1), 999, null), Json.Options)).EnsureSuccessStatusCode(); // other month

        var s = await Ok<MonthSummaryDto>(await c.GetAsync($"/api/budgets/{b.Id}/summary?month=2026-11-01"));

        Assert.Equal((1000m, 300m, 100m, 600m), (s.TotalIncome, s.TotalSpent, s.TotalSaved, s.NetLeft));
        Assert.Collection(s.Totals,
            eur => Assert.Equal(new CurrencyTotalDto("EUR", 1000, 300, 100, 600), eur),
            usd => Assert.Equal(new CurrencyTotalDto("USD", 200, 0, 50, 150), usd));
        Assert.Equal(["Fund", "Dollars"], s.Savings.Select(p => p.Name));
        Assert.Equal("USD", s.IncomeStreams.Single(i => i.Name == "Remote").Currency);
    }

    private async Task<(HttpClient Client, BudgetDto Budget)> BudgetAsync(string email, string currency)
    {
        var c = _f.CreateClient();
        (await c.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Passw0rd!x", "Saver"), Json.Options)).EnsureSuccessStatusCode();
        var ver = await c.PostAsJsonAsync("/api/auth/verify-email", new EmailCodeRequest(email, _f.Mail.LastCode(email)), Json.Options);
        var auth = await Ok<AuthResponse>(ver);
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        var budget = await Ok<BudgetDto>(await c.PostAsJsonAsync("/api/budgets", new BudgetRequest("Home", currency), Json.Options));
        return (c, budget);
    }

    private static async Task<SavingsPotDto> PotAsync(HttpClient c, Guid budgetId, Guid potId) =>
        (await Ok<List<SavingsPotDto>>(await c.GetAsync($"/api/budgets/{budgetId}/savings"))).Single(p => p.Id == potId);

    private static async Task<T> Ok<T>(HttpResponseMessage r)
    {
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        return (await r.Content.ReadFromJsonAsync<T>(Json.Options))!;
    }
}
