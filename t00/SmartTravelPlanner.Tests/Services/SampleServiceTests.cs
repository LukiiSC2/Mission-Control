using Bogus;
using NSubstitute;
using Shouldly;
using Xunit;

namespace SmartTravelPlanner.Tests.Services;

public interface ITravelGreetingService
{
    string GetGreeting(string destination);
}

public class SampleServiceTests
{
    [Fact]
    public void MockService_WithNSubstituteAndBogus_WorksWithShouldly()
    {
        // Arrange: Generate realistic fake data with Bogus
        var faker = new Faker();
        var city = faker.Address.City();
        var expectedGreeting = $"Welcome to {city}!";

        // Mock interface with NSubstitute
        var service = Substitute.For<ITravelGreetingService>();
        service.GetGreeting(city).Returns(expectedGreeting);

        // Act
        var result = service.GetGreeting(city);

        // Assert with Shouldly
        result.ShouldBe(expectedGreeting);
        result.ShouldStartWith("Welcome to");
        service.Received(1).GetGreeting(city);
    }
}
