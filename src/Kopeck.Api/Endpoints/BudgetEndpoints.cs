using System.Security.Claims;
using Kopeck.Api.Auth;
using Kopeck.Api.Data;
using Kopeck.Api.Services;
using Kopeck.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Kopeck.Api.Endpoints;

/// <summary>Budget CRUD, monthly summary and sharing. Per-budget features live in sibling files.</summary>
public static class BudgetEndpoints
{
    public static void MapBudgets(this IEndpointRouteBuilder app)
    {
        // ---- collection: list / create (no budget in the route yet) ----
        var root = app.MapGroup("/api/budgets").RequireAuthorization();

        root.MapGet("/", async (KopeckDb db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var me = user.UserId(); var email = user.UserEmail();
            var shares = await db.Shares.AsNoTracking().Where(s => s.Email == email).ToDictionaryAsync(s => s.BudgetId, s => s.Role, ct);
            var sharedIds = shares.Keys.ToList();
            var budgets = await db.Budgets.AsNoTracking().Include(b => b.Owner)
                .Where(b => b.OwnerId == me || sharedIds.Contains(b.Id)).OrderBy(b => b.Name).ToListAsync(ct);
            return budgets.Select(b => new BudgetDto(b.Id, b.Name, b.Currency,
                b.OwnerId == me ? BudgetRole.Owner : (BudgetRole)(int)shares[b.Id], b.Owner!.Email));
        });

        root.MapPost("/", async (BudgetRequest r, KopeckDb db, ClaimsPrincipal user) =>
        {
            if (!Rules.IsValidName(r.Name)) return Rules.Bad("Enter a budget name (max 80 characters).");
            var currency = (r.Currency ?? "").Trim().ToUpperInvariant();
            if (currency.Length is < 3 or > 5 || !currency.All(char.IsLetter)) return Rules.Bad("Enter a currency code such as EUR or MDL.");

            var budget = new BudgetEntity { Name = r.Name.Trim(), Currency = currency, OwnerId = user.UserId() };
            db.Budgets.Add(budget);
            await db.SaveChangesAsync();
            return Results.Created($"/api/budgets/{budget.Id}", new BudgetDto(budget.Id, budget.Name, budget.Currency, BudgetRole.Owner, user.UserEmail()));
        });

        // ---- one budget ----
        var budgetGroup = app.MapBudgetGroup();

        budgetGroup.MapGet("/summary", async (DateOnly month, HttpContext http, ISummaryService summaries, CancellationToken ct) =>
            Results.Ok(await summaries.BuildAsync(http.Access().Budget, month, ct)));

        // A shared user can remove themself from someone else's budget.
        budgetGroup.MapDelete("/leave", async (Guid budgetId, KopeckDb db, ClaimsPrincipal user) =>
            await db.Shares.Where(s => s.BudgetId == budgetId && s.Email == user.UserEmail()).ExecuteDeleteAsync() == 0
                ? Results.NotFound() : Results.NoContent());

        var owner = budgetGroup.RequireRole(BudgetRole.Owner);

        owner.MapPut("/", async (Guid budgetId, BudgetRequest r, KopeckDb db) =>
        {
            if (!Rules.IsValidName(r.Name)) return Rules.Bad("Enter a budget name (max 80 characters).");
            await db.Budgets.Where(b => b.Id == budgetId).ExecuteUpdateAsync(s => s.SetProperty(b => b.Name, r.Name.Trim()));
            return Results.NoContent();
        });

        // Children are removed by database cascade (see KopeckDb).
        owner.MapDelete("/", async (Guid budgetId, KopeckDb db) =>
        {
            await db.Budgets.Where(b => b.Id == budgetId).ExecuteDeleteAsync();
            return Results.NoContent();
        });

        // ---- sharing ----
        owner.MapGet("/shares", async (Guid budgetId, KopeckDb db, CancellationToken ct) =>
            Results.Ok(await db.Shares.AsNoTracking().Where(s => s.BudgetId == budgetId).OrderBy(s => s.Email)
                .Select(s => new ShareDto(s.Id, s.Email, s.Role)).ToListAsync(ct)));

        owner.MapPost("/shares", async (Guid budgetId, ShareRequest r, KopeckDb db, ClaimsPrincipal user) =>
        {
            var email = AuthEndpoints.NormalizeEmail(r.Email);
            if (email is null) return Rules.Bad("Enter a valid email address.");
            if (email == user.UserEmail()) return Rules.Bad("You already own this budget.");
            if (!Enum.IsDefined(r.Role)) return Rules.Bad("Unknown access level.");

            var share = await db.Shares.FirstOrDefaultAsync(s => s.BudgetId == budgetId && s.Email == email);
            if (share is null) db.Shares.Add(share = new BudgetShare { BudgetId = budgetId, Email = email });
            share.Role = r.Role;
            await db.SaveChangesAsync();
            return Results.Ok(new ShareDto(share.Id, share.Email, share.Role));
        });

        owner.MapDelete("/shares/{shareId:guid}", async (Guid budgetId, Guid shareId, KopeckDb db) =>
            await db.Shares.Where(s => s.Id == shareId && s.BudgetId == budgetId).ExecuteDeleteAsync() == 0
                ? Results.NotFound() : Results.NoContent());
    }
}
