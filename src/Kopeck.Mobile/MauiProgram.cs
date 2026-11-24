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

        var services = builder.Services;

        // Platform services, injected through their interfaces (no static .Default calls in app code).
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(Preferences.Default);
        services.AddSingleton(SecureStorage.Default);
        services.AddSingleton(DeviceInfo.Current);

        // Infrastructure: one instance for the app's lifetime.
        services.AddSingleton(new HttpClient { Timeout = TimeSpan.FromSeconds(20) });
        services.AddSingleton<IServerSettings, ServerSettings>();
        services.AddSingleton<IUserSession, UserSession>();
        services.AddSingleton<IApiClient, ApiClient>();
        services.AddSingleton<AppState>();
        services.AddSingleton<INavigationService, ShellNavigationService>();
        services.AddSingleton<IDialogService, ShellDialogService>();

        // Typed API gateways, one per feature.
        services.AddSingleton<IAuthApi, AuthApi>();
        services.AddSingleton<IBudgetsApi, BudgetsApi>();
        services.AddSingleton<ICategoriesApi, CategoriesApi>();
        services.AddSingleton<IExpensesApi, ExpensesApi>();
        services.AddSingleton<IIncomeApi, IncomeApi>();
        services.AddSingleton<IPlansApi, PlansApi>();
        services.AddSingleton<ISharesApi, SharesApi>();

        // Screens: a new page + view model each time they are opened.
        AddScreen<LoginPage, LoginViewModel>(services);
        AddScreen<RegisterPage, RegisterViewModel>(services);
        AddScreen<VerifyPage, VerifyViewModel>(services);
        AddScreen<ResetPage, ResetViewModel>(services);
        AddScreen<BudgetsPage, BudgetsViewModel>(services);
        AddScreen<DashboardPage, DashboardViewModel>(services);
        AddScreen<ExpensesPage, ExpensesViewModel>(services);
        AddScreen<IncomePage, IncomeViewModel>(services);
        AddScreen<PlanPage, PlanViewModel>(services);
        AddScreen<CategoriesPage, CategoriesViewModel>(services);
        AddScreen<SharePage, ShareViewModel>(services);
        AddScreen<MorePage, MoreViewModel>(services);

        RemoveNativeInputUnderline();
        return builder.Build();
    }

    /// <summary>
    /// Inputs sit inside a rounded "Field" border (see Styles.xaml), so Android's own underline would draw a second line.
    /// </summary>
    private static void RemoveNativeInputUnderline()
    {
#if ANDROID
        static void Clear(Android.Views.View view) =>
            view.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);

        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, _) => Clear(handler.PlatformView));
        Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("NoUnderline", (handler, _) => Clear(handler.PlatformView));
        Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping("NoUnderline", (handler, _) => Clear(handler.PlatformView));
#endif
    }

    private static void AddScreen<TPage, TViewModel>(IServiceCollection services)
        where TPage : Page where TViewModel : BaseViewModel
    {
        services.AddTransient<TViewModel>();
        services.AddTransient<TPage>();
    }
}
