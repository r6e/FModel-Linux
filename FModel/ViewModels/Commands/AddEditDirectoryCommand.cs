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
        var sourceDir = parameter as CustomDirectory ?? new CustomDirectory();
        var editableDir = new CustomDirectory(sourceDir.Header, sourceDir.DirectoryPath);

        var index = contextViewModel.GetIndex(sourceDir);
        var input = new CustomDir(editableDir);
        var owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        if (owner == null)
        {
            input.Closed += (_, _) => Apply(input.Result);
            input.Show();
            return;
        }

        var result = await input.ShowDialog<bool?>(owner);
        Apply(result);

        void Apply(bool? dialogResult)
        {
            if (dialogResult is not true || string.IsNullOrEmpty(editableDir.Header) && string.IsNullOrEmpty(editableDir.DirectoryPath))
                return;

            if (index > 1)
                contextViewModel.Edit(index, editableDir);
            else
                contextViewModel.Add(editableDir);
        }
    }
}
