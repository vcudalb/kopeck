using System.Collections.ObjectModel;
using Kopeck.Contracts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Kopeck.Mobile.ViewModels;

public partial class ShareViewModel(ViewModelContext context) : BaseViewModel(context)
{
    public ObservableCollection<ShareDto> Items { get; } = [];
    public IReadOnlyList<ShareRole> Roles { get; } = [ShareRole.Viewer, ShareRole.Editor];

    [ObservableProperty] private string _email = "";
    [ObservableProperty] private ShareRole _role = ShareRole.Viewer;

    [RelayCommand]
    private Task LoadAsync() => RunAsync(ReloadAsync);

    [RelayCommand]
    private Task AddAsync() => RunAsync(async () =>
    {
        await Api.PostAsync<ShareDto>($"{BudgetPath}/shares", new ShareRequest(Email, Role));
        Email = "";
        await ReloadAsync();
    });

    [RelayCommand]
    private Task RemoveAsync(ShareDto share) =>
        ConfirmThenRunAsync("Stop sharing", $"Remove access for {share.Email}?", "Remove", async () =>
        {
            await Api.DeleteAsync($"{BudgetPath}/shares/{share.Id}");
            await ReloadAsync();
        });

    private async Task ReloadAsync()
    {
        var shares = await Api.GetAsync<List<ShareDto>>($"{BudgetPath}/shares");
        Items.Clear();
        foreach (var s in shares) Items.Add(s);
    }
}
