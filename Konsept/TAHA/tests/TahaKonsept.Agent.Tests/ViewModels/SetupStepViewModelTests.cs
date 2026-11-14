using Shouldly;
using TahaKonsept.Agent.Core.Prerequisites;
using TahaKonsept.Agent.ViewModels;
using TahaKonsept.Agent.ViewModels.Concrete;
using Xunit;

namespace TahaKonsept.Agent.Tests.ViewModels;

/// <summary>Kurulum adımı satırının durum geçişleri.</summary>
public sealed class SetupStepViewModelTests
{
    /// <summary>Test: Required missing step is pending and needs install.</summary>
    [Fact]
    public void Required_missing_step_is_pending_and_needs_install()
    {
        var step = new SetupStepViewModel(Status("node", optional: false, installed: false));

        step.State.ShouldBe(SetupStepState.Pending);
        step.IsSelected.ShouldBeTrue();
        step.NeedsInstall.ShouldBeTrue();
        step.CanToggle.ShouldBeFalse();
        step.StatusText.ShouldBe("Kurulacak");
    }

    /// <summary>Test: Optional missing step is skipped until selected.</summary>
    [Fact]
    public void Optional_missing_step_is_skipped_until_selected()
    {
        var step = new SetupStepViewModel(Status("uv", optional: true, installed: false));

        step.State.ShouldBe(SetupStepState.Skipped);
        step.NeedsInstall.ShouldBeFalse();

        step.IsSelected = true;
        step.State.ShouldBe(SetupStepState.Pending);
        step.NeedsInstall.ShouldBeTrue();

        step.IsSelected = false;
        step.State.ShouldBe(SetupStepState.Skipped);
    }

    /// <summary>Test: Installed step shows version and cannot toggle.</summary>
    [Fact]
    public void Installed_step_shows_version_and_cannot_toggle()
    {
        var step = new SetupStepViewModel(Status("ffmpeg", optional: true, installed: true, version: "7.1"));

        step.State.ShouldBe(SetupStepState.Installed);
        step.StatusText.ShouldBe("Kurulu (7.1)");
        step.CanToggle.ShouldBeFalse();
        step.NeedsInstall.ShouldBeFalse();
        step.Percent.ShouldBe(100);
    }

    /// <summary>Test: After install attempt missing required becomes failed and installed becomes done.</summary>
    [Fact]
    public void After_install_attempt_missing_required_becomes_failed_and_installed_becomes_done()
    {
        var failed = new SetupStepViewModel(Status("node", optional: false, installed: false)) { State = SetupStepState.Running };
        failed.Refresh(Status("node", optional: false, installed: false), afterInstallAttempt: true);
        failed.State.ShouldBe(SetupStepState.Failed);
        failed.IsFailed.ShouldBeTrue();

        var done = new SetupStepViewModel(Status("node", optional: false, installed: false)) { State = SetupStepState.Running };
        done.Refresh(Status("node", optional: false, installed: true), afterInstallAttempt: true);
        done.State.ShouldBe(SetupStepState.Done);
        done.IsOk.ShouldBeTrue();
    }

    /// <summary>Test: Unselected optional is not failed after install attempt.</summary>
    [Fact]
    public void Unselected_optional_is_not_failed_after_install_attempt()
    {
        var step = new SetupStepViewModel(Status("python", optional: true, installed: false));
        step.Refresh(Status("python", optional: true, installed: false), afterInstallAttempt: true);
        step.State.ShouldBe(SetupStepState.Skipped);
    }

    /// <summary>Test: Percent is clamped.</summary>
    [Fact]
    public void Percent_is_clamped()
    {
        var step = new SetupStepViewModel(Status("node", optional: false, installed: false)) { Percent = 150 };
        step.Percent.ShouldBe(100);
        step.Percent = -3;
        step.Percent.ShouldBe(0);
    }

    internal static PrerequisiteStatus Status(string id, bool optional, bool installed, string? version = null) =>
        new(id, id.ToUpperInvariant(), id + " açıklaması", optional ? "Python + CUDA" : "Temel", optional, installed, version);
}
