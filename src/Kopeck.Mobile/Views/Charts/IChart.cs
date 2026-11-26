namespace Kopeck.Mobile.Views.Charts;

/// <summary>A drawable that can tell which data item (if any) sits under a tap.</summary>
public interface IChart : IDrawable
{
    int? HitTest(PointF point, RectF bounds);
}
