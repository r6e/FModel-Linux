using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;

namespace FModel.Views.Resources.Controls;

/// <summary>
/// An editable, filterable <see cref="ComboBox"/> for Avalonia.
/// Based on https://stackoverflow.com/a/58066259/13389331, ported from WPF.
/// </summary>
public class FilterableComboBox : ComboBox
{
    /// <summary>
    /// If true, on lost focus or Enter pressed, clears the text if it is not present in the list.
    /// </summary>
    public static readonly StyledProperty<bool> OnlyValuesInListProperty =
        AvaloniaProperty.Register<FilterableComboBox, bool>(nameof(OnlyValuesInList));

    public bool OnlyValuesInList
    {
        get => GetValue(OnlyValuesInListProperty);
        set => SetValue(OnlyValuesInListProperty, value);
    }

    /// <summary>
    /// Selected item — updates only when focus leaves or Enter is pressed.
    /// </summary>
    public static readonly StyledProperty<object> EffectivelySelectedItemProperty =
        AvaloniaProperty.Register<FilterableComboBox, object>(
            nameof(EffectivelySelectedItem),
            defaultBindingMode: BindingMode.TwoWay);

    public object EffectivelySelectedItem
    {
        get => GetValue(EffectivelySelectedItemProperty);
        set => SetValue(EffectivelySelectedItemProperty, value);
    }

    /// <summary>
    /// Fires on lost focus or Enter pressed when the selected item has changed since the last commit.
    /// </summary>
    public event Action<FilterableComboBox, object> SelectionEffectivelyChanged;

    // -----------------------------------------------------------------------
    // Private state
    // -----------------------------------------------------------------------
    private IEnumerable  _originalSource;
    private bool         _isUpdatingItems;
    private string       _currentFilter    = string.Empty;
    private bool         _textBoxFrozen;
    private TextBox      _editableTextBox;
    private bool         _shouldTriggerSelectedItemChanged;

    // Mirrors WPF UserChange<T>: wraps an action and tracks whether it is a programmatic change.
    private readonly UserChange<bool> _dropDownOpenUC;

    public FilterableComboBox()
    {
        _dropDownOpenUC = new UserChange<bool>(v => IsDropDownOpen = v);

        IsEditable = true;

        DropDownOpened   += OnDropDownOpened;
        SelectionChanged += (_, _) => _shouldTriggerSelectedItemChanged = true;
        SelectionEffectivelyChanged += (_, o) => EffectivelySelectedItem = o;
    }

