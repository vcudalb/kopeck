using Kopeck.Mobile.ViewModels;

namespace Kopeck.Mobile.Views;

/// <summary>
/// Binds a page to its view model and forwards the lifecycle: appearing loads data, disappearing cancels the request
/// in flight. Every page derives from this instead of repeating the same code-behind.
/// </summary>
public class BasePage : ContentPage
{
    private readonly BaseViewModel _viewModel;

    protected BasePage(BaseViewModel viewModel) => BindingContext = _viewModel = viewModel;

    // async void is acceptable for a lifecycle override; OnAppearingAsync goes through RunAsync, which never throws.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
    }

    protected override void OnDisappearing()
    {
        _viewModel.OnDisappearing();
        base.OnDisappearing();
    }
}
