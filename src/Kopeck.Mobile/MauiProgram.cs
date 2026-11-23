using Kopeck.Mobile.Services;
using Kopeck.Mobile.ViewModels;
using Kopeck.Mobile.Views;
using Microsoft.Extensions.Logging;

namespace Kopeck.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Infrastructure: one instance for the app's lifetime.
        builder.Services.AddSingleton<Session>();
        builder.Services.AddSingleton<AppState>();
        builder.Services.AddSingleton<ApiClient>();
        builder.Services.AddSingleton<INavigationService, ShellNavigationService>();
        builder.Services.AddSingleton<IDialogService, ShellDialogService>();
        builder.Services.AddSingleton<ViewModelContext>();

        // Screens: a new page + view model each time they are opened.
        AddScreen<LoginPage, LoginViewModel>(builder.Services);
        AddScreen<RegisterPage, RegisterViewModel>(builder.Services);
        AddScreen<VerifyPage, VerifyViewModel>(builder.Services);
        AddScreen<ResetPage, ResetViewModel>(builder.Services);
        AddScreen<BudgetsPage, BudgetsViewModel>(builder.Services);
        AddScreen<DashboardPage, DashboardViewModel>(builder.Services);
        AddScreen<ExpensesPage, ExpensesViewModel>(builder.Services);
        AddScreen<IncomePage, IncomeViewModel>(builder.Services);
        AddScreen<PlanPage, PlanViewModel>(builder.Services);
        AddScreen<CategoriesPage, CategoriesViewModel>(builder.Services);
        AddScreen<SharePage, ShareViewModel>(builder.Services);

        return builder.Build();
    }

    private static void AddScreen<TPage, TViewModel>(IServiceCollection services)
        where TPage : Page where TViewModel : class
    {
        services.AddTransient<TViewModel>();
        services.AddTransient<TPage>();
    }
}
