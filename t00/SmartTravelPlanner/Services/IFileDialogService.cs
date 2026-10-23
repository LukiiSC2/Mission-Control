using System.Threading.Tasks;

namespace MissionControl.Services;

public interface IFileDialogService
{
    Task<string?> OpenFileDialogAsync(string title, string extensionFilter, string filterName);
    Task<string?> SaveFileDialogAsync(string title, string defaultFileName, string defaultExtension);
}
