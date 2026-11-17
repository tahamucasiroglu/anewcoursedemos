using System.Text.RegularExpressions;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Rules;
using Konsept.Agent.Infrastructure.Concrete;
using Konsept.Agent.Infrastructure.Setup;
using Shouldly;

namespace Konsept.Agent.Tests;

/// <summary>Ajana gömülü bileşen listesi (spec 0009 AC-2, Req 7, 11): çekirdek, sabit sürüm, resmi HTTPS adres, SHA-256; Ollama yok.</summary>
public sealed partial class ComponentCatalogTests
{
    /// <summary>
    /// Çekirdek araçlar zorunlu ve listenin başında, sürümü sabit, adresi HTTPS ve hash'i 64 haneli; listede Ollama yok. Plan 0028 A-1: liste
    /// yerel modeller ve ortamlarıyla büyüdü; onlar isteğe bağlıdır (ilk kurulumda inmez) ve çekirdekten sonra gelir.
    /// </summary>
    [Fact]
    public void Catalog_CoreComponents_PinnedHttpsSha256_NoOllama()
    {
        var catalog = ComponentCatalog.Load();

        catalog.Take(4).Select(c => c.Id).ShouldBe(["node", "ffmpeg", "chromium", "hyperframes"]); // A-1 (plan 0018): HyperFrames eklendi (B-73/B-118).
        catalog.Take(4).ShouldAllBe(c => c.Required);
        catalog.Skip(4).ShouldAllBe(c => !c.Required);
        catalog.ShouldAllBe(c => c.Url.Scheme == "https");
        catalog.ShouldAllBe(c => Sha().IsMatch(c.Sha256));
        catalog.ShouldAllBe(c => c.DownloadBytes > 0 && c.InstalledBytes > 0);
        catalog.ShouldAllBe(c => !string.IsNullOrWhiteSpace(c.Name) && !string.IsNullOrWhiteSpace(c.Purpose) && !string.IsNullOrWhiteSpace(c.License));
        catalog.ShouldAllBe(c => !c.Url.AbsoluteUri.Contains("latest", StringComparison.OrdinalIgnoreCase));
        catalog.Single(c => c.Id == "node").CheckExpect.ShouldBe("v26.13.1");
        catalog.Single(c => c.Id == "ffmpeg").License.ShouldStartWith("LGPL");
        catalog.Single(c => c.Id == "ffmpeg").Mirrored.ShouldBeTrue();
        catalog.ShouldNotContain(c => c.Id.Contains("ollama", StringComparison.OrdinalIgnoreCase) || c.Name.Contains("Ollama", StringComparison.OrdinalIgnoreCase));
        catalog.Select(c => c.FileName).Distinct(StringComparer.Ordinal).Count().ShouldBe(catalog.Count);
    }

    /// <summary>
    /// Spec 0018 / B-118: HyperFrames sabit sürüm (0.9.43), Node'dan sonra, npm ile ajana gömülü kilit dosyasından kurulur; katalogdaki hash
    /// gömülü kilit dosyasınınkine eşit (kilit dosyası değişirse kurulum reddedilir) ve kilit dosyası yalnızca sabit sürümü çözer.
    /// </summary>
    [Fact]
    public void Catalog_HyperFrames_PinnedNpm_LockHashMatches()
    {
        var catalog = ComponentCatalog.Load().ToList();
        var hf = catalog.Single(c => c.Id == "hyperframes");
        hf.Version.ShouldBe("0.9.43");
        hf.Installer.ShouldBe(Konsept.Agent.Domain.Entities.SetupComponent.Npm);
        catalog.IndexOf(hf).ShouldBeGreaterThan(catalog.FindIndex(c => c.Id == "node"));
        using var stream = typeof(WindowsSetupHost).Assembly.GetManifestResourceStream("Konsept.Agent.Infrastructure.Setup.hyperframes.package-lock.json")!;
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        WindowsSetupHost.LockSha256(memory.ToArray()).ShouldBe(hf.Sha256);
        var lockText = System.Text.Encoding.UTF8.GetString(memory.ToArray());
        lockText.ShouldContain("\"resolved\": \"https://registry.npmjs.org/hyperframes/-/hyperframes-0.9.43.tgz\"", Case.Sensitive);
        lockText.ShouldContain("\"integrity\": \"sha512-");
    }

