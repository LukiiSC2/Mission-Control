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

public class TravelerDetailsViewModelTests
{
    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public async Task CreateTraveler_WhenFieldsAreEmpty_ShowsValidationError()
    {
        var dialogService = Substitute.For<IDialogService>();
        var fileDialogService = Substitute.For<IFileDialogService>();
        var vm = new TravelerDetailsViewModel(dialogService, fileDialogService)
        {
            TravelerName = "",
            TravelerLocation = ""
        };

        await vm.CreateTravelerCommand.ExecuteAsync(null);

        await dialogService.Received(1)
            .ShowMessageAsync(DialogTitles.ValidationError, Arg.Is<string>(msg => msg.Contains("cannot be empty")));
        vm.CurrentTraveler.ShouldBeNull();
    }

    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "critical-path")]
    public async Task CreateTraveler_WhenFieldsAreValid_SetsTravelerAndUpdatesDisplay()
    {
        var dialogService = Substitute.For<IDialogService>();
        var fileDialogService = Substitute.For<IFileDialogService>();
        var vm = new TravelerDetailsViewModel(dialogService, fileDialogService)
        {
            TravelerName = "Alice",
            TravelerLocation = "Kyiv"
        };

        await vm.CreateTravelerCommand.ExecuteAsync(null);

        vm.CurrentTraveler.ShouldNotBeNull();
        vm.CurrentTraveler.GetName().ShouldBe("Alice");
        vm.CurrentTravelerDisplay.ShouldContain("Alice");
        vm.CurrentTravelerDisplay.ShouldContain("Kyiv");
        await dialogService.Received(1)
            .ShowMessageAsync(DialogTitles.Success, Arg.Any<string>());
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public async Task SaveTraveler_WhenNoTravelerExists_ShowsError()
    {
        var dialogService = Substitute.For<IDialogService>();
        var fileDialogService = Substitute.For<IFileDialogService>();
        var vm = new TravelerDetailsViewModel(dialogService, fileDialogService);

        await vm.SaveTravelerCommand.ExecuteAsync(null);

        await dialogService.Received(1)
            .ShowMessageAsync(DialogTitles.Error, AppMessages.NoTravelerToSaveError);
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public async Task SaveTraveler_WhenCancelled_DoesNotSave()
    {
        var dialogService = Substitute.For<IDialogService>();
        var fileDialogService = Substitute.For<IFileDialogService>();
        fileDialogService.SaveFileDialogAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns((string?)null);

        var vm = new TravelerDetailsViewModel(dialogService, fileDialogService);
        vm.SetTraveler(new Traveler("Alice"));

        await vm.SaveTravelerCommand.ExecuteAsync(null);

        await dialogService.DidNotReceive()
            .ShowMessageAsync(DialogTitles.Saved, Arg.Any<string>());
    }

    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "critical-path")]
    [Trait("Category", "integration")]
    public async Task SaveTraveler_WhenValidTraveler_SavesToFileAndShowsSuccess()
    {
        var dialogService = Substitute.For<IDialogService>();
        var fileDialogService = Substitute.For<IFileDialogService>();
        string tempFile = Path.Combine(Path.GetTempPath(), $"save_test_{System.Guid.NewGuid():N}.json");

        try
        {
            fileDialogService.SaveFileDialogAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(tempFile);

            var vm = new TravelerDetailsViewModel(dialogService, fileDialogService);
            var traveler = new Traveler("Alice");
            traveler.SetLocation("Kyiv");
            vm.SetTraveler(traveler);

            await vm.SaveTravelerCommand.ExecuteAsync(null);

            File.Exists(tempFile).ShouldBeTrue();
            await dialogService.Received(1)
                .ShowMessageAsync(DialogTitles.Saved, Arg.Any<string>());
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public async Task LoadTraveler_WhenCancelled_DoesNotChangeState()
    {
        var dialogService = Substitute.For<IDialogService>();
        var fileDialogService = Substitute.For<IFileDialogService>();
        fileDialogService.OpenFileDialogAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns((string?)null);

        var vm = new TravelerDetailsViewModel(dialogService, fileDialogService);

        await vm.LoadTravelerCommand.ExecuteAsync(null);

        vm.CurrentTraveler.ShouldBeNull();
    }

    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "critical-path")]
    [Trait("Category", "integration")]
    public async Task LoadTraveler_WhenValidFile_LoadsAndSetsTravelerAndShowsSuccess()
    {
        var dialogService = Substitute.For<IDialogService>();
        var fileDialogService = Substitute.For<IFileDialogService>();

        var traveler = new Traveler("Bob");
        traveler.SetLocation("Odesa");
        traveler.AddCity("Kyiv");
        traveler.AddCity("Lviv");
        string tempFile = TravelerFaker.CreateTempTravelerFile(traveler);

        try
        {
            fileDialogService.OpenFileDialogAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(tempFile);

            var vm = new TravelerDetailsViewModel(dialogService, fileDialogService);
            Traveler? notifiedTraveler = null;
            vm.TravelerChanged += t => notifiedTraveler = t;

            await vm.LoadTravelerCommand.ExecuteAsync(null);

            vm.CurrentTraveler.ShouldNotBeNull();
            vm.CurrentTraveler.GetName().ShouldBe("Bob");
            vm.TravelerName.ShouldBe("Bob");
            vm.TravelerLocation.ShouldBe("Odesa");
            vm.CurrentTravelerDisplay.ShouldContain("Bob");
            vm.CurrentTravelerDisplay.ShouldContain("Odesa");
            notifiedTraveler.ShouldNotBeNull();
            notifiedTraveler.GetName().ShouldBe("Bob");

            await dialogService.Received(1)
                .ShowMessageAsync(DialogTitles.TravelerLoaded, Arg.Is<string>(msg => msg.Contains("Bob")));
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "integration")]
    public async Task LoadTraveler_WhenCorruptFile_ShowsErrorAndResetsTraveler()
    {
        var dialogService = Substitute.For<IDialogService>();
        var fileDialogService = Substitute.For<IFileDialogService>();
        string tempFile = TravelerFaker.CreateTempTravelerJson("{ corrupt json ... }");

        try
        {
            fileDialogService.OpenFileDialogAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(tempFile);

            var vm = new TravelerDetailsViewModel(dialogService, fileDialogService);
            vm.SetTraveler(new Traveler("OldTraveler"));

            await vm.LoadTravelerCommand.ExecuteAsync(null);

            vm.CurrentTraveler.ShouldBeNull();
            vm.LoadedTravelerInfo.ShouldBe("Failed to load traveler");
            await dialogService.Received(1)
                .ShowMessageAsync(DialogTitles.LoadError, Arg.Any<string>());
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public void SetTraveler_WhenNull_ResetsToDefaultsAndInvokesEvent()
    {
        var dialogService = Substitute.For<IDialogService>();
        var fileDialogService = Substitute.For<IFileDialogService>();
        var vm = new TravelerDetailsViewModel(dialogService, fileDialogService);

        vm.SetTraveler(new Traveler("Alice"));

        Traveler? notified = new Traveler("Initial");
        vm.TravelerChanged += t => notified = t;

        vm.SetTraveler(null);

        vm.CurrentTraveler.ShouldBeNull();
        vm.CurrentTravelerDisplay.ShouldBe(AppMessages.DefaultTravelerStatus);
        notified.ShouldBeNull();
    }
}
