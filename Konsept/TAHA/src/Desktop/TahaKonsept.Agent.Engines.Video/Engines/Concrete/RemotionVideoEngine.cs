using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Abstractions.Engines;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Abstractions.Processes.Abstract;
using TahaKonsept.Agent.Abstractions.Tools;
using TahaKonsept.Agent.Abstractions.Tools.Abstract;
using TahaKonsept.Agent.Abstractions.Video;
using TahaKonsept.Agent.Engines.Video.Engines.Base;
using TahaKonsept.Agent.Engines.Video.Fonts;
using TahaKonsept.Agent.Engines.Video.Media.Abstract;
using TahaKonsept.Agent.Engines.Video.Safety.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Video.Engines.Concrete;

/// <summary>
/// Remotion (<c>remotion@4.0.543</c>, React/TSX) video motoru. LLM yalnızca <c>Scene.tsx</c> (default export React bileşeni) yazar;
/// motor <c>index.ts</c> (registerRoot), <c>Root.tsx</c> (boyut/fps/kare sayısı istekten) ve <c>TkFonts.tsx</c> (paketli fontları
/// <c>delayRender</c> ile yükler) dosyalarını üretir. Proje şablon içinde <c>work/&lt;guid&gt;</c> altında kurulur (node_modules çözümü),
/// render sonrası silinir; kod kopyası <c>WorkDir</c>'de kalır. Görseller <c>public/assets</c> → <c>staticFile("assets/x.png")</c>.
/// İlerleme: <c>Rendered N/T</c> (%0–90) ve <c>Encoded N/T</c> (%90–100). Lisans notu: ≤3 kişilik ekip ücretsiz.
/// </summary>
public sealed partial class RemotionVideoEngine : TsxVideoEngineBase
{
    /// <summary>Motor anahtarı.</summary>
    public const string EngineKey = "remotion";

    /// <summary>Sabit Remotion sürümü.</summary>
    public const string RemotionVersion = "4.0.543";

    /// <summary>Sabit React sürümü.</summary>
    public const string ReactVersion = "19.3.1";

    private static readonly IReadOnlySet<string> Imports = new HashSet<string>(StringComparer.Ordinal) { "react", "remotion" };

    /// <summary>Motor oluşturur.</summary>
    /// <param name="paths">Ajan klasörleri.</param>
    /// <param name="tools">Araç bulucu.</param>
    /// <param name="runner">Süreç çalıştırıcı.</param>
    /// <param name="probe">Medya süresi okuyucu.</param>
    /// <param name="guard">Güvenlik denetçisi.</param>
    /// <param name="logger">Günlükçü.</param>
    public RemotionVideoEngine(IAgentPaths paths, IToolLocator tools, IProcessRunner runner, IMediaProbe probe, ISceneCodeGuard guard, ILogger<RemotionVideoEngine> logger)
        : base(paths, tools, runner, probe, guard, logger)
    {
    }

    /// <inheritdoc />
    public override string Key => EngineKey;

    /// <inheritdoc />
    public override string DisplayName => "Remotion";

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, string> Dependencies { get; } = BuildDependencies();

    /// <inheritdoc />
    protected override string CliRelativePath => "node_modules/@remotion/cli/remotion-cli.js";

    /// <inheritdoc />
    protected override IReadOnlySet<string> AllowedImports => Imports;

    /// <inheritdoc />
    public override double? ParseProgress(string line)
    {
        var match = ProgressRegex().Match(line);
        if (!match.Success)
        {
            return null;
        }

        var done = double.Parse(match.Groups["d"].Value, CultureInfo.InvariantCulture);
        var total = double.Parse(match.Groups["t"].Value, CultureInfo.InvariantCulture);
        if (total <= 0)
        {
            return null;
        }

        var ratio = Math.Clamp(done / total, 0, 1);
        return match.Groups["k"].Value == "Rendered" ? ratio * 90 : 90 + (ratio * 10);
    }

    /// <inheritdoc />
    protected override bool IsRuntimeReady() => ResolveChrome() is not null;

    /// <inheritdoc />
    protected override async Task<Result> InstallRuntimeAsync(IProgress<EngineSetupProgress>? progress, CancellationToken ct)
    {
        if (ResolveChrome() is not null)
        {
            return Result.Success();
        }

        progress?.Report(new EngineSetupProgress("Remotion: headless Chrome indiriliyor…", 80));
        var ensure = await RunNodeAsync([CliPath, "browser", "ensure"], TimeSpan.FromMinutes(15), ct).ConfigureAwait(false);
        if (ensure.IsFailure)
        {
            return ensure.Error;
        }

        return ensure.Value.Succeeded && ResolveChrome() is not null
            ? Result.Success()
            : Error.Failure("video.remotion.browser_failed", $"Remotion için Chrome indirilemedi: {Tail(ensure.Value.StandardError, ensure.Value.StandardOutput)}");
    }

