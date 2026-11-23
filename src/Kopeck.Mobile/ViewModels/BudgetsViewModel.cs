using System.Collections.ObjectModel;
using Kopeck.Contracts;
using Kopeck.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Kopeck.Mobile.ViewModels;

public partial class BudgetsViewModel(ViewModelContext context, Session session) : BaseViewModel(context)
{
    public ObservableCollection<BudgetDto> Items { get; } = [];

    [ObservableProperty] private string _newName = "";
    [ObservableProperty] private string _newCurrency = "EUR";

    public string Greeting => $"Hi, {session.DisplayName}";

    [RelayCommand]
    private Task LoadAsync() => RunAsync(async () =>
    {
        var budgets = await Api.GetAsync<List<BudgetDto>>("/api/budgets");
        Items.Clear();
        foreach (var b in budgets) Items.Add(b);
        OnPropertyChanged(nameof(Greeting));
    });

    [RelayCommand]
    private Task CreateAsync() => RunAsync(async () =>
    {
        Items.Add(await Api.PostAsync<BudgetDto>("/api/budgets", new BudgetRequest(NewName, NewCurrency)));
        NewName = "";
    });

    [RelayCommand]
    private Task OpenAsync(BudgetDto budget)
    {
        State.Budget = budget;
        return Nav.GoToAsync("dashboard");
    }

    [RelayCommand]
    private Task SignOutAsync()
    {
        session.SignOut();
        State.Reset();
        return Nav.GoToAsync("//login");
    }
}
