using System;
using System.Collections.Generic;
using System.IO;

namespace Travelling;

public class CityGraph
{
    private readonly Dictionary<string, List<(string City, int Distance)>> _adjacencyList =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly List<(string From, string To, int Distance)> _edges = new();

    public static CityGraph LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File '{filePath}' does not exist.");
        }

        var graph = new CityGraph();
        string[] lines = File.ReadAllLines(filePath);

        foreach (var rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string[] parts = line.Split(',');
            if (parts.Length != 2)
            {
                continue;
            }

            string[] cities = parts[0].Split('-');
            if (cities.Length != 2)
            {
                continue;
            }

            string cityA = cities[0].Trim();
            string cityB = cities[1].Trim();

            if (!int.TryParse(parts[1].Trim(), out int distance))
            {
                continue;
            }

            graph.AddEdge(cityA, cityB, distance);
        }

        return graph;
    }

    public void AddEdge(string from, string to, int distance)
    {
        if (!_adjacencyList.TryGetValue(from, out var fromList))
        {
            fromList = new List<(string City, int Distance)>();
            _adjacencyList[from] = fromList;
        }
        fromList.Add((to, distance));

        if (!_adjacencyList.TryGetValue(to, out var toList))
        {
            toList = new List<(string City, int Distance)>();
            _adjacencyList[to] = toList;
        }
        toList.Add((from, distance));

        _edges.Add((from, to, distance));
        _edges.Add((to, from, distance));
    }

    public List<string> FindShortestPath(string from, string to)
    {
        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
        {
            return new List<string>();
        }

        if (!_adjacencyList.ContainsKey(from) || !_adjacencyList.ContainsKey(to))
        {
            return new List<string>();
        }

        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
        {
            return new List<string> { from };
        }

        var distances = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var previous = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var unvisited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var city in _adjacencyList.Keys)
        {
            distances[city] = int.MaxValue;
            unvisited.Add(city);
        }

        distances[from] = 0;

        while (unvisited.Count > 0)
        {
            string current = null!;
            int minDistance = int.MaxValue;

            foreach (var city in unvisited)
            {
                if (distances[city] < minDistance)
                {
                    minDistance = distances[city];
                    current = city;
                }
            }

            if (current == null || minDistance == int.MaxValue)
            {
                break; // Remaining cities are unreachable
            }

            if (string.Equals(current, to, StringComparison.OrdinalIgnoreCase))
            {
                break; // Reached target
            }

            unvisited.Remove(current);

            if (_adjacencyList.TryGetValue(current, out var neighbors))
            {
                foreach (var (neighborCity, edgeDistance) in neighbors)
                {
                    if (unvisited.Contains(neighborCity))
                    {
                        int newDist = distances[current] + edgeDistance;
                        if (newDist < distances[neighborCity])
                        {
                            distances[neighborCity] = newDist;
                            previous[neighborCity] = current;
                        }
                    }
                }
            }
        }

        if (!previous.ContainsKey(to))
        {
            return new List<string>();
        }

        var path = new List<string>();
        string curr = to;
        while (curr != null)
        {
            path.Add(curr);
            previous.TryGetValue(curr, out curr!);
        }

        path.Reverse();
        return path;
    }

    public int GetPathDistance(List<string> path)
    {
        if (path == null || path.Count < 2)
        {
            return 0;
        }

        int totalDistance = 0;

        for (int i = 0; i < path.Count - 1; i++)
        {
            string from = path[i];
            string to = path[i + 1];

            if (_adjacencyList.TryGetValue(from, out var neighbors))
            {
                var edge = neighbors.Find(n => string.Equals(n.City, to, StringComparison.OrdinalIgnoreCase));
                if (edge != default)
                {
                    totalDistance += edge.Distance;
                }
                else
                {
                    return 0; // Segment not found
                }
            }
            else
            {
                return 0; // City not in graph
            }
        }

        return totalDistance;
    }

    public override string ToString()
    {
        var lines = new List<string>(_edges.Count);
        foreach (var (fromCity, toCity, distance) in _edges)
        {
            lines.Add($"{fromCity}-{toCity},{distance}");
        }

        return string.Join(Environment.NewLine, lines);
    }
}
