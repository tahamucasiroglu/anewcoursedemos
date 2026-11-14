using NSubstitute;
using Shouldly;
using TahaKonsept.Agent.Abstractions.Engines;
using TahaKonsept.Agent.Abstractions.Tools;
using TahaKonsept.Agent.Abstractions.Tools.Abstract;
using TahaKonsept.Agent.Core.Prerequisites.Abstract;
using TahaKonsept.Agent.Core.Tools.Concrete;
using TahaKonsept.BuildingBlocks.Domain.Results;
using Xunit;

namespace TahaKonsept.Agent.Core.Tests.Tools;

/// <summary><see cref="PrerequisiteToolProvisioner"/> testleri: eksik araç ilgili ön gereksinimle kurulur, varsa dokunulmaz.</summary>
public sealed class PrerequisiteToolProvisionerTests
{
    /// <summary>uv yoksa 'uv' bileşeni kurulur ve kurulumdan sonraki yol döner.</summary>
    [Fact]
    public async Task Ensure_InstallsMissingUvThroughPrerequisite()
    {
        var installed = false;
        var tools = Substitute.For<IToolLocator>();
        tools.Find(ToolKind.Uv).Returns(_ => installed ? "C:/agent/Tools/uv/uv.exe" : null);
        tools.Require(ToolKind.Uv).Returns(_ => installed
            ? Result.Success("C:/agent/Tools/uv/uv.exe")
            : Result.Failure<string>(Error.NotFound("agent.tool_missing", "uv yok")));
        var uv = Prerequisite("uv", () => installed = true);
        var other = Prerequisite("node", () => throw new InvalidOperationException("node kurulmamalı"));
        using var provisioner = new PrerequisiteToolProvisioner(tools, [other, uv]);

        var result = await provisioner.EnsureAsync(ToolKind.Uv, null, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("C:/agent/Tools/uv/uv.exe");
        await uv.Received(1).InstallAsync(Arg.Any<IProgress<EngineSetupProgress>?>(), Arg.Any<CancellationToken>());
        await other.DidNotReceive().InstallAsync(Arg.Any<IProgress<EngineSetupProgress>?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Araç zaten varsa hiçbir şey kurulmaz.</summary>
    [Fact]
    public async Task Ensure_ReturnsExistingToolWithoutInstalling()
    {
        var tools = Substitute.For<IToolLocator>();
        tools.Find(ToolKind.Uv).Returns("C:/uv.exe");
        var uv = Prerequisite("uv", () => { });
        using var provisioner = new PrerequisiteToolProvisioner(tools, [uv]);

        var result = await provisioner.EnsureAsync(ToolKind.Uv, null, TestContext.Current.CancellationToken);

        result.Value.ShouldBe("C:/uv.exe");
        await uv.DidNotReceive().InstallAsync(Arg.Any<IProgress<EngineSetupProgress>?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Python istenip uv de yoksa önce uv, sonra Python kurulur (kilitlenmeden).</summary>
    [Fact]
    public async Task Ensure_PythonInstallsUvFirst()
    {
        var order = new List<string>();
        var tools = Substitute.For<IToolLocator>();
        tools.Find(ToolKind.Uv).Returns(_ => order.Contains("uv") ? "uv.exe" : null);
        tools.Find(ToolKind.Python).Returns(_ => order.Contains("python") ? "python.exe" : null);
        tools.Require(ToolKind.Python).Returns(_ => Result.Success("python.exe"));
        using var provisioner = new PrerequisiteToolProvisioner(tools, [Prerequisite("python", () => order.Add("python")), Prerequisite("uv", () => order.Add("uv"))]);

        var result = await provisioner.EnsureAsync(ToolKind.Python, null, TestContext.Current.CancellationToken);

        result.Value.ShouldBe("python.exe");
        order.ShouldBe(["uv", "python"]);
    }

    /// <summary>Otomatik kurulamayan araçta bulucunun anlaşılır hatası döner.</summary>
    [Fact]
    public async Task Ensure_UnmappedToolReturnsLocatorError()
    {
        var tools = Substitute.For<IToolLocator>();
        tools.Find(ToolKind.ComfyUi).Returns((string?)null);
        tools.Require(ToolKind.ComfyUi).Returns(Result.Failure<string>(Error.NotFound("agent.tool_missing", "ComfyUI yok")));
        using var provisioner = new PrerequisiteToolProvisioner(tools, []);

        var result = await provisioner.EnsureAsync(ToolKind.ComfyUi, null, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("agent.tool_missing");
    }

    private static IPrerequisite Prerequisite(string id, Action onInstall)
    {
        var prerequisite = Substitute.For<IPrerequisite>();
        prerequisite.Id.Returns(id);
        prerequisite.DisplayName.Returns(id);
        prerequisite.InstallAsync(Arg.Any<IProgress<EngineSetupProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                onInstall();
                return Task.FromResult(Result.Success());
            });
        return prerequisite;
    }
}
