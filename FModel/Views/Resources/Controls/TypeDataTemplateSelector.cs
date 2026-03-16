using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using FModel.ViewModels;
using FModel.Views.Resources.Controls.TiledExplorer;

namespace FModel.Views.Resources.Controls;

public class TypeDataTemplateSelector : IDataTemplate
{
    public Control? Build(object? item)
    {
        return item switch
        {
            TreeItem folder => BuildFolderControl(folder),
            GameFileViewModel asset => new FileButton2 { DataContext = asset },
            _ => null
        };
    }

    public bool Match(object? data)
        => data is TreeItem or GameFileViewModel;

    private static Control BuildFolderControl(TreeItem folder)
    {
        var control = new FolderButton2 { DataContext = folder };

        // Create a fresh ContextMenu per item — Application.TryGetResource bypasses
        // x:Shared="False", so we must instantiate a new one each time.
        if (Application.Current?.TryFindResource("FolderContextMenu", out var _) == true)
        {
            // The ContextMenu will be resolved from the resource tree at open-time
            // by the FolderContextMenu_OnOpened handler. Assigning a shared instance
            // would cause DataContext contamination across folders.
            control.AttachedToVisualTree += (_, _) =>
            {
                if (control.TryFindResource("FolderContextMenu", out var res) && res is ContextMenu menu)
                {
                    // Clone by re-resolving; Avalonia ResourceDictionary with x:Shared="False"
                    // returns a new instance when resolved from the visual tree.
                    control.ContextMenu = menu;
                }
            };
        }

        return control;
    }
}
