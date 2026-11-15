using System.IO;
using System.Threading.Tasks;

using MissionControl.Constants;
using MissionControl.Services;
using MissionControl.ViewModels;

using NSubstitute;

using Shouldly;

using SmartTravelPlanner.Tests.TestData;

using Travelling;

using Xunit;

namespace SmartTravelPlanner.Tests.ViewModels;

public class RoutePlannerViewModelTests
{
    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public async Task PlanRoute_WhenNoTraveler_ShowsError()
    {
        var dialogService = Substitute.For<IDialogService>();
        var fileDialogService = Substitute.For<IFileDialogService>();
        var vm = new RoutePlannerViewModel(dialogService, fileDialogService, () => null)
        {
            DestinationCity = "Berlin"
        };

        await vm.PlanRouteCommand.ExecuteAsync(null);

        await dialogService.Received(1)
            .ShowMessageAsync(DialogTitles.Error, AppMessages.TravelerNotCreatedError);
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public async Task PlanRoute_WhenNoMapLoaded_ShowsError()
    {
        var dialogService = Substitute.For<IDialogService>();
        var fileDialogService = Substitute.For<IFileDialogService>();
        var traveler = new Traveler("Alice");
        traveler.SetLocation("Kyiv");

        var vm = new RoutePlannerViewModel(dialogService, fileDialogService, () => traveler)
        {
            DestinationCity = "Berlin"
        };

        await vm.PlanRouteCommand.ExecuteAsync(null);

        await dialogService.Received(1)
            .ShowMessageAsync(DialogTitles.Error, AppMessages.MapNotLoadedError);
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public async Task PlanRoute_WhenDestinationEmpty_ShowsValidationError()
    {
        var dialogService = Substitute.For<IDialogService>();
        var fileDialogService = Substitute.For<IFileDialogService>();
        var traveler = new Traveler("Alice");
        traveler.SetLocation("Kyiv");

        var vm = new RoutePlannerViewModel(dialogService, fileDialogService, () => traveler)
        {
            CurrentMap = new CityGraph(),
            DestinationCity = "   "
        };

        await vm.PlanRouteCommand.ExecuteAsync(null);

        await dialogService.Received(1)
            .ShowMessageAsync(DialogTitles.ValidationError, Arg.Is<string>(msg => msg.Contains("cannot be empty")));
    }

    [Fact]
    [Trait("Category", "negative")]
    public async Task PlanRoute_WhenDestinationUnreachable_ShowsRouteNotFound()
    {
        var dialogService = Substitute.For<IDialogService>();
        var fileDialogService = Substitute.For<IFileDialogService>();
        var traveler = new Traveler("Alice");
        traveler.SetLocation("Kyiv");

        var graph = new CityGraph();
        graph.AddEdge("Kyiv", "Lviv", 500);

        var vm = new RoutePlannerViewModel(dialogService, fileDialogService, () => traveler)
        {
            CurrentMap = graph,
            DestinationCity = "Tokyo"
        };

        await vm.PlanRouteCommand.ExecuteAsync(null);

        await dialogService.Received(1)
            .ShowMessageAsync(DialogTitles.RouteNotFound, Arg.Is<string>(msg => msg.Contains("Tokyo")));
    }

    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "critical-path")]
    public async Task PlanRoute_WhenRouteFound_UpdatesRouteAndDistance()
    {
        var dialogService = Substitute.For<IDialogService>();
        var fileDialogService = Substitute.For<IFileDialogService>();
        var traveler = new Traveler("Alice");
        traveler.SetLocation("Kyiv");

        var graph = new CityGraph();
        graph.AddEdge("Kyiv", "Lviv", 540);
        graph.AddEdge("Lviv", "Warsaw", 390);

        var vm = new RoutePlannerViewModel(dialogService, fileDialogService, () => traveler)
        {
            CurrentMap = graph,
            DestinationCity = "Warsaw"
        };

        await vm.PlanRouteCommand.ExecuteAsync(null);

        vm.RouteCities.Count.ShouldBe(3);
        vm.RouteCities[0].ShouldBe("Kyiv");
        vm.RouteCities[1].ShouldBe("Lviv");
        vm.RouteCities[2].ShouldBe("Warsaw");
        vm.TotalDistanceText.ShouldBe("930 km");
        vm.FullRouteDisplay.ShouldBe("Kyiv -> Lviv -> Warsaw");
    }

