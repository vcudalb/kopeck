using CommunityToolkit.Mvvm.Input;
using Kopeck.Contracts;
using Kopeck.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Kopeck.Mobile.ViewModels;

/// <summary>The "More" tab: budget settings (monthly plan, categories, sharing), switching budget, leaving/deleting it, and the account.</summary>
public partial class MoreViewModel(
    IBudgetsApi budgets, IUserSession session, AppState state, INavigationService nav, IDialogService dialogs, ILogger<MoreViewModel> logger)
    : BudgetViewModel(state, dialogs, logger)
{
    public string BudgetInitial => Initial(BudgetName);

    public string BudgetDetails => State.Budget is not { } b ? ""
        : b.Role == BudgetRole.Owner ? $"{b.Currency} · Owner" : $"{b.Currency} · {b.Role} · shared by {b.OwnerEmail}";

    public string UserName => session.DisplayName;
    public string UserEmail => session.Email;
    public string UserInitial => Initial(session.DisplayName);

    protected override Task LoadAsync(CancellationToken ct) => Task.CompletedTask;

    protected override void OnBudgetChanged()
    {
        base.OnBudgetChanged();
        OnPropertyChanged(nameof(BudgetInitial));
        OnPropertyChanged(nameof(BudgetDetails));
        OnPropertyChanged(nameof(UserName));
        OnPropertyChanged(nameof(UserEmail));
        OnPropertyChanged(nameof(UserInitial));
    }

    [RelayCommand] private Task OpenPlanAsync() => nav.GoToAsync("plan");
    [RelayCommand] private Task OpenYearAsync() => nav.GoToAsync("year");
    [RelayCommand] private Task OpenCategoriesAsync() => nav.GoToAsync("categories");
    [RelayCommand] private Task OpenShareAsync() => nav.GoToAsync("share");
    [RelayCommand] private Task SwitchBudgetAsync() => nav.GoToAsync("//budgets");

    [RelayCommand]
    private Task LeaveAsync() => ConfirmThenRunAsync("Leave budget", "Remove this shared budget from your account?", "Leave", async ct =>
    {
        await budgets.LeaveAsync(BudgetId, ct);
        State.Reset();
        await nav.GoToAsync("//budgets");
    });

    [RelayCommand]
    private Task DeleteBudgetAsync() => ConfirmThenRunAsync("Delete budget",
        "Delete this budget with all its categories, plans, expenses and income? People it is shared with lose access. This can't be undone.",
        "Delete", async ct =>
        {
            await budgets.DeleteAsync(BudgetId, ct);
            State.Reset();
            await nav.GoToAsync("//budgets");
        });

    [RelayCommand]
    private async Task SignOutAsync()
    {
        if (!await Dialogs.ConfirmAsync("Sign out", "Sign out of Kopeck on this device?", "Sign out")) return;
        session.SignOut();
        State.Reset();
        await nav.GoToAsync("//login");
    }

    internal static string Initial(string text) => string.IsNullOrWhiteSpace(text) ? "?" : text.Trim()[..1].ToUpperInvariant();
}
