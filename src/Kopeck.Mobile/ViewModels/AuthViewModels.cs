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

public partial class VerifyViewModel(IAuthApi auth, IUserSession session, AppState state, INavigationService nav, ILogger<VerifyViewModel> logger)
    : BaseViewModel(logger)
{
    [ObservableProperty] private string _code = "";
    [ObservableProperty] private string _info = "";

    public string Email => state.PendingEmail;

    [RelayCommand]
    private Task VerifyAsync() => RunAsync(async ct =>
    {
        await session.SignInAsync(await auth.VerifyEmailAsync(new EmailCodeRequest(Email, Code), ct));
        Code = "";
        await nav.GoToAsync("//budgets");
    });

    [RelayCommand]
    private Task ResendAsync() => RunAsync(async ct =>
    {
        await auth.ResendCodeAsync(Email, ct);
        Info = "A new code was sent.";
    });
}

public partial class ResetViewModel(IAuthApi auth, IDialogService dialogs, INavigationService nav, ILogger<ResetViewModel> logger)
    : BaseViewModel(logger)
{
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _code = "";
    [ObservableProperty] private string _newPassword = "";
    [ObservableProperty] private string _info = "";

    [RelayCommand]
    private Task SendCodeAsync() => RunAsync(async ct =>
    {
        await auth.ForgotPasswordAsync(Email, ct);
        Info = "If that email has an account, a code was sent.";
    });

    [RelayCommand]
    private Task ResetAsync() => RunAsync(async ct =>
    {
        await auth.ResetPasswordAsync(new ResetPasswordRequest(Email, Code, NewPassword), ct);
        (NewPassword, Code) = ("", "");
        await dialogs.AlertAsync("Password changed", "You can now sign in.");
        await nav.BackAsync();
    });
}
