using System.Collections.ObjectModel;
using Kopeck.Contracts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Kopeck.Mobile.ViewModels;

public partial class CategoriesViewModel(ViewModelContext context) : BaseViewModel(context)
{
    public ObservableCollection<CategoryDto> Items { get; } = [];

    [ObservableProperty] private string _newName = "";

    [RelayCommand]
    private Task LoadAsync() => RunAsync(ReloadAsync);

    [RelayCommand]
    private Task AddAsync() => RunAsync(async () =>
    {
        await Api.PostAsync<CategoryDto>($"{BudgetPath}/categories", new CategoryRequest(NewName, null));
        NewName = "";
        await ReloadAsync();
    });

    [RelayCommand]
    private async Task RenameAsync(CategoryDto category)
    {
        var name = await Dialogs.PromptAsync("Rename category", "New name", category.Name, maxLength: 80);
        if (string.IsNullOrWhiteSpace(name) || name == category.Name) return;
        await RunAsync(async () =>
        {
            await Api.PutAsync<CategoryDto>($"{BudgetPath}/categories/{category.Id}", new CategoryRequest(name, category.Color));
            await ReloadAsync();
        });
    }

    [RelayCommand]
    private Task DeleteAsync(CategoryDto category) =>
        ConfirmThenRunAsync("Delete category", $"Delete '{category.Name}'?", "Delete", async () =>
        {
            await Api.DeleteAsync($"{BudgetPath}/categories/{category.Id}");
            await ReloadAsync();
        });

    private async Task ReloadAsync()
    {
        var categories = await Api.GetAsync<List<CategoryDto>>($"{BudgetPath}/categories");
        Items.Clear();
        foreach (var c in categories) Items.Add(c);
    }
}
