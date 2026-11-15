using System;
using System.Collections.Generic;
using System.IO;

using Bogus;

using MissionControl.Services;

using Shouldly;

using SmartTravelPlanner.Tests.TestData;

using Travelling;

using Xunit;

namespace SmartTravelPlanner.Tests.Domain;

public class CityGraphTests
{
    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "integration")]
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
    [Trait("Category", "positive")]
    [Trait("Category", "integration")]
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
    [Trait("Category", "positive")]
    [Trait("Category", "integration")]
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
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public void LoadFromFile_MissingFile_ThrowsFileNotFoundException()
    {
        Should.Throw<System.IO.FileNotFoundException>(() =>
            CityGraph.LoadFromFile("non_existent_map_12345.txt")
        );
    }

    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "critical-path")]
    [Trait("Category", "integration")]
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
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    [Trait("Category", "integration")]
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
    [Trait("Category", "positive")]
    [Trait("Category", "boundary")]
    [Trait("Category", "integration")]
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

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public void AddValidatedEdge_DuplicateReverseDirection_ThrowsInvalidOperationException()
    {
        var graph = new CityGraph();
        graph.AddValidatedEdge("Kyiv", "Kharkiv", 480);

        Should.Throw<InvalidOperationException>(() =>
            graph.AddValidatedEdge("kharkiv", "KYIV", 480));
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public void AddValidatedEdge_SameCity_ThrowsArgumentException()
    {
        var graph = new CityGraph();

        Should.Throw<ArgumentException>(() =>
            graph.AddValidatedEdge("Kyiv", "kyiv", 100));
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public void AddValidatedEdge_NonPositiveDistance_ThrowsArgumentOutOfRangeException()
    {
        var graph = new CityGraph();

        Should.Throw<ArgumentOutOfRangeException>(() =>
            graph.AddValidatedEdge("Kyiv", "Lviv", 0));
    }

    [Fact]
    [Trait("Category", "positive")]
    public void WithoutEdge_CaseInsensitive_RemovesBothDirections()
    {
        var graph = new CityGraph();
        graph.AddEdge("Kyiv", "Lviv", 540);

        var updated = graph.WithoutEdge("lviv", "KYIV", 540, out bool removed);

        removed.ShouldBeTrue();
        updated.HasEdge("Kyiv", "Lviv").ShouldBeFalse();
        updated.ToString().ShouldBeEmpty();
    }

    [Fact]
    [Trait("Category", "negative")]
    public void WithoutEdge_NonExistentEdge_ReturnsOriginalAndFalse()
    {
        var graph = new CityGraph();
        graph.AddEdge("Kyiv", "Lviv", 540);

        var updated = graph.WithoutEdge("Kyiv", "Odesa", 540, out bool removed);

        removed.ShouldBeFalse();
        updated.ShouldBeSameAs(graph);
    }
}
