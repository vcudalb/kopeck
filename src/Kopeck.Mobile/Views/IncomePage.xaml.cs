using Kopeck.Mobile.ViewModels;

namespace Kopeck.Mobile.Views;

public partial class IncomePage : ContentPage
{
    private readonly IncomeViewModel _vm;

    public IncomePage(IncomeViewModel vm)
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
