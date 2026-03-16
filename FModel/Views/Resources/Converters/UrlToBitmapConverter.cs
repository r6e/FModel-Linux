using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace FModel.Views.Resources.Converters;

/// <summary>
/// Converts an HTTP(S) URL string to an Avalonia <see cref="Bitmap"/>.
/// Downloads the image synchronously on first use (binding evaluation).
/// Returns null on failure so the UI does not crash.
/// </summary>
public class UrlToBitmapConverter : IValueConverter
{
  public static readonly UrlToBitmapConverter Instance = new();
  private static readonly HttpClient _http = new();

  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    if (value is not string url || string.IsNullOrWhiteSpace(url))
      return null;

    try
    {
      using var stream = _http.GetStreamAsync(url).GetAwaiter().GetResult();
      var ms = new MemoryStream();
      stream.CopyTo(ms);
      ms.Position = 0;
      return new Bitmap(ms);
    }
    catch
    {
      return null;
    }
  }

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
      => throw new NotImplementedException();
}
