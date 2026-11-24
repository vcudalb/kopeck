using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kopeck.Contracts;
using Kopeck.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Kopeck.Mobile.ViewModels;

/// <summary>Income entries of the open month, booked against any number of revenue streams.</summary>
public partial class IncomeViewModel(IIncomeApi income, AppState state, IDialogService dialogs, ILogger<IncomeViewModel> logger)
    : BudgetViewModel(state, dialogs, logger)
{
    public ObservableCollection<IncomeDto> Items { get; } = [];
    public ObservableCollection<IncomeStreamDto> Streams { get; } = [];

    [ObservableProperty] private IncomeStreamDto? _selectedStream;
    [ObservableProperty] private string _amount = "";
    [ObservableProperty] private string _note = "";
    [ObservableProperty] private DateTime _date = state.DefaultEntryDate;
    [ObservableProperty] private string _totalText = "";

    protected override void OnFormOpening()
    {
        (Amount, Note) = ("", "");
        Date = State.DefaultEntryDate;
    }

    [RelayCommand]
    private async Task AddStreamAsync()
    {
        var name = await Dialogs.PromptAsync("New revenue stream", "e.g. Main salary, Freelance", maxLength: 80);
        if (string.IsNullOrWhiteSpace(name)) return;
        await RunAsync(async ct =>
        {
            var stream = await income.CreateStreamAsync(BudgetId, new IncomeStreamRequest(name.Trim()), ct);
            await LoadAsync(ct);
            SelectedStream = Streams.FirstOrDefault(s => s.Id == stream.Id);
        });
    }

    [RelayCommand]
    private Task AddAsync() => RunAsync(async ct =>
    {
        if (SelectedStream is null) { Error = "Add a revenue stream first."; return; }
        if (!Money.TryParse(Amount, out var amount)) { Error = "Enter a valid amount."; return; }

        await income.CreateAsync(BudgetId, new IncomeRequest(SelectedStream.Id, DateOnly.FromDateTime(Date), amount, Note), ct);
        IsFormOpen = false;
        await LoadAsync(ct);
    });

    [RelayCommand]
    private Task DeleteAsync(IncomeDto entry) =>
        ConfirmThenRunAsync("Delete income", $"{entry.StreamName} {entry.Amount:N2}?", "Delete", async ct =>
        {
            await income.DeleteAsync(BudgetId, entry.Id, ct);
            await LoadAsync(ct);
        });

    protected override async Task LoadAsync(CancellationToken ct)
    {
        Streams.ReplaceWith(await income.ListStreamsAsync(BudgetId, ct));
        SelectedStream = Streams.FirstOrDefault(s => s.Id == SelectedStream?.Id) ?? Streams.FirstOrDefault();
        Items.ReplaceWith(await income.ListAsync(BudgetId, State.Month, ct));
        TotalText = $"{Items.Sum(i => i.Amount):N2} {Currency}";
    }
}
