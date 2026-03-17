using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace FModel.Views.Resources.Converters;

/// <summary>
/// Converts <see cref="ELoadingMode"/> to <see cref="SelectionMode"/>.
/// "Multiple" mode uses Extended (multi-select), all other modes use Single.
/// </summary>
public class LoadingModeToSelectionModeConverter : IValueConverter
{
    public static readonly LoadingModeToSelectionModeConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            ELoadingMode.Multiple => SelectionMode.Multiple,
            _ => SelectionMode.Single
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
