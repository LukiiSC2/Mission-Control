using System;
using System.Threading.Tasks;

namespace MissionControl.Services;

public static class DialogServiceExtensions
{
    public static Task ExecuteWithDialogAsync(
        this IDialogService dialogService,
        Func<string> action,
        string successTitle,
        string errorTitle,
        Func<Exception, string> formatError,
        Action? onError = null)
    {
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(action);

        return ExecuteCoreAsync(
            dialogService,
            async () =>
            {
                string successMessage = action();
                await dialogService.ShowMessageAsync(successTitle, successMessage);
            },
            errorTitle,
            formatError,
            onError);
    }

    public static Task ExecuteWithErrorDialogAsync(
        this IDialogService dialogService,
        Action action,
        string errorTitle,
        Func<Exception, string>? formatError = null,
        Action? onError = null)
    {
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(action);

        return ExecuteCoreAsync(
            dialogService,
            () =>
            {
                action();
                return Task.CompletedTask;
            },
            errorTitle,
            formatError ?? (ex => ex.Message),
            onError);
    }

    private static async Task ExecuteCoreAsync(
        IDialogService dialogService,
        Func<Task> action,
        string errorTitle,
        Func<Exception, string> formatError,
        Action? onError)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            onError?.Invoke();
            string errorMessage = formatError(ex);
            await dialogService.ShowMessageAsync(errorTitle, errorMessage);
        }
    }
}
