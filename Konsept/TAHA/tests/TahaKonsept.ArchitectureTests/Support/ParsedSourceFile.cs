using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TahaKonsept.ArchitectureTests.Support;

/// <summary>
/// Ayrıştırılmış tek kaynak dosya: yol, sözdizimi kökü ve tür bildirimi yardımcıları
/// (<see cref="TopLevelTypes"/>, <see cref="AllTypes"/>, <see cref="NestedTypes"/>).
/// </summary>
/// <param name="Path">Mutlak dosya yolu.</param>
/// <param name="Root">Roslyn sözdizimi kökü.</param>
public sealed record ParsedSourceFile(string Path, CompilationUnitSyntax Root)
{
    /// <summary>Kökten göreli yol.</summary>
    public string RelativePath => RepositoryPaths.Relative(Path);

    /// <summary>Dosyadaki tüm tür bildirimleri (class/struct/record/interface/enum/delegate; iç içe olanlar dahil).</summary>
    public IReadOnlyList<MemberDeclarationSyntax> AllTypes =>
        Root.DescendantNodes().OfType<MemberDeclarationSyntax>().Where(IsTypeDeclaration).ToList();

    /// <summary>Dosya/namespace düzeyindeki tür bildirimleri.</summary>
    public IReadOnlyList<MemberDeclarationSyntax> TopLevelTypes =>
        AllTypes.Where(t => !t.Ancestors().OfType<TypeDeclarationSyntax>().Any(IsRealType)).ToList();

    /// <summary>Başka bir türün içinde bildirilmiş türler.</summary>
    public IReadOnlyList<MemberDeclarationSyntax> NestedTypes =>
        AllTypes.Where(t => t.Ancestors().OfType<TypeDeclarationSyntax>().Any(IsRealType)).ToList();

    /// <summary>Dosyada top-level statement var mı (Program.cs).</summary>
    public bool HasTopLevelStatements => Root.Members.OfType<GlobalStatementSyntax>().Any();

    /// <summary>Tür bildiriminin adını döner.</summary>
    /// <param name="declaration">Bildirim.</param>
    /// <returns>Ad.</returns>
    public static string NameOf(MemberDeclarationSyntax declaration) => declaration switch
    {
        BaseTypeDeclarationSyntax t => t.Identifier.ValueText,
        DelegateDeclarationSyntax d => d.Identifier.ValueText,
        _ => string.Empty,
    };

    /// <summary>Tür parametresi adları (generic değilse boş).</summary>
    /// <param name="declaration">Bildirim.</param>
    /// <returns>Tür parametreleri.</returns>
    public static IReadOnlyList<string> TypeParametersOf(MemberDeclarationSyntax declaration) => declaration switch
    {
        TypeDeclarationSyntax t when t.TypeParameterList is not null => t.TypeParameterList.Parameters.Select(p => p.Identifier.ValueText).ToList(),
        DelegateDeclarationSyntax d when d.TypeParameterList is not null => d.TypeParameterList.Parameters.Select(p => p.Identifier.ValueText).ToList(),
        _ => [],
    };

    private static bool IsTypeDeclaration(MemberDeclarationSyntax node) =>
        node switch
        {
            TypeDeclarationSyntax t => IsRealType(t),
            EnumDeclarationSyntax => true,
            DelegateDeclarationSyntax => true,
            _ => false,
        };

    private static bool IsRealType(TypeDeclarationSyntax node) =>
        // C# 14 'extension(...)' blokları tür değil, üye grubudur.
        !string.Equals(node.Kind().ToString(), "ExtensionBlockDeclaration", StringComparison.Ordinal);
}
