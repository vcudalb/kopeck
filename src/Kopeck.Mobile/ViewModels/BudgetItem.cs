using Kopeck.Contracts;

namespace Kopeck.Mobile.ViewModels;

/// <summary>One budget in the list.</summary>
public sealed class BudgetItem(BudgetDto budget)
{
    public BudgetDto Budget { get; } = budget;
    public string Name => Budget.Name;
    public string Initial => MoreViewModel.Initial(Budget.Name);
    public string Details => Budget.Role == BudgetRole.Owner ? Budget.Currency : $"{Budget.Currency} · shared by {Budget.OwnerEmail}";
    public string RoleText => Budget.Role.ToString();
}
