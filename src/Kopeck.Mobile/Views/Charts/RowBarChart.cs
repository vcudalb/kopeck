namespace Kopeck.Mobile.Views.Charts;

/// <summary>A thin bar for one row of a ranked list: <paramref name="fraction"/> of the widest row, in the row's colour.</summary>
public sealed class RowBarChart(double fraction, Color color) : IChart
{
    private const float Radius = 3;

    public void Draw(ICanvas canvas, RectF bounds)
    {
        canvas.FillColor = ChartPalette.Grid;
        canvas.FillRoundedRectangle(bounds, Radius);
        if (fraction <= 0) return;

        canvas.FillColor = color;
        var width = Math.Max((float)Math.Clamp(fraction, 0, 1) * bounds.Width, 2 * Radius);
        canvas.FillRoundedRectangle(bounds.Left, bounds.Top, width, bounds.Height, Radius);
    }

    public int? HitTest(PointF point, RectF bounds) => null;
}
