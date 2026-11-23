using System.Collections.ObjectModel;
using Kopeck.Contracts;
using Kopeck.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Kopeck.Mobile.ViewModels;

public partial class ExpensesViewModel(ViewModelContext context) : BaseViewModel(context)
{
    public ObservableCollection<ExpenseDto> Items { get; } = [];
    public ObservableCollection<CategoryDto> Categories { get; } = [];

    [ObservableProperty] private CategoryDto? _selectedCategory;
    [ObservableProperty] private string _amount = "";
    [ObservableProperty] private string _note = "";
    [ObservableProperty] private DateTime _date = DateTime.Today;
    [ObservableProperty] private string _monthText = "";

    [RelayCommand]
    private Task LoadAsync() => RunAsync(ReloadAsync);

    [RelayCommand]
    private Task AddAsync() => RunAsync(async () =>
    {
        if (SelectedCategory is null) { Error = "Create a category first."; return; }
        if (!Money.TryParse(Amount, out var amount)) { Error = "Enter a valid amount."; return; }

        await Api.PostAsync<ExpenseDto>($"{BudgetPath}/expenses",
            new ExpenseRequest(SelectedCategory.Id, DateOnly.FromDateTime(Date), amount, Note));
        (Amount, Note) = ("", "");
        await ReloadAsync();
    });

    [RelayCommand]
    private Task DeleteAsync(ExpenseDto expense) =>
        ConfirmThenRunAsync("Delete expense", $"{expense.CategoryName} {expense.Amount:N2}?", "Delete", async () =>
        {
            await Api.DeleteAsync($"{BudgetPath}/expenses/{expense.Id}");
            await ReloadAsync();
        });

    private async Task ReloadAsync()
    {
        MonthText = State.Month.ToString("MMMM yyyy");

        var categories = await Api.GetAsync<List<CategoryDto>>($"{BudgetPath}/categories");
        Categories.Clear();
        foreach (var c in categories) Categories.Add(c);
        SelectedCategory ??= Categories.FirstOrDefault();

        var expenses = await Api.GetAsync<List<ExpenseDto>>($"{BudgetPath}/expenses?month={ApiClient.Month(State.Month)}");
        Items.Clear();
        foreach (var e in expenses) Items.Add(e);
    }
}