    [Fact]
    [Trait("Category", "positive")]
    public void ClearRoute_ResetsRouteDisplayAndDistance()
    {
        var traveler = new Traveler("Alice");
        traveler.AddCity("Kyiv");
        traveler.AddCity("Lviv");

        var vm = new RoutePlannerViewModel(
            Substitute.For<IDialogService>(),
            Substitute.For<IFileDialogService>(),
            () => traveler)
        {
            TotalDistanceText = "500 km",
            FullRouteDisplay = "Kyiv -> Lviv"
        };
        vm.RouteCities.Add("Kyiv");
        vm.RouteCities.Add("Lviv");

        vm.ClearRouteCommand.Execute(null);

        vm.RouteCities.ShouldBeEmpty();
        vm.TotalDistanceText.ShouldBe(AppMessages.ZeroDistance);
        vm.FullRouteDisplay.ShouldBe(AppMessages.RouteClearedStatus);
        traveler.GetStopCount().ShouldBe(0);
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public async Task LoadMap_WhenCancelled_DoesNotChangeMap()
    {
        var dialogService = Substitute.For<IDialogService>();
        var fileDialogService = Substitute.For<IFileDialogService>();
        fileDialogService.OpenFileDialogAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns((string?)null);

        var vm = new RoutePlannerViewModel(dialogService, fileDialogService, () => null);

        await vm.LoadMapCommand.ExecuteAsync(null);

        vm.CurrentMap.ShouldBeNull();
        await dialogService.DidNotReceive().ShowMessageAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "critical-path")]
    [Trait("Category", "integration")]
    public async Task LoadMap_WhenValidFile_LoadsMapAndShowsSuccess()
    {
        var dialogService = Substitute.For<IDialogService>();
        var fileDialogService = Substitute.For<IFileDialogService>();
        string[] mapLines = ["Kyiv-Lviv,500", "Lviv-Warsaw,400"];
        string mapFile = MapFaker.CreateTempMapFile(mapLines);

        try
        {
            fileDialogService.OpenFileDialogAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(mapFile);

            var vm = new RoutePlannerViewModel(dialogService, fileDialogService, () => null);

            await vm.LoadMapCommand.ExecuteAsync(null);

            vm.CurrentMap.ShouldNotBeNull();
            vm.LoadedMapInfo.ShouldContain(Path.GetFileName(mapFile));
            await dialogService.Received(1)
                .ShowMessageAsync(DialogTitles.MapLoaded, Arg.Any<string>());
        }
        finally
        {
            if (File.Exists(mapFile)) File.Delete(mapFile);
        }
    }

