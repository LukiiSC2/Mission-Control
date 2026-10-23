using System.Threading.Tasks;

namespace MissionControl.Services;

public interface IDialogService
{
    Task ShowMessageAsync(string title, string message);
}