    // -----------------------------------------------------------------------
    // Template
    // -----------------------------------------------------------------------
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _editableTextBox = e.NameScope.Find<TextBox>("PART_EditableTextBox");
        if (_editableTextBox != null)
            new TextBoxUserChangeTracker(_editableTextBox).UserTextChanged += OnUserTextChanged;
    }

    // -----------------------------------------------------------------------
    // ItemsSource interception — capture original, rebuild filtered view
    // -----------------------------------------------------------------------
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ItemsSourceProperty && !_isUpdatingItems)
        {
            _originalSource = change.GetNewValue<IEnumerable>();
            AttachSourceFilter(_originalSource);
        }
    }

    private void AttachSourceFilter(IEnumerable source)
    {
        // No-op: we copy to a new list on demand in RefreshFilter.
        // Rebind to a fresh list so the control starts unfiltered.
        ApplyFilter();
    }

    // -----------------------------------------------------------------------
    // Key handling
    // -----------------------------------------------------------------------
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Down && !IsDropDownOpen)
        {
            IsDropDownOpen = true;
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            ClearFilter();
            Text = "";
            IsDropDownOpen = true;
        }
        else if (e.Key is Key.Enter or Key.Tab)
        {
            CheckSelectedItem();
            TriggerSelectedItemChanged();
        }
    }

    // -----------------------------------------------------------------------
    // Focus handling — trigger effective selection on focus loss
    // -----------------------------------------------------------------------
    protected override void OnLostFocus(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLostFocus(e);

        // Only commit when focus truly leaves the ComboBox AND its editable part.
        var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
        if (focused == null || (focused != (Avalonia.Input.IInputElement)this &&
                                focused != (Avalonia.Input.IInputElement)_editableTextBox))
        {
            CheckSelectedItem();
            TriggerSelectedItemChanged();
        }
    }

    // -----------------------------------------------------------------------
    // Filter helpers
    // -----------------------------------------------------------------------
    private void OnDropDownOpened(object sender, EventArgs e)
    {
        if (_dropDownOpenUC.IsUserChange)
            ClearFilter();
    }

    private void OnUserTextChanged(object sender, EventArgs e)
    {
        if (_textBoxFrozen) return;

        var tb = _editableTextBox;
        if (tb == null) return;

        var selLen = tb.SelectionEnd - tb.SelectionStart;
        _currentFilter = (tb.SelectionStart + selLen == tb.Text.Length)
            ? tb.Text.Substring(0, tb.SelectionStart).ToLower()
            : tb.Text.ToLower();

        RefreshFilter();
    }

    public void ClearFilter()
    {
        if (string.IsNullOrEmpty(_currentFilter)) return;
        _currentFilter = "";
        ApplyFilter();
    }

    private void RefreshFilter()
    {
        if (_originalSource == null) return;

        FreezeTextBoxState(() =>
        {
            var wasOpen = IsDropDownOpen;
            // Close then re-open to force list refresh (mirrors the WPF view.Refresh() trick).
            _dropDownOpenUC.Set(false);
            ApplyFilter();

            if (!string.IsNullOrEmpty(_currentFilter) || wasOpen)
                _dropDownOpenUC.Set(true);

            // Try to restore SelectedItem if text matches an item..
            if (SelectedItem == null)
            {
                foreach (var item in _originalSource)
                    if (item?.ToString() == Text)
                    {
                        SelectedItem = item;
                        break;
                    }
            }
        });
    }

    private void ApplyFilter()
    {
        _isUpdatingItems = true;
        try
        {
            ItemsSource = string.IsNullOrEmpty(_currentFilter)
                ? _originalSource?.Cast<object>().ToList()
                : _originalSource?.Cast<object>()
                                  .Where(x => x?.ToString()?.ToLower().Contains(_currentFilter) == true)
                                  .ToList();
        }
        finally
        {
            _isUpdatingItems = false;
        }
    }

    private void FreezeTextBoxState(Action action)
    {
        _textBoxFrozen = true;
        var tb       = _editableTextBox;
        var text     = Text;
        var selStart = tb?.SelectionStart ?? 0;
        var selEnd   = tb?.SelectionEnd   ?? 0;
        action();
        Text = text;
        if (tb != null) { tb.SelectionStart = selStart; tb.SelectionEnd = selEnd; }
        _textBoxFrozen = false;
    }

    private void CheckSelectedItem()
    {
        if (OnlyValuesInList)
            Text = SelectedItem?.ToString() ?? "";
    }

    private bool FilterItem(object value)
    {
        if (value == null) return false;
        if (_currentFilter.Length == 0) return true;
        return value.ToString()!.ToLower().Contains(_currentFilter);
    }

    private void TriggerSelectedItemChanged()
    {
        if (!_shouldTriggerSelectedItemChanged) return;
        SelectionEffectivelyChanged?.Invoke(this, SelectedItem);
        _shouldTriggerSelectedItemChanged = false;
    }

    // -----------------------------------------------------------------------
    // Inner helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Detects user-driven text changes in a <see cref="TextBox"/> vs. programmatic ones.
    /// </summary>
    private sealed class TextBoxUserChangeTracker
    {
        private readonly TextBox      _textBox;
        private readonly List<Key>    _pressedKeys = [];
        private          string       _lastText;
        private          bool         _isTextInput;

        public event EventHandler UserTextChanged;

        public TextBoxUserChangeTracker(TextBox textBox)
        {
            _textBox  = textBox;
            _lastText = textBox.Text ?? "";

            textBox.TextInput += (_, _) => _isTextInput = true;

            textBox.TextChanged += (_, e) =>
            {
                var isUserChange = _pressedKeys.Count > 0 || _isTextInput || _lastText == (_textBox.Text ?? "");
                _isTextInput = false;
                _lastText    = _textBox.Text ?? "";
                if (isUserChange)
                    UserTextChanged?.Invoke(this, e);
            };

            textBox.KeyDown += (_, e) =>
            {
                if (e.Key is Key.Back or Key.Space && !_pressedKeys.Contains(e.Key))
                    _pressedKeys.Add(e.Key);

                // Extend back-selection to include previous char (mirrors WPF PreviewKeyDown Back handling).
                if (e.Key == Key.Back)
                {
                    var selLen = _textBox.SelectionEnd - _textBox.SelectionStart;
                    if (_textBox.SelectionStart > 0 && selLen > 0 &&
                        _textBox.SelectionEnd == (_textBox.Text?.Length ?? 0))
                    {
                        _textBox.SelectionStart--;
                        // SelectionEnd stays the same — length effectively +1.
                        e.Handled = true;
                        UserTextChanged?.Invoke(this, e);
                    }
                }
            };

            textBox.KeyUp += (_, e) => _pressedKeys.Remove(e.Key);

            textBox.LostFocus += (_, _) =>
            {
                _pressedKeys.Clear();
                _isTextInput = false;
            };
        }
    }

    /// <summary>Wraps an action and records whether it is a user-initiated change.</summary>
    private sealed class UserChange<T>
    {
        private readonly Action<T> _action;
        public bool IsUserChange { get; private set; } = true;

        public UserChange(Action<T> action) => _action = action;

        public void Set(T val)
        {
            try   { IsUserChange = false; _action(val); }
            finally { IsUserChange = true; }
        }
    }
}
