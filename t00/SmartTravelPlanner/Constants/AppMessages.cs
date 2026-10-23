namespace MissionControl.Constants;

public static class AppMessages
{
    public static string FieldCannotBeEmpty(string fieldName) => $"{fieldName} cannot be empty!";
    public static string TravelerCreatedSuccess(string name) => $"Traveler '{name}' created successfully!";
    public static string TravelerDeletedSuccess(string name) => $"Traveler '{name}' deleted successfully!";
    public static string TravelerSavedSuccess(string path) => $"Traveler data successfully saved to:\n{path}";
    public static string TravelerLoadedSuccess(string name) => $"Traveler '{name}' loaded successfully!";
    public static string FailedToLoadTravelerError(string error) => $"Failed to load traveler file:\n{error}";
    public static string MapLoadedSuccess(string path) => $"Map successfully loaded from:\n{path}";
    public static string DestinationUnreachableError(string dest) => $"Destination '{dest}' is unreachable or not in the map!";
    public static string InvalidJsonFileError(string error) => $"Invalid or corrupted .json file:\n{error}";
    public static string FailedToLoadMapError(string error) => $"Failed to load map file:\n{error}";
    public static string FailedToSaveFileError(string error) => $"Failed to save traveler:\n{error}";

    public const string TravelerNotCreatedError = "Please create or load a traveler first!";
    public const string MapNotLoadedError = "Please load a city map (.txt file) first!";
    public const string NoTravelerToDeleteError = "No traveler to delete!";
    public const string NoTravelerToSaveError = "No traveler to save. Please create a traveler first!";

    public const string DefaultTravelerStatus = "No traveler created yet.";
    public const string DefaultMapStatus = "Map not loaded";
    public const string DefaultRouteStatus = "No route planned";
    public const string RouteClearedStatus = "Route cleared";
    public const string ZeroDistance = "0 km";
}
