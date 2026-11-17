using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using Konsept.Agent.Infrastructure.Concrete;
using Konsept.Agent.Infrastructure.Setup;
using Shouldly;

namespace Konsept.Agent.Tests;

/// <summary>
/// Kurulum motorunun yerel model biçimleri (spec 0028 AC-3, AC-4, AC-10): çok dosyalı ağırlık bileşeni (dosya başına boyut + SHA-256, liste
/// hash'i, açılış bütünlüğü), Python paket ortamı (kurulu Python + uv, kilit hash'i, önbellek) ve ön kontrolde Atlandı bileşenler.
/// Sahte indirici, gerçek hash ve dosyalar geçici klasörde.
/// </summary>
public sealed class SetupCoordinatorModelTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("anewconcept-models-");
    private readonly FakeSetupHost _host = new();

    private string Root => Path.Combine(_root.FullName, "anewconcept");

    /// <inheritdoc />
    public void Dispose() => _root.Delete(recursive: true);

    /// <summary>AC-3: dosyalar iner, doğrulanır, dosya listesi yazılır; yeniden açılışta Kuruldu; boyutu değişen dosya Onarılmalı.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Files_InstallVerifyManifest_IntegrityBySize()
    {
        var model = _host.AddFiles("model", ("onnx/a.onnx", "ağırlık A"), ("voice.json", "{}"));
        var setup = await CreateAsync([model]);
        State(setup, "model").Status.ShouldBe(ComponentStatus.Skipped); // İsteğe bağlı: ilk kurulumda inmez.

        (await setup.InstallAsync(["model"], TestContext.Current.CancellationToken)).ShouldBeTrue();

        State(setup, "model").Status.ShouldBe(ComponentStatus.Installed);
        var dir = Path.Combine(Root, "components", "model");
        (await File.ReadAllTextAsync(Path.Combine(dir, "onnx", "a.onnx"), TestContext.Current.CancellationToken)).ShouldBe("ağırlık A");
        (await File.ReadAllTextAsync(Path.Combine(dir, SetupComponent.FilesManifestName), TestContext.Current.CancellationToken))
            .ShouldBe(Konsept.Agent.Domain.Rules.ComponentRules.FilesManifest(model.Files!));
        Directory.Exists(dir + ".partial").ShouldBeFalse();

        var reopened = await CreateAsync([model]);
        State(reopened, "model").Status.ShouldBe(ComponentStatus.Installed);

        await File.AppendAllTextAsync(Path.Combine(dir, "voice.json"), " ", TestContext.Current.CancellationToken);
        var tampered = await CreateAsync([model]);
        State(tampered, "model").Status.ShouldBe(ComponentStatus.NeedsRepair);
    }

    /// <summary>AC-3: SHA-256'sı uyuşmayan dosya kurulmaz (bileşen Hata, klasör yok); düzelince yalnızca o dosya yeniden iner.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Files_HashMismatch_FailsThenRetryRedownloadsOnlyBadFile()
    {
        var model = _host.AddFiles("model", ("a.bin", "birinci"), ("b.bin", "ikinci"));
        var bad = model.Files![1].Url;
        var good = _host.Content[bad];
        _host.Content[bad] = [.. good.Reverse()];
        var setup = await CreateAsync([model]);

        await setup.InstallAsync(["model"], TestContext.Current.CancellationToken);

        var state = State(setup, "model");
        (state.Status, state.Failure, state.FailedStep).ShouldBe((ComponentStatus.Failed, SetupFailure.VerifyFailed, InstallStep.Verify));
        Directory.Exists(Path.Combine(Root, "components", "model")).ShouldBeFalse();

        _host.Content[bad] = good;
        await setup.InstallAsync(["model"], TestContext.Current.CancellationToken);

        State(setup, "model").Status.ShouldBe(ComponentStatus.Installed);
        _host.Downloads.Count(u => u == model.Files[0].Url).ShouldBe(1);
        _host.Downloads.Count(u => u == bad).ShouldBe(2);
    }

    /// <summary>AC-3: katalogdaki liste hash'i dosyalarla uyuşmazsa ya da yol klasör dışına çıkıyorsa hiçbir şey inmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Files_ManifestMismatchOrUnsafePath_FailsWithoutDownload()
    {
        var wrongHash = _host.AddFiles("wrong", ("a.bin", "x")) with { Sha256 = new string('0', 64) };
        var escape = _host.AddFiles("escape", ("../dışarı.bin", "x"));
        var setup = await CreateAsync([wrongHash, escape]);

        await setup.InstallAsync(["wrong", "escape"], TestContext.Current.CancellationToken);

        State(setup, "wrong").Failure.ShouldBe(SetupFailure.VerifyFailed);
        State(setup, "escape").Failure.ShouldBe(SetupFailure.VerifyFailed);
        _host.Downloads.ShouldBeEmpty();
        File.Exists(Path.Combine(Root, "components", "dışarı.bin")).ShouldBeFalse();
    }

    /// <summary>AC-4: Python ortamı kurulu Python ve uv ile, katalogdaki kilit hash'i ve varyantla, ajan içi önbellekle kurulur; indirme yok.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Python_UsesInstalledRuntimeAndUv_LockHash_NoDownload()
    {
        var python = _host.Add(SetupComponent.PythonRuntimeId, required: false, check: null);
        var uv = _host.Add(SetupComponent.UvId, required: false, check: null);
        var env = FakeSetupHost.AddPython("speech", variant: "cpu");
        var setup = await CreateAsync([python, uv, env]);

        await setup.InstallAsync(["python", "uv", "speech"], TestContext.Current.CancellationToken);

        State(setup, "speech").Status.ShouldBe(ComponentStatus.Installed);
        var call = _host.PythonInstalls.ShouldHaveSingleItem();
        call.ShouldBe((
            "speech",
            "cpu",
            env.Sha256,
            Path.Combine(Root, "components", "uv", "bin/tool.exe"),
            Path.Combine(Root, "components", "python", "bin/tool.exe"),
            Path.Combine(Root, "downloads", "uv-cache")));
        _host.Downloads.ShouldNotContain(env.Url);
        File.Exists(Path.Combine(Root, "components", "speech", "pkg", "__init__.py")).ShouldBeTrue();
    }

    /// <summary>AC-4: Python ya da uv kurulu değilse ortam kurulmaz; uv başarısızsa kısmi klasör kalmaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Python_MissingRuntimeOrFailure_Fails()
    {
        var python = _host.Add(SetupComponent.PythonRuntimeId, required: false, check: null);
        var uv = _host.Add(SetupComponent.UvId, required: false, check: null);
        var env = FakeSetupHost.AddPython("speech");
        var setup = await CreateAsync([python, uv, env]);

        await setup.InstallAsync(["speech"], TestContext.Current.CancellationToken);
        State(setup, "speech").Status.ShouldBe(ComponentStatus.Failed);
        _host.PythonInstalls.ShouldBeEmpty();

        _host.PythonSucceeds = false;
        await setup.InstallAsync(["python", "uv", "speech"], TestContext.Current.CancellationToken);
        State(setup, "speech").Status.ShouldBe(ComponentStatus.Failed);
        Directory.Exists(Path.Combine(Root, "components", "speech.partial")).ShouldBeFalse();
        Directory.Exists(Path.Combine(Root, "components", "speech")).ShouldBeFalse();
    }

    /// <summary>AC-10: ön kontrol yalnızca kurulacak bileşenleri sayar; Atlandı (isteğe bağlı) büyük model alan hesabına girmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Preflight_SkippedNotCounted()
    {
        var core = _host.Add("core");
        var huge = _host.AddFiles("huge", ("w.bin", "x")) with { DownloadBytes = 20_000_000_000, InstalledBytes = 20_000_000_000 };
        var setup = await CreateAsync([core, huge]);

        setup.Preflight().RequiredBytes.ShouldBe(Konsept.Agent.Domain.Rules.SetupRules.RequiredBytes([core]));
    }

    private async Task<SetupCoordinator> CreateAsync(IReadOnlyList<SetupComponent> components)
    {
        var setup = new SetupCoordinator(components, _host, new FileSetupStateStore(Root), Root, Path.Combine(_root.FullName, "Videolar"), null);
        await setup.InspectAsync(TestContext.Current.CancellationToken);
        return setup;
    }

    private static ComponentState State(SetupCoordinator setup, string id) => setup.Components.Single(c => c.Component.Id == id);
}
