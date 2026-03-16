using System;
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

        // Clean up any existing subscription stored on the item
        if (item.GetValue(SubscriptionProperty) is IDisposable oldSub)
        {
            oldSub.Dispose();
            item.SetValue(SubscriptionProperty, null);
        }

        if (value)
        {
            var sub = item.GetObservable(TreeViewItem.IsSelectedProperty)
                .Subscribe(isSelected =>
                {
                    if (isSelected)
                        item.BringIntoView();
                });
            item.SetValue(SubscriptionProperty, sub);
        }
    }

    /// <summary>
    /// Internal attached property to store the observable subscription for cleanup.
    /// </summary>
    private static readonly AttachedProperty<IDisposable?> SubscriptionProperty =
        AvaloniaProperty.RegisterAttached<TreeViewItemBehavior, TreeViewItem, IDisposable?>("Subscription");
}
