namespace Kopeck.Mobile.Views.Charts;

/// <summary>One part of a <see cref="ShareBarChart"/>.</summary>
public sealed record ShareSegment(decimal Amount, Color Color);

/// <summary>
/// A single horizontal bar split into parts (e.g. spending by category). Parts are separated by a 2px gap in the surface
/// colour and only the bar's two outer ends are rounded. The names and amounts are listed next to it, so the bar never
/// carries identity by colour alone.
/// </summary>
public sealed class ShareBarChart(IReadOnlyList<ShareSegment> segments) : IChart
{
    private const float Gap = 2;
    private const float Radius = 4;

    public void Draw(ICanvas canvas, RectF bounds)
    {
        var total = segments.Sum(s => s.Amount);
        var parts = segments.Where(s => s.Amount > 0).ToList();
        if (total <= 0 || parts.Count == 0)
        {
            canvas.FillColor = ChartPalette.Grid;
            canvas.FillRoundedRectangle(bounds, Radius);
            return;
        }

        var path = new PathF();
        path.AppendRoundedRectangle(bounds, Radius);
        canvas.SaveState();
        canvas.ClipPath(path);

        var usable = bounds.Width - Gap * (parts.Count - 1);
        var x = bounds.Left;
        foreach (var part in parts)
        {
            var width = (float)(part.Amount / total) * usable;
            canvas.FillColor = part.Color;
            canvas.FillRectangle(x, bounds.Top, Math.Max(width, 1), bounds.Height);
            x += width + Gap;
        }

        canvas.RestoreState();
    }

    public int? HitTest(PointF point, RectF bounds) => null;
}
