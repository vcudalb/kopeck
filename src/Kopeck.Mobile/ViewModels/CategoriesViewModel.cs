using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Kopeck.Contracts;
using Kopeck.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Kopeck.Mobile.ViewModels;

/// <summary>Categories of the open budget: "+" adds, tap renames, swipe renames or deletes.</summary>
public partial class CategoriesViewModel(ICategoriesApi categories, AppState state, IDialogService dialogs, ILogger<CategoriesViewModel> logger)
    : BudgetViewModel(state, dialogs, logger)
{
    public ObservableCollection<CategoryDto> Items { get; } = [];

    [RelayCommand]
    private async Task AddAsync()
    {
        var name = await Dialogs.PromptAsync("New category", "e.g. Home, Bills, Fuel", maxLength: 80);
        if (string.IsNullOrWhiteSpace(name)) return;
        await RunAsync(async ct =>
        {
            await categories.CreateAsync(BudgetId, new CategoryRequest(name.Trim(), null), ct);
            await LoadAsync(ct);
        });
    }

    [RelayCommand]
    private async Task RenameAsync(CategoryDto category)
    {
        if (!CanEdit) return;
        var name = await Dialogs.PromptAsync("Rename category", "New name", category.Name, maxLength: 80);
        if (string.IsNullOrWhiteSpace(name) || name == category.Name) return;
        await RunAsync(async ct =>
        {
            await categories.UpdateAsync(BudgetId, category.Id, new CategoryRequest(name.Trim(), category.Color), ct);
            await LoadAsync(ct);
        });
    }

    [RelayCommand]
    private Task DeleteAsync(CategoryDto category) =>
        ConfirmThenRunAsync("Delete category", $"Delete \"{category.Name}\"?", "Delete", async ct =>
        {
            await categories.DeleteAsync(BudgetId, category.Id, ct);
            await LoadAsync(ct);
        });

    protected override async Task LoadAsync(CancellationToken ct) => Items.ReplaceWith(await categories.ListAsync(BudgetId, ct));
}
