using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using MissionControl.Views.Dialogs;

namespace MissionControl.Services;

public class AvaloniaDialogService : IDialogService, IFileDialogService
{
    private static Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }

    public async Task ShowMessageAsync(string title, string message)
    {
        var owner = GetMainWindow();
        var dialog = new MessageDialogWindow(title, message);

        if (owner != null)
        {
            await dialog.ShowDialog(owner);
        }
        else
        {
            dialog.Show();
        }
    }

    public async Task<string?> OpenFileDialogAsync(string title, string extensionFilter, string filterName)
    {
        var owner = GetMainWindow();
        if (owner == null) return null;

        var topLevel = TopLevel.GetTopLevel(owner);
        if (topLevel?.StorageProvider == null) return null;

        var ext = extensionFilter.TrimStart('.');
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType(filterName)
                {
                    Patterns = new[] { $"*.{ext}" }
                }
            }
        });

        if (files.Count > 0)
        {
            return files[0].TryGetLocalPath() ?? files[0].Path.LocalPath;
        }

        return null;
    }

    public async Task<string?> SaveFileDialogAsync(string title, string defaultFileName, string defaultExtension)
    {
        var owner = GetMainWindow();
        if (owner == null) return null;

        var topLevel = TopLevel.GetTopLevel(owner);
        if (topLevel?.StorageProvider == null) return null;

        var ext = defaultExtension.TrimStart('.');
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = defaultFileName,
            DefaultExtension = ext
        });

        if (file != null)
        {
            return file.TryGetLocalPath() ?? file.Path.LocalPath;
        }

        return null;
    }
}
