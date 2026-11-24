using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Kopeck.Contracts;

namespace Kopeck.Mobile.ViewModels;

public partial class PlanRow(PlanDto plan) : ObservableObject
{
    public Guid CategoryId { get; } = plan.CategoryId;
    public string Name { get; } = plan.CategoryName;
    public decimal OriginalAmount { get; } = plan.Amount;

    [ObservableProperty]
    private string _amountText = plan.Amount == 0 ? "" : plan.Amount.ToString("0.##", CultureInfo.InvariantCulture);
}
