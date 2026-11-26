using Kopeck.Application.Auth;
using Kopeck.Application.Budgets;
using Kopeck.Application.Categories;
using Kopeck.Application.Expenses;
using Kopeck.Application.Incomes;
using Kopeck.Application.Planning;
using Kopeck.Application.Savings;
using Kopeck.Application.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Kopeck.Application;

public static class DependencyInjection
{
    /// <summary>Registers the use cases. Infrastructure must provide the abstractions in <c>Kopeck.Application.Abstractions</c>.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<History.EntryAudit>();
        services.AddScoped<IBudgetAccessResolver, BudgetAccessResolver>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<ISharingService, SharingService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<IIncomeService, IncomeService>();
        services.AddScoped<IPlanService, PlanService>();
        services.AddScoped<ISavingsService, SavingsService>();
        services.AddScoped<ISummaryService, SummaryService>();

        services.AddScoped<IRequestPipelineExecutor, RequestPipelineExecutor>();
        services.AddRequestValidators();
        return services;
    }

    /// <summary>Registers every <see cref="IRequestValidator{TRequest}"/> in this assembly, so a new validator needs no wiring.</summary>
    private static void AddRequestValidators(this IServiceCollection services)
    {
        var types = typeof(DependencyInjection).Assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false });
        foreach (var type in types)
        {
            var contracts = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestValidator<>));
            foreach (var contract in contracts) services.AddScoped(contract, type);
        }
    }
}
