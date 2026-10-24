using System;
using System.Linq;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

using MissionControl.Views.Dialogs;

namespace MissionControl.Services;

public class AvaloniaDialogService : IDialogService, IFileDialogService
{
    private static Window? GetOwnerWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.Windows.FirstOrDefault(w => w.IsActive) ?? desktop.MainWindow;
        }

        return null;
    }

    private static IStorageProvider? GetStorageProvider()
    {
        var owner = GetOwnerWindow();
        return owner?.StorageProvider;
    }

    public async Task ShowMessageAsync(string title, string message)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            await Dispatcher.UIThread.InvokeAsync(() => ShowMessageAsync(title, message));
            return;
        }

        var owner = GetOwnerWindow();
        var dialog = new MessageDialogWindow(title, message);

        if (owner is null)
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dialog.Show();
            return;
        }

        await dialog.ShowDialog(owner);
    }

    public async Task<string?> OpenFileDialogAsync(string title, string extensionFilter, string filterName)
    {
        var storage = GetStorageProvider();
        if (storage is null)
        {
            return null;
        }

        var ext = extensionFilter.TrimStart('.');
        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType(filterName)
                {
                    Patterns = [$"*.{ext}"]
                }
            ]
        });

        if (files.Count == 0)
        {
            return null;
        }

        return files[0].TryGetLocalPath() ?? files[0].Path.LocalPath;
    }

    public async Task<string?> SaveFileDialogAsync(string title, string defaultFileName, string defaultExtension)
    {
        var storage = GetStorageProvider();
        if (storage is null)
        {
            return null;
        }

        var ext = defaultExtension.TrimStart('.');
        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = defaultFileName,
            DefaultExtension = ext
        });

        if (file is null)
        {
            return null;
        }

        return file.TryGetLocalPath() ?? file.Path.LocalPath;
    }
}
