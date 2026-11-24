using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kopeck.Contracts;
using Kopeck.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Kopeck.Mobile.ViewModels;

public partial class RegisterViewModel(IAuthApi auth, AppState state, INavigationService nav, ILogger<RegisterViewModel> logger)
    : BaseViewModel(logger)
{
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _displayName = "";
    [ObservableProperty] private string _password = "";

    [RelayCommand]
    private Task CreateAccountAsync() => RunAsync(async ct =>
    {
        await auth.RegisterAsync(new RegisterRequest(Email, Password, DisplayName), ct);
        state.PendingEmail = Email;
        Password = "";
        await nav.GoToAsync("verify");
    });
}
