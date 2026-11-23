using Kopeck.Mobile.ViewModels;

namespace Kopeck.Mobile.Views;

public partial class BudgetsPage : ContentPage
{
    private readonly BudgetsViewModel _vm;

    public BudgetsPage(BudgetsViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.LoadCommand.Execute(null);
    }
}
