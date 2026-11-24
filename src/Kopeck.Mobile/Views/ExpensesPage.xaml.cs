using Kopeck.Mobile.ViewModels;

namespace Kopeck.Mobile.Views;

public partial class ExpensesPage : BasePage
{
    public ExpensesPage(ExpensesViewModel vm) : base(vm) => InitializeComponent();
}
