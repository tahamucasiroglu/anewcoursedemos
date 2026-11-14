using TahaKonsept.ArchitectureTests.Support;
using Shouldly;
using Xunit;

namespace TahaKonsept.ArchitectureTests.Rules;

/// <summary>
/// (b) Dosya adı = tür adı. Generic türlerde <c>Name.cs</c> ya da (aynı adlı generic olmayan kardeş varsa) <c>Name{T}.cs</c> kabul edilir.
/// </summary>
public sealed class FileNameMatchesTypeTests
{
    /// <summary>Top-level türün adı dosya adıyla aynıdır.</summary>
    [Fact]
    public void File_name_equals_type_name()
    {
        var violations = new List<string>();
        foreach (var file in SourceFiles.All.Where(f => f.TopLevelTypes.Count == 1))
        {
            var type = file.TopLevelTypes[0];
            var fileName = Path.GetFileNameWithoutExtension(file.Path);
            var name = ParsedSourceFile.NameOf(type);
            var typeParameters = ParsedSourceFile.TypeParametersOf(type);
            var genericName = typeParameters.Count == 0 ? null : $"{name}{{{string.Join(",", typeParameters)}}}";

            if (!string.Equals(fileName, name, StringComparison.Ordinal) && !string.Equals(fileName, genericName, StringComparison.Ordinal))
            {
                violations.Add($"{file.RelativePath}: tür adı '{name}'");
            }
        }

        violations.ShouldBeEmpty("Dosya adı tür adıyla aynı olmalı:\n" + string.Join("\n", violations));
    }
}
