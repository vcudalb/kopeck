using Kopeck.Contracts;
using Kopeck.Mobile.Services;

namespace Kopeck.Mobile.ViewModels;

/// <summary>One deposit or withdrawal in a pot's history.</summary>
public sealed class SavingsEntryRow(SavingsEntryDto entry, string currency)
{
    private static readonly Color DepositColor = Color.FromArgb("#2E7D32");
    private static readonly Color WithdrawalColor = Color.FromArgb("#C62828");

    public SavingsEntryDto Entry => entry;
    public string Title => entry.Amount < 0 ? "Taken out" : "Added";
    public string AmountText => (entry.Amount < 0 ? "−" : "+") + CurrencyText.Format(Math.Abs(entry.Amount), currency);
    public Color AmountColor => entry.Amount < 0 ? WithdrawalColor : DepositColor;
    public DateTime Date => entry.Date.ToDateTime(TimeOnly.MinValue);

    /// <summary>The note and, in shared budgets, who booked it.</summary>
    public string Details => string.Join(" · ", new[] { entry.Note, entry.CreatedBy is { Length: > 0 } by ? $"by {by}" : null }
        .Where(s => !string.IsNullOrWhiteSpace(s)));

    public bool HasDetails => Details.Length > 0;
}
