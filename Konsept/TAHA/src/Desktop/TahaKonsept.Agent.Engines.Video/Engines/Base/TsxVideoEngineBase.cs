using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Abstractions.Processes.Abstract;
using TahaKonsept.Agent.Abstractions.Tools.Abstract;
using TahaKonsept.Agent.Engines.Video.Fonts;
using TahaKonsept.Agent.Engines.Video.Media.Abstract;
using TahaKonsept.Agent.Engines.Video.Safety.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Video.Engines.Base;

/// <summary>
/// Sahne kodu tek bir TSX bileşen dosyası olan motorların (Remotion, Revideo) ortak tabanı.
/// Yapı kontrolü: <c>export default</c> zorunlu, <c>import</c> yalnızca <see cref="AllowedImports"/> listesindeki modüllerden ya da
/// göreli yollardan yapılamaz (tek dosya). Paketlenen fontların <c>FontFace</c> yükleme listesini üretmeye yardım eder.
/// </summary>
public abstract partial class TsxVideoEngineBase : NodeVideoEngineBase
{
    /// <summary>Taban oluşturur.</summary>
    /// <param name="paths">Ajan klasörleri.</param>
    /// <param name="tools">Araç bulucu.</param>
    /// <param name="runner">Süreç çalıştırıcı.</param>
    /// <param name="probe">Medya süresi okuyucu.</param>
    /// <param name="guard">Güvenlik denetçisi.</param>
    /// <param name="logger">Günlükçü.</param>
    protected TsxVideoEngineBase(IAgentPaths paths, IToolLocator tools, IProcessRunner runner, IMediaProbe probe, ISceneCodeGuard guard, ILogger logger)
        : base(paths, tools, runner, probe, guard, logger)
    {
    }

    /// <summary>Sahne kodunun içe aktarabileceği modüller.</summary>
    protected abstract IReadOnlySet<string> AllowedImports { get; }

    /// <summary>Koddaki <c>import ... from "x"</c> modül adlarını döner.</summary>
    /// <param name="code">TSX kodu.</param>
    /// <returns>Modül adları.</returns>
    public static IReadOnlyList<string> FindImports(string code) =>
        ImportRegex().Matches(code).Select(m => m.Groups["m"].Value).ToList();

    /// <inheritdoc />
    protected override Result ValidateStructure(string sceneCode)
    {
        if (!DefaultExportRegex().IsMatch(sceneCode))
        {
            return Error.Validation($"video.{Key}.no_default_export", "Sahne dosyası 'export default' ile tek bir bileşen/sahne dışa aktarmalı.");
        }

        var bad = FindImports(sceneCode).Where(m => !AllowedImports.Contains(m)).Distinct(StringComparer.Ordinal).ToList();
        return bad.Count == 0
            ? Result.Success()
            : Error.Validation($"video.{Key}.forbidden_import", $"İzin verilmeyen import: {string.Join(", ", bad)}. İzinli: {string.Join(", ", AllowedImports.Order(StringComparer.Ordinal))}.");
    }

    /// <summary>Paketlenen fontlar için <c>[aile, ağırlık, dosya]</c> TypeScript dizi öğelerini üretir.</summary>
    /// <param name="filePrefix">Dosya yolu öneki (ör. <c>fonts/</c>).</param>
    /// <returns>TypeScript dizi içeriği.</returns>
    protected static string BuildFontFaceList(string filePrefix)
    {
        var sb = new StringBuilder();
        foreach (var font in BundledFontCatalog.Fonts)
        {
            foreach (var weight in font.Weights)
            {
                foreach (var subset in new[] { "latin", "latin-ext" })
                {
                    sb.Append(CultureInfo.InvariantCulture, $"  [\"{font.Family}\", {weight}, \"{filePrefix}{font.FilePrefix}-{subset}-{weight}-normal.woff2\"],\n");
                }
            }
        }

        return sb.ToString();
    }

    [GeneratedRegex(@"\bimport\s+(?:[\w*{}\s,]+\s+from\s+)?['""](?<m>[^'""]+)['""]", RegexOptions.CultureInvariant)]
    private static partial Regex ImportRegex();

    [GeneratedRegex(@"\bexport\s+default\b", RegexOptions.CultureInvariant)]
    private static partial Regex DefaultExportRegex();
}
