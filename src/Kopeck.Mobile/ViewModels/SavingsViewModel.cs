using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kopeck.Contracts;
using Kopeck.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Kopeck.Mobile.ViewModels;

/// <summary>
/// The Savings tab: every pot with its balance and progress. "+" creates a pot (in the budget currency unless another
/// is picked); tapping a pot opens it to add or take out money.
/// </summary>
public partial class SavingsViewModel(ISavingsApi savings, AppState state, INavigationService nav, IDialogService dialogs, ILogger<SavingsViewModel> logger)
    : BudgetViewModel(state, dialogs, logger)
{
    /// <summary>New pots take the next colour, so pots are easy to tell apart without asking the user.</summary>
    private static readonly string[] Palette = ["#2E7D32", "#1565C0", "#6A1B9A", "#EF6C00", "#00838F", "#AD1457", "#5D4037", "#455A64"];

    public ObservableCollection<SavingsPotRow> Pots { get; } = [];

    [ObservableProperty] private string _totalText = "";
    [ObservableProperty] private bool _hasPots;

    [ObservableProperty] private string _newName = "";
    [ObservableProperty] private string _newCurrency = "";
    [ObservableProperty] private string _newTarget = "";

    public IReadOnlyList<string> CurrencyOptions => CurrencyText.Choices(State.Currency);

    protected override void OnBudgetChanged()
    {
        base.OnBudgetChanged();
        OnPropertyChanged(nameof(CurrencyOptions));
    }

    protected override void OnFormOpening() => (NewName, NewCurrency, NewTarget) = ("", State.Currency, "");

    [RelayCommand]
    private Task CreateAsync() => RunAsync(async ct =>
    {
        if (string.IsNullOrWhiteSpace(NewName)) { Error = "Give the pot a name."; return; }
        decimal? target = null;
        if (!string.IsNullOrWhiteSpace(NewTarget))
        {
            if (!Money.TryParse(NewTarget, out var parsed)) { Error = "Enter a valid target, or leave it empty."; return; }
            target = parsed;
        }

        var color = Palette[Pots.Count % Palette.Length];
        var pot = await savings.CreatePotAsync(BudgetId, new SavingsPotRequest(NewName.Trim(), color, NewCurrency, target), ct);
        IsFormOpen = false;
        await LoadAsync(ct);
        await OpenAsync(Pots.FirstOrDefault(p => p.Pot.Id == pot.Id));   // straight to the first deposit
    });

    [RelayCommand]
    private Task OpenAsync(SavingsPotRow? row)
    {
        if (row is null) return Task.CompletedTask;
        State.Pot = row.Pot;
        return nav.GoToAsync("savings-pot");
    }

    protected override async Task LoadAsync(CancellationToken ct)
    {
        var pots = await savings.ListPotsAsync(BudgetId, ct);
        Pots.ReplaceWith(pots.Select(p => new SavingsPotRow(p)));
        HasPots = Pots.Count > 0;
        TotalText = CurrencyText.Join(pots.Select(p => (p.Currency, p.Balance)), State.Currency);
    }
}
