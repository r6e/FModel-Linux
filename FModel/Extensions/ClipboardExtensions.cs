using System;
using System.IO;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using FModel.Views;
using Serilog;

namespace FModel.Extensions;

public static class ClipboardExtensions
{
    /// <summary>
    /// Copies PNG image bytes to the system clipboard. Fire-and-forget; runs on the UI thread.
    /// </summary>
    public static void SetImage(byte[] pngBytes)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                var clipboard = MainWindow.YesWeCats?.Clipboard;
                if (clipboard == null)
                    return;

                var dataObject = new DataObject();
                // Keep both MIME and generic bitmap formats for better cross-app paste compatibility.
                dataObject.Set("image/png", pngBytes);
                dataObject.Set("PNG", pngBytes);
                dataObject.Set(DataFormats.Bitmap, new Bitmap(new MemoryStream(pngBytes)));
                await clipboard.SetDataObjectAsync(dataObject);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to copy image to clipboard");
            }
        });
    }
}
