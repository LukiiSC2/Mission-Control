using System;
using System.Threading.Tasks;

namespace MissionControl.Services;

public static class DialogServiceExtensions
{
    public static async Task ExecuteWithDialogAsync(
        this IDialogService dialogService,
        Func<string> action,
        string successTitle,
        string errorTitle,
        Func<Exception, string> formatError,
        Action? onError = null)
    {
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            string successMessage = action();
            await dialogService.ShowMessageAsync(successTitle, successMessage);
        }
        catch (Exception ex)
        {
            onError?.Invoke();
            string errorMessage = formatError(ex);
            await dialogService.ShowMessageAsync(errorTitle, errorMessage);
        }
    }
}
