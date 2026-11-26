using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Kopeck.Contracts;

namespace Kopeck.Api.Tests;

public class CategoryStarTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _f;
    public CategoryStarTests(ApiFactory f) => _f = f;

    [Fact]
    public async Task Starred_categories_come_first_in_lists_the_plan_and_the_summary()
    {
        var c = await SignInAsync("star@test.dev");
        var b = await Ok<BudgetDto>(await c.PostAsJsonAsync("/api/budgets", new BudgetRequest("Home", "EUR"), Json.Options));
        var api = $"/api/budgets/{b.Id}";
        foreach (var name in new[] { "Bills", "Fuel", "Groceries" })
            (await c.PostAsJsonAsync($"{api}/categories", new CategoryRequest(name, null), Json.Options)).EnsureSuccessStatusCode();
        var fuel = (await Ok<List<CategoryDto>>(await c.GetAsync($"{api}/categories"))).Single(x => x.Name == "Fuel");
        var groceries = (await Ok<List<CategoryDto>>(await c.GetAsync($"{api}/categories"))).Single(x => x.Name == "Groceries");

        var starred = await Ok<CategoryDto>(await c.PutAsync($"{api}/categories/{groceries.Id}/star", null));
        Assert.True(starred.IsStarred);
        (await c.PutAsync($"{api}/categories/{groceries.Id}/star", null)).EnsureSuccessStatusCode();   // repeating is harmless
        (await c.PutAsync($"{api}/categories/{fuel.Id}/star", null)).EnsureSuccessStatusCode();

        var list = await Ok<List<CategoryDto>>(await c.GetAsync($"{api}/categories"));
        Assert.Equal(["Fuel", "Groceries", "Bills"], list.Select(x => x.Name));
        Assert.Equal([true, true, false], list.Select(x => x.IsStarred));

        var plan = await Ok<List<PlanDto>>(await c.GetAsync($"{api}/plans?month=2026-11-01"));
        Assert.Equal(["Fuel", "Groceries", "Bills"], plan.Select(x => x.CategoryName));

        // starred first even when another category is over its plan
        var bills = list.Single(x => x.Name == "Bills");
        (await c.PostAsJsonAsync($"{api}/expenses", new ExpenseRequest(bills.Id, new(2026, 11, 3), 50, null), Json.Options)).EnsureSuccessStatusCode();
        var summary = await Ok<MonthSummaryDto>(await c.GetAsync($"{api}/summary?month=2026-11-01"));
        Assert.Equal(["Fuel", "Groceries", "Bills"], summary.Categories.Select(x => x.Name));
        Assert.True(summary.Categories[0].IsStarred);

        var unstarred = await Ok<CategoryDto>(await c.DeleteAsync($"{api}/categories/{fuel.Id}/star"));
        Assert.False(unstarred.IsStarred);
        Assert.Equal(["Groceries", "Bills", "Fuel"], (await Ok<List<CategoryDto>>(await c.GetAsync($"{api}/categories"))).Select(x => x.Name));

        // the star goes with the category
        (await c.DeleteAsync($"{api}/categories/{groceries.Id}")).EnsureSuccessStatusCode();
        Assert.DoesNotContain(await Ok<List<CategoryDto>>(await c.GetAsync($"{api}/categories")), x => x.IsStarred);
    }

    [Fact]
    public async Task Stars_are_personal_and_viewers_can_star()
    {
        var owner = await SignInAsync("star-owner@test.dev");
        var viewer = await SignInAsync("star-viewer@test.dev");
        var b = await Ok<BudgetDto>(await owner.PostAsJsonAsync("/api/budgets", new BudgetRequest("Shared", "EUR"), Json.Options));
        var api = $"/api/budgets/{b.Id}";
        (await owner.PostAsJsonAsync($"{api}/shares", new ShareRequest("star-viewer@test.dev", ShareRole.Viewer), Json.Options)).EnsureSuccessStatusCode();
        var home = await Ok<CategoryDto>(await owner.PostAsJsonAsync($"{api}/categories", new CategoryRequest("Home", null), Json.Options));

        (await viewer.PutAsync($"{api}/categories/{home.Id}/star", null)).EnsureSuccessStatusCode();

        Assert.True((await Ok<List<CategoryDto>>(await viewer.GetAsync($"{api}/categories"))).Single().IsStarred);
        Assert.False((await Ok<List<CategoryDto>>(await owner.GetAsync($"{api}/categories"))).Single().IsStarred);
    }

    [Fact]
    public async Task Starring_a_category_of_another_budget_is_not_found()
    {
        var c = await SignInAsync("star-other@test.dev");
        var mine = await Ok<BudgetDto>(await c.PostAsJsonAsync("/api/budgets", new BudgetRequest("Mine", "EUR"), Json.Options));
        var other = await Ok<BudgetDto>(await c.PostAsJsonAsync("/api/budgets", new BudgetRequest("Other", "EUR"), Json.Options));
        var cat = await Ok<CategoryDto>(await c.PostAsJsonAsync($"/api/budgets/{other.Id}/categories", new CategoryRequest("Fun", null), Json.Options));

        var r = await c.PutAsync($"/api/budgets/{mine.Id}/categories/{cat.Id}/star", null);
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    private async Task<HttpClient> SignInAsync(string email)
    {
        var c = _f.CreateClient();
        (await c.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Passw0rd!x", "Starry"), Json.Options)).EnsureSuccessStatusCode();
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
