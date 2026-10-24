using System;
using System.Threading.Tasks;

using MissionControl.Services;

using NSubstitute;

using Shouldly;

using Xunit;

namespace SmartTravelPlanner.Tests.Services;

public class DialogServiceExtensionsTests
{
    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "smoke")]
    [Trait("Category", "critical-path")]
    public async Task ExecuteWithDialogAsync_WhenActionSucceeds_ShowsSuccessMessageAndDoesNotInvokeOnError()
    {
        var dialogService = Substitute.For<IDialogService>();
        bool onErrorCalled = false;

        await dialogService.ExecuteWithDialogAsync(
            action: () => "Operation completed successfully",
            successTitle: "Success",
            errorTitle: "Error",
            formatError: ex => $"Failed: {ex.Message}",
            onError: () => onErrorCalled = true);

        await dialogService.Received(1)
            .ShowMessageAsync("Success", "Operation completed successfully");
        onErrorCalled.ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public async Task ExecuteWithDialogAsync_WhenActionThrows_InvokesOnErrorAndShowsFormattedError()
    {
        var dialogService = Substitute.For<IDialogService>();
        bool onErrorCalled = false;

        await dialogService.ExecuteWithDialogAsync(
            action: () => throw new InvalidOperationException("Something went wrong"),
            successTitle: "Success",
            errorTitle: "Error",
            formatError: ex => $"Custom formatted: {ex.Message}",
            onError: () => onErrorCalled = true);

        await dialogService.Received(1)
            .ShowMessageAsync("Error", "Custom formatted: Something went wrong");
        onErrorCalled.ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public async Task ExecuteWithDialogAsync_WhenDialogServiceIsNull_ThrowsArgumentNullException()
    {
        IDialogService? nullService = null;

        await Should.ThrowAsync<ArgumentNullException>(() =>
            nullService!.ExecuteWithDialogAsync(
                action: () => "result",
                successTitle: "Success",
                errorTitle: "Error",
                formatError: ex => ex.Message));
    }
}

