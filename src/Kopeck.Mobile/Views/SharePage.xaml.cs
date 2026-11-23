using Kopeck.Mobile.ViewModels;

namespace Kopeck.Mobile.Views;

public partial class SharePage : ContentPage
{
    private readonly ShareViewModel _vm;

    public SharePage(ShareViewModel vm)
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
