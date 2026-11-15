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

public sealed record MapEdgeItem(string From, string To, int Distance)
{
    public string DistanceText => $"{Distance} km";
}

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
    private string _fullMapDisplay = AppMessages.DefaultMapStatus;

    [ObservableProperty]
    private CityGraph? _currentMap;

    [ObservableProperty]
    private string _cityOne = string.Empty;

    [ObservableProperty]
    private string _cityTwo = string.Empty;

    [ObservableProperty]
    private string _cityDistance = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedEdge))]
    private MapEdgeItem? _selectedEdge;

    [ObservableProperty]
    private bool _hasMapEdges;

    public bool HasSelectedEdge => SelectedEdge is not null;

    public ObservableCollection<string> RouteCities { get; } = new();
    public ObservableCollection<MapEdgeItem> MapEdges { get; } = new();

    [RelayCommand]
    public async Task LoadMapAsync()
    {
        var filePath = await _fileDialogService.OpenFileDialogAsync(
            DialogTitles.LoadCityMap,
            FileFilterConstants.TxtExtension,
            FileFilterConstants.TxtFilterDescription);

        if (string.IsNullOrEmpty(filePath)) return;

        await _dialogService.ExecuteWithDialogAsync(
            action: () =>
            {
                CurrentMap = CityGraph.LoadFromFile(filePath);
                LoadedMapInfo = $"Map: {Path.GetFileName(filePath)}";
                UpdateMapDisplay(CurrentMap);
                return AppMessages.MapLoadedSuccess(filePath);
            },
            successTitle: DialogTitles.MapLoaded,
            errorTitle: DialogTitles.MapError,
            formatError: ex => AppMessages.FailedToLoadMapError(ex.Message),
            onError: () =>
            {
                CurrentMap = null;
                LoadedMapInfo = "Failed to load map";
            });
    }

    [RelayCommand]
    public async Task PlanRouteAsync()
    {
        var traveler = _getCurrentTraveler();

        if (traveler is null)
        {
            await _dialogService.ShowMessageAsync(DialogTitles.Error, AppMessages.TravelerNotCreatedError);
            return;
        }

        if (CurrentMap is null)
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
    public async Task SaveRouteAsync()
    {
        var traveler = _getCurrentTraveler();
        if (traveler is null || traveler.GetStopCount() == 0)
        {
            await _dialogService.ShowMessageAsync(DialogTitles.Error, AppMessages.NoRouteToSaveError);
            return;
        }

        var filePath = await _fileDialogService.SaveFileDialogAsync(
            DialogTitles.SaveRoute,
            FileFilterConstants.DefaultRouteFileName,
            FileFilterConstants.TxtExtension);

        if (string.IsNullOrEmpty(filePath))
        {
            return;
        }

        await _dialogService.ExecuteWithDialogAsync(
            action: () =>
            {
                string content = string.Join(Environment.NewLine,
                    $"Traveler: {traveler.GetName()}",
                    $"Current Location: {traveler.GetLocation() ?? "Unknown"}",
                    $"Route: {traveler.GetRoute()}",
                    $"Total Distance: {TotalDistanceText}");
                File.WriteAllText(filePath, content + Environment.NewLine);
                return AppMessages.RouteSavedSuccess(filePath);
            },
            successTitle: DialogTitles.Saved,
            errorTitle: DialogTitles.SaveError,
            formatError: ex => AppMessages.FailedToSaveRouteError(ex.Message));
    }

    [RelayCommand]
    public void ClearRoute()
    {
        var traveler = _getCurrentTraveler();
        traveler?.ClearRoute();

        RouteCities.Clear();
        FullRouteDisplay = AppMessages.RouteClearedStatus;
        TotalDistanceText = AppMessages.ZeroDistance;
    }

    [RelayCommand]
    public async Task AddEdgeAsync()
    {
        var input = await TryParseEdgeInputAsync();
        if (!input.IsValid)
        {
            return;
        }

        await _dialogService.ExecuteWithErrorDialogAsync(
            action: () =>
            {
                var map = CurrentMap ?? new CityGraph();
                map.AddValidatedEdge(input.CityOne, input.CityTwo, input.Distance);

                if (CurrentMap is null)
                {
                    CurrentMap = map;
                    LoadedMapInfo = AppMessages.CustomMapLoadedInfo;
                }

                ClearRoute();
                UpdateMapDisplay(CurrentMap);
                ClearEdgeInputs();
            },
            errorTitle: DialogTitles.ValidationError);
    }

    [RelayCommand]
    public async Task RemoveEdgeAsync()
    {
        if (CurrentMap is null)
        {
            await _dialogService.ShowMessageAsync(DialogTitles.Error, AppMessages.MapNotLoadedError);
            return;
        }

        var input = await TryParseEdgeInputAsync();
        if (!input.IsValid)
        {
            return;
        }

        await _dialogService.ExecuteWithErrorDialogAsync(
            action: () =>
            {
                CurrentMap = CurrentMap.WithoutEdge(input.CityOne, input.CityTwo, input.Distance, out bool removed);
                if (!removed)
                {
                    throw new InvalidOperationException(AppMessages.EdgeNotFoundError);
                }

                ClearRoute();
                UpdateMapDisplay(CurrentMap);
                ClearEdgeInputs();
            },
            errorTitle: DialogTitles.ValidationError);
    }

    private async Task<(bool IsValid, string CityOne, string CityTwo, int Distance)> TryParseEdgeInputAsync()
    {
        if (string.IsNullOrWhiteSpace(CityOne) || string.IsNullOrWhiteSpace(CityTwo))
        {
            await _dialogService.ShowMessageAsync(DialogTitles.ValidationError, AppMessages.BothCitiesRequiredError);
            return (false, string.Empty, string.Empty, 0);
        }

        if (!int.TryParse(CityDistance?.Trim(), out int distance) || distance <= 0)
        {
            await _dialogService.ShowMessageAsync(DialogTitles.ValidationError, AppMessages.InvalidDistanceError);
            return (false, string.Empty, string.Empty, 0);
        }

        return (true, CityOne.Trim(), CityTwo.Trim(), distance);
    }

    private void ClearEdgeInputs()
    {
        CityOne = string.Empty;
        CityTwo = string.Empty;
        CityDistance = string.Empty;
    }

    [RelayCommand]
    public async Task DeleteSelectedEdgeAsync()
    {
        if (CurrentMap is null || SelectedEdge is null)
        {
            return;
        }

        var edge = SelectedEdge;
        await _dialogService.ExecuteWithErrorDialogAsync(
            action: () =>
            {
                CurrentMap = CurrentMap.WithoutEdge(edge.From, edge.To, edge.Distance, out bool removed);
                if (!removed)
                {
                    throw new InvalidOperationException(AppMessages.EdgeNotFoundError);
                }

                SelectedEdge = null;
                ClearRoute();
                UpdateMapDisplay(CurrentMap);
            },
            errorTitle: DialogTitles.ValidationError);
    }

    public void UpdateMapDisplay(CityGraph? map)
    {
        MapEdges.Clear();
        SelectedEdge = null;

        if (map is null)
        {
            HasMapEdges = false;
            FullMapDisplay = AppMessages.DefaultMapStatus;
            return;
        }

        foreach (var (from, to, distance) in map.GetUniqueEdges())
        {
            MapEdges.Add(new MapEdgeItem(from, to, distance));
        }

        HasMapEdges = MapEdges.Count > 0;
        string mapText = map.ToString();
        FullMapDisplay = string.IsNullOrWhiteSpace(mapText) ? AppMessages.EmptyMapStatus : mapText;
    }
    public void UpdateRouteDisplay(Traveler traveler)
    {
        RouteCities.Clear();
        foreach (var city in traveler.Route)
        {
            RouteCities.Add(city);
        }

        if (CurrentMap is not null && traveler.Route.Count >= 2)
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
