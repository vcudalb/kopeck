using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kopeck.Contracts;
using Kopeck.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Kopeck.Mobile.ViewModels;

/// <summary>Expenses of the open month: "+" opens a sheet to add one, tapping an expense opens it for editing, swipe deletes.</summary>
public partial class ExpensesViewModel(
    IExpensesApi expenses, ICategoriesApi categories, AppState state, INavigationService nav, IDialogService dialogs, ILogger<ExpensesViewModel> logger)
    : BudgetViewModel(state, dialogs, logger)
{
    public ObservableCollection<ExpenseDto> Items { get; } = [];
    public ObservableCollection<CategoryDto> Categories { get; } = [];

    [ObservableProperty] private CategoryDto? _selectedCategory;
    [ObservableProperty] private string _amount = "";
    [ObservableProperty] private string _note = "";
    [ObservableProperty] private DateTime _date = state.DefaultEntryDate;
    [ObservableProperty] private string _totalText = "";

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsEditing), nameof(FormTitle), nameof(SubmitText), nameof(AuditLine))]
    private ExpenseDto? _editing;

    public bool IsEditing => Editing is not null;
    public string FormTitle => IsEditing ? "Edit expense" : "New expense";
    public string SubmitText => IsEditing ? "Save changes" : "Add expense";

    /// <summary>Under the title while editing: "Added by Alice · 1 Oct 2026, 09:12 · Edited by Bob · 2 Oct 2026, 18:40".</summary>
    public string AuditLine => Editing is not { } e ? ""
        : "Added by " + AuditText.By(e.CreatedBy, e.CreatedAt) + (e.UpdatedAt is { } u ? "\nEdited by " + AuditText.By(e.UpdatedBy, u) : "");

    /// <summary>Opens the history of the expense being edited: who added or changed it, when, and what changed.</summary>
    [RelayCommand]
    private Task OpenHistoryAsync()
    {
        if (Editing is null) return Task.CompletedTask;
        State.HistoryExpense = Editing;
        return nav.GoToAsync("history");
    }

    protected override void OnFormOpening() => ClearForm();

    protected override void OnFormClosed() => Editing = null;

    [RelayCommand]
    private Task SubmitAsync() => RunAsync(async ct =>
    {
        if (SelectedCategory is null) { Error = "Create a category first (More → Categories)."; return; }
        if (!Money.TryParse(Amount, out var amount)) { Error = "Enter a valid amount."; return; }

        var request = new ExpenseRequest(SelectedCategory.Id, DateOnly.FromDateTime(Date), amount, Note);
        if (Editing is { } editing) await expenses.UpdateAsync(BudgetId, editing.Id, request, ct);
        else await expenses.CreateAsync(BudgetId, request, ct);

        IsFormOpen = false;
        ClearForm();
        await LoadAsync(ct);
    });

    [RelayCommand]
    private void Edit(ExpenseDto expense)
    {
        if (!CanEdit) return;
        Error = "";
        Editing = expense;
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == expense.CategoryId);
        Amount = expense.Amount.ToString("0.##", CultureInfo.InvariantCulture);
        Note = expense.Note ?? "";
        Date = expense.Date.ToDateTime(TimeOnly.MinValue);
        IsFormOpen = true;
    }

    [RelayCommand]
    private Task DeleteAsync(ExpenseDto expense) =>
        ConfirmThenRunAsync("Delete expense", $"{expense.CategoryName} {expense.Amount:N2}?", "Delete", async ct =>
        {
            await expenses.DeleteAsync(BudgetId, expense.Id, ct);
            if (Editing?.Id == expense.Id) { IsFormOpen = false; ClearForm(); }
            await LoadAsync(ct);
        });

    private void ClearForm()
    {
        Editing = null;
        (Amount, Note) = ("", "");
        Date = State.DefaultEntryDate;
    }

    protected override async Task LoadAsync(CancellationToken ct)
    {
        Categories.ReplaceWith(await categories.ListAsync(BudgetId, ct));
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == SelectedCategory?.Id) ?? Categories.FirstOrDefault();
        Items.ReplaceWith(await expenses.ListAsync(BudgetId, State.Month, ct));
        TotalText = $"{Items.Sum(e => e.Amount):N2} {Currency}";
        OpenQuickAdd();
    }

    /// <summary>Coming from the Overview's "+" on a category: open a new expense in it (the month is already the one on screen).</summary>
    private void OpenQuickAdd()
    {
        if (State.QuickAddCategoryId is not { } id) return;
        State.QuickAddCategoryId = null;
        if (!CanEdit || Categories.FirstOrDefault(c => c.Id == id) is not { } category) return;
        ClearForm();
        Error = "";
        SelectedCategory = category;
        IsFormOpen = true;
    }
}
