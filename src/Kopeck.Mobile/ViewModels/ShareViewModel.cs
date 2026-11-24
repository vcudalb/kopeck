using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kopeck.Contracts;
using Kopeck.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Kopeck.Mobile.ViewModels;

public partial class ShareViewModel(ISharesApi shares, AppState state, IDialogService dialogs, ILogger<ShareViewModel> logger)
    : BudgetViewModel(state, dialogs, logger)
{
    public ObservableCollection<ShareDto> Items { get; } = [];
    public IReadOnlyList<ShareRole> Roles { get; } = [ShareRole.Viewer, ShareRole.Editor];

    [ObservableProperty] private string _email = "";
    [ObservableProperty] private ShareRole _role = ShareRole.Viewer;

    [RelayCommand]
    private Task AddAsync() => RunAsync(async ct =>
    {
        await shares.ShareAsync(BudgetId, new ShareRequest(Email, Role), ct);
        Email = "";
        await LoadAsync(ct);
    });

    [RelayCommand]
    private Task RemoveAsync(ShareDto share) =>
        ConfirmThenRunAsync("Stop sharing", $"Remove access for {share.Email}?", "Remove", async ct =>
        {
            await shares.RemoveAsync(BudgetId, share.Id, ct);
            await LoadAsync(ct);
        });

    protected override async Task LoadAsync(CancellationToken ct) => Items.ReplaceWith(await shares.ListAsync(BudgetId, ct));
}
