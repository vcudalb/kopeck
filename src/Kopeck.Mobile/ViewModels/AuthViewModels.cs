using System.Net;
using Kopeck.Contracts;
using Kopeck.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Kopeck.Mobile.ViewModels;

public partial class LoginViewModel(ViewModelContext context, Session session) : BaseViewModel(context)
{
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string _serverUrl = ApiClient.BaseUrl;

    [RelayCommand]
    private Task SignInAsync() => RunAsync(async () =>
    {
        if (!ApiClient.TrySetBaseUrl(ServerUrl, out var urlError)) { Error = urlError; return; }
        try
        {
            var auth = await Api.PostAsync<AuthResponse>("/api/auth/login", new LoginRequest(Email, Password));
            await session.SignInAsync(auth);
            Password = "";
            await Nav.GoToAsync("//budgets");
        }
        catch (ApiException ex) when (ex.Status == HttpStatusCode.Forbidden)
        {
            // The account exists but its email isn't verified yet: send a fresh code and continue there.
            State.PendingEmail = Email;
            await Api.PostAsync("/api/auth/resend-code", new EmailRequest(Email));
            await Nav.GoToAsync("verify");
        }
    });

    [RelayCommand] private Task RegisterAsync() => Nav.GoToAsync("register");
    [RelayCommand] private Task ForgotPasswordAsync() => Nav.GoToAsync("reset");
}

public partial class RegisterViewModel(ViewModelContext context) : BaseViewModel(context)
{
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _displayName = "";
    [ObservableProperty] private string _password = "";

    [RelayCommand]
    private Task CreateAccountAsync() => RunAsync(async () =>
    {
        await Api.PostAsync("/api/auth/register", new RegisterRequest(Email, Password, DisplayName));
        State.PendingEmail = Email;
        Password = "";
        await Nav.GoToAsync("verify");
    });
}

public partial class VerifyViewModel(ViewModelContext context, Session session) : BaseViewModel(context)
{
    [ObservableProperty] private string _code = "";
    [ObservableProperty] private string _info = "";

    public string Email => State.PendingEmail;

    [RelayCommand]
    private Task VerifyAsync() => RunAsync(async () =>
    {
        var auth = await Api.PostAsync<AuthResponse>("/api/auth/verify-email", new EmailCodeRequest(Email, Code));
        await session.SignInAsync(auth);
        Code = "";
        await Nav.GoToAsync("//budgets");
    });

    [RelayCommand]
    private Task ResendAsync() => RunAsync(async () =>
    {
        await Api.PostAsync("/api/auth/resend-code", new EmailRequest(Email));
        Info = "A new code was sent.";
    });
}

public partial class ResetViewModel(ViewModelContext context) : BaseViewModel(context)
{
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _code = "";
    [ObservableProperty] private string _newPassword = "";
    [ObservableProperty] private string _info = "";

    [RelayCommand]
    private Task SendCodeAsync() => RunAsync(async () =>
    {
        await Api.PostAsync("/api/auth/forgot-password", new EmailRequest(Email));
        Info = "If that email has an account, a code was sent.";
    });

    [RelayCommand]
    private Task ResetAsync() => RunAsync(async () =>
    {
        await Api.PostAsync("/api/auth/reset-password", new ResetPasswordRequest(Email, Code, NewPassword));
        (NewPassword, Code) = ("", "");
        await Dialogs.AlertAsync("Password changed", "You can now sign in.");
        await Nav.BackAsync();
    });
}
