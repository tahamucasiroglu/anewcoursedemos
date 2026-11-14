using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TahaKonsept.ArchitectureTests.Support;
using Shouldly;
using Xunit;

namespace TahaKonsept.ArchitectureTests.Rules;

/// <summary>
/// (d) İnce controller: somut (abstract olmayan) <c>*Controller</c> sınıflarının public aksiyonları ya expression-bodied
/// ya da tek ifadeli bloktur. İş mantığı serviste olur.
/// </summary>
public sealed class ThinControllerTests
{
    /// <summary>Aksiyonlar tek satırdır.</summary>
    [Fact]
    public void Controller_actions_are_single_statement()
    {
        var violations = new List<string>();
        foreach (var file in SourceFiles.All)
        {
            var controllers = file.AllTypes
                .OfType<ClassDeclarationSyntax>()
                .Where(c => c.Identifier.ValueText.EndsWith("Controller", StringComparison.Ordinal))
                .Where(c => !c.Modifiers.Any(SyntaxKind.AbstractKeyword));

            foreach (var controller in controllers)
            {
                var actions = controller.Members
                    .OfType<MethodDeclarationSyntax>()
                    .Where(m => m.Modifiers.Any(SyntaxKind.PublicKeyword))
                    .Where(m => !m.Modifiers.Any(SyntaxKind.StaticKeyword))
                    .Where(m => !m.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.Name.ToString() is "NonAction" or "NonActionAttribute"));

                foreach (var action in actions)
                {
                    var isThin = action.ExpressionBody is not null
                        || (action.Body is not null && action.Body.Statements.Count <= 1);
                    if (!isThin)
                    {
                        violations.Add($"{file.RelativePath}: {controller.Identifier.ValueText}.{action.Identifier.ValueText} ({action.Body?.Statements.Count} ifade)");
                    }
                }
            }
        }

        violations.ShouldBeEmpty("Controller aksiyonu tek satır olmalı:\n" + string.Join("\n", violations));
    }
}
