using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace FModel.Views.Resources.Converters;

/// <summary>
/// Converts <see cref="ELoadingMode"/> to <see cref="SelectionMode"/>.
/// Explicit mapping keeps behavior stable if new loading modes are introduced.
/// </summary>
public class LoadingModeToSelectionModeConverter : IValueConverter
{
    public static readonly LoadingModeToSelectionModeConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            ELoadingMode.Multiple => SelectionMode.Multiple,
            ELoadingMode.All => SelectionMode.Multiple,
            ELoadingMode.AllButNew => SelectionMode.Multiple,
            ELoadingMode.AllButModified => SelectionMode.Multiple,
            ELoadingMode.AllButPatched => SelectionMode.Multiple,
            _ => SelectionMode.Single
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
