using System.Globalization;

namespace Kopeck.Mobile.Views.Charts;

/// <summary>One bar series of a <see cref="MonthlyBarsChart"/>: 12 values, January first.</summary>
public sealed record BarSeries(string Name, IReadOnlyList<decimal> Values, Color Color);

/// <summary>
/// Twelve months of columns on one axis: one or two bar series side by side, plus an optional marker series drawn as a
/// short line across each month (e.g. the planned amount). Bars are thin with 4px rounded tops anchored to the baseline,
/// grid lines are recessive, and the tapped month is highlighted (its values are shown as text by the page).
/// </summary>
public sealed class MonthlyBarsChart(IReadOnlyList<BarSeries> bars, IReadOnlyList<decimal>? markers, int? selectedMonth) : IChart
{
    private const float AxisWidth = 34;   // left gutter for the value labels
    private const float LabelHeight = 18; // bottom gutter for the month names
    private const float BarGap = 2;
    private const float Radius = 4;
    private const float FontSize = 10;

    private static readonly string[] MonthNames = Enumerable.Range(1, 12).Select(ShortMonth).ToArray();

    /// <summary>At most three letters ("Jan"), so twelve labels fit across a phone screen.</summary>
    public static string ShortMonth(int month)
    {
        var name = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(month).TrimEnd('.');
        return name.Length <= 3 ? name : name[..3];
    }

    public void Draw(ICanvas canvas, RectF bounds)
    {
        var plot = Plot(bounds);
        var max = Math.Max(bars.SelectMany(s => s.Values).Concat(markers ?? []).DefaultIfEmpty(0).Max(), 0);
        var (step, top) = NiceScale(max);

        canvas.FontSize = FontSize;
        canvas.FontColor = ChartPalette.Text;

        // grid lines and value labels: 0 and up to three steps
        for (var value = 0m; value <= top; value += step)
        {
            var y = plot.Bottom - (float)(value / top) * plot.Height;
            canvas.StrokeColor = ChartPalette.Grid;
            canvas.StrokeSize = 1;
            canvas.DrawLine(plot.Left, y, plot.Right, y);
            canvas.DrawString(Compact(value), bounds.Left, y - 7, AxisWidth - 6, 14, HorizontalAlignment.Right, VerticalAlignment.Center);
        }

        var slot = plot.Width / 12;
        var groupWidth = slot * 0.64f;
        var barWidth = (groupWidth - BarGap * (bars.Count - 1)) / Math.Max(bars.Count, 1);

        for (var m = 0; m < 12; m++)
        {
            var slotLeft = plot.Left + m * slot;
            if (m == selectedMonth)
            {
                canvas.FillColor = ChartPalette.Highlight;
                canvas.FillRoundedRectangle(slotLeft + 1, plot.Top, slot - 2, plot.Height + LabelHeight, 6);
            }

            var x = slotLeft + (slot - groupWidth) / 2;
            foreach (var series in bars)
            {
                var value = series.Values[m];
                if (value > 0)
                {
                    var height = Math.Max((float)(value / top) * plot.Height, 2);
                    canvas.FillColor = series.Color;
                    canvas.FillRoundedRectangle(x, plot.Bottom - height, barWidth, height, Radius, Radius, 0, 0);
                }
                x += barWidth + BarGap;
            }

            if (markers is not null && markers[m] > 0)
            {
                var y = plot.Bottom - (float)(markers[m] / top) * plot.Height;
                canvas.StrokeColor = ChartPalette.Marker;
                canvas.StrokeSize = 2;
                canvas.StrokeLineCap = LineCap.Round;
                canvas.DrawLine(slotLeft + slot * 0.12f, y, slotLeft + slot * 0.88f, y);
            }

            canvas.FontColor = m == selectedMonth ? ChartPalette.Marker : ChartPalette.Text;
            canvas.DrawString(MonthNames[m], slotLeft, plot.Bottom + 2, slot, LabelHeight - 2, HorizontalAlignment.Center, VerticalAlignment.Center);
        }
    }

    /// <summary>The month under the tap (the whole column is the target, not just the bar).</summary>
    public int? HitTest(PointF point, RectF bounds)
    {
        var plot = Plot(bounds);
        if (point.X < plot.Left || point.X > plot.Right) return null;
        return Math.Clamp((int)((point.X - plot.Left) / (plot.Width / 12)), 0, 11);
    }

    private static RectF Plot(RectF bounds) =>
        new(bounds.Left + AxisWidth, bounds.Top + 6, bounds.Width - AxisWidth - 2, bounds.Height - LabelHeight - 6);

    /// <summary>A round step so the axis shows 0 and up to three tidy values above the data.</summary>
    private static (decimal Step, decimal Top) NiceScale(decimal max)
    {
        if (max <= 0) return (1, 3);
        var rough = (double)max / 3;
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(rough)));
        var step = new[] { 1, 2, 2.5, 5, 10 }.Select(f => f * magnitude).First(s => s >= rough);
        var stepM = (decimal)step;
        return (stepM, Math.Ceiling(max / stepM) * stepM);
    }

    private static string Compact(decimal value) => value switch
    {
        >= 1_000_000 => (value / 1_000_000).ToString("0.#", CultureInfo.CurrentCulture) + "M",
        >= 1_000 => (value / 1_000).ToString("0.#", CultureInfo.CurrentCulture) + "k",
        _ => value.ToString("0", CultureInfo.CurrentCulture),
    };
}
