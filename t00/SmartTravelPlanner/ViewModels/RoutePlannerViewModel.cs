using System;
using System.IO;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MissionControl.Services;
using MissionControl.Constants;

using Travelling;

namespace MissionControl.ViewModels;
    
public partial class RoutePlannerViewModel(
    IDialogService dialogService,
    IFileDialogService fileDialogService,
    Func<Traveler?> getCurrentTraveler) : ViewModelBase
{
    private readonly IDialogService _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
    private readonly IFileDialogService _fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
    private readonly Func<Traveler?> _getCurrentTraveler = getCurrentTraveler ?? throw new ArgumentNullException(nameof(getCurrentTraveler));

    [ObservableProperty]
    private string _destinationCity = string.Empty;

    [ObservableProperty]
    private string _loadedMapInfo = AppMessages.DefaultMapStatus;

    [ObservableProperty]
    private string _totalDistanceText = AppMessages.ZeroDistance;

    [ObservableProperty]
    private string _fullRouteDisplay = AppMessages.DefaultRouteStatus;

    [ObservableProperty]
    private CityGraph? _currentMap;

    public ObservableCollection<string> RouteCities { get; } = new();
    
    [RelayCommand]
    public async Task LoadMapAsync()
    {
        var filePath = await _fileDialogService.OpenFileDialogAsync(
            DialogTitles.LoadCityMap,
            FileFilterConstants.TxtExtension,
            FileFilterConstants.TxtFilterDescription);
        if (string.IsNullOrEmpty(filePath)) return;

        try
        {
            CurrentMap = CityGraph.LoadFromFile(filePath);
            LoadedMapInfo = $"Map: {Path.GetFileName(filePath)}";

            string successMessage = AppMessages.MapLoadedSuccess(filePath);
            await _dialogService.ShowMessageAsync(DialogTitles.MapLoaded, successMessage);
        }
        catch (Exception ex)
        {
            CurrentMap = null;
            LoadedMapInfo = "Failed to load map";

            string errorMessage = AppMessages.FailedToLoadMapError(ex.Message);
            await _dialogService.ShowMessageAsync(DialogTitles.MapError, errorMessage);
        }
    }

    [RelayCommand]
    public async Task PlanRouteAsync()
    {
        var traveler = _getCurrentTraveler();

        if (traveler == null)
        {
            await _dialogService.ShowMessageAsync(DialogTitles.Error, AppMessages.TravelerNotCreatedError);
            return;
        }

        if (CurrentMap == null)
        {
            await _dialogService.ShowMessageAsync(DialogTitles.Error, AppMessages.MapNotLoadedError);
            return;
        }

        if (string.IsNullOrWhiteSpace(DestinationCity))
        {
            string emptyFieldError = AppMessages.FieldCannotBeEmpty(nameof(DestinationCity));
            await _dialogService.ShowMessageAsync(DialogTitles.ValidationError, emptyFieldError);
            return;
        }

        bool success = traveler.PlanRouteTo(DestinationCity.Trim(), CurrentMap);

        if (!success || traveler.GetStopCount() == 0)
        {
            string unreachableError = AppMessages.DestinationUnreachableError(DestinationCity);
            await _dialogService.ShowMessageAsync(DialogTitles.RouteNotFound, unreachableError);
            return;
        }

        UpdateRouteDisplay(traveler);
    }
    [RelayCommand]
    public async Task ClearRouteAsync()
    {
        var traveler = _getCurrentTraveler();
        traveler?.ClearRoute();

        RouteCities.Clear();
        FullRouteDisplay = AppMessages.RouteClearedStatus;
        TotalDistanceText = AppMessages.ZeroDistance;
    }

    public void UpdateRouteDisplay(Traveler traveler)
    {
        RouteCities.Clear();
        foreach (var city in traveler.Route)
        {
            RouteCities.Add(city);
        }

        if (CurrentMap != null && traveler.Route.Count >= 2)
        {
            int distance = CurrentMap.GetPathDistance(traveler.Route);
            TotalDistanceText = $"{distance} km";
        }
        else
        {
            TotalDistanceText = traveler.Route.Count == 1 ? AppMessages.ZeroDistance : "—";
        }

        FullRouteDisplay = traveler.GetRoute();
    }
}