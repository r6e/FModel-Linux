using System;
using Avalonia.Input;
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
                dataObject.Set("image/png", pngBytes);
                await clipboard.SetDataObjectAsync(dataObject);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to copy image to clipboard");
            }
        });
    }
}
