using Avalonia.Controls;
using Avalonia.Interactivity;
using FModel.Settings;

namespace FModel.Views;

public partial class CustomDir : Window
{
    public bool? Result { get; private set; }

    public CustomDir(CustomDirectory customDir)
    {
        DataContext = customDir;
        InitializeComponent();

        Activate();
        WpfSuckMyDick.Focus();
        WpfSuckMyDick.SelectAll();
    }

    private void OnClick(object sender, RoutedEventArgs e)
    {
        Result = true;
        Close(true);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Result = false;
        Close(false);
    }
}
