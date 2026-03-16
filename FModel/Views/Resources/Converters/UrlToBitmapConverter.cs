using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Net.Http;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace FModel.Views.Resources.Converters;

/// <summary>
/// Converts an HTTP(S) URL string to an Avalonia <see cref="Bitmap"/>.
/// Downloads are synchronous (binding evaluation runs on UI thread) but cached
/// by URL so only the first encounter of each unique URL blocks briefly.
/// Returns null on failure so the UI does not crash.
/// </summary>
/// <remarks>
/// A fully asynchronous approach (e.g. AsyncImageLoader.AvaloniaUI NuGet or a ViewModel
/// property with INotifyPropertyChanged) would avoid the initial per-URL block, but is
/// out of scope for this migration PR. The update view typically has 2–5 unique author
/// avatars, making the one-time cost acceptable.
/// </remarks>
public class UrlToBitmapConverter : IValueConverter
{
  public static readonly UrlToBitmapConverter Instance = new();
  private static readonly HttpClient _http = new();
  private static readonly ConcurrentDictionary<string, Bitmap?> _cache = new();

  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    if (value is not string url || string.IsNullOrWhiteSpace(url))
      return null;

    return _cache.GetOrAdd(url, static (key, http) =>
    {
      try
      {
        using var response = http.Send(new HttpRequestMessage(HttpMethod.Get, key));
        response.EnsureSuccessStatusCode();
        using var stream = response.Content.ReadAsStream();
        var ms = new MemoryStream();
        stream.CopyTo(ms);
        ms.Position = 0;
        return new Bitmap(ms);
      }
      catch
      {
        return null;
      }
    }, _http);
  }

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
      => throw new NotImplementedException();
}
