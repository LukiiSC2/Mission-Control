using System;
using System.Collections.Generic;
using System.IO;

using Bogus;

using Shouldly;

using SmartTravelPlanner.Tests.TestData;

using Travelling;

using Xunit;

namespace SmartTravelPlanner.Tests.Domain;

public class TravelerTests
{
    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "integration")]
    public void SaveToFile_FormatsRouteArrayOnSingleLine_MatchingTest3()
    {
        // Charlie New York Boston Chicago San Francisco Los Angeles
        // Expected Output:
        // {
        //   "name": "Charlie",
        //   "currentLocation": "New York",
        //   "route": ["Boston", "Chicago", "San Francisco", "Los Angeles"]
        // }

        var traveler = new Traveler("Charlie");
        traveler.SetLocation("New York");
        traveler.AddCity("Boston");
        traveler.AddCity("Chicago");
        traveler.AddCity("San Francisco");
        traveler.AddCity("Los Angeles");

        string tempFile = TravelerFaker.CreateTempTravelerFile(traveler);
        try
        {
            string json = File.ReadAllText(tempFile);

            // Verify the single line route format
            json.ShouldContain("\"route\": [\"Boston\", \"Chicago\", \"San Francisco\", \"Los Angeles\"]");
            json.ShouldContain("\"name\": \"Charlie\"");
            json.ShouldContain("\"currentLocation\": \"New York\"");

            // Verify route is NOT broken down into multiple lines
            json.ShouldNotContain("[\n    \"Boston\"");
            json.ShouldNotContain("[\r\n    \"Boston\"");
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
    public void SaveToFile_EmptyRoute_FormatsAsEmptyArray()
    {
        var traveler = new Traveler("Alice");
        traveler.SetLocation("Berlin");

        string tempFile = TravelerFaker.CreateTempTravelerFile(traveler);
        try
        {
            string json = File.ReadAllText(tempFile);
            json.ShouldContain("\"route\": []");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "integration")]
    public void LoadFromFile_CanReadBothSingleLineAndMultiLineJson()
    {
        string singleLineJson =
            "{\n" +
            "  \"name\": \"Bob\",\n" +
            "  \"currentLocation\": \"Paris\",\n" +
            "  \"route\": [\"London\", \"Brussels\"]\n" +
            "}";

        string multiLineJson =
            "{\n" +
            "  \"name\": \"Bob\",\n" +
            "  \"currentLocation\": \"Paris\",\n" +
            "  \"route\": [\n" +
            "    \"London\",\n" +
            "    \"Brussels\"\n" +
            "  ]\n" +
            "}";

        string file1 = TravelerFaker.CreateTempTravelerJson(singleLineJson);
        string file2 = TravelerFaker.CreateTempTravelerJson(multiLineJson);

        try
        {
            var t1 = Traveler.LoadFromFile(file1);
            var t2 = Traveler.LoadFromFile(file2);

            t1.GetName().ShouldBe("Bob");
            t1.GetLocation().ShouldBe("Paris");
            t1.GetRoute().ShouldBe("London -> Brussels");

            t2.GetName().ShouldBe("Bob");
            t2.GetLocation().ShouldBe("Paris");
            t2.GetRoute().ShouldBe("London -> Brussels");
        }
        finally
        {
            if (File.Exists(file1)) File.Delete(file1);
            if (File.Exists(file2)) File.Delete(file2);
        }
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public void LoadFromFile_MissingFile_ThrowsTravellingFileNotFoundException()
    {
        var ex = Should.Throw<FileNotFoundException>(() =>
            Traveler.LoadFromFile("missing_traveler_file.json")
        );
        ex.Message.ShouldBe("File doesn’t exist");
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "integration")]
    public void LoadFromFile_CorruptedData_ThrowsFileLoadException()
    {
        string corruptedJson = "{ \"name\": \"Bob\", invalid json... }";
        string tempFile = TravelerFaker.CreateTempTravelerJson(corruptedJson);

        try
        {
            var ex = Should.Throw<FileLoadException>(() =>
                Traveler.LoadFromFile(tempFile)
            );
            ex.Message.ShouldBe("Invalid travel data");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "boundary")]
    public void Capitalization_FormatsNamesProperly()
    {
        var traveler = new Traveler("Alice");
        traveler.SetLocation("new    York");
        traveler.GetLocation().ShouldBe("New York");

        traveler.AddCity("san francisco");
        traveler.GetRoute().ShouldBe("San Francisco");
    }

    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "critical-path")]
    [Trait("Category", "integration")]
    public void PlanRouteTo_WithCityGraph_UpdatesRouteToShortestPath()
    {
        string[] mapLines =
        [
            "Lviv-Krakow,330",
            "Krakow-Vienna,450",
            "Lviv-Prague,860",
            "Prague-Vienna,300"
        ];
        string mapFile = MapFaker.CreateTempMapFile(mapLines);

        try
        {
            var graph = CityGraph.LoadFromFile(mapFile);
            var traveler = new Traveler("Alice");
            traveler.SetLocation("Lviv");

            bool success = traveler.PlanRouteTo("Vienna", graph);
            success.ShouldBeTrue();
            traveler.GetRoute().ShouldBe("Lviv -> Krakow -> Vienna");
        }
        finally
        {
            if (File.Exists(mapFile)) File.Delete(mapFile);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [Trait("Category", "positive")]
    [Trait("Category", "integration")]
    public void BogusGeneratedTravelers_SerializeAndDeserializePreservingData(int iteration)
    {
        _ = iteration;
        var traveler = TravelerFaker.GenerateTraveler(routeStops: 4);
        string tempFile = TravelerFaker.CreateTempTravelerFile(traveler);

        try
        {
            var loaded = Traveler.LoadFromFile(tempFile);
            loaded.GetName().ShouldBe(traveler.GetName());
            loaded.GetLocation().ShouldBe(traveler.GetLocation());
            loaded.GetRoute().ShouldBe(traveler.GetRoute());
            loaded.GetStopCount().ShouldBe(traveler.GetStopCount());
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    [Trait("Category", "positive")]
    public void Clone_CreatesIndependentDeepCopy()
    {
        var traveler = TravelerFaker.GenerateTraveler(routeStops: 3);
        var clone = (Traveler)traveler.Clone();

        clone.ShouldBe(traveler);

        clone.AddCity("Tokyo");
        clone.GetStopCount().ShouldBe(traveler.GetStopCount() + 1);
    }
}
