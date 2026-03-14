using System;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace FModel.Views.Resources.Controls;

/// <summary>
/// Provides a bindable <see cref="VerticalOffsetProperty"/> attached property for
/// <see cref="ScrollViewer"/>.
/// When set, scrolls the viewer to that offset immediately and keeps the property
/// in sync as the user scrolls (two-way).
/// Replaces the WPF <c>DependencyProperty</c>-based implementation.
/// </summary>
public static class CustomScrollViewer
{
    // Tracks which ScrollViewer instances already have a ScrollChanged subscription,
    // using a weak key so GC can reclaim viewers that leave the visual tree.
    private static readonly ConditionalWeakTable<ScrollViewer, object> _subscribed = new();

    /// <summary>Bindable vertical-offset attached property.</summary>
    public static readonly AttachedProperty<double> VerticalOffsetProperty =
        AvaloniaProperty.RegisterAttached<ScrollViewer, double>(
            "VerticalOffset",
            typeof(CustomScrollViewer),
            defaultValue: double.NaN,
            inherits: false,
            defaultBindingMode: BindingMode.TwoWay);

    public static double GetVerticalOffset(ScrollViewer viewer)
        => viewer.GetValue(VerticalOffsetProperty);

    public static void SetVerticalOffset(ScrollViewer viewer, double value)
        => viewer.SetValue(VerticalOffsetProperty, value);

    static CustomScrollViewer()
    {
        VerticalOffsetProperty.Changed.AddClassHandler<ScrollViewer>(OnVerticalOffsetChanged);
    }

    private static void OnVerticalOffsetChanged(ScrollViewer viewer, AvaloniaPropertyChangedEventArgs e)
    {
        var value = e.GetNewValue<double>();
        if (double.IsNaN(value))
            return;

        // Short-circuit re-entrancy: the ScrollChanged handler below calls SetCurrentValue,
        // which re-triggers this callback with the viewer's current position.  If the viewer
        // is already at the requested offset there is nothing to do.
        if (Math.Abs(viewer.Offset.Y - value) < 1e-6)
            return;

        // Scroll immediately when the property is set from a binding.
        viewer.Offset = viewer.Offset.WithY(value);

        // Subscribe once per ScrollViewer instance to sync the property back when the user
        // scrolls.  ConditionalWeakTable keeps the key weak so the viewer can be GC'd normally.
        if (_subscribed.TryGetValue(viewer, out _))
            return;

        _subscribed.Add(viewer, null);
        viewer.ScrollChanged += (_, se) =>
        {
            if (se.OffsetDelta.Y == 0)
                return;
            // Update the attached property so two-way bindings stay in sync.
            // The re-entrancy guard above prevents an infinite update loop.
            viewer.SetCurrentValue(VerticalOffsetProperty, viewer.Offset.Y);
        };
    }
}
