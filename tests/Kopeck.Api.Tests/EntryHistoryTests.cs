using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Kopeck.Contracts;

namespace Kopeck.Api.Tests;

public class EntryHistoryTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _f;
    public EntryHistoryTests(ApiFactory f) => _f = f;

    [Fact]
    public async Task Expense_history_records_who_did_what_and_the_values_that_changed()
    {
        var owner = await SignInAsync("hist-owner@test.dev", "Olivia");
        var editor = await SignInAsync("hist-editor@test.dev", "Eddie");
        var b = await Ok<BudgetDto>(await owner.PostAsJsonAsync("/api/budgets", new BudgetRequest("Home", "EUR"), Json.Options));
        var api = $"/api/budgets/{b.Id}";
        (await owner.PostAsJsonAsync($"{api}/shares", new ShareRequest("hist-editor@test.dev", ShareRole.Editor), Json.Options)).EnsureSuccessStatusCode();
        var fuel = await Ok<CategoryDto>(await owner.PostAsJsonAsync($"{api}/categories", new CategoryRequest("Fuel", null), Json.Options));
        var food = await Ok<CategoryDto>(await owner.PostAsJsonAsync($"{api}/categories", new CategoryRequest("Food", null), Json.Options));

        var before = DateTime.UtcNow.AddMinutes(-1);
        var created = await Ok<ExpenseDto>(await owner.PostAsJsonAsync($"{api}/expenses", new ExpenseRequest(fuel.Id, new(2026, 10, 1), 20, null), Json.Options));
        Assert.Equal("Olivia", created.CreatedBy);
        Assert.InRange(created.CreatedAt, before, DateTime.UtcNow.AddMinutes(1));
        Assert.Null(created.UpdatedAt);

        // saving the same values is not a change
        var same = await Ok<ExpenseDto>(await editor.PutAsJsonAsync($"{api}/expenses/{created.Id}", new ExpenseRequest(fuel.Id, new(2026, 10, 1), 20, null), Json.Options));
        Assert.Null(same.UpdatedAt);

        var updated = await Ok<ExpenseDto>(await editor.PutAsJsonAsync($"{api}/expenses/{created.Id}", new ExpenseRequest(food.Id, new(2026, 10, 1), 25.5m, "Lunch"), Json.Options));
        Assert.Equal(("Olivia", "Eddie"), (updated.CreatedBy, updated.UpdatedBy));
        Assert.NotNull(updated.UpdatedAt);

        var history = await Ok<List<EntryChangeDto>>(await owner.GetAsync($"{api}/expenses/{created.Id}/history"));
        Assert.Equal([EntryAction.Updated, EntryAction.Created], history.Select(h => h.Action));

        var edit = history[0];
        Assert.Equal("Eddie", edit.By);
        Assert.Equal(
            [new FieldChangeDto("Category", "Fuel", "Food"), new FieldChangeDto("Amount", "20.00", "25.50"), new FieldChangeDto("Note", null, "Lunch")],
            edit.Changes);

        var add = history[1];
        Assert.Equal("Olivia", add.By);
        Assert.Equal(
            [new FieldChangeDto("Category", null, "Fuel"), new FieldChangeDto("Date", null, "2026-10-01"), new FieldChangeDto("Amount", null, "20.00")],
            add.Changes);
    }

    [Fact]
    public async Task A_deleted_expense_keeps_its_history_and_other_budgets_cannot_read_it()
    {
        var c = await SignInAsync("hist-delete@test.dev", "Dana");
        var b = await Ok<BudgetDto>(await c.PostAsJsonAsync("/api/budgets", new BudgetRequest("Home", "EUR"), Json.Options));
        var other = await Ok<BudgetDto>(await c.PostAsJsonAsync("/api/budgets", new BudgetRequest("Other", "EUR"), Json.Options));
        var api = $"/api/budgets/{b.Id}";
        var cat = await Ok<CategoryDto>(await c.PostAsJsonAsync($"{api}/categories", new CategoryRequest("Bills", null), Json.Options));
        var e = await Ok<ExpenseDto>(await c.PostAsJsonAsync($"{api}/expenses", new ExpenseRequest(cat.Id, new(2026, 11, 3), 42, "Water"), Json.Options));

        (await c.DeleteAsync($"{api}/expenses/{e.Id}")).EnsureSuccessStatusCode();

        var history = await Ok<List<EntryChangeDto>>(await c.GetAsync($"{api}/expenses/{e.Id}/history"));
        Assert.Equal([EntryAction.Deleted, EntryAction.Created], history.Select(h => h.Action));
        Assert.Contains(new FieldChangeDto("Amount", "42.00", null), history[0].Changes);

        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/budgets/{other.Id}/expenses/{e.Id}/history")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"{api}/expenses/{Guid.NewGuid()}/history")).StatusCode);
    }

    [Fact]
    public async Task Income_and_savings_entries_are_stamped_with_who_added_them()
    {
        var c = await SignInAsync("hist-stamp@test.dev", "Sam");
        var b = await Ok<BudgetDto>(await c.PostAsJsonAsync("/api/budgets", new BudgetRequest("Home", "EUR"), Json.Options));
        var api = $"/api/budgets/{b.Id}";
        var stream = await Ok<IncomeStreamDto>(await c.PostAsJsonAsync($"{api}/income-streams", new IncomeStreamRequest("Salary"), Json.Options));
        var income = await Ok<IncomeDto>(await c.PostAsJsonAsync($"{api}/incomes", new IncomeRequest(stream.Id, new(2026, 11, 5), 1000, null), Json.Options));
        var pot = await Ok<SavingsPotDto>(await c.PostAsJsonAsync($"{api}/savings", new SavingsPotRequest("Fund"), Json.Options));
        var entry = await Ok<SavingsEntryDto>(await c.PostAsJsonAsync($"{api}/savings/{pot.Id}/entries", new SavingsEntryRequest(SavingsEntryKind.Deposit, new(2026, 11, 6), 50, null), Json.Options));

        Assert.Equal("Sam", income.CreatedBy);
        Assert.NotEqual(default, income.CreatedAt);
        Assert.Equal("Sam", entry.CreatedBy);
        Assert.NotEqual(default, entry.CreatedAt);
    }

    private async Task<HttpClient> SignInAsync(string email, string name)
    {
        var c = _f.CreateClient();
        (await c.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Passw0rd!x", name), Json.Options)).EnsureSuccessStatusCode();
        var auth = await Ok<AuthResponse>(await c.PostAsJsonAsync("/api/auth/verify-email", new EmailCodeRequest(email, _f.Mail.LastCode(email)), Json.Options));
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return c;
    }

    private static async Task<T> Ok<T>(HttpResponseMessage r)
    {
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        return (await r.Content.ReadFromJsonAsync<T>(Json.Options))!;
    }
}
