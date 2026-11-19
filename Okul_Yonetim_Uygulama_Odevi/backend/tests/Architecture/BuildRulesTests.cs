using Shouldly;

namespace OkulYonetim.Tests.Architecture;

/// <summary>
/// Derleme kurallarının gevşetilmediğini doğrular (spec 0001 AC-6/AC-7): dosya başına tek tip ve dosya adı = tip
/// adı hata seviyesinde, tüm projelerde uyarı = hata açık, tek-tip kuralı interface/enum/struct'ı da kapsıyor.
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
