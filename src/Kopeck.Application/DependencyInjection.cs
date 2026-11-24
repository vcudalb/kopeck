using Kopeck.Application.Auth;
using Kopeck.Application.Budgets;
using Kopeck.Application.Categories;
using Kopeck.Application.Expenses;
using Kopeck.Application.Incomes;
using Kopeck.Application.Planning;
using Microsoft.Extensions.DependencyInjection;

namespace Kopeck.Application;

public static class DependencyInjection
{
    /// <summary>Registers the use cases. Infrastructure must provide the abstractions in <c>Kopeck.Application.Abstractions</c>.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IBudgetAccessResolver, BudgetAccessResolver>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<ISharingService, SharingService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<IIncomeService, IncomeService>();
        services.AddScoped<IPlanService, PlanService>();
        services.AddScoped<ISummaryService, SummaryService>();
        return services;
    }
}
