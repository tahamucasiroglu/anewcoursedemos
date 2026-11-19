using Shouldly;

namespace OkulYonetim.Tests.Architecture;

/// <summary>
/// Derleme kurallarının gevşetilmediğini doğrular (spec 0001 AC-6/AC-7): dosya başına tek tip ve dosya adı = tip
/// adı hata seviyesinde, tüm projelerde uyarı = hata açık, tek-tip kuralı interface/enum/struct'ı da kapsıyor.
/// Tüm csproj / Directory.Build.* / .editorconfig / .cs dosyaları taranır: hiçbiri bu kuralları gevşetemez.
/// Kuralın çalıştığının kendisi VERIFY'da kasıtlı bozmayla gösterilir.
/// </summary>
public sealed class BuildRulesTests
{
    private static readonly string BackendRoot = FindBackendRoot();

    [Theory]
    [InlineData("SA1402")]
    [InlineData("SA1649")]
    [InlineData("CA2254")]
    [InlineData("CA1304")]
    [InlineData("CA1309")]
    [InlineData("CA1311")]
    public void EditorConfig_RuleIsError(string ruleId)
    {
        string[] lines = File.ReadAllLines(Path.Combine(BackendRoot, ".editorconfig"));

        lines.Select(l => l.Replace(" ", string.Empty, StringComparison.Ordinal))
            .ShouldContain($"dotnet_diagnostic.{ruleId}.severity=error");
    }

    [Fact]
    public void EditorConfig_OneTypePerFileRules_AreErrors_ForAllTypeKinds()
    {
        string stylecop = File.ReadAllText(Path.Combine(BackendRoot, "stylecop.json"));

        foreach (string kind in new[] { "\"class\"", "\"interface\"", "\"struct\"", "\"enum\"" })
        {
            stylecop.ShouldContain(kind);
        }
    }

    [Fact]
    public void DirectoryBuildProps_TreatWarningsAsErrors_IsEnabled()
    {
        string props = File.ReadAllText(Path.Combine(BackendRoot, "Directory.Build.props"));

        props.ShouldContain("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>");
        props.ShouldContain("<GenerateDocumentationFile>true</GenerateDocumentationFile>");
        props.ShouldContain("<PackageReference Include=\"StyleCop.Analyzers\">");
    }

    [Fact]
    public void NoProjectOrEditorConfig_WeakensProtectedRules()
    {
        string[] protectedIds = ["SA1402", "SA1649", "CA2254", "CA1304", "CA1309", "CA1311"];
        List<string> violations = [];

        foreach (string file in BackendFiles("*.csproj", "Directory.Build.props", "Directory.Build.targets"))
        {
            foreach (string line in File.ReadAllLines(file))
            {
                string compact = line.Replace(" ", string.Empty, StringComparison.Ordinal);
                bool weakens =
                    compact.Contains("<TreatWarningsAsErrors>false", StringComparison.OrdinalIgnoreCase)
                    || compact.Contains("<WarningsNotAsErrors>", StringComparison.OrdinalIgnoreCase)
                    || compact.Contains("<CodeAnalysisTreatWarningsAsErrors>false", StringComparison.OrdinalIgnoreCase)
                    || (compact.Contains("<NoWarn>", StringComparison.OrdinalIgnoreCase) && protectedIds.Any(id => compact.Contains(id, StringComparison.OrdinalIgnoreCase)));
                if (weakens)
                {
                    violations.Add($"{Relative(file)}: {line.Trim()}");
                }
            }
        }

        foreach (string file in BackendFiles(".editorconfig"))
        {
            foreach (string line in File.ReadAllLines(file))
            {
                string compact = line.Replace(" ", string.Empty, StringComparison.Ordinal);
                foreach (string id in protectedIds)
                {
                    string prefix = $"dotnet_diagnostic.{id}.severity=";
                    if (compact.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && !compact.Equals(prefix + "error", StringComparison.OrdinalIgnoreCase))
                    {
                        violations.Add($"{Relative(file)}: {line.Trim()}");
                    }
                }
            }
        }

        foreach (string file in BackendFiles("*.cs"))
        {
            foreach (string line in File.ReadAllLines(file))
            {
                if (line.Contains("#pragma warning disable", StringComparison.Ordinal) && protectedIds.Any(id => line.Contains(id, StringComparison.Ordinal)))
                {
                    violations.Add($"{Relative(file)}: {line.Trim()}");
                }
            }
        }

        violations.ShouldBeEmpty("Korunan derleme kuralları (dosya başına tek tip, uyarı = hata, log şablonu, Türkçe I/ı) gevşetilmiş: " + string.Join(" | ", violations));
    }

    private static IEnumerable<string> BackendFiles(params string[] patterns) =>
        patterns
            .SelectMany(pattern => Directory.EnumerateFiles(BackendRoot, pattern, SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase);

    private static string Relative(string path) => Path.GetRelativePath(BackendRoot, path);

    private static string FindBackendRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("backend/Directory.Build.props bulunamadı");
    }
}