    [Fact]
    [Trait("Category", "positive")]
    public async Task AddEdge_WhenMapIsNull_CreatesCustomMapAndUpdatesDisplay()
    {
        var dialogService = Substitute.For<IDialogService>();
        var fileDialogService = Substitute.For<IFileDialogService>();
        var vm = new RoutePlannerViewModel(dialogService, fileDialogService, () => null)
        {
            CityOne = " Kyiv ",
            CityTwo = " Kharkiv ",
            CityDistance = "480"
        };

        await vm.AddEdgeCommand.ExecuteAsync(null);

        vm.CurrentMap.ShouldNotBeNull();
        vm.CurrentMap.HasEdge("Kyiv", "Kharkiv").ShouldBeTrue();
        vm.LoadedMapInfo.ShouldBe(AppMessages.CustomMapLoadedInfo);
        vm.FullMapDisplay.ShouldContain("Kyiv-Kharkiv,480");
        vm.CityOne.ShouldBeEmpty();
        vm.CityTwo.ShouldBeEmpty();
        vm.CityDistance.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public async Task AddEdge_WhenReverseEdgeAlreadyExists_ShowsValidationError()
    {
        var dialogService = Substitute.For<IDialogService>();
        var fileDialogService = Substitute.For<IFileDialogService>();
        var graph = new CityGraph();
        graph.AddEdge("Kyiv", "Kharkiv", 480);

        var vm = new RoutePlannerViewModel(dialogService, fileDialogService, () => null)
        {
            CurrentMap = graph,
            CityOne = "kharkiv",
            CityTwo = "KYIV",
            CityDistance = "500"
        };

        await vm.AddEdgeCommand.ExecuteAsync(null);

        await dialogService.Received(1).ShowMessageAsync(
            DialogTitles.ValidationError,
            Arg.Is<string>(msg => msg.Contains("already exists")));
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public async Task AddEdge_WhenInvalidDistanceText_ShowsValidationError()
    {
        var dialogService = Substitute.For<IDialogService>();
        var fileDialogService = Substitute.For<IFileDialogService>();
        var vm = new RoutePlannerViewModel(dialogService, fileDialogService, () => null)
        {
            CityOne = "Kyiv",
            CityTwo = "Lviv",
            CityDistance = "abc"
        };

        await vm.AddEdgeCommand.ExecuteAsync(null);

        await dialogService.Received(1)
            .ShowMessageAsync(DialogTitles.ValidationError, AppMessages.InvalidDistanceError);
        vm.CurrentMap.ShouldBeNull();
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public async Task AddEdge_WhenSameCity_ShowsValidationError()
    {
        var dialogService = Substitute.For<IDialogService>();
        var fileDialogService = Substitute.For<IFileDialogService>();
        var vm = new RoutePlannerViewModel(dialogService, fileDialogService, () => null)
        {
            CityOne = "Kyiv",
            CityTwo = "kyiv",
            CityDistance = "100"
        };

        await vm.AddEdgeCommand.ExecuteAsync(null);

        await dialogService.Received(1)
            .ShowMessageAsync(DialogTitles.ValidationError, Arg.Is<string>(msg => msg.Contains("cannot be the same")));
    }

    [Fact]
    [Trait("Category", "positive")]
    public async Task RemoveEdge_WithWhitespaceAndDifferentCase_RemovesEdgeAndUpdatesDisplay()
    {
        var dialogService = Substitute.For<IDialogService>();
        var fileDialogService = Substitute.For<IFileDialogService>();
        var graph = new CityGraph();
        graph.AddEdge("Kyiv", "Lviv", 540);

        var vm = new RoutePlannerViewModel(dialogService, fileDialogService, () => null)
        {
            CurrentMap = graph,
            CityOne = " lviv ",
            CityTwo = " KYIV ",
            CityDistance = "540"
        };

        await vm.RemoveEdgeCommand.ExecuteAsync(null);

        vm.CurrentMap.HasEdge("Kyiv", "Lviv").ShouldBeFalse();
        vm.FullMapDisplay.ShouldBe(AppMessages.EmptyMapStatus);
        vm.CityOne.ShouldBeEmpty();
        vm.CityTwo.ShouldBeEmpty();
        vm.CityDistance.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Category", "negative")]
    public async Task RemoveEdge_WhenEdgeNotFound_ShowsValidationError()
    {
        var dialogService = Substitute.For<IDialogService>();
        var fileDialogService = Substitute.For<IFileDialogService>();
        var graph = new CityGraph();
        graph.AddEdge("Kyiv", "Lviv", 540);

        var vm = new RoutePlannerViewModel(dialogService, fileDialogService, () => null)
        {
            CurrentMap = graph,
            CityOne = "Kyiv",
            CityTwo = "Odesa",
            CityDistance = "480"
        };

        await vm.RemoveEdgeCommand.ExecuteAsync(null);

        await dialogService.Received(1)
            .ShowMessageAsync(DialogTitles.ValidationError, AppMessages.EdgeNotFoundError);
    }

    [Fact]
    [Trait("Category", "positive")]
    public async Task DeleteSelectedEdge_WhenEdgeSelected_RemovesEdgeAndClearsSelection()
    {
        var dialogService = Substitute.For<IDialogService>();
        var fileDialogService = Substitute.For<IFileDialogService>();
        var graph = new CityGraph();
        graph.AddEdge("Kyiv", "Kharkiv", 480);

        var vm = new RoutePlannerViewModel(dialogService, fileDialogService, () => null)
        {
            CurrentMap = graph
        };
        vm.UpdateMapDisplay(graph);

        vm.MapEdges.Count.ShouldBe(1);
        vm.HasMapEdges.ShouldBeTrue();
        vm.SelectedEdge = vm.MapEdges[0];
        vm.HasSelectedEdge.ShouldBeTrue();

        await vm.DeleteSelectedEdgeCommand.ExecuteAsync(null);

        vm.CurrentMap.HasEdge("Kyiv", "Kharkiv").ShouldBeFalse();
        vm.MapEdges.ShouldBeEmpty();
        vm.HasMapEdges.ShouldBeFalse();
        vm.SelectedEdge.ShouldBeNull();
        vm.HasSelectedEdge.ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public async Task SaveRoute_WhenNoTraveler_ShowsError()
    {
        var dialogService = Substitute.For<IDialogService>();
        var fileDialogService = Substitute.For<IFileDialogService>();
        var vm = new RoutePlannerViewModel(dialogService, fileDialogService, () => null);

        await vm.SaveRouteCommand.ExecuteAsync(null);

        await dialogService.Received(1)
            .ShowMessageAsync(DialogTitles.Error, AppMessages.NoRouteToSaveError);
        await fileDialogService.DidNotReceiveWithAnyArgs()
            .SaveFileDialogAsync(default!, default!, default!);
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public async Task SaveRoute_WhenRouteIsEmpty_ShowsError()
    {
        var dialogService = Substitute.For<IDialogService>();
        var fileDialogService = Substitute.For<IFileDialogService>();
        var traveler = new Traveler("Alice");
        traveler.SetLocation("Kyiv");
        var vm = new RoutePlannerViewModel(dialogService, fileDialogService, () => traveler);

        await vm.SaveRouteCommand.ExecuteAsync(null);

        await dialogService.Received(1)
            .ShowMessageAsync(DialogTitles.Error, AppMessages.NoRouteToSaveError);
    }

    [Fact]
    [Trait("Category", "positive")]
    public async Task SaveRoute_WhenValidRoute_WritesTxtFileAndShowsSuccess()
    {
        var dialogService = Substitute.For<IDialogService>();
        var fileDialogService = Substitute.For<IFileDialogService>();
        var tempFile = Path.Combine(Path.GetTempPath(), $"route_test_{System.Guid.NewGuid():N}.txt");
        fileDialogService.SaveFileDialogAsync(
                DialogTitles.SaveRoute,
                FileFilterConstants.DefaultRouteFileName,
                FileFilterConstants.TxtExtension)
            .Returns(tempFile);

        var traveler = new Traveler("Alice");
        traveler.SetLocation("Kyiv");
        traveler.AddCity("Kyiv");
        traveler.AddCity("Lviv");

        var vm = new RoutePlannerViewModel(dialogService, fileDialogService, () => traveler)
        {
            TotalDistanceText = "540 km"
        };

        try
        {
            await vm.SaveRouteCommand.ExecuteAsync(null);

            File.Exists(tempFile).ShouldBeTrue();
            var text = await File.ReadAllTextAsync(tempFile, TestContext.Current.CancellationToken);
            text.ShouldContain("Traveler: Alice");
            text.ShouldContain("Current Location: Kyiv");
            text.ShouldContain("Route: Kyiv -> Lviv");
            text.ShouldContain("Total Distance: 540 km");

            await dialogService.Received(1)
                .ShowMessageAsync(DialogTitles.Saved, AppMessages.RouteSavedSuccess(tempFile));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
