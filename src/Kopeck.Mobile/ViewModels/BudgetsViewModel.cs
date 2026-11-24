using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kopeck.Contracts;
using Kopeck.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Kopeck.Mobile.ViewModels;

public partial class BudgetsViewModel(
    IBudgetsApi budgets, IUserSession session, AppState state, INavigationService nav, ILogger<BudgetsViewModel> logger)
    : BaseViewModel(logger)
{
    public ObservableCollection<BudgetDto> Items { get; } = [];

    [ObservableProperty] private string _newName = "";
    [ObservableProperty] private string _newCurrency = "EUR";

    public string Greeting => $"Hi, {session.DisplayName}";

    public override Task OnAppearingAsync() => RunAsync(async ct =>
    {
        Items.ReplaceWith(await budgets.ListAsync(ct));
        OnPropertyChanged(nameof(Greeting));
    });

    [RelayCommand]
    private Task CreateAsync() => RunAsync(async ct =>
    {
        Items.Add(await budgets.CreateAsync(new BudgetRequest(NewName, NewCurrency), ct));
        NewName = "";
    });

    [RelayCommand]
    private Task OpenAsync(BudgetDto budget)
    {
        state.Budget = budget;
        return nav.GoToAsync("dashboard");
    }

    [RelayCommand]
    private Task SignOutAsync()
    {
        session.SignOut();
        state.Reset();
        return nav.GoToAsync("//login");
    }
}

internal static class CollectionExtensions
{
    public static void ReplaceWith<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items) collection.Add(item);
    }
}
