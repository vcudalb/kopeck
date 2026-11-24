using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kopeck.Contracts;
using Kopeck.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Kopeck.Mobile.ViewModels;

public partial class LoginViewModel(
    IAuthApi auth, IUserSession session, IServerSettings server, AppState state, INavigationService nav, ILogger<LoginViewModel> logger)
    : BaseViewModel(logger)
{
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string _serverUrl = server.BaseUrl;
    [ObservableProperty] private bool _showServer;

    /// <summary>The server address is a developer setting, so it stays folded away until asked for.</summary>
    [RelayCommand] private void ToggleServer() => ShowServer = !ShowServer;

    [RelayCommand]
    private Task SignInAsync() => RunAsync(async ct =>
    {
        if (!server.TrySetBaseUrl(ServerUrl, out var urlError)) { Error = urlError; return; }
        try
        {
            await session.SignInAsync(await auth.LoginAsync(new LoginRequest(Email, Password), ct));
            Password = "";
            await nav.GoToAsync("//budgets");
        }
        catch (ApiException ex) when (ex.Status == HttpStatusCode.Forbidden)
        {
            // The account exists but its email isn't verified yet: send a fresh code and continue there.
            state.PendingEmail = Email;
            await auth.ResendCodeAsync(Email, ct);
            await nav.GoToAsync("verify");
        }
    });

    [RelayCommand] private Task RegisterAsync() => nav.GoToAsync("register");
    [RelayCommand] private Task ForgotPasswordAsync() => nav.GoToAsync("reset");
}
