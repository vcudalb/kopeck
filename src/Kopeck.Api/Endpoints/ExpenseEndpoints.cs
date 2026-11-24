using Kopeck.Api.Http;
using Kopeck.Application.Expenses;
using Kopeck.Contracts;
using Kopeck.Domain.Budgets;

namespace Kopeck.Api.Endpoints;

public sealed class ExpenseEndpoints : IEndpointModule
{
    public void Map(IEndpointRouteBuilder app)
    {
        var budget = app.MapBudgetGroup();
        var read = budget.MapGroup("/expenses");
        var write = budget.RequireAccess(AccessLevel.Editor).MapGroup("/expenses");

        read.MapGet("/", async (Guid budgetId, DateOnly month, Guid? categoryId, IExpenseService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(budgetId, month, categoryId, ct)));

        write.MapPost("/", async (Guid budgetId, ExpenseRequest r, IExpenseService svc, CancellationToken ct) =>
            (await svc.CreateAsync(budgetId, r, ct)).ToCreated(e => $"/api/budgets/{budgetId}/expenses/{e.Id}"));

        write.MapPut("/{expenseId:guid}", async (Guid budgetId, Guid expenseId, ExpenseRequest r, IExpenseService svc, CancellationToken ct) =>
            (await svc.UpdateAsync(budgetId, expenseId, r, ct)).ToHttp());

        write.MapDelete("/{expenseId:guid}", async (Guid budgetId, Guid expenseId, IExpenseService svc, CancellationToken ct) =>
            (await svc.DeleteAsync(budgetId, expenseId, ct)).ToHttp());
    }
}
