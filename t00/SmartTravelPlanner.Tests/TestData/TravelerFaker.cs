using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Bogus;

using Travelling;

namespace SmartTravelPlanner.Tests.TestData;

public static class TravelerFaker
{
    private static readonly Faker FakerInstance = new("en");

    public static Traveler GenerateTraveler(string? name = null, string? location = null, int routeStops = 3)
    {
        string travelerName = name ?? FakerInstance.Name.FirstName();
        var traveler = new Traveler(travelerName);

        string loc = location ?? FakerInstance.Address.City();
        traveler.SetLocation(loc);

        for (int i = 0; i < routeStops; i++)
        {
            traveler.AddCity(FakerInstance.Address.City());
        }

        return traveler;
    }

    public static string CreateTempTravelerJson(string json)
    {
        string path = Path.Combine(Path.GetTempPath(), $"traveler_{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }

    public static string CreateTempTravelerFile(Traveler traveler)
    {
        string path = Path.Combine(Path.GetTempPath(), $"traveler_{Guid.NewGuid():N}.json");
        traveler.SaveToFile(path);
        return path;
    }

    /// <summary>
    /// Generates expected single-line route JSON string matching challenge formatting.
    /// </summary>
    public static string FormatExpectedJson(string name, string? location, IEnumerable<string> route)
    {
        var routeArray = "[" + string.Join(", ", route.Select(r => $"\"{r}\"")) + "]";
        var locPart = location == null ? "null" : $"\"{location}\"";
        return
            "{\n" +
            $"  \"name\": \"{name}\",\n" +
            $"  \"currentLocation\": {locPart},\n" +
            $"  \"route\": {routeArray}\n" +
            "}";
    }
}
