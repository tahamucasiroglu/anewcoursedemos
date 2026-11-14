using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TahaKonsept.ArchitectureTests.Support;
using Shouldly;
using Xunit;

namespace TahaKonsept.ArchitectureTests.Rules;

/// <summary>
/// (c) Her tür bildiriminin (public/internal/private fark etmez) <c>/// &lt;summary&gt;</c> (ya da <c>&lt;inheritdoc/&gt;</c>) belgesi vardır.
/// </summary>
public sealed class SummaryDocumentationTests
{
    /// <summary>Tüm türlerde summary bulunur.</summary>
    [Fact]
    public void Every_type_has_summary_documentation()
    {
        var violations = SourceFiles.All
            .SelectMany(f => f.AllTypes.Where(t => !HasSummary(t)).Select(t => $"{f.RelativePath}: {ParsedSourceFile.NameOf(t)}"))
            .ToList();

        violations.ShouldBeEmpty("Summary eksik:\n" + string.Join("\n", violations));
    }

    private static bool HasSummary(SyntaxNode declaration) =>
        declaration
            .GetLeadingTrivia()
            .Select(t => t.GetStructure())
            .OfType<DocumentationCommentTriviaSyntax>()
            .SelectMany(d => d.Content)
            .Any(IsSummaryOrInheritDoc);

    private static bool IsSummaryOrInheritDoc(XmlNodeSyntax node) => node switch
    {
        XmlElementSyntax e => e.StartTag.Name.LocalName.ValueText is "summary" or "inheritdoc",
        XmlEmptyElementSyntax e => e.Name.LocalName.ValueText == "inheritdoc",
        _ => false,
    };
}
