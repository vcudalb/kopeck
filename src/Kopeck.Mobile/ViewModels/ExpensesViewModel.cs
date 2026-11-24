using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kopeck.Contracts;
using Kopeck.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Kopeck.Mobile.ViewModels;

/// <summary>Expenses of the open month: add, edit (swipe → Edit fills the form) and delete.</summary>
public partial class ExpensesViewModel(
    IExpensesApi expenses, ICategoriesApi categories, AppState state, IDialogService dialogs, ILogger<ExpensesViewModel> logger)
    : BudgetViewModel(state, dialogs, logger)
{
    public ObservableCollection<ExpenseDto> Items { get; } = [];
    public ObservableCollection<CategoryDto> Categories { get; } = [];

    [ObservableProperty] private CategoryDto? _selectedCategory;
    [ObservableProperty] private string _amount = "";
    [ObservableProperty] private string _note = "";
    [ObservableProperty] private DateTime _date = state.DefaultEntryDate;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsEditing), nameof(SubmitText))]
    private ExpenseDto? _editing;

    public bool IsEditing => Editing is not null;
    public string SubmitText => IsEditing ? "Save changes" : "Add expense";

    public override Task OnAppearingAsync() => RunAsync(ReloadAsync);

    [RelayCommand]
    private Task SubmitAsync() => RunAsync(async ct =>
    {
        if (SelectedCategory is null) { Error = "Create a category first."; return; }
        if (!Money.TryParse(Amount, out var amount)) { Error = "Enter a valid amount."; return; }

        var request = new ExpenseRequest(SelectedCategory.Id, DateOnly.FromDateTime(Date), amount, Note);
        if (Editing is { } editing) await expenses.UpdateAsync(BudgetId, editing.Id, request, ct);
        else await expenses.CreateAsync(BudgetId, request, ct);

        ClearForm();
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private void Edit(ExpenseDto expense)
    {
        Editing = expense;
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == expense.CategoryId);
        Amount = expense.Amount.ToString("0.##", CultureInfo.InvariantCulture);
        Note = expense.Note ?? "";
        Date = expense.Date.ToDateTime(TimeOnly.MinValue);
    }

    [RelayCommand]
    private void CancelEdit() => ClearForm();

    [RelayCommand]
    private Task DeleteAsync(ExpenseDto expense) =>
        ConfirmThenRunAsync("Delete expense", $"{expense.CategoryName} {expense.Amount:N2}?", "Delete", async ct =>
        {
            await expenses.DeleteAsync(BudgetId, expense.Id, ct);
            if (Editing?.Id == expense.Id) ClearForm();
            await ReloadAsync(ct);
        });

    private void ClearForm()
    {
        Editing = null;
        (Amount, Note) = ("", "");
        Date = State.DefaultEntryDate;
    }

    private async Task ReloadAsync(CancellationToken ct)
    {
        RefreshMonthText();
        Categories.ReplaceWith(await categories.ListAsync(BudgetId, ct));
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == SelectedCategory?.Id) ?? Categories.FirstOrDefault();
        Items.ReplaceWith(await expenses.ListAsync(BudgetId, State.Month, ct));
    }
}
