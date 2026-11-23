using Kopeck.Mobile.ViewModels;

namespace Kopeck.Mobile.Views;

public partial class ResetPage : ContentPage
{
    public ResetPage(ResetViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
