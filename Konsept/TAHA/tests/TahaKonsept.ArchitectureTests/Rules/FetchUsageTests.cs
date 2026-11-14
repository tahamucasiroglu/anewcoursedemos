using System.Text.RegularExpressions;
using TahaKonsept.ArchitectureTests.Support;
using Shouldly;
using Xunit;

namespace TahaKonsept.ArchitectureTests.Rules;

/// <summary>
/// (e) Tarayıcıda <c>fetch</c> YASAK: <c>src/**/wwwroot/**/*.js</c> (üçüncü taraf <c>wwwroot/lib</c> ve <c>vendor</c> hariç)
/// içinde yorumlar çıkarıldıktan sonra <c>fetch(</c> kullanımı olmamalı. Tüm istekler jQuery <c>$.ajax</c> ile yapılır.
/// </summary>
public sealed partial class FetchUsageTests
{
    /// <summary>Kendi JS dosyalarımızda fetch yok.</summary>
    [Fact]
    public void No_fetch_calls_in_own_javascript()
    {
        var violations = new List<string>();
        if (Directory.Exists(RepositoryPaths.Source))
        {
            var scripts = Directory
                .EnumerateFiles(RepositoryPaths.Source, "*.js", SearchOption.AllDirectories)
                .Where(IsOwnWebScript);

            foreach (var script in scripts)
            {
                var code = StripComments(File.ReadAllText(script));
                if (FetchCall().IsMatch(code))
                {
                    violations.Add(RepositoryPaths.Relative(script));
                }
            }
        }

        violations.ShouldBeEmpty("fetch( kullanımı yasak (jQuery $.ajax kullanın):\n" + string.Join("\n", violations));
    }

    private static bool IsOwnWebScript(string path)
    {
        var segments = Path.GetRelativePath(RepositoryPaths.Source, path)
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .ToList();
        var wwwroot = segments.FindIndex(s => s.Equals("wwwroot", StringComparison.OrdinalIgnoreCase));
        if (wwwroot < 0)
        {
            return false;
        }

        var below = segments.Skip(wwwroot + 1).ToList();
        var isVendor = below.Any(s => s.Equals("lib", StringComparison.OrdinalIgnoreCase)
            || s.Equals("vendor", StringComparison.OrdinalIgnoreCase)
            || s.Equals("node_modules", StringComparison.OrdinalIgnoreCase));
        var isBuildOutput = segments.Any(s => s.Equals("bin", StringComparison.OrdinalIgnoreCase) || s.Equals("obj", StringComparison.OrdinalIgnoreCase));
        return !isVendor && !isBuildOutput;
    }

    private static string StripComments(string code) => LineComment().Replace(BlockComment().Replace(code, " "), string.Empty);

    [GeneratedRegex(@"(?<![\w$])fetch\s*\(")]
    private static partial Regex FetchCall();

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex BlockComment();

    [GeneratedRegex(@"(?<!:)//[^\n]*")]
    private static partial Regex LineComment();
}
