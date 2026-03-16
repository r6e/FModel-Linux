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

        // Resolve context menu from visual-tree resources so x:Shared="False"
        // returns a fresh instance per folder control.
        control.AttachedToVisualTree += (_, _) =>
        {
            if (control.TryFindResource("FolderContextMenu", out var res) && res is ContextMenu menu)
            {
                control.ContextMenu = menu;
            }
        };

        return control;
    }
}
