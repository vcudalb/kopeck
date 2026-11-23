using Kopeck.Mobile.ViewModels;

namespace Kopeck.Mobile.Views;

public partial class PlanPage : ContentPage
{
    private readonly PlanViewModel _vm;

    public PlanPage(PlanViewModel vm)
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
