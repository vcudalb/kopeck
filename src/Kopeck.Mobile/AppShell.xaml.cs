using Kopeck.Mobile.Views;

namespace Kopeck.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        // Pages pushed on top of a Shell item (the tab pages have their routes in AppShell.xaml).
        Routing.RegisterRoute("register", typeof(RegisterPage));
        Routing.RegisterRoute("verify", typeof(VerifyPage));
        Routing.RegisterRoute("reset", typeof(ResetPage));
        Routing.RegisterRoute("categories", typeof(CategoriesPage));
        Routing.RegisterRoute("share", typeof(SharePage));
    }
}
