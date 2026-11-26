using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Kopeck.Contracts;
using Kopeck.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Kopeck.Mobile.ViewModels;

/// <summary>
/// The history of one expense, newest first: who added, changed or deleted it, when, and each value before → after.
/// Opened from the expense sheet's "History" link.
/// </summary>
public partial class HistoryViewModel(IExpensesApi expenses, AppState state, IDialogService dialogs, ILogger<HistoryViewModel> logger)
    : BudgetViewModel(state, dialogs, logger)
{
    public ObservableCollection<HistoryItem> Items { get; } = [];

    [ObservableProperty] private string _subject = "";
    [ObservableProperty] private string _subjectDetail = "";

    protected override async Task LoadAsync(CancellationToken ct)
    {
        if (State.HistoryExpense is not { } expense) return;
        Subject = expense.CategoryName;
        SubjectDetail = $"{CurrencyText.Format(expense.Amount, Currency)} · {expense.Date.ToString("d MMM yyyy", CultureInfo.CurrentCulture)}"
                        + (string.IsNullOrWhiteSpace(expense.Note) ? "" : $" · {expense.Note}");

        var history = await expenses.HistoryAsync(BudgetId, expense.Id, ct);
        Items.ReplaceWith(history.Select(ToItem));
    }

    private HistoryItem ToItem(EntryChangeDto change)
    {
        var (heading, accent) = change.Action switch
        {
            EntryAction.Created => ("Added", Color.FromArgb("#2E7D32")),
            EntryAction.Updated => ("Edited", Color.FromArgb("#C27C00")),
            _ => ("Deleted", Color.FromArgb("#C62828")),
        };
        var lines = change.Action switch
        {
            EntryAction.Updated => change.Changes.Select(c => $"{c.Field}: {Show(c.Field, c.Old)} → {Show(c.Field, c.New)}"),
            EntryAction.Created when change.Changes.Count == 0 => ["Added before history was kept, so the values aren't known."],
            EntryAction.Created => change.Changes.Select(c => $"{c.Field}: {Show(c.Field, c.New)}"),
            _ => change.Changes.Select(c => $"{c.Field}: {Show(c.Field, c.Old)}"),
        };
        return new HistoryItem(heading, "by " + AuditText.By(change.By, change.At), string.Join("\n", lines), accent);
    }

    /// <summary>History values are stored as plain text (yyyy-MM-dd, 20.00); show them the way the rest of the app does.</summary>
    private string Show(string field, string? value)
    {
        if (string.IsNullOrEmpty(value)) return "—";
        if (field == "Date" && DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d.ToString("d MMM yyyy", CultureInfo.CurrentCulture);
        if (field == "Amount" && decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            return CurrencyText.Format(amount, Currency);
        return value;
    }
}