    /// <inheritdoc />
    protected override async Task<Result<SceneProject>> PrepareProjectAsync(SceneRenderRequest request, CancellationToken ct)
    {
        var codeCopy = Path.Combine(request.WorkDir, string.Create(CultureInfo.InvariantCulture, $"remotion-scene-{request.SceneIndex:D3}.tsx"));
        await File.WriteAllTextAsync(codeCopy, request.SceneCode, Utf8NoBom, ct).ConfigureAwait(false);

        var dir = CreateTemplateWorkDir();
        var publicDir = Path.Combine(dir, "public");
        CopyBundledFonts(Path.Combine(publicDir, BundledFontCatalog.FontsFolder));
        var frames = Math.Max(1, (int)Math.Round(request.DurationSec * request.Fps, MidpointRounding.AwayFromZero));

        await File.WriteAllTextAsync(Path.Combine(dir, "Scene.tsx"), request.SceneCode, Utf8NoBom, ct).ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(dir, "index.ts"), "import { registerRoot } from \"remotion\";\nimport { Root } from \"./Root\";\nregisterRoot(Root);\n", Utf8NoBom, ct).ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(dir, "Root.tsx"), BuildRoot(request.Width, request.Height, request.Fps, frames), Utf8NoBom, ct).ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(dir, "TkFonts.tsx"), BuildFontsModule(), Utf8NoBom, ct).ConfigureAwait(false);
        return new SceneProject(dir, Path.Combine(publicDir, "assets"), Path.Combine(dir, "index.ts"), DeleteAfterRender: true);
    }

    /// <inheritdoc />
    protected override Result<IReadOnlyList<string>> BuildRenderArguments(SceneProject project, SceneRenderRequest request)
    {
        var args = new List<string>
        {
            CliPath, "render", project.EntryPath, "Scene", Path.GetFullPath(request.OutputPath),
            "--public-dir=" + Path.Combine(project.ProjectDir, "public"),
            "--codec=h264",
            "--overwrite",
            "--log=info",
        };
        if (Tools.Find(ToolKind.ChromeHeadlessShell) is { } chrome)
        {
            args.Add("--browser-executable=" + chrome);
        }

        return args;
    }

    private static string BuildRoot(int width, int height, int fps, int frames) =>
        string.Create(CultureInfo.InvariantCulture, $$"""
            import React from "react";
            import { Composition } from "remotion";
            import { TkFonts } from "./TkFonts";
            import Scene from "./Scene";

            const Wrapped: React.FC = () => (<><TkFonts /><Scene /></>);

            export const Root: React.FC = () => (
              <Composition id="Scene" component={Wrapped} width={{{width}}} height={{{height}}} fps={{{fps}}} durationInFrames={{{frames}}} />
            );

            """);

    private static string BuildFontsModule() =>
        "import { continueRender, delayRender, staticFile } from \"remotion\";\n\n" +
        "const faces: Array<[string, number, string]> = [\n" + BuildFontFaceList("fonts/") + "];\n\n" +
        "const handle = delayRender(\"tk-fonts\");\n" +
        "Promise.all(faces.map(([family, weight, file]) => new FontFace(family, `url('${staticFile(file)}') format('woff2')`, { weight: String(weight) })\n" +
        "  .load().then((loaded) => { document.fonts.add(loaded); }).catch(() => undefined)))\n" +
        "  .then(() => continueRender(handle));\n\n" +
        "export const TkFonts = (): null => null;\n";

    private static Dictionary<string, string> BuildDependencies() => new(StringComparer.Ordinal)
    {
        ["remotion"] = RemotionVersion,
        ["@remotion/cli"] = RemotionVersion,
        ["react"] = ReactVersion,
        ["react-dom"] = ReactVersion,
        ["@fontsource/inter"] = BundledFontCatalog.PackageVersion,
        ["@fontsource/montserrat"] = BundledFontCatalog.PackageVersion,
        ["@fontsource/playfair-display"] = BundledFontCatalog.PackageVersion,
    };

    private string? ResolveChrome()
    {
        if (Tools.Find(ToolKind.ChromeHeadlessShell) is { } chrome && File.Exists(chrome))
        {
            return chrome;
        }

        var exe = OperatingSystem.IsWindows() ? "chrome-headless-shell.exe" : "chrome-headless-shell";
        return FindInTemplate(Path.Combine("node_modules", ".remotion"), exe);
    }

    [GeneratedRegex(@"\b(?<k>Rendered|Encoded)\s+(?<d>\d+)\s*/\s*(?<t>\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex ProgressRegex();
}
