using Kopeck.Api.Filters;
using Kopeck.Application.Expenses;
using Kopeck.Contracts;
using Kopeck.Domain.Budgets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Kopeck.Api.Controllers;

[Authorize]
[Route("api/budgets/{budgetId:guid}/expenses")]
[BudgetAccess]
public sealed class ExpensesController(IExpenseService expenses) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid budgetId, [FromQuery, BindRequired] DateOnly month, [FromQuery] Guid? categoryId, CancellationToken ct) =>
        Ok(await expenses.ListAsync(budgetId, month, categoryId, ct));

    [HttpPost]
    [BudgetAccess(AccessLevel.Editor)]
    [ServiceFilter(typeof(RequestValidationFilter<ExpenseRequest>))]
    public async Task<IActionResult> Create(Guid budgetId, ExpenseRequest request, CancellationToken ct) =>
        CreatedFromResult(await expenses.CreateAsync(budgetId, request, ct), e => $"/api/budgets/{budgetId}/expenses/{e.Id}");

    [HttpPut("{expenseId:guid}")]
    [BudgetAccess(AccessLevel.Editor)]
    [ServiceFilter(typeof(RequestValidationFilter<ExpenseRequest>))]
    public async Task<IActionResult> Update(Guid budgetId, Guid expenseId, ExpenseRequest request, CancellationToken ct) =>
        FromResult(await expenses.UpdateAsync(budgetId, expenseId, request, ct));

    /// <summary>Who added, changed or deleted the expense, when, and the values involved (newest first). Viewers can read it.</summary>
    [HttpGet("{expenseId:guid}/history")]
    public async Task<IActionResult> History(Guid budgetId, Guid expenseId, CancellationToken ct) =>
        FromResult(await expenses.HistoryAsync(budgetId, expenseId, ct));

    [HttpDelete("{expenseId:guid}")]
    [BudgetAccess(AccessLevel.Editor)]
    public async Task<IActionResult> Delete(Guid budgetId, Guid expenseId, CancellationToken ct) =>
        FromResult(await expenses.DeleteAsync(budgetId, expenseId, ct));
}
