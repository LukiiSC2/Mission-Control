using MissionControl.ViewModels;
using Shouldly;
using Xunit;

namespace SmartTravelPlanner.Tests.ViewModels;

public class MainViewModelTests
{
    [Fact]
    public void Greeting_ByDefault_ShouldHaveExpectedValue()
    {
        // Arrange
        var viewModel = new MainViewModel();

        // Act & Assert using Shouldly
        viewModel.Greeting.ShouldBe("Welcome to Avalonia!");
    }

    [Fact]
    public void Greeting_WhenChanged_ShouldUpdateValue()
    {
        // Arrange
        var viewModel = new MainViewModel();

        // Act
        viewModel.Greeting = "Hello, .NET 10!";

        // Assert using Shouldly
        viewModel.Greeting.ShouldBe("Hello, .NET 10!");
    }
}
