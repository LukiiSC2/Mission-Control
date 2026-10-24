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

public class MainViewModelTests
{
    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "smoke")]
    public void ChildViewModels_AreInitializedProperly()
    {
        var dialogService = Substitute.For<IDialogService>();
        var fileDialogService = Substitute.For<IFileDialogService>();

        var vm = new MainViewModel(dialogService, fileDialogService);

        vm.TravelerDetails.ShouldNotBeNull();
        vm.RoutePlanner.ShouldNotBeNull();
        vm.CreateTravelerCommand.ShouldNotBeNull();
        vm.SaveTravelerCommand.ShouldNotBeNull();
        vm.LoadTravelerCommand.ShouldNotBeNull();
        vm.LoadMapCommand.ShouldNotBeNull();
        vm.PlanRouteCommand.ShouldNotBeNull();
        vm.ClearRouteCommand.ShouldNotBeNull();
        vm.DeleteTravelerCommand.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public void TravelerChanged_WhenSetToNull_ClearsRouteInRoutePlanner()
    {
        var dialogService = Substitute.For<IDialogService>();
        var fileDialogService = Substitute.For<IFileDialogService>();
        var vm = new MainViewModel(dialogService, fileDialogService);

        var traveler = new Traveler("Alice");
        traveler.AddCity("Kyiv");
        traveler.AddCity("Lviv");
        vm.TravelerDetails.SetTraveler(traveler);

        vm.RoutePlanner.TotalDistanceText = "500 km";
        vm.RoutePlanner.FullRouteDisplay = "Kyiv -> Lviv";
        vm.RoutePlanner.RouteCities.Add("Kyiv");
        vm.RoutePlanner.RouteCities.Add("Lviv");

        vm.TravelerDetails.SetTraveler(null);

        vm.RoutePlanner.RouteCities.ShouldBeEmpty();
        vm.RoutePlanner.TotalDistanceText.ShouldBe(AppMessages.ZeroDistance);
        vm.RoutePlanner.FullRouteDisplay.ShouldBe(AppMessages.RouteClearedStatus);
    }

    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "critical-path")]
    public void TravelerChanged_WhenTravelerHasExistingRoute_UpdatesRoutePlanner()
    {
        var dialogService = Substitute.For<IDialogService>();
        var fileDialogService = Substitute.For<IFileDialogService>();
        var vm = new MainViewModel(dialogService, fileDialogService);

        var graph = new CityGraph();
        graph.AddEdge("Kyiv", "Lviv", 540);
        vm.RoutePlanner.CurrentMap = graph;

        var traveler = new Traveler("Bob");
        traveler.SetLocation("Kyiv");
        traveler.AddCity("Kyiv");
        traveler.AddCity("Lviv");

        vm.TravelerDetails.SetTraveler(traveler);

        vm.RoutePlanner.RouteCities.Count.ShouldBe(2);
        vm.RoutePlanner.RouteCities[0].ShouldBe("Kyiv");
        vm.RoutePlanner.RouteCities[1].ShouldBe("Lviv");
        vm.RoutePlanner.TotalDistanceText.ShouldBe("540 km");
        vm.RoutePlanner.FullRouteDisplay.ShouldBe("Kyiv -> Lviv");
    }

    [Fact]
    [Trait("Category", "positive")]
    public void ClearRouteCommand_ExecutesRoutePlannerClearRoute()
    {
        var dialogService = Substitute.For<IDialogService>();
        var fileDialogService = Substitute.For<IFileDialogService>();
        var vm = new MainViewModel(dialogService, fileDialogService);

        var traveler = new Traveler("Alice");
        traveler.AddCity("Kyiv");
        vm.TravelerDetails.SetTraveler(traveler);

        vm.RoutePlanner.RouteCities.Add("Kyiv");
        vm.RoutePlanner.FullRouteDisplay = "Kyiv";

        vm.ClearRouteCommand.Execute(null);

        vm.RoutePlanner.RouteCities.ShouldBeEmpty();
        vm.RoutePlanner.FullRouteDisplay.ShouldBe(AppMessages.RouteClearedStatus);
    }
}
