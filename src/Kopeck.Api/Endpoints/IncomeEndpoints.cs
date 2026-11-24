using Kopeck.Api.Http;
using Kopeck.Application.Incomes;
using Kopeck.Contracts;
using Kopeck.Domain.Budgets;

namespace Kopeck.Api.Endpoints;

/// <summary>Revenue streams and the dated income entries booked against them.</summary>
public sealed class IncomeEndpoints : IEndpointModule
{
    public void Map(IEndpointRouteBuilder app)
    {
        var read = app.MapBudgetGroup();
        var write = read.RequireAccess(AccessLevel.Editor);

        read.MapGet("/income-streams", async (Guid budgetId, IIncomeService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListStreamsAsync(budgetId, ct)));

        write.MapPost("/income-streams", async (Guid budgetId, IncomeStreamRequest r, IIncomeService svc, CancellationToken ct) =>
            (await svc.CreateStreamAsync(budgetId, r, ct)).ToCreated(s => $"/api/budgets/{budgetId}/income-streams/{s.Id}"));

        write.MapDelete("/income-streams/{streamId:guid}", async (Guid budgetId, Guid streamId, IIncomeService svc, CancellationToken ct) =>
            (await svc.DeleteStreamAsync(budgetId, streamId, ct)).ToHttp());

        read.MapGet("/incomes", async (Guid budgetId, DateOnly month, IIncomeService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(budgetId, month, ct)));

        write.MapPost("/incomes", async (Guid budgetId, IncomeRequest r, IIncomeService svc, CancellationToken ct) =>
            (await svc.CreateAsync(budgetId, r, ct)).ToCreated(i => $"/api/budgets/{budgetId}/incomes/{i.Id}"));

        write.MapDelete("/incomes/{incomeId:guid}", async (Guid budgetId, Guid incomeId, IIncomeService svc, CancellationToken ct) =>
            (await svc.DeleteAsync(budgetId, incomeId, ct)).ToHttp());
    }
}
