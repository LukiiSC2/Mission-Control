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


public partial class MainViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly IFileDialogService _fileDialogService;
    public MainViewModel()
    : this(new AvaloniaDialogService(), new AvaloniaDialogService())
    {}
    public MainViewModel(IDialogService dialogService)
        : this(dialogService, (dialogService as IFileDialogService) ?? new AvaloniaDialogService())
    {}
    public MainViewModel(IDialogService dialogService, IFileDialogService fileDialogService)
    {
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));

        TravelerDetails = new TravelerDetailsViewModel(_dialogService, _fileDialogService);
        RoutePlanner = new RoutePlannerViewModel(_dialogService, _fileDialogService, () => TravelerDetails.CurrentTraveler);

        TravelerDetails.TravelerChanged += traveler =>
        {
            if (traveler == null)
            {
                //RoutePlanner.ClearRoute();
            }
        };
    }

    [RelayCommand]
    private static void Exit()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
    public TravelerDetailsViewModel TravelerDetails { get; }
    public RoutePlannerViewModel RoutePlanner { get; }
    public IAsyncRelayCommand LoadMapCommand => RoutePlanner.LoadMapCommand;
    public IAsyncRelayCommand CreateTravelerCommand => TravelerDetails.CreateTravelerCommand;
    public IAsyncRelayCommand SaveTravelerCommand => TravelerDetails.SaveTravelerCommand;
    public IAsyncRelayCommand LoadTravelerCommand => TravelerDetails.LoadTravelerCommand;
    public IAsyncRelayCommand PlanRouteCommand => RoutePlanner.PlanRouteCommand;
    public IAsyncRelayCommand ClearRouteCommand => RoutePlanner.ClearRouteCommand;
    public IAsyncRelayCommand DeleteTravelerCommand => TravelerDetails.DeleteTravelerCommand;
}
