using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FModel.Views.Resources.Controls;

public partial class ImagePopout : Window
{
    public ImagePopout()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        var panel = this.FindControl<DockPanel>("RootPanel");
        if (panel != null && MagnifierManager.GetMagnifier(panel) == null)
        {
            MagnifierManager.SetMagnifier(panel, new Magnifier { Radius = 150, ZoomFactor = 0.7 });
        }
    }
}