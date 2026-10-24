using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MissionControl.Constants;
using MissionControl.Services;

using Travelling;

namespace MissionControl.ViewModels;

public partial class TravelerDetailsViewModel(IDialogService dialogService, IFileDialogService fileDialogService) : ViewModelBase
{
    private readonly IDialogService _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
    private readonly IFileDialogService _fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));

    [ObservableProperty]
    private string _travelerName = string.Empty;

    [ObservableProperty]
    private string _travelerLocation = string.Empty;

    [ObservableProperty]
    private string _currentTravelerDisplay = AppMessages.DefaultTravelerStatus;

    [ObservableProperty]
    private Traveler? _currentTraveler;

    [ObservableProperty]
    private string _loadedTravelerInfo = AppMessages.DefaultTravelerStatus;

    public event Action<Traveler?>? TravelerChanged;

    [RelayCommand]
    public async Task CreateTravelerAsync()
    {
        if (string.IsNullOrWhiteSpace(TravelerName) || string.IsNullOrWhiteSpace(TravelerLocation))
        {
            string errorMessage = AppMessages.FieldCannotBeEmpty(
                $"{nameof(TravelerName)} and {nameof(TravelerLocation)}"
            );

            await _dialogService.ShowMessageAsync(DialogTitles.ValidationError, errorMessage);
            return;
        }

        var traveler = new Traveler(TravelerName.Trim());
        traveler.SetLocation(TravelerLocation.Trim());

        SetTraveler(traveler);

        string successMessage = AppMessages.TravelerCreatedSuccess(traveler.GetName());
        await _dialogService.ShowMessageAsync(DialogTitles.Success, successMessage);
    }

    [RelayCommand]
    public async Task SaveTravelerAsync()
    {
        var traveler = CurrentTraveler;
        if (traveler == null)
        {
            await _dialogService.ShowMessageAsync(DialogTitles.Error, AppMessages.NoTravelerToSaveError);
            return;
        }

        var filePath = await _fileDialogService.SaveFileDialogAsync(
            DialogTitles.SaveTraveler,
            FileFilterConstants.DefaultTravelerFileName,
            FileFilterConstants.JsonExtension);

        if (string.IsNullOrEmpty(filePath))
        {
            return;
        }

        await _dialogService.ExecuteWithDialogAsync(
            action: () =>
            {
                traveler.SaveToFile(filePath);
                return AppMessages.TravelerSavedSuccess(filePath);
            },
            successTitle: DialogTitles.Saved,
            errorTitle: DialogTitles.SaveError,
            formatError: ex => AppMessages.FailedToSaveFileError(ex.Message));
    }

    [RelayCommand]
    public async Task LoadTravelerAsync()
    {
        var filePath = await _fileDialogService.OpenFileDialogAsync(
            DialogTitles.LoadTraveler,
            FileFilterConstants.JsonExtension,
            FileFilterConstants.JsonFilterDescription);

        if (string.IsNullOrEmpty(filePath)) return;

        await _dialogService.ExecuteWithDialogAsync(
            action: () =>
            {
                var loaded = Traveler.LoadFromFile(filePath);
                SetTraveler(loaded);
                LoadedTravelerInfo = $"Traveler: {Path.GetFileName(filePath)}";
                return AppMessages.TravelerLoadedSuccess(loaded.GetName());
            },
            successTitle: DialogTitles.TravelerLoaded,
            errorTitle: DialogTitles.LoadError,
            formatError: ex => AppMessages.FailedToLoadTravelerError(ex.Message),
            onError: () =>
            {
                SetTraveler(null);
                LoadedTravelerInfo = "Failed to load traveler";
            });
    }

    [RelayCommand]
    public async Task DeleteTraveler()
    {
        if (CurrentTraveler is null)
        {
            await _dialogService.ShowMessageAsync(DialogTitles.Error, AppMessages.NoTravelerToDeleteError);
            return;
        }

        string previousName = CurrentTraveler.GetName();
        SetTraveler(null);

        TravelerName = string.Empty;
        TravelerLocation = string.Empty;

        string successMessage = AppMessages.TravelerDeletedSuccess(previousName);
        await _dialogService.ShowMessageAsync(DialogTitles.Success, successMessage);
    }

    public void SetTraveler(Traveler? traveler)
    {
        CurrentTraveler = traveler;

        if (traveler is null)
        {
            CurrentTravelerDisplay = AppMessages.DefaultTravelerStatus;
            TravelerChanged?.Invoke(null);
            return;
        }

        TravelerName = traveler.GetName();
        TravelerLocation = traveler.GetLocation() ?? string.Empty;
        CurrentTravelerDisplay = $"Traveler: {traveler.GetName()} | Location: {traveler.GetLocation()}";

        TravelerChanged?.Invoke(CurrentTraveler);
    }
}
