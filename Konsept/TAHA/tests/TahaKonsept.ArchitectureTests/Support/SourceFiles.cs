using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TahaKonsept.ArchitectureTests.Support;

/// <summary>
/// <c>src/</c> altındaki C# dosyalarını Roslyn ile ayrıştırır (bir kez, önbellekli).
/// Hariç: <c>bin/</c>, <c>obj/</c>, <c>Migrations/</c>, <c>node_modules/</c>, <c>*.Designer.cs</c>, <c>*ModelSnapshot.cs</c>, <c>*.g.cs</c>.
/// </summary>
public static class SourceFiles
{
    private static readonly string[] ExcludedSegments = ["bin", "obj", "Migrations", "node_modules"];

    private static readonly Lazy<IReadOnlyList<ParsedSourceFile>> Parsed = new(ParseAll);

    /// <summary>Ayrıştırılmış tüm kaynak dosyalar.</summary>
    public static IReadOnlyList<ParsedSourceFile> All => Parsed.Value;

    /// <summary>Dosya kural dışı mı.</summary>
    /// <param name="path">Mutlak yol.</param>
    /// <returns>Hariçse <c>true</c>.</returns>
    public static bool IsExcluded(string path)
    {
        var segments = Path.GetRelativePath(RepositoryPaths.Source, path).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (segments.Any(s => ExcludedSegments.Contains(s, StringComparer.OrdinalIgnoreCase)))
        {
            return true;
        }

        var name = Path.GetFileName(path);
        return name.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<ParsedSourceFile> ParseAll()
    {
        if (!Directory.Exists(RepositoryPaths.Source))
        {
            return [];
        }

        var options = CSharpParseOptions.Default
            .WithLanguageVersion(LanguageVersion.Preview)
            .WithDocumentationMode(DocumentationMode.Parse);

        return Directory
            .EnumerateFiles(RepositoryPaths.Source, "*.cs", SearchOption.AllDirectories)
            .Where(p => !IsExcluded(p))
            .Select(p => new ParsedSourceFile(p, CSharpSyntaxTree.ParseText(File.ReadAllText(p), options, p).GetCompilationUnitRoot()))
            .ToList();
    }
}
