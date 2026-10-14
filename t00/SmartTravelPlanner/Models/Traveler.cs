using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using SmartTravelPlanner.Converters;

namespace Travelling;

public class Traveler : ICloneable
{
    private string _name;
    private string? _currentLocation;
    private List<string> _route;

    [JsonPropertyName("name")]
    public string Name
    {
        get => _name;
        set => _name = value;
    }

    [JsonPropertyName("currentLocation")]
    public string? CurrentLocation
    {
        get => _currentLocation;
        set => SetLocation(value ?? string.Empty);
    }

    [JsonPropertyName("route")]
    [JsonConverter(typeof(SingleLineStringListConverter))]
    public List<string> Route
    {
        get => _route;
        set
        {
            _route.Clear();
            if (value != null)
            {
                foreach (var city in value)
                {
                    AddCity(city);
                }
            }
        }
    }

    public Traveler(string name)
    {
        _name = name;
        _route = new List<string>();
    }

    public string GetName()
    {
        return _name;
    }

    public void SetLocation(string location)
    {
        if (!string.IsNullOrEmpty(location))
        {
            _currentLocation = CapitalizeFirstLetter(location);
        }
        else
        {
            _currentLocation = location;
        }
    }

    public string? GetLocation()
    {
        return _currentLocation;
    }

    public void AddCity(string city)
    {
        if (string.IsNullOrEmpty(city))
        {
            throw new ArgumentException("City name cannot be null or empty.");
        }

        string formattedCity = CapitalizeFirstLetter(city);
        _route.Add(formattedCity);
    }

    public bool RemoveCity(string city)
    {
        if (string.IsNullOrEmpty(city))
        {
            return false;
        }

        int index = _route.FindIndex(c =>
            string.Equals(c, city, StringComparison.OrdinalIgnoreCase)
        );
        if (index >= 0)
        {
            _route.RemoveAt(index);
            return true;
        }

        return false;
    }

    public string GetRoute()
    {
        return string.Join(" -> ", _route);
    }

    public void ClearRoute()
    {
        _route.Clear();
    }

    public int GetStopCount()
    {
        return _route.Count;
    }

    public string? GetNextStop()
    {
        if (_route.Count > 0)
        {
            return _route[0];
        }

        return null;
    }

    public bool HasCity(string city)
    {
        if (string.IsNullOrEmpty(city))
        {
            return false;
        }

        return _route.Exists(c => string.Equals(c, city, StringComparison.OrdinalIgnoreCase));
    }

    public void SortRoute()
    {
        _route.Sort();
    }

    public string this[int index]
    {
        get { return _route[index]; }
    }

    public override bool Equals(object? obj)
    {
        if (obj is Traveler other)
        {
            return string.Equals(this._name, other._name, StringComparison.Ordinal)
                && string.Equals(this._currentLocation, other._currentLocation, StringComparison.Ordinal);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_name, _currentLocation);
    }

    public static bool operator ==(Traveler? a, Traveler? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        return a.Equals(b);
    }

    public static bool operator !=(Traveler? a, Traveler? b)
    {
        return !(a == b);
    }

    public object Clone()
    {
        Traveler clone = new Traveler(this._name);
        clone._currentLocation = this._currentLocation;

        foreach (string city in this._route)
        {
            clone._route.Add(city);
        }

        return clone;
    }

    public override string ToString()
    {
        return $"Traveler: {GetName()} | Location: {GetLocation()} | Route: {GetRoute()}";
    }

    private static readonly char[] SpaceSeparator = [' '];
    private static readonly JsonSerializerOptions IndentedJsonOptions = new() { WriteIndented = true };

    private static string CapitalizeFirstLetter(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var words = input.Split(SpaceSeparator, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length == 1)
            {
                words[i] = words[i].ToUpperInvariant();
            }
            else
            {
                words[i] = char.ToUpperInvariant(words[i][0]) + words[i].Substring(1).ToLowerInvariant();
            }
        }
        return string.Join(" ", words);
    }

    public virtual string GetTravelSummary()
    {
        return ToString();
    }

    public void SaveToFile(string filePath)
    {
        string jsonString = JsonSerializer.Serialize(this, IndentedJsonOptions);
        File.WriteAllText(filePath, jsonString);
    }

    public static Traveler LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File doesn’t exist");
        }

        try
        {
            string jsonString = File.ReadAllText(filePath);
            var result = JsonSerializer.Deserialize<Traveler>(jsonString);
            if (result == null || string.IsNullOrEmpty(result.GetName()))
            {
                throw new FileLoadException("Invalid travel data");
            }
            return result;
        }
        catch (FileNotFoundException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new FileLoadException("Invalid travel data");
        }
    }

    public bool PlanRouteTo(string destination, CityGraph map)
    {
        if (map == null || string.IsNullOrEmpty(destination))
        {
            Console.WriteLine("No route!");
            return false;
        }

        string? startCity = !string.IsNullOrEmpty(_currentLocation)
            ? _currentLocation
            : (_route.Count > 0 ? _route[0] : null);

        if (string.IsNullOrEmpty(startCity))
        {
            Console.WriteLine("No route!");
            return false;
        }

        List<string> shortestPath = map.FindShortestPath(startCity, destination);

        if (shortestPath == null || shortestPath.Count == 0)
        {
            Console.WriteLine("No route!");
            return false;
        }

        _route.Clear();
        foreach (var city in shortestPath)
        {
            _route.Add(CapitalizeFirstLetter(city));
        }

        return true;
    }
}
