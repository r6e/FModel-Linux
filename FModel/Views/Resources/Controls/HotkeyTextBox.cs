using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using FModel.Framework;

namespace FModel.Views.Resources.Controls;

/// <summary>
/// Read-only TextBox that displays and captures a keyboard hotkey.
/// Based on https://tyrrrz.me/blog/hotkey-editor-control-in-wpf, ported to Avalonia.
/// </summary>
public class HotkeyTextBox : TextBox
{
    public static readonly StyledProperty<Hotkey> HotKeyProperty =
        AvaloniaProperty.Register<HotkeyTextBox, Hotkey>(
            nameof(HotKey),
            defaultValue: new Hotkey(Key.None),
            defaultBindingMode: BindingMode.TwoWay);

    public Hotkey HotKey
    {
        get => GetValue(HotKeyProperty);
        set => SetValue(HotKeyProperty, value);
    }

    static HotkeyTextBox()
    {
        HotKeyProperty.Changed.AddClassHandler<HotkeyTextBox>(
            (control, _) => control.Text = control.HotKey.ToString());
    }

    public HotkeyTextBox()
    {
        IsReadOnly = true;
        // Remove the default context menu (Cut/Copy/Paste are meaningless on a read-only hotkey box).
        ContextMenu = null;
        Text = HotKey.ToString();
    }

    private static bool HasKeyChar(Key key) =>
        // A – Z
        key is >= Key.A and <= Key.Z or
        // 0 – 9
        Key.D0 and <= Key.D9 or
        // Numpad 0 – 9
        Key.NumPad0 and <= Key.NumPad9 or
        // Punctuation / symbols
        Key.OemQuestion or Key.OemQuotes or Key.OemPlus or
        Key.OemOpenBrackets or Key.OemCloseBrackets or Key.OemMinus or
        Key.Oem1 or Key.Oem5 or Key.Oem7 or
        Key.OemPeriod or Key.OemComma or
        Key.Add or Key.Divide or Key.Multiply or Key.Subtract or Key.Decimal or
        Key.Oem102;

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // Swallow every key press — this control only captures, never inputs text.
        e.Handled = true;

        var modifiers = e.KeyModifiers;
        var key = e.Key;

        switch (key)
        {
            // Nothing pressed.
            case Key.None:
                return;
            // Delete / Backspace / Escape without modifiers → clear the hotkey.
            case Key.Delete or Key.Back or Key.Escape when modifiers == KeyModifiers.None:
                HotKey = new Hotkey(Key.None);
                return;
            // Modifier-only key presses are not valid hotkeys.
            case Key.LeftCtrl:
            case Key.RightCtrl:
            case Key.LeftAlt:
            case Key.RightAlt:
            case Key.LeftShift:
            case Key.RightShift:
            case Key.LWin:
            case Key.RWin:
            case Key.Clear:
            case Key.Apps:
            // Enter / Space / Tab without modifiers — ignore.
            case Key.Enter or Key.Space or Key.Tab when modifiers == KeyModifiers.None:
                return;
            default:
                HotKey = new Hotkey(key, modifiers);
                break;
        }
    }
}