using System.Text;
using Avalonia.Input;

namespace FModel.Framework;

public class Hotkey : ViewModel
{
    private Key _key;
    public Key Key
    {
        get => _key;
        set => SetProperty(ref _key, value);
    }

    private KeyModifiers _modifiers;
    public KeyModifiers Modifiers
    {
        get => _modifiers;
        set => SetProperty(ref _modifiers, value);
    }

    public Hotkey(Key key, KeyModifiers modifiers = KeyModifiers.None)
    {
        Key = key;
        Modifiers = modifiers;
    }

    /// <summary>Returns true when <paramref name="key"/> and <paramref name="modifiers"/> match this hotkey.</summary>
    public bool IsTriggered(Key key, KeyModifiers modifiers)
    {
        return key == Key && modifiers.HasFlag(Modifiers);
    }

    public override string ToString()
    {
        var str = new StringBuilder();

        if (Modifiers.HasFlag(KeyModifiers.Control))
            str.Append("Ctrl + ");
        if (Modifiers.HasFlag(KeyModifiers.Shift))
            str.Append("Shift + ");
        if (Modifiers.HasFlag(KeyModifiers.Alt))
            str.Append("Alt + ");
        if (Modifiers.HasFlag(KeyModifiers.Meta))
            str.Append("Win + ");

        str.Append(Key);
        return str.ToString();
    }
}