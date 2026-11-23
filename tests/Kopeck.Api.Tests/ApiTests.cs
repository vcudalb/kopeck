using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Kopeck.Api.Auth;
using Kopeck.Api.Data;
using Kopeck.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kopeck.Api.Tests;

public class CapturingEmail : IEmailSender
{
    public readonly List<(string To, string Body)> Sent = [];
    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    { lock (Sent) Sent.Add((to, body)); return Task.CompletedTask; }
    public string LastCode(string to) { lock (Sent) return Regex.Match(Sent.Last(m => m.To == to).Body, @"\d{6}").Value; }
}

public class ApiFactory : WebApplicationFactory<Program>
{
    public CapturingEmail Mail { get; } = new();
    private readonly SqliteConnection _conn = new("DataSource=:memory:");

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder b)
    {
        _conn.Open();
        b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?> { ["RateLimit:AuthPerMinute"] = "10000" }));
        b.ConfigureServices(s =>
        {
            s.RemoveAll<DbContextOptions<KopeckDb>>();
            s.AddDbContext<KopeckDb>(o => o.UseSqlite(_conn));
            s.RemoveAll<IEmailSender>();
            s.AddSingleton<IEmailSender>(Mail);
        });
    }
    protected override void Dispose(bool disposing) { base.Dispose(disposing); _conn.Dispose(); }
}

