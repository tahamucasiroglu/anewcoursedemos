using OkulYonetim.Infrastructure.Security;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Administration;

/// <summary>Geçici şifre (spec 0005, Ö-1): 12 karakter, harf ve rakam karışık, 0/O/1/l/I yok, her seferinde farklı.</summary>
public sealed class TemporaryPasswordGeneratorTests
{
    [Fact]
    public void Generate_ProducesReadableMixedTwelveCharacterPasswords()
    {
        TemporaryPasswordGenerator generator = new();
        string[] passwords = [.. Enumerable.Range(0, 500).Select(_ => generator.Generate())];

        passwords.ShouldAllBe(p => p.Length == 12);
        passwords.ShouldAllBe(p => p.Any(char.IsAsciiDigit) && p.Any(char.IsAsciiLetter));
        passwords.ShouldAllBe(p => p.All(char.IsAsciiLetterOrDigit));
        passwords.ShouldAllBe(p => !p.Any(c => "0O1lI".Contains(c, StringComparison.Ordinal)));
        passwords.Distinct(StringComparer.Ordinal).Count().ShouldBe(500);
    }
}
