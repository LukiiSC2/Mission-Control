using System;
using System.Collections.Generic;

using Travelling;

namespace MissionControl.Services;

public static class CityGraphExtensions
{
    private static readonly string[] LineSeparators = ["\r\n", "\n"];

    public static IReadOnlyList<(string From, string To, int Distance)> GetUniqueEdges(this CityGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var result = new List<(string From, string To, int Distance)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string raw = graph.ToString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return result;
        }

        foreach (string rawLine in raw.Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries))
        {
            string line = rawLine.Trim();
            int commaIndex = line.LastIndexOf(',');
            if (commaIndex <= 0)
            {
                continue;
            }

            string citiesPart = line[..commaIndex];
            string distancePart = line[(commaIndex + 1)..].Trim();
            int dashIndex = citiesPart.IndexOf('-');
            if (dashIndex <= 0 || dashIndex >= citiesPart.Length - 1)
            {
                continue;
            }

            string fromCity = citiesPart[..dashIndex].Trim();
            string toCity = citiesPart[(dashIndex + 1)..].Trim();
            if (!int.TryParse(distancePart, out int distance))
            {
                continue;
            }

            string key = string.Compare(fromCity, toCity, StringComparison.OrdinalIgnoreCase) <= 0
                ? $"{fromCity}\0{toCity}"
                : $"{toCity}\0{fromCity}";

            if (seen.Add(key))
            {
                result.Add((fromCity, toCity, distance));
            }
        }

        return result;
    }

    public static bool HasEdge(this CityGraph graph, string from, string to)
    {
        ArgumentNullException.ThrowIfNull(graph);

        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
        {
            return false;
        }

        foreach (var (edgeFrom, edgeTo, _) in graph.GetUniqueEdges())
        {
            if ((string.Equals(edgeFrom, from, StringComparison.OrdinalIgnoreCase) &&
                 string.Equals(edgeTo, to, StringComparison.OrdinalIgnoreCase)) ||
                (string.Equals(edgeFrom, to, StringComparison.OrdinalIgnoreCase) &&
                 string.Equals(edgeTo, from, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    public static void AddValidatedEdge(this CityGraph graph, string from, string to, int distance)
    {
        ArgumentNullException.ThrowIfNull(graph);

        if (string.IsNullOrWhiteSpace(from))
        {
            throw new ArgumentException("Source city cannot be empty.", nameof(from));
        }

        if (string.IsNullOrWhiteSpace(to))
        {
            throw new ArgumentException("Destination city cannot be empty.", nameof(to));
        }

        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Source and destination cities cannot be the same.");
        }

        if (distance <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(distance), "Distance must be greater than zero.");
        }

        if (graph.HasEdge(from, to))
        {
            throw new InvalidOperationException($"Edge between '{from}' and '{to}' already exists.");
        }

        graph.AddEdge(from, to, distance);
    }

    public static CityGraph WithoutEdge(this CityGraph graph, string from, string to, int distance, out bool removed)
    {
        ArgumentNullException.ThrowIfNull(graph);

        removed = false;
        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to) || distance <= 0)
        {
            return graph;
        }

        var updated = new CityGraph();
        foreach (var edge in graph.GetUniqueEdges())
        {
            bool isMatch = edge.Distance == distance &&
                ((string.Equals(edge.From, from, StringComparison.OrdinalIgnoreCase) &&
                  string.Equals(edge.To, to, StringComparison.OrdinalIgnoreCase)) ||
                 (string.Equals(edge.From, to, StringComparison.OrdinalIgnoreCase) &&
                  string.Equals(edge.To, from, StringComparison.OrdinalIgnoreCase)));

            if (isMatch)
            {
                removed = true;
                continue;
            }

            updated.AddEdge(edge.From, edge.To, edge.Distance);
        }

        return removed ? updated : graph;
    }
}
