using Kopeck.Mobile.ViewModels;

namespace Kopeck.Mobile.Views;

public partial class VerifyPage : ContentPage
{
    public VerifyPage(VerifyViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
