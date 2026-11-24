using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kopeck.Contracts;
using Kopeck.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Kopeck.Mobile.ViewModels;

public partial class PlanViewModel(IPlansApi plans, AppState state, IDialogService dialogs, ILogger<PlanViewModel> logger)
    : BudgetViewModel(state, dialogs, logger)
{
    public ObservableCollection<PlanRow> Rows { get; } = [];

    [ObservableProperty] private string _totalText = "";

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
        if (changes.Count == 0) { await Dialogs.AlertAsync("Nothing to save", "No planned amounts were changed."); return; }

        await plans.SaveAsync(BudgetId, new SavePlansRequest(State.Month, changes), ct);
        await LoadAsync(ct);
        await Dialogs.AlertAsync("Plan saved", $"Planned amounts for {State.MonthText} were updated.");
    });

    [RelayCommand]
    private Task CopyPreviousMonthAsync() => RunAsync(async ct =>
    {
        var result = await plans.CopyAsync(BudgetId, new CopyPlanRequest(State.Month.AddMonths(-1), State.Month), ct);
        await LoadAsync(ct);
        await Dialogs.AlertAsync("Copy from last month", result.Copied == 0
            ? "Nothing to copy: every category planned last month already has an amount this month."
            : $"Copied {result.Copied} planned amount(s) from last month.");
    });

    protected override async Task LoadAsync(CancellationToken ct)
    {
        var list = await plans.ListAsync(BudgetId, State.Month, ct);
        Rows.ReplaceWith(list.Select(p => new PlanRow(p)));
        TotalText = $"{list.Sum(p => p.Amount):N2} {Currency}";
    }
}
