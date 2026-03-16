using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using FModel.Framework;
using FModel.Settings;
using FModel.Views;

namespace FModel.ViewModels.Commands;

public class AddEditDirectoryCommand : ViewModelCommand<CustomDirectoriesViewModel>
{
    public AddEditDirectoryCommand(CustomDirectoriesViewModel contextViewModel) : base(contextViewModel)
    {
    }

    public override async void Execute(CustomDirectoriesViewModel contextViewModel, object parameter)
    {
        if (parameter is not CustomDirectory customDir)
            customDir = new CustomDirectory();

        var index = contextViewModel.GetIndex(customDir);
        var input = new CustomDir(customDir);
        var owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        if (owner == null)
            return;

        var result = await input.ShowDialog<bool?>(owner);

        if (result is not true || string.IsNullOrEmpty(customDir.Header) && string.IsNullOrEmpty(customDir.DirectoryPath))
            return;

        if (index > 1)
            contextViewModel.Edit(index, customDir);
        else
            contextViewModel.Add(customDir);
    }
}
