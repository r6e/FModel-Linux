using Avalonia;
using Avalonia.Controls;

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

        if (MagnifierManager.GetMagnifier(RootPanel) == null)
        {
            MagnifierManager.SetMagnifier(RootPanel, new Magnifier { Radius = 150, ZoomFactor = 0.7 });
        }
    }
}
