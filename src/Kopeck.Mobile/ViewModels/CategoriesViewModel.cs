using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kopeck.Contracts;
using Kopeck.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Kopeck.Mobile.ViewModels;

public partial class CategoriesViewModel(ICategoriesApi categories, AppState state, IDialogService dialogs, ILogger<CategoriesViewModel> logger)
    : BudgetViewModel(state, dialogs, logger)
{
    public ObservableCollection<CategoryDto> Items { get; } = [];

    [ObservableProperty] private string _newName = "";

    public override Task OnAppearingAsync() => RunAsync(ReloadAsync);

    [RelayCommand]
    private Task AddAsync() => RunAsync(async ct =>
    {
        await categories.CreateAsync(BudgetId, new CategoryRequest(NewName, null), ct);
        NewName = "";
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private async Task RenameAsync(CategoryDto category)
    {
        var name = await Dialogs.PromptAsync("Rename category", "New name", category.Name, maxLength: 80);
        if (string.IsNullOrWhiteSpace(name) || name == category.Name) return;
        await RunAsync(async ct =>
        {
            await categories.UpdateAsync(BudgetId, category.Id, new CategoryRequest(name, category.Color), ct);
            await ReloadAsync(ct);
        });
    }

    [RelayCommand]
    private Task DeleteAsync(CategoryDto category) =>
        ConfirmThenRunAsync("Delete category", $"Delete '{category.Name}'?", "Delete", async ct =>
        {
            await categories.DeleteAsync(BudgetId, category.Id, ct);
            await ReloadAsync(ct);
        });

    private async Task ReloadAsync(CancellationToken ct) => Items.ReplaceWith(await categories.ListAsync(BudgetId, ct));
}
