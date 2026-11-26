using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kopeck.Contracts;
using Kopeck.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Kopeck.Mobile.ViewModels;

/// <summary>
/// One savings pot: balance and target at the top, "Add money" / "Take out" buttons, and the full history.
/// The pot's settings (name, target, currency while empty) are edited in a second sheet.
/// </summary>
public partial class SavingsPotViewModel(ISavingsApi savings, AppState state, INavigationService nav, IDialogService dialogs, ILogger<SavingsPotViewModel> logger)
    : BudgetViewModel(state, dialogs, logger)
{
    public ObservableCollection<SavingsEntryRow> Entries { get; } = [];

    [ObservableProperty] private SavingsPotRow? _pot;
    [ObservableProperty] private bool _hasEntries;

    // add / take out sheet (the base class's form)
    [ObservableProperty] private SavingsEntryKind _kind;
    [ObservableProperty] private string _amount = "";
    [ObservableProperty] private DateTime _date = state.Today;
    [ObservableProperty] private string _note = "";

    // settings sheet
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ShowAddButton))] private bool _isEditOpen;
    [ObservableProperty] private string _editName = "";
    [ObservableProperty] private string _editTarget = "";
    [ObservableProperty] private string _editCurrency = "";

    public string FormTitle => Kind == SavingsEntryKind.Deposit ? "Add money" : "Take money out";
    public string SaveText => Kind == SavingsEntryKind.Deposit ? "Add to pot" : "Take out";
    public string AmountLabel => $"Amount ({Pot?.Pot.Currency ?? State.Currency})";
    public bool CanChangeCurrency => !HasEntries;
    public IReadOnlyList<string> CurrencyOptions => CurrencyText.Choices(State.Currency);
    public override bool ShowAddButton => CanEdit && !IsFormOpen && !IsEditOpen;

    partial void OnKindChanged(SavingsEntryKind value)
    {
        OnPropertyChanged(nameof(FormTitle));
        OnPropertyChanged(nameof(SaveText));
    }

    partial void OnPotChanged(SavingsPotRow? value) => OnPropertyChanged(nameof(AmountLabel));
    partial void OnHasEntriesChanged(bool value) => OnPropertyChanged(nameof(CanChangeCurrency));

    protected override void OnFormOpening() => (Amount, Note, Date) = ("", "", State.Today);

    [RelayCommand]
    private void Deposit() => OpenEntryForm(SavingsEntryKind.Deposit);

    [RelayCommand]
    private void Withdraw() => OpenEntryForm(SavingsEntryKind.Withdrawal);

    [RelayCommand]
    private Task SaveEntryAsync() => RunAsync(async ct =>
    {
        if (Pot is null) return;
        if (!Money.TryParse(Amount, out var amount)) { Error = "Enter a valid amount."; return; }

        await savings.AddEntryAsync(BudgetId, Pot.Pot.Id, new SavingsEntryRequest(Kind, DateOnly.FromDateTime(Date), amount, Note), ct);
        IsFormOpen = false;
        await LoadAsync(ct);
    });

    [RelayCommand]
    private Task DeleteEntryAsync(SavingsEntryRow row) =>
        ConfirmThenRunAsync("Delete entry", $"{row.Title} {row.AmountText}?", "Delete", async ct =>
        {
            await savings.DeleteEntryAsync(BudgetId, row.Entry.PotId, row.Entry.Id, ct);
            await LoadAsync(ct);
        });

    [RelayCommand]
    private void OpenEdit()
    {
        if (Pot is null) return;
        Error = "";
        EditName = Pot.Pot.Name;
        EditTarget = Pot.Pot.Target is { } t ? t.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) : "";
        EditCurrency = Pot.Pot.Currency;
        OnPropertyChanged(nameof(CurrencyOptions));
        IsEditOpen = true;
    }

    [RelayCommand]
    private void CloseEdit() => (IsEditOpen, Error) = (false, "");

    [RelayCommand]
    private Task SaveEditAsync() => RunAsync(async ct =>
    {
        if (Pot is null) return;
        if (string.IsNullOrWhiteSpace(EditName)) { Error = "Give the pot a name."; return; }
        decimal? target = null;
        if (!string.IsNullOrWhiteSpace(EditTarget))
        {
            if (!Money.TryParse(EditTarget, out var parsed)) { Error = "Enter a valid target, or leave it empty."; return; }
            target = parsed;
        }

        var updated = await savings.UpdatePotAsync(BudgetId, Pot.Pot.Id,
            new SavingsPotRequest(EditName.Trim(), Pot.Pot.Color, EditCurrency, target), ct);
        State.Pot = updated;
        IsEditOpen = false;
        await LoadAsync(ct);
    });

    [RelayCommand]
    private Task DeletePotAsync() =>
        Pot is null ? Task.CompletedTask
        : ConfirmThenRunAsync("Delete pot", $"Delete \"{Pot.Name}\" and its whole history? This can't be undone.", "Delete", async ct =>
        {
            await savings.DeletePotAsync(BudgetId, Pot.Pot.Id, ct);
            State.Pot = null;
            IsEditOpen = false;
            await nav.BackAsync();
        });

    protected override async Task LoadAsync(CancellationToken ct)
    {
        if (State.Pot is not { } open) return;

        // Re-read the pot for its current balance (it may have changed on another device).
        var pots = await savings.ListPotsAsync(BudgetId, ct);
        var pot = pots.FirstOrDefault(p => p.Id == open.Id);
        if (pot is null) { await nav.BackAsync(); return; }

        State.Pot = pot;
        Pot = new SavingsPotRow(pot);
        Entries.ReplaceWith((await savings.ListEntriesAsync(BudgetId, pot.Id, ct)).Select(e => new SavingsEntryRow(e, pot.Currency)));
        HasEntries = Entries.Count > 0;
    }

    private void OpenEntryForm(SavingsEntryKind kind)
    {
        Kind = kind;
        OpenFormCommand.Execute(null);
    }
}
