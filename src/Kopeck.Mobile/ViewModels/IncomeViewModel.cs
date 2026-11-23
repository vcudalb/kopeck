using System.Collections.ObjectModel;
using Kopeck.Contracts;
using Kopeck.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Kopeck.Mobile.ViewModels;

/// <summary>Income entries, booked against any number of revenue streams.</summary>
public partial class IncomeViewModel(ViewModelContext context) : BaseViewModel(context)
{
    public ObservableCollection<IncomeDto> Items { get; } = [];
    public ObservableCollection<IncomeStreamDto> Streams { get; } = [];

    [ObservableProperty] private IncomeStreamDto? _selectedStream;
    [ObservableProperty] private string _amount = "";
    [ObservableProperty] private string _note = "";
    [ObservableProperty] private DateTime _date = DateTime.Today;
    [ObservableProperty] private string _monthText = "";

    [RelayCommand]
    private Task LoadAsync() => RunAsync(ReloadAsync);

    [RelayCommand]
    private async Task AddStreamAsync()
    {
        var name = await Dialogs.PromptAsync("New revenue stream", "e.g. Main salary, Freelance", maxLength: 80);
        if (string.IsNullOrWhiteSpace(name)) return;
        await RunAsync(async () =>
        {
            var stream = await Api.PostAsync<IncomeStreamDto>($"{BudgetPath}/income-streams", new IncomeStreamRequest(name));
            await ReloadAsync();
            SelectedStream = Streams.FirstOrDefault(s => s.Id == stream.Id);
        });
    }

    [RelayCommand]
    private Task AddAsync() => RunAsync(async () =>
    {
        if (SelectedStream is null) { Error = "Add an income stream first."; return; }
        if (!Money.TryParse(Amount, out var amount)) { Error = "Enter a valid amount."; return; }

        await Api.PostAsync<IncomeDto>($"{BudgetPath}/incomes",
            new IncomeRequest(SelectedStream.Id, DateOnly.FromDateTime(Date), amount, Note));
        (Amount, Note) = ("", "");
        await ReloadAsync();
    });

    [RelayCommand]
    private Task DeleteAsync(IncomeDto income) =>
        ConfirmThenRunAsync("Delete income", $"{income.StreamName} {income.Amount:N2}?", "Delete", async () =>
        {
            await Api.DeleteAsync($"{BudgetPath}/incomes/{income.Id}");
            await ReloadAsync();
        });

    private async Task ReloadAsync()
    {
        MonthText = State.Month.ToString("MMMM yyyy");

        var streams = await Api.GetAsync<List<IncomeStreamDto>>($"{BudgetPath}/income-streams");
        Streams.Clear();
        foreach (var s in streams) Streams.Add(s);
        SelectedStream ??= Streams.FirstOrDefault();

        var incomes = await Api.GetAsync<List<IncomeDto>>($"{BudgetPath}/incomes?month={ApiClient.Month(State.Month)}");
        Items.Clear();
        foreach (var i in incomes) Items.Add(i);
    }
}
