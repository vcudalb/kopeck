using System.Collections.ObjectModel;
using System.Globalization;
using Kopeck.Contracts;
using Kopeck.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Kopeck.Mobile.ViewModels;

public partial class PlanRow(PlanDto plan) : ObservableObject
{
    public Guid CategoryId { get; } = plan.CategoryId;
    public string Name { get; } = plan.CategoryName;
    public decimal OriginalAmount { get; } = plan.Amount;

    [ObservableProperty]
    private string _amountText = plan.Amount == 0 ? "" : plan.Amount.ToString("0.##", CultureInfo.InvariantCulture);
}

public partial class PlanViewModel(ViewModelContext context) : BaseViewModel(context)
{
    public ObservableCollection<PlanRow> Rows { get; } = [];

    [ObservableProperty] private string _monthText = "";

    [RelayCommand]
    private Task LoadAsync() => RunAsync(ReloadAsync);

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async () =>
    {
        // Validate everything first so a typo in row 5 doesn't leave rows 1-4 saved and the rest not.
        var changes = new List<PlanRequest>();
        foreach (var row in Rows)
        {
            var amount = 0m;
            if (!string.IsNullOrWhiteSpace(row.AmountText) && !Money.TryParse(row.AmountText, out amount))
            {
                Error = $"Invalid amount for {row.Name}.";
                return;
            }
            if (amount != row.OriginalAmount) changes.Add(new PlanRequest(row.CategoryId, State.Month, amount));
        }

        foreach (var change in changes) await Api.PutAsync($"{BudgetPath}/plans", change);
        await ReloadAsync();
        await Dialogs.AlertAsync("Saved", "Planned amounts updated.");
    });

    [RelayCommand]
    private Task CopyPreviousMonthAsync() => RunAsync(async () =>
    {
        await Api.PostAsync($"{BudgetPath}/plans/copy", new CopyPlanRequest(State.Month.AddMonths(-1), State.Month));
        await ReloadAsync();
    });

    private async Task ReloadAsync()
    {
        MonthText = State.Month.ToString("MMMM yyyy");
        var plans = await Api.GetAsync<List<PlanDto>>($"{BudgetPath}/plans?month={ApiClient.Month(State.Month)}");
        Rows.Clear();
        foreach (var p in plans) Rows.Add(new PlanRow(p));
    }
}
