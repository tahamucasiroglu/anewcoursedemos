using TahaKonsept.ArchitectureTests.Support;
using Shouldly;
using Xunit;

namespace TahaKonsept.ArchitectureTests.Rules;

/// <summary>
/// (a) Dosya başına tek tür: her .cs dosyasında tam bir top-level tür (tür içermeyen dosyalar — global using, top-level statements — serbest)
/// ve hiçbir iç içe (nested) tür yok.
/// </summary>
public sealed class SingleTypePerFileTests
{
    /// <summary>Her dosyada en çok bir top-level tür bulunur.</summary>
    [Fact]
    public void Each_file_declares_at_most_one_top_level_type()
    {
        var violations = SourceFiles.All
            .Where(f => f.TopLevelTypes.Count > 1)
            .Select(f => $"{f.RelativePath}: {string.Join(", ", f.TopLevelTypes.Select(ParsedSourceFile.NameOf))}")
            .ToList();

        violations.ShouldBeEmpty("Dosya başına tek tür kuralı ihlali:\n" + string.Join("\n", violations));
    }

    /// <summary>Hiçbir tür başka bir türün içinde tanımlanmaz.</summary>
    [Fact]
    public void No_file_declares_nested_types()
    {
        var violations = SourceFiles.All
            .SelectMany(f => f.NestedTypes.Select(t => $"{f.RelativePath}: {ParsedSourceFile.NameOf(t)}"))
            .ToList();

        violations.ShouldBeEmpty("İç içe tür yasak:\n" + string.Join("\n", violations));
    }

    /// <summary>Kaynak taraması gerçekten dosya buluyor (yanlış pozitif geçişi önler).</summary>
    [Fact]
    public void Source_scan_finds_files()
    {
        SourceFiles.All.Count.ShouldBeGreaterThan(10);
    }
}
