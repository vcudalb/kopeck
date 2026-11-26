using System.Windows.Input;

namespace Kopeck.Mobile.Views.Charts;

/// <summary>
/// Hosts an <see cref="IChart"/>: redraws whenever a new chart object is bound, and turns a tap into
/// <see cref="TapCommand"/> with the index of the item under the finger.
/// </summary>
public sealed class ChartView : GraphicsView
{
    public static readonly BindableProperty ChartProperty = BindableProperty.Create(
        nameof(Chart), typeof(IChart), typeof(ChartView), propertyChanged: (b, _, value) =>
        {
            var view = (ChartView)b;
            view.Drawable = value as IDrawable;
            view.Invalidate();
        });

    public static readonly BindableProperty TapCommandProperty =
        BindableProperty.Create(nameof(TapCommand), typeof(ICommand), typeof(ChartView));

    public ChartView()
    {
        EndInteraction += (_, e) =>
        {
            if (Chart is null || TapCommand is null || !e.IsInsideBounds || e.Touches.Length == 0) return;
            var index = Chart.HitTest(e.Touches[0], new RectF(0, 0, (float)Width, (float)Height));
            if (index is { } i && TapCommand.CanExecute(i)) TapCommand.Execute(i);
        };
    }

    public IChart? Chart
    {
        get => (IChart?)GetValue(ChartProperty);
        set => SetValue(ChartProperty, value);
    }

    public ICommand? TapCommand
    {
        get => (ICommand?)GetValue(TapCommandProperty);
        set => SetValue(TapCommandProperty, value);
    }
}
