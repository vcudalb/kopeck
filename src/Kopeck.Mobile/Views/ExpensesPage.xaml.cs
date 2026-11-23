using Kopeck.Mobile.ViewModels;

namespace Kopeck.Mobile.Views;

public partial class ExpensesPage : ContentPage
{
    private readonly ExpensesViewModel _vm;

    public ExpensesPage(ExpensesViewModel vm)
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
