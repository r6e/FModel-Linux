using Avalonia;
using Avalonia.Controls;

namespace FModel.Views.Resources.Controls;

public sealed class TreeViewItemBehavior
{
    public static bool GetIsBroughtIntoViewWhenSelected(TreeViewItem treeViewItem)
    {
        return treeViewItem.GetValue(IsBroughtIntoViewWhenSelectedProperty);
    }

    public static void SetIsBroughtIntoViewWhenSelected(TreeViewItem treeViewItem, bool value)
    {
        treeViewItem.SetValue(IsBroughtIntoViewWhenSelectedProperty, value);
    }

    public static readonly AttachedProperty<bool> IsBroughtIntoViewWhenSelectedProperty =
        AvaloniaProperty.RegisterAttached<TreeViewItemBehavior, TreeViewItem, bool>("IsBroughtIntoViewWhenSelected");

    static TreeViewItemBehavior()
    {
        IsBroughtIntoViewWhenSelectedProperty.Changed.AddClassHandler<TreeViewItem>(OnIsBroughtIntoViewWhenSelectedChanged);
    }

    private static void OnIsBroughtIntoViewWhenSelectedChanged(TreeViewItem item, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is not bool value)
            return;

        if (value)
            item.PropertyChanged += OnTreeViewItemPropertyChanged;
        else
            item.PropertyChanged -= OnTreeViewItemPropertyChanged;
    }

    private static void OnTreeViewItemPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TreeViewItem.IsSelectedProperty && e.NewValue is true && sender is TreeViewItem item)
        {
            item.BringIntoView();
        }
    }
}
