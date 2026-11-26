using Kopeck.Contracts;
using Kopeck.Mobile.Services;

namespace Kopeck.Mobile.ViewModels;

/// <summary>Presentation of one savings pot: balance and progress towards its target.</summary>
public sealed class SavingsPotRow(SavingsPotDto pot)
{
    public SavingsPotDto Pot => pot;
    public string Name => pot.Name;
    public Color Color => Color.FromArgb(pot.Color);
    public string BalanceText => CurrencyText.Format(pot.Balance, pot.Currency);
    public bool HasTarget => pot.Target is > 0;

    /// <summary>"42% of 5,000.00 EUR", "Target reached", or a hint when there is no target.</summary>
    public string TargetText => pot.Target is not > 0 ? "No target"
        : pot.Balance >= pot.Target ? $"Target of {CurrencyText.Format(pot.Target.Value, pot.Currency)} reached"
        : $"{Percent:0}% of {CurrencyText.Format(pot.Target.Value, pot.Currency)}";

    public double Progress => HasTarget ? Math.Clamp((double)(pot.Balance / pot.Target!.Value), 0, 1) : 0;

    private decimal Percent => pot.Target is > 0 ? pot.Balance / pot.Target.Value * 100 : 0;
}
