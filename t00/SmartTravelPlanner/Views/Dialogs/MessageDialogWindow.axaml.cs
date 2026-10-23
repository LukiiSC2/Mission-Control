using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MissionControl.Views.Dialogs;

public partial class MessageDialogWindow : Window
{
    public MessageDialogWindow()
    {
        InitializeComponent();
    }

    public MessageDialogWindow(string title, string message) : this()
    {
        Title = title;
        TitleBlock.Text = title;
        ContentBlock.Text = message;
    }

    private void OnOkButtonClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
