using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace FModel.Views.Resources.Controls;

/// <summary>
/// Avalonia replacement for WPF MagnifierAdorner.
/// Placed in the AdornerLayer as a Canvas that positions the <see cref="Magnifier"/> at the cursor.
/// </summary>
public class MagnifierAdorner : Canvas
{
    private readonly Control _adornedElement;
    private readonly Magnifier _magnifier;
    private Point _currentPointerPosition;
    private double _currentZoomFactor;

    public MagnifierAdorner(Control adornedElement, Magnifier magnifier)
    {
        _adornedElement = adornedElement;
        _magnifier = magnifier;
        _currentZoomFactor = magnifier.ZoomFactor;

        // The canvas must be transparent to hit-testing so pointer events reach the adorned control.
        IsHitTestVisible = false;

        Children.Add(_magnifier);
        UpdateViewBox();

        // Subscribe to pointer-move on the adorned element to track cursor position.
        _adornedElement.PointerMoved += OnAdornedElementPointerMoved;
    }

    public void Detach()
    {
        _adornedElement.PointerMoved -= OnAdornedElementPointerMoved;
    }

    private void OnAdornedElementPointerMoved(object? sender, PointerEventArgs e)
    {
        // Position relative to this adorner canvas (same coordinate space as the adorner layer).
        var pt = e.GetPosition(this);

        if (_currentPointerPosition == pt && _magnifier.ZoomFactor == _currentZoomFactor)
            return;

        if (_magnifier.IsFrozen)
            return;

        _currentPointerPosition = pt;
        _currentZoomFactor = _magnifier.ZoomFactor;

        UpdateViewBox();
        PositionMagnifier();
    }

    public void UpdateViewBox()
    {
        var location = CalculateViewBoxLocation();
        _magnifier.ViewBox = new Rect(location, _magnifier.ViewBox.Size);
        _magnifier.UpdateViewBox();
    }

    private Point CalculateViewBoxLocation()
    {
        // Position of the cursor relative to the adorner canvas.
        var adornerPos = _currentPointerPosition;
        // Position of the cursor relative to the adorned element.
        var elementPos = _adornedElement.PointToClient(PointToScreen(adornerPos));

        var offsetX = elementPos.X - adornerPos.X;
        var offsetY = elementPos.Y - adornerPos.Y;

        // Account for the target control's offset within its parent coordinate space.
        Point parentOffset = default;
        if (_magnifier.Target != null)
        {
            var offsetVec = _magnifier.Target.TranslatePoint(default, _adornedElement);
            if (offsetVec.HasValue)
                parentOffset = offsetVec.Value;
        }

        var left = _currentPointerPosition.X - (_magnifier.ViewBox.Width / 2 + offsetX) + parentOffset.X;
        var top  = _currentPointerPosition.Y - (_magnifier.ViewBox.Height / 2 + offsetY) + parentOffset.Y;
        return new Point(left, top);
    }

    private void PositionMagnifier()
    {
        var x = _currentPointerPosition.X - _magnifier.Width / 2;
        var y = _currentPointerPosition.Y - _magnifier.Height / 2;
        SetLeft(_magnifier, x);
        SetTop(_magnifier, y);
    }
}