    /// <summary>
    /// Spec 0028 AC-1: dört yerel model ve ortamları listede, isteğe bağlı, aynasız; bağımlılıklar listede ve kendilerinden önce; her modelin
    /// çalıştırma şablonu çözülebilir (araç kapanışta, yer tutucular bilinen, betik gömülü); dosyalar commit'e sabit HTTPS adresli, SHA-256'lı,
    /// liste hash'i katalogdakiyle aynı; boyutlar dosyaların toplamı; görsel modelleri ekran kartı ister, ses modelleri istemez.
    /// </summary>
    [Fact]
    public void Catalog_LocalModels_Pinned_Resolvable()
    {
        var catalog = ComponentCatalog.Load().ToList();
        var ids = catalog.Select(c => c.Id).ToList();
        var models = catalog.Where(c => c.Run is not null).Select(c => c.Id).ToList();
        models.ShouldBe(["z-image-turbo", "flux2-klein-4b", "supertonic-3", "chatterbox-multilingual-v3"], ignoreOrder: true);
        foreach (var c in catalog.Skip(4))
        {
            c.Mirrored.ShouldBeFalse(c.Id);
            c.DownloadBytes.ShouldBeGreaterThan(0, c.Id);
            c.InstalledBytes.ShouldBeGreaterThanOrEqualTo(c.DownloadBytes, c.Id);
            foreach (var dependency in c.Requires ?? [])
            {
                ids.IndexOf(dependency).ShouldBeInRange(0, ids.IndexOf(c.Id) - 1, c.Id + " → " + dependency);
            }
        }

        foreach (var c in catalog.Where(c => c.Installer == SetupComponent.FilesInstaller))
        {
            c.Executable.ShouldBe(SetupComponent.FilesManifestName);
            c.Files.ShouldNotBeEmpty(c.Id);
            c.Files!.ShouldAllBe(f => f.Url.Scheme == "https" && f.Url.Host == "huggingface.co" && Commit().IsMatch(f.Url.AbsolutePath) && Sha().IsMatch(f.Sha256) && f.Bytes > 0);
            c.Files!.ShouldAllBe(f => ComponentRules.IsSafeRelativePath(f.Path));
            ComponentRules.FilesManifestSha256(c.Files!).ShouldBe(c.Sha256, c.Id);
            c.DownloadBytes.ShouldBe(c.Files!.Sum(f => f.Bytes), c.Id);
        }

        foreach (var id in models)
        {
            var model = catalog.Single(c => c.Id == id);
            var run = model.Run!;
            var closure = ComponentRules.Closure(id, catalog);
            closure.ShouldContain(run.Tool, id);
            run.TimeoutSeconds.ShouldBeGreaterThan(0);
            foreach (var name in ModelRunTemplate.Names(run.Args))
            {
                var known = name == ModelRunTemplate.Self || ModelRunTemplate.RuntimeNames.Contains(name) || closure.Contains(name)
                    || (name.StartsWith(ModelRunTemplate.ScriptPrefix, StringComparison.Ordinal)
                        && typeof(LocalModelStages).Assembly.GetManifestResourceInfo(LocalModelStages.ScriptResourcePrefix + name[ModelRunTemplate.ScriptPrefix.Length..]) is not null);
                known.ShouldBeTrue(id + ": " + name);
            }

            (model.Gpu is not null).ShouldBe(run.Task == ModelRun.Image, id);
            ModelRunTemplate.Names(run.Args).ShouldContain(run.Task == ModelRun.Image ? ModelRunTemplate.Prompt : ModelRunTemplate.Input, id);
        }

        catalog.Single(c => c.Id == "supertonic-3").License.ShouldBe("OpenRAIL-M");
        catalog.Single(c => c.Id == "z-image-turbo").Gpu!.MinVramMb.ShouldBe(4096);
    }

    /// <summary>Spec 0028 AC-4, AC-5: her Python ortamının (ve varyantının) gömülü kilit dosyası var, hash'i katalogdakiyle aynı, her paket hash'li; varyant karta göre çözülür.</summary>
    [Fact]
    public void Catalog_PythonLocks_HashMatch_Variants()
    {
        var catalog = ComponentCatalog.Load();
        foreach (var env in catalog.Where(c => c.Installer == SetupComponent.Python))
        {
            env.Requires.ShouldBe([SetupComponent.PythonRuntimeId, SetupComponent.UvId], ignoreOrder: true);
            var locks = env.Variants is { } variants ? variants.Select(v => (v.Id, v.Sha256)).ToList() : [(null as string, env.Sha256)];
            foreach (var (variant, sha) in locks)
            {
                using var stream = typeof(WindowsSetupHost).Assembly.GetManifestResourceStream(WindowsSetupHost.PythonLockResource(env.Id, variant)).ShouldNotBeNull(env.Id + variant);
                using var memory = new MemoryStream();
                stream.CopyTo(memory);
                WindowsSetupHost.LockSha256(memory.ToArray()).ShouldBe(sha, env.Id + variant);
                var text = System.Text.Encoding.UTF8.GetString(memory.ToArray());
                text.ShouldContain("--hash=sha256:");
                text.Split('\n').Where(l => Regex.IsMatch(l, "^[a-z0-9]", RegexOptions.IgnoreCase)).ShouldAllBe(l => l.Contains("=="));
            }
        }

        ComponentCatalog.Load(new GpuInfo("RTX 4080", GpuInfo.Nvidia, 16L << 30)).Single(c => c.Id == "speech-torch").Variant.ShouldBe("cuda");
        ComponentCatalog.Load(new GpuInfo("RTX 3060", GpuInfo.Nvidia, 6L << 30)).Single(c => c.Id == "speech-torch").Variant.ShouldBe("cpu");
        ComponentCatalog.Load(null).Single(c => c.Id == "speech-torch").Variant.ShouldBe("cpu");
    }

    [GeneratedRegex("/resolve/[0-9a-f]{40}/")]
    private static partial Regex Commit();

    [GeneratedRegex("^[0-9a-f]{64}$")]
    private static partial Regex Sha();
}
