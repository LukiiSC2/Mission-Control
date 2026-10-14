using System;
using System.Collections.Generic;
using System.IO;

using Bogus;

using Shouldly;

using SmartTravelPlanner.Tests.TestData;

using Travelling;

using Xunit;

namespace SmartTravelPlanner.Tests.Domain;

public class CityGraphTests
{
    [Fact]
    public void ToString_MustPreserveInsertionOrder_MatchingTest4Screenshot()
    {
        // Input:
        // Rome-Florence,280
        // Rome-Milan,580
        // Rome-Venice,530
        // Rome-Naples,230
        //
        // Expected:
        // Rome-Florence,280
        // Florence-Rome,280
        // Rome-Milan,580
        // Milan-Rome,580
        // Rome-Venice,530
        // Venice-Rome,530
        // Rome-Naples,230
        // Naples-Rome,230

        string[] inputLines =
        [
            "Rome-Florence,280",
            "Rome-Milan,580",
            "Rome-Venice,530",
            "Rome-Naples,230"
        ];

        string tempFile = MapFaker.CreateTempMapFile(inputLines);
        try
        {
            var graph = CityGraph.LoadFromFile(tempFile);
            string output = graph.ToString();

            string expected =
                $"Rome-Florence,280{Environment.NewLine}" +
                $"Florence-Rome,280{Environment.NewLine}" +
                $"Rome-Milan,580{Environment.NewLine}" +
                $"Milan-Rome,580{Environment.NewLine}" +
                $"Rome-Venice,530{Environment.NewLine}" +
                $"Venice-Rome,530{Environment.NewLine}" +
                $"Rome-Naples,230{Environment.NewLine}" +
                "Naples-Rome,230";

            output.ShouldBe(expected);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ToString_MustPreserveInsertionOrder_MatchingTest1()
    {
        // Input:
        // Kyiv-Lviv,540
        // Lviv-Warsaw,390
        // Kyiv-Odesa,480
        //
        // Expected:
        // Kyiv-Lviv,540
        // Lviv-Kyiv,540
        // Lviv-Warsaw,390
        // Warsaw-Lviv,390
        // Kyiv-Odesa,480
        // Odesa-Kyiv,480

        string[] inputLines =
        [
            "Kyiv-Lviv,540",
            "Lviv-Warsaw,390",
            "Kyiv-Odesa,480"
        ];

        string tempFile = MapFaker.CreateTempMapFile(inputLines);
        try
        {
            var graph = CityGraph.LoadFromFile(tempFile);
            string output = graph.ToString();

            string expected =
                $"Kyiv-Lviv,540{Environment.NewLine}" +
                $"Lviv-Kyiv,540{Environment.NewLine}" +
                $"Lviv-Warsaw,390{Environment.NewLine}" +
                $"Warsaw-Lviv,390{Environment.NewLine}" +
                $"Kyiv-Odesa,480{Environment.NewLine}" +
                "Odesa-Kyiv,480";

            output.ShouldBe(expected);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ToString_WithRandomBogusMap_PreservesAllEdgePairsInOrder()
    {
        var edges = MapFaker.GenerateEdges(6);
        var lines = MapFaker.ToMapLines(edges);
        string tempFile = MapFaker.CreateTempMapFile(lines);

        try
        {
            var graph = CityGraph.LoadFromFile(tempFile);
            string output = graph.ToString();

            string expected = MapFaker.GetExpectedToString(lines);
            output.ShouldBe(expected);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void LoadFromFile_MissingFile_ThrowsFileNotFoundException()
    {
        Should.Throw<System.IO.FileNotFoundException>(() =>
            CityGraph.LoadFromFile("non_existent_map_12345.txt")
        );
    }

    [Fact]
    public void FindShortestPath_FindsOptimalDijkstraRoute()
    {
        string[] lines =
        [
            "Lviv-Krakow,330",
            "Krakow-Vienna,450",
            "Lviv-Prague,860",
            "Prague-Vienna,300"
        ];
        string tempFile = MapFaker.CreateTempMapFile(lines);

        try
        {
            var graph = CityGraph.LoadFromFile(tempFile);

            // Via Krakow: 330 + 450 = 780. Via Prague: 860 + 300 = 1160.
            var path = graph.FindShortestPath("Lviv", "Vienna");
            path.ShouldBe(["Lviv", "Krakow", "Vienna"]);

            int distance = graph.GetPathDistance(path);
            distance.ShouldBe(780);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void FindShortestPath_UnreachableDestination_ReturnsEmpty()
    {
        string[] lines =
        [
            "Kyiv-Lviv,540",
            "Lviv-Warsaw,390"
        ];
        string tempFile = MapFaker.CreateTempMapFile(lines);

        try
        {
            var graph = CityGraph.LoadFromFile(tempFile);
            var path = graph.FindShortestPath("Kyiv", "Tokyo");
            path.ShouldBeEmpty();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void FindShortestPath_SameStartAndDestination_ReturnsSingleCity()
    {
        string[] lines = ["Kyiv-Lviv,540"];
        string tempFile = MapFaker.CreateTempMapFile(lines);

        try
        {
            var graph = CityGraph.LoadFromFile(tempFile);
            var path = graph.FindShortestPath("Kyiv", "Kyiv");
            path.ShouldBe(["Kyiv"]);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
