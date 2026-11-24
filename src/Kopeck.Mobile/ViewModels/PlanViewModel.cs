using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kopeck.Contracts;
using Kopeck.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Kopeck.Mobile.ViewModels;

public partial class PlanRow(PlanDto plan) : ObservableObject
{
    public Guid CategoryId { get; } = plan.CategoryId;
    public string Name { get; } = plan.CategoryName;
    public decimal OriginalAmount { get; } = plan.Amount;

    [ObservableProperty]
    private string _amountText = plan.Amount == 0 ? "" : plan.Amount.ToString("0.##", CultureInfo.InvariantCulture);
}

public partial class PlanViewModel(IPlansApi plans, AppState state, IDialogService dialogs, ILogger<PlanViewModel> logger)
    : BudgetViewModel(state, dialogs, logger)
{
    public ObservableCollection<PlanRow> Rows { get; } = [];

    public override Task OnAppearingAsync() => RunAsync(ReloadAsync);

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async ct =>
    {
        // Validate every row first, then send them in one request that the server commits all-or-nothing.
        var changes = new List<PlanAmount>();
        foreach (var row in Rows)
        {
            var amount = 0m;
            if (!string.IsNullOrWhiteSpace(row.AmountText) && !Money.TryParse(row.AmountText, out amount))
            {
                Error = $"Invalid amount for {row.Name}.";
                return;
            }
            if (amount != row.OriginalAmount) changes.Add(new PlanAmount(row.CategoryId, amount));
        }
        if (changes.Count == 0) return;

        await plans.SaveAsync(BudgetId, new SavePlansRequest(State.Month, changes), ct);
        await ReloadAsync(ct);
        await Dialogs.AlertAsync("Saved", "Planned amounts updated.");
    });

    [RelayCommand]
    private Task CopyPreviousMonthAsync() => RunAsync(async ct =>
    {
        var result = await plans.CopyAsync(BudgetId, new CopyPlanRequest(State.Month.AddMonths(-1), State.Month), ct);
        await ReloadAsync(ct);
        if (result.Copied == 0) Error = "Nothing to copy: last month has no plans this month is missing.";
    });

    private async Task ReloadAsync(CancellationToken ct)
    {
        RefreshMonthText();
        Rows.ReplaceWith((await plans.ListAsync(BudgetId, State.Month, ct)).Select(p => new PlanRow(p)));
    }
}