public class ApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _f;
    public ApiTests(ApiFactory f) => _f = f;

    private HttpClient Client() => _f.CreateClient();

    private async Task<HttpClient> SignUp(string email, string name = "Tester")
    {
        var c = Client();
        var reg = await c.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Passw0rd!x", name), Json.Options);
        Assert.Equal(HttpStatusCode.Accepted, reg.StatusCode);
        var ver = await c.PostAsJsonAsync("/api/auth/verify-email", new EmailCodeRequest(email, _f.Mail.LastCode(email)), Json.Options);
        ver.EnsureSuccessStatusCode();
        var auth = (await ver.Content.ReadFromJsonAsync<AuthResponse>(Json.Options))!;
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return c;
    }

    private static async Task<T> Ok<T>(HttpResponseMessage r)
    {
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        return (await r.Content.ReadFromJsonAsync<T>(Json.Options))!;
    }

    [Fact]
    public async Task Login_requires_verified_email_and_correct_password()
    {
        var c = Client();
        await c.PostAsJsonAsync("/api/auth/register", new RegisterRequest("a@test.dev", "Passw0rd!x", "A"), Json.Options);

        var unverified = await c.PostAsJsonAsync("/api/auth/login", new LoginRequest("a@test.dev", "Passw0rd!x"), Json.Options);
        Assert.Equal(HttpStatusCode.Forbidden, unverified.StatusCode);

        var wrongCode = await c.PostAsJsonAsync("/api/auth/verify-email", new EmailCodeRequest("a@test.dev", "000000"), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, wrongCode.StatusCode);

        (await c.PostAsJsonAsync("/api/auth/verify-email", new EmailCodeRequest("a@test.dev", _f.Mail.LastCode("a@test.dev")), Json.Options)).EnsureSuccessStatusCode();
        (await c.PostAsJsonAsync("/api/auth/login", new LoginRequest("A@Test.dev", "Passw0rd!x"), Json.Options)).EnsureSuccessStatusCode();
        var bad = await c.PostAsJsonAsync("/api/auth/login", new LoginRequest("a@test.dev", "nope-nope1"), Json.Options);
        Assert.Equal(HttpStatusCode.Unauthorized, bad.StatusCode);
    }

    [Fact]
    public async Task Weak_password_and_bad_email_are_rejected()
    {
        var c = Client();
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/auth/register", new RegisterRequest("x@test.dev", "short", "X"), Json.Options)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/auth/register", new RegisterRequest("not-an-email", "Passw0rd!x", "X"), Json.Options)).StatusCode);
    }

    [Fact]
    public async Task Password_reset_flow_works()
    {
        var c = await SignUp("reset@test.dev");
        await c.PostAsJsonAsync("/api/auth/forgot-password", new EmailRequest("reset@test.dev"), Json.Options);
        var r = await c.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequest("reset@test.dev", _f.Mail.LastCode("reset@test.dev"), "NewPassw0rd!"), Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);
        (await Client().PostAsJsonAsync("/api/auth/login", new LoginRequest("reset@test.dev", "NewPassw0rd!"), Json.Options)).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Endpoints_require_authentication()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Client().GetAsync("/api/budgets")).StatusCode);
    }

    [Fact]
    public async Task Planned_vs_real_reports_left_and_overspend_per_category_with_multiple_incomes()
    {
        var c = await SignUp("owner@test.dev", "Owner");
        var b = await Ok<BudgetDto>(await c.PostAsJsonAsync("/api/budgets", new BudgetRequest("Home", "MDL"), Json.Options));
        var food = await Ok<CategoryDto>(await c.PostAsJsonAsync($"/api/budgets/{b.Id}/categories", new CategoryRequest("Food", null), Json.Options));
        var bills = await Ok<CategoryDto>(await c.PostAsJsonAsync($"/api/budgets/{b.Id}/categories", new CategoryRequest("Bills", "#112233"), Json.Options));
        var fun = await Ok<CategoryDto>(await c.PostAsJsonAsync($"/api/budgets/{b.Id}/categories", new CategoryRequest("Fun", null), Json.Options));

        // duplicate category names (case-insensitive) are refused
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/budgets/{b.Id}/categories", new CategoryRequest("food", null), Json.Options)).StatusCode);

        // two revenue streams
        var s1 = await Ok<IncomeStreamDto>(await c.PostAsJsonAsync($"/api/budgets/{b.Id}/income-streams", new IncomeStreamRequest("Main Salary"), Json.Options));
        var s2 = await Ok<IncomeStreamDto>(await c.PostAsJsonAsync($"/api/budgets/{b.Id}/income-streams", new IncomeStreamRequest("Second Salary"), Json.Options));
        (await c.PostAsJsonAsync($"/api/budgets/{b.Id}/incomes", new IncomeRequest(s1.Id, new(2026, 10, 5), 90000, null), Json.Options)).EnsureSuccessStatusCode();
        (await c.PostAsJsonAsync($"/api/budgets/{b.Id}/incomes", new IncomeRequest(s2.Id, new(2026, 10, 7), 60000, null), Json.Options)).EnsureSuccessStatusCode();
        (await c.PostAsJsonAsync($"/api/budgets/{b.Id}/incomes", new IncomeRequest(s1.Id, new(2026, 11, 5), 1, null), Json.Options)).EnsureSuccessStatusCode(); // other month

        // plan October
        foreach (var (cat, amt) in new[] { (food.Id, 3000m), (bills.Id, 4000m) })
            Assert.Equal(HttpStatusCode.NoContent, (await c.PutAsJsonAsync($"/api/budgets/{b.Id}/plans", new PlanRequest(cat, new(2026, 10, 17), amt), Json.Options)).StatusCode);

        // real expenses: Food over plan by 500, Bills under, Fun unplanned
        async Task Spend(Guid cat, decimal amt, int day) =>
            (await c.PostAsJsonAsync($"/api/budgets/{b.Id}/expenses", new ExpenseRequest(cat, new(2026, 10, day), amt, "x"), Json.Options)).EnsureSuccessStatusCode();
        await Spend(food.Id, 2000, 2); await Spend(food.Id, 1500, 9);
        await Spend(bills.Id, 1000, 3);
        await Spend(fun.Id, 250, 4);

        var sum = await Ok<MonthSummaryDto>(await c.GetAsync($"/api/budgets/{b.Id}/summary?month=2026-10-01"));
        Assert.Equal(150000, sum.TotalIncome);
        Assert.Equal(7000, sum.TotalPlanned);
        Assert.Equal(4750, sum.TotalSpent);
        Assert.Equal(750, sum.TotalOverspend);              // 500 over on Food + 250 unplanned Fun
        Assert.Equal(143000, sum.Unallocated);
        Assert.Equal(145250, sum.NetLeft);
        Assert.Equal(2, sum.IncomeStreams.Count);

        var f = sum.Categories.Single(x => x.Name == "Food");
        Assert.Equal((3000m, 3500m, -500m, 500m, CategoryStatus.Over), (f.Planned, f.Spent, f.Left, f.OverBy, f.Status));
        var bl = sum.Categories.Single(x => x.Name == "Bills");
        Assert.Equal((3000m, 0m, CategoryStatus.OnTrack), (bl.Left, bl.OverBy, bl.Status));
        var fn = sum.Categories.Single(x => x.Name == "Fun");
        Assert.Equal((CategoryStatus.Unplanned, 250m), (fn.Status, fn.OverBy));

        // copy plan to next month; doesn't overwrite
        var copy = await c.PostAsJsonAsync($"/api/budgets/{b.Id}/plans/copy", new CopyPlanRequest(new(2026, 10, 1), new(2026, 11, 1)), Json.Options);
        copy.EnsureSuccessStatusCode();
        var nov = await Ok<MonthSummaryDto>(await c.GetAsync($"/api/budgets/{b.Id}/summary?month=2026-11-20"));
        Assert.Equal(7000, nov.TotalPlanned);
        Assert.Equal(0, nov.TotalSpent);

        // category with expenses can't be deleted
        Assert.Equal(HttpStatusCode.Conflict, (await c.DeleteAsync($"/api/budgets/{b.Id}/categories/{food.Id}")).StatusCode);
    }

    [Fact]
    public async Task Sharing_grants_view_or_edit_access_and_blocks_strangers()
    {
        var owner = await SignUp("o2@test.dev", "Owner Two");
        var b = await Ok<BudgetDto>(await owner.PostAsJsonAsync("/api/budgets", new BudgetRequest("Shared", "EUR"), Json.Options));
        var cat = await Ok<CategoryDto>(await owner.PostAsJsonAsync($"/api/budgets/{b.Id}/categories", new CategoryRequest("Food", null), Json.Options));

        // share with someone who hasn't registered yet — works once they sign up with that verified email
        (await owner.PostAsJsonAsync($"/api/budgets/{b.Id}/shares", new ShareRequest("Viewer@Test.dev", ShareRole.Viewer), Json.Options)).EnsureSuccessStatusCode();
        var viewer = await SignUp("viewer@test.dev", "Viewer");
        var stranger = await SignUp("stranger@test.dev", "Nobody");

        var list = await Ok<List<BudgetDto>>(await viewer.GetAsync("/api/budgets"));
        Assert.Equal(BudgetRole.Viewer, list.Single(x => x.Id == b.Id).Role);
        Assert.Empty(await Ok<List<BudgetDto>>(await stranger.GetAsync("/api/budgets")));

        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync($"/api/budgets/{b.Id}/summary?month=2026-10-01")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.PostAsJsonAsync($"/api/budgets/{b.Id}/expenses", new ExpenseRequest(cat.Id, new(2026, 10, 1), 5, null), Json.Options)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.GetAsync($"/api/budgets/{b.Id}/shares")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/budgets/{b.Id}/summary?month=2026-10-01")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.DeleteAsync($"/api/budgets/{b.Id}")).StatusCode);

        // upgrade to editor
        (await owner.PostAsJsonAsync($"/api/budgets/{b.Id}/shares", new ShareRequest("viewer@test.dev", ShareRole.Editor), Json.Options)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, (await viewer.PostAsJsonAsync($"/api/budgets/{b.Id}/expenses", new ExpenseRequest(cat.Id, new(2026, 10, 1), 5, null), Json.Options)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.DeleteAsync($"/api/budgets/{b.Id}")).StatusCode);

        // revoke
        var shares = await Ok<List<ShareDto>>(await owner.GetAsync($"/api/budgets/{b.Id}/shares"));
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/budgets/{b.Id}/shares/{shares.Single().Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await viewer.GetAsync($"/api/budgets/{b.Id}/summary?month=2026-10-01")).StatusCode);
    }

    [Fact]
    public async Task Repeated_wrong_passwords_lock_the_account_even_for_the_right_password()
    {
        await SignUp("lock@test.dev");
        var c = Client();
        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsJsonAsync("/api/auth/login", new LoginRequest("lock@test.dev", "wrong-pass1"), Json.Options)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsJsonAsync("/api/auth/login", new LoginRequest("lock@test.dev", "Passw0rd!x"), Json.Options)).StatusCode);
    }

    [Fact]
    public async Task Deleting_a_budget_removes_everything_under_it_and_only_the_owner_can()
    {
        var owner = await SignUp("del@test.dev");
        var b = await Ok<BudgetDto>(await owner.PostAsJsonAsync("/api/budgets", new BudgetRequest("Temp", "EUR"), Json.Options));
        var cat = await Ok<CategoryDto>(await owner.PostAsJsonAsync($"/api/budgets/{b.Id}/categories", new CategoryRequest("Food", null), Json.Options));
        var stream = await Ok<IncomeStreamDto>(await owner.PostAsJsonAsync($"/api/budgets/{b.Id}/income-streams", new IncomeStreamRequest("Job"), Json.Options));
        (await owner.PostAsJsonAsync($"/api/budgets/{b.Id}/expenses", new ExpenseRequest(cat.Id, new(2026, 10, 1), 5, null), Json.Options)).EnsureSuccessStatusCode();
        (await owner.PostAsJsonAsync($"/api/budgets/{b.Id}/incomes", new IncomeRequest(stream.Id, new(2026, 10, 1), 5, null), Json.Options)).EnsureSuccessStatusCode();
        await owner.PutAsJsonAsync($"/api/budgets/{b.Id}/plans", new PlanRequest(cat.Id, new(2026, 10, 1), 10), Json.Options);
        await owner.PostAsJsonAsync($"/api/budgets/{b.Id}/shares", new ShareRequest("friend@test.dev", ShareRole.Editor), Json.Options);

        var friend = await SignUp("friend@test.dev");
        Assert.Equal(HttpStatusCode.Forbidden, (await friend.DeleteAsync($"/api/budgets/{b.Id}")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/budgets/{b.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/api/budgets/{b.Id}/summary?month=2026-10-01")).StatusCode);
        using var scope = _f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KopeckDb>();
        Assert.False(await db.Expenses.AnyAsync(e => e.BudgetId == b.Id));
        Assert.False(await db.Incomes.AnyAsync(e => e.BudgetId == b.Id));
        Assert.False(await db.Plans.AnyAsync(e => e.BudgetId == b.Id));
        Assert.False(await db.Shares.AnyAsync(e => e.BudgetId == b.Id));
        Assert.Empty(await Ok<List<BudgetDto>>(await friend.GetAsync("/api/budgets")));
    }

    [Fact]
    public async Task Amounts_with_more_than_two_decimals_are_rejected()
    {
        var c = await SignUp("dec@test.dev");
        var b = await Ok<BudgetDto>(await c.PostAsJsonAsync("/api/budgets", new BudgetRequest("D", "EUR"), Json.Options));
        var cat = await Ok<CategoryDto>(await c.PostAsJsonAsync($"/api/budgets/{b.Id}/categories", new CategoryRequest("X", null), Json.Options));
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync($"/api/budgets/{b.Id}/expenses", new ExpenseRequest(cat.Id, new(2026, 10, 1), 1.234m, null), Json.Options)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await c.PostAsJsonAsync($"/api/budgets/{b.Id}/expenses", new ExpenseRequest(cat.Id, new(2026, 10, 1), 1.23m, null), Json.Options)).StatusCode);
    }
}
