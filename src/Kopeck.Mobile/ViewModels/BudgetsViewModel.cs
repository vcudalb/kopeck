using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kopeck.Contracts;
using Kopeck.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Kopeck.Mobile.ViewModels;

public partial class BudgetsViewModel(
    IBudgetsApi budgets, IUserSession session, AppState state, INavigationService nav, IDialogService dialogs, ILogger<BudgetsViewModel> logger)
    : BaseViewModel(logger)
{
    public ObservableCollection<BudgetItem> Items { get; } = [];

    [ObservableProperty] private string _newName = "";
    [ObservableProperty] private string _newCurrency = "EUR";
    [ObservableProperty] private bool _isEmpty;

    public string Greeting => $"Hi, {session.DisplayName}";

    public override Task OnAppearingAsync() => RunAsync(async ct =>
    {
        OnPropertyChanged(nameof(Greeting));
        Items.ReplaceWith((await budgets.ListAsync(ct)).Select(b => new BudgetItem(b)));
        IsEmpty = Items.Count == 0;
    });

    protected override void OnFormOpening() => (NewName, NewCurrency) = ("", "EUR");

    [RelayCommand]
    private Task CreateAsync() => RunAsync(async ct =>
    {
        if (string.IsNullOrWhiteSpace(NewName)) { Error = "Give the budget a name."; return; }
        var created = await budgets.CreateAsync(new BudgetRequest(NewName.Trim(), NewCurrency.Trim().ToUpperInvariant()), ct);
        Items.Add(new BudgetItem(created));
        IsEmpty = false;
        IsFormOpen = false;
    });

    [RelayCommand]
    private Task OpenAsync(BudgetItem item)
    {
        state.Budget = item.Budget;
        return nav.GoToAsync("//main/overview");
    }

    [RelayCommand]
    private async Task SignOutAsync()
    {
        if (!await dialogs.ConfirmAsync("Sign out", "Sign out of Kopeck on this device?", "Sign out")) return;
        session.SignOut();
        state.Reset();
        await nav.GoToAsync("//login");
    }
}
