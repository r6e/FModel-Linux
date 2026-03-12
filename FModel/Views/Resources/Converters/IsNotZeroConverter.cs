using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace FModel.Views.Resources.Converters;

public class IsNotZeroConverter : IValueConverter
{
  public static readonly IsNotZeroConverter Instance = new();

  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
      => value is int i ? i > 0 : value is not null;

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
      => throw new NotImplementedException();
}
