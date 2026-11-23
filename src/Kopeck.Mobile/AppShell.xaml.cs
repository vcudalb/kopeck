using Kopeck.Mobile.Views;

namespace Kopeck.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("register", typeof(RegisterPage));
        Routing.RegisterRoute("verify", typeof(VerifyPage));
        Routing.RegisterRoute("reset", typeof(ResetPage));
        Routing.RegisterRoute("dashboard", typeof(DashboardPage));
        Routing.RegisterRoute("expenses", typeof(ExpensesPage));
        Routing.RegisterRoute("income", typeof(IncomePage));
        Routing.RegisterRoute("plan", typeof(PlanPage));
        Routing.RegisterRoute("categories", typeof(CategoriesPage));
        Routing.RegisterRoute("share", typeof(SharePage));
    }
}
