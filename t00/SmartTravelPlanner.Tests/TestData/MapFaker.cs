using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Bogus;

namespace SmartTravelPlanner.Tests.TestData;

public static class MapFaker
{
    private static readonly Faker FakerInstance = new("en");

    public static readonly string[] SampleCities =
    [
        "Rome", "Florence", "Milan", "Venice", "Naples",
        "Kyiv", "Lviv", "Odesa", "Warsaw", "Krakow", "Vienna", "Prague",
        "New York", "Boston", "Chicago", "San Francisco", "Los Angeles", "Las Vegas"
    ];

    public record MapEdge(string From, string To, int Distance);

    /// <summary>
    /// Generates a list of valid edges (A-B, Distance).
    /// </summary>
    public static List<MapEdge> GenerateEdges(int count = 5)
    {
        var cities = FakerInstance.PickRandom(SampleCities, Math.Min(SampleCities.Length, count + 2)).ToList();
        var edges = new List<MapEdge>();

        for (int i = 0; i < cities.Count - 1 && edges.Count < count; i++)
        {
            int distance = FakerInstance.Random.Number(50, 2000);
            edges.Add(new MapEdge(cities[i], cities[i + 1], distance));
        }

        return edges;
    }

    /// <summary>
    /// Formats edges as text lines: "CityA-CityB,Distance".
    /// </summary>
    public static string[] ToMapLines(IEnumerable<MapEdge> edges)
    {
        return edges.Select(e => $"{e.From}-{e.To},{e.Distance}").ToArray();
    }

    /// <summary>
    /// Writes map lines to a temporary file and returns its path.
    /// </summary>
    public static string CreateTempMapFile(params string[] lines)
    {
        string path = Path.Combine(Path.GetTempPath(), $"map_{Guid.NewGuid():N}.txt");
        File.WriteAllLines(path, lines);
        return path;
    }

    /// <summary>
    /// Returns expected CityGraph.ToString() representation for the given input lines,
    /// where each line "A-B,Dist" produces "A-B,Dist" followed by "B-A,Dist".
    /// </summary>
    public static string GetExpectedToString(IEnumerable<string> mapLines)
    {
        var result = new List<string>();
        foreach (var rawLine in mapLines)
        {
            // A-B,Dist
            string line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Part[0] = A-B, Part[1] = Dist
            string[] parts = line.Split(',');
            if (parts.Length != 2) continue;

            string[] cities = parts[0].Split('-');
            if (cities.Length != 2) continue;

            string cityA = cities[0].Trim();
            string cityB = cities[1].Trim();
            string dist = parts[1].Trim();

            result.Add($"{cityA}-{cityB},{dist}");
            result.Add($"{cityB}-{cityA},{dist}");
        }

        return string.Join(Environment.NewLine, result);
    }
}
