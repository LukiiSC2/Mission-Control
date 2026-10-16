using ReactiveUI;
using System.Reactive;
using Travelling;

namespace MissionControl.ViewModels;

public class MainViewModel : ReactiveObject{
    private string _travelerCreateStatus = "No traveler created yet";
    private string _travelerName = "";
    private string _travelerLocation = "";
    private Traveler? _traveler;

    public string TravelerCreateStatus{
        get => _travelerCreateStatus;
        set => this.RaiseAndSetIfChanged(ref _travelerCreateStatus, value);
    }
    public string TravelerName{
        get => _travelerName;
        set => this.RaiseAndSetIfChanged(ref _travelerName, value);
    }
    public string TravelerLocation{
        get => _travelerLocation;
        set => this.RaiseAndSetIfChanged(ref _travelerLocation, value);
    }
    public Traveler? CurrentTraveler{
        get => _traveler;
        private set => this.RaiseAndSetIfChanged(ref _traveler, value);
    }

    public ReactiveCommand<Unit, Unit> CreateTraveler { get; }
    public ReactiveCommand<Unit, Unit> SaveTraveler { get; }

    public MainViewModel()
    {
        CreateTraveler = ReactiveCommand.Create<Unit, Unit>(_ =>
        {
            if (string.IsNullOrWhiteSpace(TravelerName) || string.IsNullOrWhiteSpace(TravelerLocation)){
                TravelerCreateStatus = "Traveler wasn`t created";
            }
            else {
                Traveler traveler = new Traveler(TravelerName.Trim());
                traveler.SetLocation(TravelerLocation.Trim());
                CurrentTraveler = traveler;
                TravelerCreateStatus = $"Traveler {TravelerName} from {TravelerLocation} created";
            }
            return Unit.Default;
        });
        SaveTraveler = ReactiveCommand.Create<Unit, Unit>(_ =>
        {
            if (CurrentTraveler == null){
                TravelerCreateStatus = "Traveler wasn`t save";
                return Unit.Default;
            }
            try {
                string dataFolder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../data"));
                Directory.CreateDirectory(dataFolder);
                string filePath = Path.Combine(dataFolder, $"traveler{CurrentTraveler.GetName()}.json");
                CurrentTraveler.SaveToFile(filePath);
                TravelerCreateStatus = $"Traveler saved successfully!\nPath: {filePath}";
            }
            catch (Exception exception) {
                TravelerCreateStatus = $"Failed to save traveler: {exception.Message}";
            }
            return Unit.Default;
        });
    }
}
